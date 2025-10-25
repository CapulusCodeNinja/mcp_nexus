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
    private readonly ConcurrentDictionary<string, HashSet<string>> m_SessionCommands;

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
    /// Initializes a new instance of the <see cref="ExtensionScripts"/> class with default dependencies.
    /// </summary>
    /// <param name="engine">The debug engine instance for callback operations.</param>
    public ExtensionScripts(IDebugEngine engine)
        : this(engine, new CallbackServerManager(engine), new FileSystem(), new ProcessManager())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionScripts"/> class with injected dependencies.
    /// </summary>
    /// <param name="engine">The debug engine instance for callback operations.</param>
    /// <param name="callbackServerManager">The callback server manager.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    internal ExtensionScripts(
        IDebugEngine engine,
        ICallbackServerManager callbackServerManager,
        IFileSystem fileSystem,
        IProcessManager processManager)
    {
        m_Logger = LogManager.GetCurrentClassLogger();

        m_Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        m_CallbackServerManager = callbackServerManager ?? throw new ArgumentNullException(nameof(callbackServerManager));
        m_Manager = new Manager(fileSystem);
        m_Executor = new Executor(m_Manager, new TokenValidator(), processManager);
        m_CommandCache = new ConcurrentDictionary<string, CommandInfo>();
        m_SessionCommands = new ConcurrentDictionary<string, HashSet<string>>();
        m_RunningExtensions = new ConcurrentDictionary<string, ExtensionStatus>();
        m_Disposed = false;

        // Start the callback server asynchronously
        _ = StartCallbackServerAsync();
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
        }
    }

    /// <summary>
    /// Enqueues an extension script for execution and returns a command ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="extensionName">The name of the extension to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension.</param>
    /// <returns>The command ID for tracking this execution.</returns>
    public string EnqueueExtensionScript(string sessionId, string extensionName, object? parameters = null)
    {
        m_Logger.Info("Enqueuing extension script '{ExtensionName}' in session {SessionId}", extensionName, sessionId);

        // Generate unique command ID using engine's ID generation to avoid duplicates
        var commandId = GenerateCommandId(sessionId);
        var queuedTime = DateTime.Now;

        // Store initial queued state
        var commandInfo = CommandInfo.Queued(commandId, $"Extension: {extensionName}", queuedTime);
        m_CommandCache[commandId] = commandInfo;

        // Track session → command mapping
        _ = m_SessionCommands.AddOrUpdate(
            sessionId,
            _ => new HashSet<string> { commandId },
            (_, set) =>
            {
                lock (set)
                {
                    // Add commandId to the set
                    var added = set.Add(commandId);
                    // We don't need the return value, but this satisfies the compiler
                    if (added) { /* Command was added */ }
                }
                return set;
            });

        // Start the extension execution asynchronously
        _ = Task.Run(async () =>
        {
            var cts = new CancellationTokenSource();

            try
            {
                // Update to executing state
                var startTime = DateTime.Now;
                m_CommandCache[commandId] = CommandInfo.Executing(commandId, $"Extension: {extensionName}", queuedTime, startTime);

                // Execute the extension
                var result = await m_Executor.ExecuteAsync(extensionName, sessionId, parameters, commandId, null, cts.Token);

                // Store the process ID for cancellation support
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

                // Update to completed/failed state
                var endTime = DateTime.Now;
                m_CommandCache[commandId] = CommandInfo.Completed(
                    commandId,
                    $"Extension: {extensionName}",
                    queuedTime,
                    startTime,
                    endTime,
                    result.Output ?? string.Empty,
                    result.Success,
                    result.Success ? null : (result.Error ?? "Extension execution failed"));
            }
            catch (OperationCanceledException)
            {
                // Extension was cancelled
                var endTime = DateTime.Now;
                m_CommandCache[commandId] = CommandInfo.Cancelled(
                    commandId,
                    $"Extension: {extensionName}",
                    queuedTime,
                    commandInfo.StartTime,
                    endTime);

                m_Logger.Warn("Extension script {ExtensionName} with command ID {CommandId} was cancelled", extensionName, commandId);
            }
            catch (TimeoutException ex)
            {
                // Extension timed out
                var endTime = DateTime.Now;
                m_CommandCache[commandId] = CommandInfo.TimedOut(
                    commandId,
                    $"Extension: {extensionName}",
                    queuedTime,
                    commandInfo.StartTime ?? queuedTime,
                    endTime,
                    ex.Message);

                m_Logger.Warn("Extension script {ExtensionName} with command ID {CommandId} timed out: {Message}", extensionName, commandId, ex.Message);
            }
            catch (Exception ex)
            {
                // Extension failed with error
                var endTime = DateTime.Now;
                m_CommandCache[commandId] = CommandInfo.Completed(
                    commandId,
                    $"Extension: {extensionName}",
                    queuedTime,
                    commandInfo.StartTime ?? queuedTime,
                    endTime,
                    string.Empty,
                    false,
                    ex.Message);

                m_Logger.Error(ex, "Extension script {ExtensionName} with command ID {CommandId} failed", extensionName, commandId);
            }
            finally
            {
                // Remove from running extensions
                _ = m_RunningExtensions.TryRemove(commandId, out _);
                cts.Dispose();
            }
        });

        return commandId;
    }

    /// <summary>
    /// Gets the status of a specific extension command.
    /// </summary>
    /// <param name="commandId">The command ID to query.</param>
    /// <returns>The command info if found, null otherwise.</returns>
    public CommandInfo? GetCommandStatus(string commandId)
    {
        _ = m_CommandCache.TryGetValue(commandId, out var commandInfo);
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
        lock (commandIds)
        {
            foreach (var commandId in commandIds)
            {
                if (m_CommandCache.TryGetValue(commandId, out var commandInfo))
                {
                    commands.Add(commandInfo);
                }
            }
        }

        return commands;
    }

    /// <summary>
    /// Cancels a running extension command.
    /// </summary>
    /// <param name="commandId">The command ID to cancel.</param>
    /// <returns>True if the command was cancelled, false if not found or already completed.</returns>
    public bool CancelCommand(string commandId)
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
                var process = System.Diagnostics.Process.GetProcessById(status.ProcessId);
                if (!process.HasExited)
                {
                    process.Kill();
                }
                process.Dispose(); // Dispose the process handle immediately
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
                    commandId,
                    commandInfo.Command,
                    commandInfo.QueuedTime,
                    commandInfo.StartTime,
                    endTime);
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
            return;
        }

        // Cancel all running extensions for this session
        List<string> commandIdList;
        lock (commandIds)
        {
            commandIdList = commandIds.ToList();
        }

        foreach (var commandId in commandIdList)
        {
            if (m_RunningExtensions.ContainsKey(commandId))
            {
                _ = CancelCommand(commandId);
            }
        }

        // Remove session from tracking (but keep results in cache for potential final queries)
        _ = m_SessionCommands.TryRemove(sessionId, out _);

        m_Logger.Info("Closed all extension scripts for session {SessionId}", sessionId);
    }

    /// <summary>
    /// Generates a unique command ID for extension scripts.
    /// Uses format cmd-{sessionId}-{timestamp} to avoid duplicates with regular commands.
    /// </summary>
    /// <param name="sessionId">The session ID to include in the command ID.</param>
    /// <returns>A unique command ID in the format cmd-{sessionId}-{number}.</returns>
    private static string GenerateCommandId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        // Use timestamp-based approach for session-specific IDs
        // This ensures uniqueness within the session and follows the cmd-{sessionId}-{number} format
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"cmd-{sessionId}-{timestamp}";
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
            m_Logger.Info("Extension scripts disposed successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error disposing extension scripts");
        }

        GC.SuppressFinalize(this);
    }
}
