using System.Collections.Concurrent;

using Nexus.Config;
using Nexus.Engine.Extensions;
using Nexus.Engine.Internal;
using Nexus.Engine.Preprocessing;
using Nexus.Engine.Share;
using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using NLog;

namespace Nexus.Engine;
/// <summary>
/// Main implementation of the debug engine that manages CDB sessions and command execution.
/// </summary>
public class DebugEngine : IDebugEngine
{
    /// <summary>
    /// Logger for debug engine operations.
    /// </summary>
    private readonly Logger m_Logger;

    /// <summary>
    /// File system abstraction for file operations.
    /// </summary>
    private readonly IFileSystem m_FileSystem;

    /// <summary>
    /// Process manager abstraction for process operations.
    /// </summary>
    private readonly IProcessManager m_ProcessManager;

    /// <summary>
    /// Extension scripts manager for PowerShell-based debugging workflows.
    /// </summary>
    private readonly IExtensionScripts m_ExtensionScripts;

    /// <summary>
    /// Dictionary of active debug sessions keyed by session ID.
    /// </summary>
    private readonly ConcurrentDictionary<string, DebugSession> m_Sessions = new();

    /// <summary>
    /// Dictionary tracking session creation timestamps keyed by session ID.
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTime> m_SessionCreationTimes = new();

    /// <summary>
    /// Indicates whether this instance has been disposed.
    /// </summary>
    private volatile bool m_Disposed = false;

