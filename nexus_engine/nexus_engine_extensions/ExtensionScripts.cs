using System.Collections.Concurrent;

using Nexus.Engine.Extensions.Callback;
using Nexus.Engine.Extensions.Core;
using Nexus.Engine.Extensions.Models;
using Nexus.Engine.Extensions.Security;
using Nexus.Engine.Share;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using NLog;

namespace Nexus.Engine.Extensions;

/// <summary>
/// Manages extension scripts with integrated callback server.
/// </summary>
public class ExtensionScripts : IExtensionScripts, IAsyncDisposable
{
    /// <summary>
    /// Executor for running extension scripts.
    /// </summary>
    private readonly Executor m_Executor;

    /// <summary>
    /// Manager for discovering and loading extension scripts.
    /// </summary>
    private readonly Manager m_Manager;

    /// <summary>
    /// Logger for extension script operations.
    /// </summary>
    private readonly Logger m_Logger;

    /// <summary>
    /// Cache of command information keyed by command ID.
    /// </summary>
    private readonly ConcurrentDictionary<string, CommandInfo> m_CommandCache;

    /// <summary>
    /// Mapping of session IDs to their associated command IDs.
    /// </summary>
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> m_SessionCommands;

    /// <summary>
    /// Status of currently running extensions keyed by command ID.
    /// </summary>
    private readonly ConcurrentDictionary<string, ExtensionStatus> m_RunningExtensions;

    /// <summary>
    /// Debug engine instance for callback operations.
    /// </summary>
    private readonly IDebugEngine m_Engine;

    /// <summary>
    /// Manager for the extension callback server.
    /// </summary>
    private readonly ICallbackServerManager m_CallbackServerManager;

    /// <summary>
    /// Flag indicating if resources have been disposed.
    /// </summary>
    private bool m_Disposed;

