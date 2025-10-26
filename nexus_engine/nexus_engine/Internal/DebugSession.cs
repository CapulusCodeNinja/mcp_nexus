using Nexus.Engine.Preprocessing;
using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using NLog;

namespace Nexus.Engine.Internal;

/// <summary>
/// Internal implementation of a debug session that manages a single CDB process and command queue.
/// </summary>
internal class DebugSession : IDisposable
{
    private readonly Logger m_Logger;
    private readonly string m_DumpFilePath;
    private readonly string? m_SymbolPath;
    private readonly CdbSession m_CdbSession;
    private readonly CommandQueue m_CommandQueue;
    private readonly object m_StateLock = new();
    private volatile SessionState m_State = SessionState.Initializing;
    private volatile bool m_Disposed = false;

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public string SessionId
    {
        get;
    }

    /// <summary>
    /// Gets the current state of the session.
    /// </summary>
    public SessionState State
    {
        get
        {
            lock (m_StateLock)
            {
                return m_State;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the session is active.
    /// </summary>
    public bool IsActive => State == SessionState.Active;

    /// <summary>
    /// Occurs when a command's state changes.
    /// </summary>
    public event EventHandler<CommandStateChangedEventArgs>? CommandStateChanged;

    /// <summary>
    /// Occurs when the session's state changes.
    /// </summary>
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugSession"/> class.
    /// </summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <param name="dumpFilePath">The path to the dump file to analyze.</param>
    /// <param name="symbolPath">Optional symbol path for symbol resolution.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="commandPreprocessor">Optional command preprocessor for WSL path conversion and directory creation.</param>
    /// <exception cref="ArgumentNullException">Thrown when sessionId or dumpFilePath is null.</exception>
    public DebugSession(
        string sessionId,
        string dumpFilePath,
        string? symbolPath,
        IFileSystem fileSystem,
        IProcessManager processManager,
        CommandPreprocessor commandPreprocessor)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        m_DumpFilePath = dumpFilePath ?? throw new ArgumentNullException(nameof(dumpFilePath));
        m_SymbolPath = symbolPath;

        m_Logger = LogManager.GetCurrentClassLogger();

        // Create CDB session
        m_CdbSession = new CdbSession(fileSystem, processManager, commandPreprocessor);

        // Create command queue
        m_CommandQueue = new CommandQueue(SessionId);

        // Subscribe to command queue events
        m_CommandQueue.CommandStateChanged += OnCommandStateChanged;
    }

    /// <summary>
    /// Initializes the debug session.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            m_Logger.Info("Initializing debug session {SessionId}", SessionId);
            SetState(SessionState.Initializing);

            // Initialize CDB session
            await m_CdbSession.InitializeAsync(SessionId, m_DumpFilePath, m_SymbolPath, cancellationToken);

            // Start command queue
            await m_CommandQueue.StartAsync(m_CdbSession, cancellationToken);

            SetState(SessionState.Active);
            m_Logger.Info("Debug session {SessionId} initialized successfully", SessionId);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to initialize debug session {SessionId}", SessionId);
            SetState(SessionState.Faulted);
            throw;
        }
    }

    /// <summary>
    /// Enqueues a command for execution.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The command identifier.</returns>
    public string EnqueueCommand(string command)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        return m_CommandQueue.EnqueueCommand(command);
    }

    /// <summary>
    /// Gets the information about a command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command information.</returns>
    public async Task<CommandInfo> GetCommandInfoAsync(string commandId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        return await m_CommandQueue.GetCommandInfoAsync(commandId, cancellationToken);
    }

    /// <summary>
    /// Gets the current information about a command without waiting for completion.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>The command information, or null if not found.</returns>
    public CommandInfo? GetCommandInfo(string commandId)
    {
        ThrowIfDisposed();
        return m_CommandQueue.GetCommandInfo(commandId);
    }

    /// <summary>
    /// Gets the information about all commands in the session.
    /// </summary>
    /// <returns>A dictionary of command IDs to their information.</returns>
    public Dictionary<string, CommandInfo> GetAllCommandInfos()
    {
        ThrowIfDisposed();
        return m_CommandQueue.GetAllCommandInfos();
    }

    /// <summary>
    /// Cancels a command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>True if the command was found and cancelled, false otherwise.</returns>
    public bool CancelCommand(string commandId)
    {
        ThrowIfDisposed();
        return m_CommandQueue.CancelCommand(commandId);
    }

    /// <summary>
    /// Cancels all commands in the session.
    /// </summary>
    /// <param name="reason">Optional reason for cancellation.</param>
    /// <returns>The number of commands that were cancelled.</returns>
    public int CancelAllCommands(string? reason = null)
    {
        ThrowIfDisposed();
        return m_CommandQueue.CancelAllCommands(reason);
    }

    /// <summary>
    /// Disposes the debug session.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Logger.Info("Disposing debug session {SessionId}", SessionId);

        try
        {
            SetState(SessionState.Closing);

            // Cancel all commands
            var cancelledCount = m_CommandQueue.CancelAllCommands("Session closing");
            m_Logger.Debug("Cancelled {Count} commands during session disposal", cancelledCount);

            // Stop command queue
            await m_CommandQueue.StopAsync();

            // Dispose CDB session
            await m_CdbSession.DisposeAsync();

            SetState(SessionState.Closed);
            m_Logger.Info("Debug session {SessionId} disposed successfully", SessionId);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error disposing debug session {SessionId}", SessionId);
            SetState(SessionState.Faulted);
            throw;
        }
        finally
        {
            m_Disposed = true;
        }
    }

    /// <summary>
    /// Disposes of the debug session and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        try
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error during synchronous disposal of session {SessionId}", SessionId);
        }
    }

    /// <summary>
    /// Handles command state changed events from the command queue and forwards them with session context.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected void OnCommandStateChanged(object? sender, CommandStateChangedEventArgs e)
    {
        // Forward the event with session context
        var args = new CommandStateChangedEventArgs
        {
            SessionId = SessionId,
            CommandId = e.CommandId,
            OldState = e.OldState,
            NewState = e.NewState,
            Timestamp = e.Timestamp,
            Command = e.Command
        };

        CommandStateChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Sets the session state and notifies listeners if the state changed.
    /// </summary>
    /// <param name="newState">The new session state.</param>
    protected void SetState(SessionState newState)
    {
        SessionState oldState;
        lock (m_StateLock)
        {
            oldState = m_State;
            m_State = newState;
        }

        if (oldState != newState)
        {
            var args = new SessionStateChangedEventArgs
            {
                SessionId = SessionId,
                OldState = oldState,
                NewState = newState,
                Timestamp = DateTime.Now
            };

            SessionStateChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Throws an exception if the session has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the session is disposed.</exception>
    protected void ThrowIfDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(DebugSession));
        }
    }

    /// <summary>
    /// Throws an exception if the session is not in an active state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the session is not active.</exception>
    protected void ThrowIfNotActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException($"Session {SessionId} is not active (current state: {State})");
        }
    }
}