    /// <summary>
    /// Gets the singleton instance of the debug engine.
    /// </summary>
    public static IDebugEngine Instance { get; } = new DebugEngine();

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEngine"/> class with default dependencies.
    /// </summary>
    internal DebugEngine() : this(new FileSystem(), new ProcessManager())
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEngine"/> class with specified dependencies.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    internal DebugEngine(IFileSystem fileSystem, IProcessManager processManager)
    {
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));

        m_ExtensionScripts = new ExtensionScripts(this);

        m_Logger = LogManager.GetCurrentClassLogger();
        m_Logger.Info("DebugEngine initialized with max {MaxSessions} concurrent sessions", Settings.GetInstance().Get().McpNexus.SessionManagement.MaxConcurrentSessions);
    }

    /// <summary>
    /// Occurs when a command's state changes.
    /// </summary>
    public event EventHandler<CommandStateChangedEventArgs>? CommandStateChanged;

    /// <summary>
    /// Occurs when a session's state changes.
    /// </summary>
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <summary>
    /// Creates a new debug session for analyzing a dump file.
    /// </summary>
    /// <param name="dumpFilePath">The path to the dump file to analyze.</param>
    /// <param name="symbolPath">Optional symbol path for debugging symbols.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the session ID.</returns>
    /// <exception cref="ArgumentException">Thrown when dumpFilePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the dump file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of concurrent sessions is reached.</exception>
    public async Task<string> CreateSessionAsync(string dumpFilePath, string? symbolPath = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateSessionId(dumpFilePath, nameof(dumpFilePath));

        if (!m_FileSystem.FileExists(dumpFilePath))
        {
            throw new FileNotFoundException($"Dump file not found: {dumpFilePath}", dumpFilePath);
        }

        if (m_Sessions.Count >= Settings.GetInstance().Get().McpNexus.SessionManagement.MaxConcurrentSessions)
        {
            throw new InvalidOperationException($"Maximum number of concurrent sessions ({Settings.GetInstance().Get().McpNexus.SessionManagement.MaxConcurrentSessions}) reached");
        }

        var sessionId = SessionIdGenerator.Instance.GenerateSessionId();
        var creationTime = DateTime.Now;
        m_Logger.Info("Creating debug session {SessionId} for dump file: {DumpFilePath}", sessionId, dumpFilePath);

        try
        {

            var preprocessor = new CommandPreprocessor(m_FileSystem);
            var session = new Internal.DebugSession(sessionId, dumpFilePath, symbolPath, m_FileSystem, m_ProcessManager, preprocessor);

            // Subscribe to session events
            session.CommandStateChanged += OnSessionCommandStateChanged;
            session.SessionStateChanged += OnSessionStateChanged;

            // Initialize the session
            await session.InitializeAsync(cancellationToken);

            // Add to active sessions
            if (!m_Sessions.TryAdd(sessionId, session))
            {
                await session.DisposeAsync();
                throw new InvalidOperationException($"Failed to add session {sessionId} to active sessions");
            }

            // Track session creation time
            _ = m_SessionCreationTimes.TryAdd(sessionId, creationTime);

            m_Logger.Info("Debug session {SessionId} created successfully", sessionId);
            return sessionId;
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to create debug session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Closes a debug session and cleans up resources.
    /// </summary>
    /// <param name="sessionId">The session ID to close.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    public async Task CloseSessionAsync(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        m_Logger.Info("Closing debug session {SessionId}", sessionId);

        if (m_Sessions.TryRemove(sessionId, out var session))
        {
            try
            {
                // Get session creation time
                var closedAt = DateTime.Now;
                var openedAt = m_SessionCreationTimes.TryGetValue(sessionId, out var creationTime) ? creationTime : closedAt;
                var totalDuration = closedAt - openedAt;

                // Collect command statistics from the session
                var sessionCommands = session.GetAllCommandInfos();
                var extensionCommands = m_ExtensionScripts.GetSessionCommands(sessionId);

                // Combine all commands
                var allCommands = new List<CommandInfo>(sessionCommands.Values);
                allCommands.AddRange(extensionCommands);

                // Count commands by state
                var completedCount = allCommands.Count(c => c.State == CommandState.Completed);
                var failedCount = allCommands.Count(c => c.State == CommandState.Failed);
                var cancelledCount = allCommands.Count(c => c.State == CommandState.Cancelled);
                var timedOutCount = allCommands.Count(c => c.State == CommandState.Timeout);
                var totalCount = allCommands.Count;

                // Emit session statistics
                Statistics.EmitSessionStats(
                    m_Logger,
                    sessionId,
                    openedAt,
                    closedAt,
                    totalDuration,
                    totalCount,
                    completedCount,
                    failedCount,
                    cancelledCount,
                    timedOutCount,
                    allCommands);

                // Close all extension scripts for this session
                m_ExtensionScripts.CloseSession(sessionId);

                // Unsubscribe from events
                session.CommandStateChanged -= OnSessionCommandStateChanged;
                session.SessionStateChanged -= OnSessionStateChanged;

                await session.DisposeAsync();

                // Remove session creation time tracking
                _ = m_SessionCreationTimes.TryRemove(sessionId, out _);

                m_Logger.Info("Debug session {SessionId} closed successfully", sessionId);
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Error closing debug session {SessionId}", sessionId);
                throw;
            }
        }
        else
        {
            m_Logger.Warn("Session {SessionId} not found for closing", sessionId);
        }
    }

    /// <summary>
    /// Checks if a session is currently active.
    /// </summary>
    /// <param name="sessionId">The session ID to check.</param>
    /// <returns>True if the session is active, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    public bool IsSessionActive(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        return m_Sessions.TryGetValue(sessionId, out var session) && session.IsActive;
    }

    /// <summary>
    /// Enqueues a command for execution in the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID to execute the command in.</param>
    /// <param name="command">The debug command to execute.</param>
    /// <returns>The unique command ID for tracking the command.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or command is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active or the command queue is full.</exception>
    public string EnqueueCommand(string sessionId, string command)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommand(command, nameof(command));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException($"Session {sessionId} is not active");
        }

        var commandId = session.EnqueueCommand(command);
        m_Logger.Debug("Command {CommandId} enqueued in session {SessionId}: {Command}", commandId, sessionId, command);
        return commandId;
    }

    /// <summary>
    /// Enqueues an extension script for execution in the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID to execute the extension script in.</param>
    /// <param name="extensionName">The name of the extension to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the extension.</param>
    /// <returns>The unique command ID for tracking the extension execution.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or extensionName is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active or the extension is not found.</exception>
    public string EnqueueExtensionScript(string sessionId, string extensionName, object? parameters = null)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateExtensionName(extensionName, nameof(extensionName));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException($"Session {sessionId} is not active");
        }

        // Delegate to the extensions library - it will handle all context management internally
        var commandId = m_ExtensionScripts.EnqueueExtensionScript(sessionId, extensionName, parameters);

        // Fire state changed event for the engine's tracking
        OnSessionCommandStateChanged(this, new CommandStateChangedEventArgs
        {
            SessionId = sessionId,
            CommandId = commandId,
            OldState = CommandState.Queued,
            NewState = CommandState.Queued,
            Timestamp = DateTime.Now,
            Command = $"Extension: {extensionName}"
        });

        return commandId;
    }

    /// <summary>
    /// Gets the information about a command.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID to get the information for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command information.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or commandId is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the command is not found.</exception>
    public async Task<CommandInfo> GetCommandInfoAsync(string sessionId, string commandId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommandId(commandId, nameof(commandId));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        if (!session.IsActive)
        {
            throw new InvalidOperationException($"Session {sessionId} is not active");
        }

        try
        {
            // First try to get from the session's command queue
            return await session.GetCommandInfoAsync(commandId, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            // If not found in session, check the extensions library
            var extensionCommandInfo = m_ExtensionScripts.GetCommandStatus(commandId);
            if (extensionCommandInfo != null)
            {
                return extensionCommandInfo;
            }

            // Re-throw the original exception if not found in extensions either
            throw;
        }
    }

    /// <summary>
    /// Gets the current information about a command without waiting for completion.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID to get the information for.</param>
    /// <returns>The command information, or null if the command is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or commandId is null or empty.</exception>
    public CommandInfo? GetCommandInfo(string sessionId, string commandId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommandId(commandId, nameof(commandId));

        // First check the session's command cache
        if (m_Sessions.TryGetValue(sessionId, out var session))
        {
            var commandInfo = session.GetCommandInfo(commandId);
            if (commandInfo != null)
            {
                return commandInfo;
            }
        }

        // If not found in session, check the extensions library
        return m_ExtensionScripts.GetCommandStatus(commandId);
    }

    /// <summary>
    /// Gets the information about all commands in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A dictionary of command IDs to their information.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    public Dictionary<string, CommandInfo> GetAllCommandInfos(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        var allCommands = new Dictionary<string, CommandInfo>();

        // Get commands from the session
        if (m_Sessions.TryGetValue(sessionId, out var session))
        {
            allCommands = session.GetAllCommandInfos();
        }

        // Add extension commands for this session
        var extensionCommands = m_ExtensionScripts.GetSessionCommands(sessionId);
        foreach (var extCmd in extensionCommands)
        {
            allCommands[extCmd.CommandId] = extCmd;
        }

        return allCommands;
    }

    /// <summary>
    /// Cancels a queued or executing command.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandId">The command ID to cancel.</param>
    /// <returns>True if the command was found and cancelled, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId or commandId is null or empty.</exception>
    public bool CancelCommand(string sessionId, string commandId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommandId(commandId, nameof(commandId));

        // Try to cancel in the session first
        if (m_Sessions.TryGetValue(sessionId, out var session) && session.CancelCommand(commandId))
        {
            return true;
        }

        // If not found in session, try to cancel in the extensions library
        return m_ExtensionScripts.CancelCommand(sessionId, commandId);
    }

    /// <summary>
    /// Cancels all commands in a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="reason">Optional reason for cancellation.</param>
    /// <returns>The number of commands that were cancelled.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    public int CancelAllCommands(string sessionId, string? reason = null)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        return !m_Sessions.TryGetValue(sessionId, out var session) ? 0 : session.CancelAllCommands(reason);
    }

    /// <summary>
    /// Gets the current state of a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The session state, or null if the session is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or empty.</exception>
    public SessionState? GetSessionState(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        return !m_Sessions.TryGetValue(sessionId, out var session) ? null : session.State;
    }


    /// <summary>
    /// Disposes of the debug engine and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        // Mark as disposed first to prevent new operations
        m_Disposed = true;

        m_Logger.Info("Disposing DebugEngine with {SessionCount} active sessions", m_Sessions.Count);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Close all sessions with proper async handling
        var closeTasks = m_Sessions.Values.Select(async session =>
        {
            try
            {
                // Dispose session first, then unsubscribe to avoid race conditions
                await session.DisposeAsync();
                session.CommandStateChanged -= OnSessionCommandStateChanged;
                session.SessionStateChanged -= OnSessionStateChanged;
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Error disposing session {SessionId}", session.SessionId);
            }
        }).ToArray();

        try
        {
            Task.WaitAll(closeTasks, cts.Token);
        }
        catch (OperationCanceledException)
        {
            m_Logger.Warn("Session disposal timed out after 30 seconds");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error waiting for sessions to close during disposal");
        }

        m_Sessions.Clear();
        m_Logger.Info("DebugEngine disposed");
    }

    /// <summary>
    /// Handles command state changed events from sessions.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected void OnSessionCommandStateChanged(object? sender, CommandStateChangedEventArgs e)
    {
        // Forward the event
        CommandStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Handles session state changed events from sessions.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        // Forward the event
        SessionStateChanged?.Invoke(this, e);
    }


    /// <summary>
    /// Validates that a session ID is not null or empty.
    /// </summary>
    /// <param name="sessionId">The session ID to validate.</param>
    /// <param name="paramName">The parameter name for the exception message.</param>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or whitespace.</exception>
    protected static void ValidateSessionId(string sessionId, string paramName)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", paramName);
        }
    }

    /// <summary>
    /// Validates that a command ID is not null or empty.
    /// </summary>
    /// <param name="commandId">The command ID to validate.</param>
    /// <param name="paramName">The parameter name for the exception message.</param>
    /// <exception cref="ArgumentException">Thrown when commandId is null or whitespace.</exception>
    protected static void ValidateCommandId(string commandId, string paramName)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            throw new ArgumentException("Command ID cannot be null or empty", paramName);
        }
    }

    /// <summary>
    /// Validates that a command is not null or empty.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="paramName">The parameter name for the exception message.</param>
    /// <exception cref="ArgumentException">Thrown when command is null or whitespace.</exception>
    protected static void ValidateCommand(string command, string paramName)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", paramName);
        }
    }

    /// <summary>
    /// Throws an exception if the engine has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the engine has been disposed.</exception>
    protected void ThrowIfDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(DebugEngine));
        }
    }


    /// <summary>
    /// Validates that an extension name is not null or empty.
    /// </summary>
    /// <param name="extensionName">The extension name to validate.</param>
    /// <param name="paramName">The parameter name for the exception message.</param>
    /// <exception cref="ArgumentException">Thrown when extensionName is null or whitespace.</exception>
    protected static void ValidateExtensionName(string extensionName, string paramName)
    {
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            throw new ArgumentException("Extension name cannot be null or empty", paramName);
        }
    }

}