    /// <summary>
    /// Task for callback server initialization.
    /// </summary>
    private Task? m_CallbackServerInitTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionScripts"/> class with default dependencies.
    /// </summary>
    /// <param name="engine">The debug engine instance for callback operations.</param>
    public ExtensionScripts(IDebugEngine engine)
        : this(engine, new TokenValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionScripts"/> class with default dependencies.
    /// </summary>
    /// <param name="engine">The debug engine instance for callback operations.</param>
    /// <param name="tokenValidator">The token validator for validating extension script callbacks.</param>
    internal ExtensionScripts(IDebugEngine engine, TokenValidator tokenValidator)
        : this(engine, tokenValidator, new CallbackServerManager(engine, tokenValidator), new FileSystem(), new ProcessManager())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionScripts"/> class with injected dependencies.
    /// </summary>
    /// <param name="engine">The debug engine instance for callback operations.</param>
    /// <param name="tokenValidator">The token validator for validating extension script callbacks.</param>
    /// <param name="callbackServerManager">The callback server manager.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    internal ExtensionScripts(
        IDebugEngine engine,
        TokenValidator tokenValidator,
        ICallbackServerManager callbackServerManager,
        IFileSystem fileSystem,
        IProcessManager processManager)
    {
        m_Logger = LogManager.GetCurrentClassLogger();

        m_Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        m_CallbackServerManager = callbackServerManager ?? throw new ArgumentNullException(nameof(callbackServerManager));
        m_Manager = new Manager(fileSystem);
        m_Executor = new Executor(m_Manager, tokenValidator, processManager);
        m_CommandCache = new ConcurrentDictionary<string, CommandInfo>();
        m_SessionCommands = new ConcurrentDictionary<string, ConcurrentBag<string>>();
        m_RunningExtensions = new ConcurrentDictionary<string, ExtensionStatus>();
        m_Disposed = false;

        // Start the callback server asynchronously with error handling
        m_CallbackServerInitTask = StartCallbackServerAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                m_Logger.Error(t.Exception, "Critical: Extension callback server initialization failed");
            }
        }, TaskScheduler.Default);
    }

    /// <summary>
    /// Starts the extension callback HTTP server asynchronously.
    /// </summary>
    private async Task StartCallbackServerAsync()
    {
        try
        {
            await m_CallbackServerManager.StartAsync();

            // Update the executor with the actual callback URL
            m_Executor.UpdateCallbackUrl(m_CallbackServerManager.CallbackUrl);

            m_Logger.Info("Extension system initialized with callback URL: {CallbackUrl}", m_CallbackServerManager.CallbackUrl);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to start extension callback server during initialization");
            throw;
        }
    }

    /// <summary>
    /// Ensures the callback server is initialized before use.
    /// </summary>
    private async Task EnsureCallbackServerInitializedAsync()
    {
        if (m_CallbackServerInitTask != null)
        {
            await m_CallbackServerInitTask;
            m_CallbackServerInitTask = null;
        }
    }

    /// <summary>
    /// Enqueues an extension script for execution and returns a command ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="extensionName">The name of the extension to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command ID for tracking this execution.</returns>
    public async Task<string> EnqueueExtensionScriptAsync(string sessionId, string extensionName, object? parameters = null)
    {
        m_Logger.Info("Starting extension script '{ExtensionName}' in session {SessionId}", extensionName, sessionId);

        // Ensure callback server is ready
        await EnsureCallbackServerInitializedAsync();

        // Generate unique command ID using centralized generator
        var commandId = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
        var queuedTime = DateTime.Now;

        // Store initial queued state
        var commandInfo = CommandInfo.Enqueued(sessionId, commandId, $"Extension: {extensionName}", queuedTime, null);
        m_CommandCache[commandId] = commandInfo;

        // Track session → command mapping
        _ = m_SessionCommands.AddOrUpdate(
            sessionId,
            _ => new ConcurrentBag<string> { commandId },
            (_, bag) =>
            {
                bag.Add(commandId);
                return bag;
            });

        // Create cancellation token source for this extension
        var cts = new CancellationTokenSource();

        // Start the extension execution asynchronously
        _ = Task.Run(async () => await ExecuteExtensionAsync(extensionName, sessionId, parameters, commandId, queuedTime, cts));

        return commandId;
    }

    /// <summary>
    /// Executes an extension script and handles all outcome scenarios.
    /// </summary>
    /// <param name="extensionName">The name of the extension.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="parameters">Extension parameters.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="cts">Cancellation token source.</param>
    private async Task ExecuteExtensionAsync(string extensionName, string sessionId, object? parameters, string commandId, DateTime queuedTime, CancellationTokenSource cts)
    {
        try
        {
            // Update to executing state
            var startTime = DateTime.Now;
            MarkExtensionAsExecuting(sessionId, commandId, extensionName, queuedTime, startTime);

            // Execute the extension
            var result = await m_Executor.ExecuteAsync(extensionName, sessionId, parameters, commandId, null, cts.Token);

            // Store process for cancellation support
            StoreRunningExtension(commandId, sessionId, extensionName, parameters, startTime, cts, result);

            // Handle extension result
            HandleCommandInfo(extensionName, sessionId, commandId, queuedTime, startTime, result);
        }
        catch (OperationCanceledException)
        {
            HandleExtensionCancellation(extensionName, sessionId, commandId, queuedTime);
        }
        catch (TimeoutException ex)
        {
            HandleExtensionTimeout(extensionName, sessionId, commandId, queuedTime, ex);
        }
        catch (Exception ex)
        {
            HandleExtensionFailure(extensionName, sessionId, commandId, queuedTime, ex);
        }
        finally
        {
            // Cleanup
            _ = m_RunningExtensions.TryRemove(commandId, out _);
            cts.Dispose();
        }
    }

    /// <summary>
    /// Marks the extension as executing.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="startTime">When execution started.</param>
    private void MarkExtensionAsExecuting(string sessionId, string commandId, string extensionName, DateTime queuedTime, DateTime startTime)
    {
        m_CommandCache[commandId] = CommandInfo.Executing(sessionId, commandId, $"Extension: {extensionName}", queuedTime, startTime, null);
    }

    /// <summary>
    /// Stores the running extension for cancellation support.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="parameters">Extension parameters.</param>
    /// <param name="startTime">When execution started.</param>
    /// <param name="cts">Cancellation token source.</param>
    /// <param name="result">The extension result.</param>
    private void StoreRunningExtension(string commandId, string sessionId, string extensionName, object? parameters, DateTime startTime, CancellationTokenSource cts, CommandInfo result)
    {
        if (result.ProcessId.HasValue)
        {
            var status = new ExtensionStatus
            {
                CommandId = commandId,
                SessionId = sessionId,
                ExtensionName = extensionName,
                ProcessId = result.ProcessId.Value,
                StartTime = startTime,
                CancellationTokenSource = cts,
                Parameters = parameters
            };
            m_RunningExtensions[commandId] = status;
        }
    }

    /// <summary>
    /// Handles the extension execution result and emits statistics.
    /// </summary>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="startTime">When execution started.</param>
    /// <param name="result">The extension result.</param>
    private void HandleCommandInfo(string extensionName, string sessionId, string commandId, DateTime queuedTime, DateTime startTime, CommandInfo result)
    {
        var endTime = DateTime.Now;
        var commandInfo = CreateCommandInfoFromResult(sessionId, commandId, extensionName, queuedTime, startTime, endTime, result);
        var finalState = DetermineCommandState(result);

        // Store in cache
        m_CommandCache[commandId] = commandInfo;

        // Emit statistics
        EmitExtensionStatistics(finalState, sessionId, commandId, extensionName, queuedTime, startTime, endTime);
    }

    /// <summary>
    /// Creates a CommandInfo from the extension result.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="startTime">When execution started.</param>
    /// <param name="endTime">When execution ended.</param>
    /// <param name="result">The extension result.</param>
    /// <returns>The created CommandInfo.</returns>
    private CommandInfo CreateCommandInfoFromResult(string sessionId, string commandId, string extensionName, DateTime queuedTime, DateTime startTime, DateTime endTime, CommandInfo result)
    {
        if (result.State == CommandState.Completed)
        {
            return CommandInfo.Completed(
                sessionId,
                commandId,
                $"Extension: {extensionName}",
                queuedTime,
                startTime,
                endTime,
                result.AggregatedOutput ?? string.Empty,
                result.ErrorMessage ?? string.Empty,
                result.ProcessId);
        }
        else if (result.State == CommandState.Timeout)
        {
            return CommandInfo.TimedOut(
                sessionId,
                commandId,
                $"Extension: {extensionName}",
                queuedTime,
                startTime,
                endTime,
                result.AggregatedOutput ?? string.Empty,
                result.ErrorMessage ?? string.Empty,
                result.ProcessId);
        }

        return CommandInfo.Completed(
            sessionId,
            commandId,
            $"Extension: {extensionName}",
            queuedTime,
            startTime,
            endTime,
            result.AggregatedOutput ?? string.Empty,
            result.ErrorMessage ?? "Extension execution failed",
            result.ProcessId);
    }

    /// <summary>
    /// Determines the final CommandState from the extension result.
    /// </summary>
    /// <param name="result">The extension result.</param>
    /// <returns>The CommandState.</returns>
    private CommandState DetermineCommandState(CommandInfo result)
    {
        if (result.State == CommandState.Completed)
        {
            return CommandState.Completed;
        }
        else if (result.State == CommandState.Timeout)
        {
            return CommandState.Timeout;
        }

        return CommandState.Failed;
    }

    /// <summary>
    /// Emits command statistics for the extension execution.
    /// </summary>
    /// <param name="finalState">The final command state.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="startTime">When execution started.</param>
    /// <param name="endTime">When execution ended.</param>
    private void EmitExtensionStatistics(CommandState finalState, string sessionId, string commandId, string extensionName, DateTime queuedTime, DateTime startTime, DateTime endTime)
    {
        var executionTime = endTime - startTime;
        var queueTime = startTime - queuedTime;
        var totalTime = endTime - queuedTime;

        Statistics.EmitCommandStats(
            m_Logger,
            finalState,
            sessionId,
            commandId,
            null, // Extensions are never batched
            $"Extension: {extensionName}",
            queuedTime,
            startTime,
            endTime,
            queueTime,
            executionTime,
            totalTime);
    }

    /// <summary>
    /// Handles extension cancellation.
    /// </summary>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    private void HandleExtensionCancellation(string extensionName, string sessionId, string commandId, DateTime queuedTime)
    {
        var endTime = DateTime.Now;
        var commandInfo = m_CommandCache.TryGetValue(commandId, out var existingInfo) ? existingInfo : null;
        var cancelStartTime = commandInfo?.StartTime ?? queuedTime;

        m_CommandCache[commandId] = CommandInfo.Cancelled(
            sessionId,
            commandId,
            $"Extension: {extensionName}",
            queuedTime,
            cancelStartTime,
            endTime,
            string.Empty,
            "Extension execution was cancelled",
            commandInfo?.ProcessId);

        m_Logger.Warn("Extension script {ExtensionName} with command ID {CommandId} was cancelled", extensionName, commandId);

        // Emit statistics
        EmitExtensionStatistics(CommandState.Cancelled, sessionId, commandId, extensionName, queuedTime, cancelStartTime, endTime);
    }

    /// <summary>
    /// Handles extension timeout.
    /// </summary>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="ex">The timeout exception.</param>
    private void HandleExtensionTimeout(string extensionName, string sessionId, string commandId, DateTime queuedTime, TimeoutException ex)
    {
        var endTime = DateTime.Now;
        var commandInfo = m_CommandCache.TryGetValue(commandId, out var existingInfo) ? existingInfo : null;
        var timeoutStartTime = commandInfo?.StartTime ?? queuedTime;

        m_CommandCache[commandId] = CommandInfo.TimedOut(
            sessionId,
            commandId,
            $"Extension: {extensionName}",
            queuedTime,
            timeoutStartTime,
            endTime,
            string.Empty,
            ex.Message,
            commandInfo?.ProcessId);

        m_Logger.Warn("Extension script {ExtensionName} with command ID {CommandId} timed out: {Message}", extensionName, commandId, ex.Message);

        // Emit statistics
        EmitExtensionStatistics(CommandState.Timeout, sessionId, commandId, extensionName, queuedTime, timeoutStartTime, endTime);
    }

    /// <summary>
    /// Handles extension failure.
    /// </summary>
    /// <param name="extensionName">The extension name.</param>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID.</param>
    /// <param name="queuedTime">When the command was queued.</param>
    /// <param name="ex">The exception.</param>
    private void HandleExtensionFailure(string extensionName, string sessionId, string commandId, DateTime queuedTime, Exception ex)
    {
        var endTime = DateTime.Now;
        var commandInfo = m_CommandCache.TryGetValue(commandId, out var existingInfo) ? existingInfo : null;
        var failStartTime = commandInfo?.StartTime ?? queuedTime;

        m_CommandCache[commandId] = CommandInfo.Completed(
            sessionId,
            commandId,
            $"Extension: {extensionName}",
            queuedTime,
            failStartTime,
            endTime,
            string.Empty,
            ex.Message,
            commandInfo?.ProcessId);

        m_Logger.Error(ex, "Extension script {ExtensionName} with command ID {CommandId} failed", extensionName, commandId);

        // Emit statistics
        EmitExtensionStatistics(CommandState.Failed, sessionId, commandId, extensionName, queuedTime, failStartTime, endTime);
    }

    /// <summary>
    /// Gets the status of a specific extension command.
    /// </summary>
    /// <param name="commandId">The command ID to query.</param>
    /// <returns>The command info if found, null otherwise.</returns>
    public CommandInfo? GetCommandStatus(string commandId)
    {
        _ = m_CommandCache.TryGetValue(commandId, out var commandInfo);
        commandInfo?.IncrementReadCount();
        return commandInfo;
    }

    /// <summary>
    /// Gets all extension commands for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to query.</param>
    /// <returns>List of command info for all extension commands in the session.</returns>
    public List<CommandInfo> GetSessionCommands(string sessionId)
    {
        if (!m_SessionCommands.TryGetValue(sessionId, out var commandIds))
        {
            return new List<CommandInfo>();
        }

        var commands = new List<CommandInfo>();
        foreach (var commandId in commandIds)
        {
            if (m_CommandCache.TryGetValue(commandId, out var commandInfo))
            {
                commands.Add(commandInfo);
            }
        }

        return commands;
    }

    /// <summary>
    /// Cancels a running extension command.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID to cancel.</param>
    /// <returns>True if the command was cancelled, false if not found or already completed.</returns>
    public bool CancelCommand(string sessionId, string commandId)
    {
        m_Logger.Info("Cancelling extension command {CommandId}", commandId);

        // Check if the extension is still running
        if (!m_RunningExtensions.TryGetValue(commandId, out var status))
        {
            m_Logger.Debug("Extension command {CommandId} not found or already completed", commandId);
            return false;
        }

        try
        {
            // Kill the process using process ID
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(status.ProcessId);
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (ArgumentException)
            {
                // Process already exited or doesn't exist
                m_Logger.Debug("Process {ProcessId} for command {CommandId} already exited", status.ProcessId, commandId);
            }

            // Cancel the token
            status.CancellationTokenSource.Cancel();

            // Update cache to cancelled state
            var endTime = DateTime.Now;
            if (m_CommandCache.TryGetValue(commandId, out var commandInfo))
            {
                m_CommandCache[commandId] = CommandInfo.Cancelled(
                    sessionId,
                    commandId,
                    commandInfo.Command,
                    commandInfo.QueuedTime,
                    commandInfo.StartTime ?? DateTime.Now,
                    endTime,
                    string.Empty,
                    "Extension execution was cancelled",
                    commandInfo.ProcessId);
            }

            m_Logger.Info("Extension command {CommandId} cancelled successfully", commandId);
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error cancelling extension command {CommandId}", commandId);
            return false;
        }
    }

    /// <summary>
    /// Closes all extension scripts for a session.
    /// </summary>
    /// <param name="sessionId">The session ID to close extensions for.</param>
    public void CloseSession(string sessionId)
    {
        m_Logger.Info("Closing all extension scripts for session {SessionId}", sessionId);

        // Get all command IDs for this session
        if (!m_SessionCommands.TryGetValue(sessionId, out var commandIds))
        {
            m_Logger.Debug("No extension commands found for session {SessionId}", sessionId);

            // Still reset the command ID generator for this session
            _ = CommandIdGenerator.Instance.ResetSession(sessionId);
            return;
        }

        // Cancel all running extensions for this session
        var commandIdList = commandIds.ToList();

        foreach (var commandId in commandIdList)
        {
            if (m_RunningExtensions.ContainsKey(commandId))
            {
                _ = CancelCommand(sessionId, commandId);
            }
        }

        // Remove session from tracking (but keep results in cache for potential final queries)
        _ = m_SessionCommands.TryRemove(sessionId, out _);

        // Reset the command ID generator for this session
        _ = CommandIdGenerator.Instance.ResetSession(sessionId);

        m_Logger.Info("Closed all extension scripts for session {SessionId}", sessionId);
    }

    /// <summary>
    /// Disposes resources asynchronously, including the callback server.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // Dispose the callback server manager
        try
        {
            await m_CallbackServerManager.DisposeAsync();
            m_Logger.Info("Extension callback server disposed successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error disposing extension callback server");
        }

        // Dispose the extension manager
        try
        {
            m_Manager.Dispose();
            m_Logger.Info("Extension manager disposed successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error disposing extension manager");
        }

        m_Logger.Info("Extension scripts disposed successfully");

        GC.SuppressFinalize(this);
    }
}
