using NLog;

using WinAiDbg.Config;
using WinAiDbg.Engine.Batch;
using WinAiDbg.Engine.Preprocessing;
using WinAiDbg.Engine.Share;
using WinAiDbg.Engine.Share.Events;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;

namespace WinAiDbg.Engine.Internal;

/// <summary>
/// Internal implementation of a debug session that manages a single CDB process and command queue.
/// </summary>
internal class DebugSession : IDisposable
{
    private readonly Logger m_Logger;
    private readonly ISettings m_Settings;
    private readonly IFileSystem m_FileSystem;
    private readonly IFileCleanupQueue m_FileCleanupQueue;
    private readonly string m_DumpFilePath;
    private readonly string? m_SymbolPath;
    private readonly CdbSession m_CdbSession;
    private readonly CommandQueue m_CommandQueue;
    private readonly IBatchProcessor m_BatchProcessor;
    private readonly ReaderWriterLockSlim m_StateLock = new();
    private SessionState m_State = SessionState.Initializing;
    private volatile bool m_Disposed = false;

    /// <summary>
    /// Stores the last activity timestamp for this session as ticks, updated lock-free for performance.
    /// </summary>
    private long m_LastActivityTicks;

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
            m_StateLock.EnterReadLock();
            try
            {
                return m_State;
            }
            finally
            {
                m_StateLock.ExitReadLock();
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
    /// <param name="settings">The product settings.</param>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="fileCleanupQueue">The file cleanup queue for deferred file deletion.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="batchProcessor">The batch processing engine.</param>
    /// <param name="commandPreprocessor">Optional command preprocessor for WSL path conversion and directory creation.</param>
    /// <exception cref="ArgumentNullException">Thrown when sessionId or dumpFilePath is null.</exception>
    public DebugSession(
        string sessionId,
        string dumpFilePath,
        string? symbolPath,
        ISettings settings,
        IFileSystem fileSystem,
        IFileCleanupQueue fileCleanupQueue,
        IProcessManager processManager,
        IBatchProcessor batchProcessor,
        CommandPreprocessor commandPreprocessor)
    {
        m_Settings = settings;
        m_FileSystem = fileSystem;
        m_FileCleanupQueue = fileCleanupQueue;
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        m_DumpFilePath = dumpFilePath ?? throw new ArgumentNullException(nameof(dumpFilePath));
        m_SymbolPath = symbolPath;
        m_BatchProcessor = batchProcessor;

        m_Logger = LogManager.GetCurrentClassLogger();

        // Create CDB session
        m_CdbSession = new CdbSession(m_Settings, fileSystem, processManager, commandPreprocessor);

        // Create command queue
        m_CommandQueue = new CommandQueue(SessionId, m_Settings, m_BatchProcessor);

        // Subscribe to command queue events
        m_CommandQueue.CommandStateChanged += OnCommandStateChanged;

        // Seed last activity to now
        m_LastActivityTicks = DateTime.Now.Ticks;
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

            UpdateLastActivity();

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

        UpdateLastActivity();

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
    /// <returns>A task representing the asynchronous disposal operation.</returns>
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

            if (m_Settings.Get().WinAiDbg.SessionManagement.DeleteDumpFileOnSessionClose && !string.IsNullOrWhiteSpace(m_DumpFilePath))
            {
                m_Logger.Info("Enqueueing dump file {DumpFilePath} for cleanup (session {SessionId})", m_DumpFilePath, SessionId);
                m_FileCleanupQueue.Enqueue(m_DumpFilePath);
            }

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
        finally
        {
            m_StateLock?.Dispose();
        }
    }

    /// <summary>
    /// Handles command state changed events from the command queue and forwards them with session context.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    protected void OnCommandStateChanged(object? sender, CommandStateChangedEventArgs e)
    {
        // Update last activity whenever a command state changes
        UpdateLastActivity();

        // Forward the event with session context
        var args = new CommandStateChangedEventArgs
        {
            SessionId = SessionId,
            CommandId = e.CommandId,
            OldState = e.OldState,
            NewState = e.NewState,
            Timestamp = e.Timestamp,
            Command = e.Command,
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
        m_StateLock.EnterWriteLock();
        try
        {
            oldState = m_State;
            m_State = newState;
        }
        finally
        {
            m_StateLock.ExitWriteLock();
        }

        if (oldState != newState)
        {
            var args = new SessionStateChangedEventArgs
            {
                SessionId = SessionId,
                OldState = oldState,
                NewState = newState,
                Timestamp = DateTime.Now,
            };

            SessionStateChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Gets the last activity time for this session.
    /// </summary>
    public DateTime LastActivityTime => new DateTime(Volatile.Read(ref m_LastActivityTicks));

    /// <summary>
    /// Registers an activity on this session by updating the last activity timestamp.
    /// </summary>
    internal void RegisterActivity()
    {
        UpdateLastActivity();
    }

    /// <summary>
    /// Updates the last activity timestamp to the current local time in a lock-free manner.
    /// </summary>
    protected void UpdateLastActivity()
    {
        _ = Interlocked.Exchange(ref m_LastActivityTicks, DateTime.Now.Ticks);
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
