using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.engine.batch;
using nexus.engine.Configuration;
using nexus.engine.Events;
using nexus.engine.Models;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;
using System.Collections.Concurrent;

namespace nexus.engine.Internal;

/// <summary>
/// Internal implementation of a debug session that manages a single CDB process and command queue.
/// </summary>
internal class DebugSession : IDisposable
{
    private readonly ILogger<DebugSession> m_Logger;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly string m_SessionId;
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
    public string SessionId => m_SessionId;

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
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="dumpFilePath">The path to the dump file.</param>
    /// <param name="symbolPath">Optional symbol path.</param>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="fileSystem">The file system interface.</param>
    /// <param name="processManager">The process manager interface.</param>
    /// <param name="batchProcessor">Optional batch processor for command batching.</param>
    public DebugSession(
        string sessionId,
        string dumpFilePath,
        string? symbolPath,
        DebugEngineConfiguration configuration,
        IFileSystem fileSystem,
        IProcessManager processManager,
        IBatchProcessor batchProcessor)
    {
        m_SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        m_DumpFilePath = dumpFilePath ?? throw new ArgumentNullException(nameof(dumpFilePath));
        m_SymbolPath = symbolPath;
        m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        m_Logger = serviceProvider.GetRequiredService<ILogger<DebugSession>>();

        // Create CDB session
        var cdbLogger = loggerFactory.CreateLogger<CdbSession>();
        m_CdbSession = new CdbSession(m_Configuration, cdbLogger, fileSystem, processManager);

        // Create command queue
        var queueLogger = loggerFactory.CreateLogger<CommandQueue>();
        m_CommandQueue = new CommandQueue(m_SessionId, m_Configuration, queueLogger, batchProcessor);

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
            m_Logger.LogInformation("Initializing debug session {SessionId}", m_SessionId);
            SetState(SessionState.Initializing);

            // Initialize CDB session
            await m_CdbSession.InitializeAsync(m_DumpFilePath, m_SymbolPath, cancellationToken);

            // Start command queue
            await m_CommandQueue.StartAsync(m_CdbSession, cancellationToken);

            SetState(SessionState.Active);
            m_Logger.LogInformation("Debug session {SessionId} initialized successfully", m_SessionId);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to initialize debug session {SessionId}", m_SessionId);
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
            return;

        m_Logger.LogInformation("Disposing debug session {SessionId}", m_SessionId);

        try
        {
            SetState(SessionState.Closing);

            // Cancel all commands
            var cancelledCount = m_CommandQueue.CancelAllCommands("Session closing");
            m_Logger.LogDebug("Cancelled {Count} commands during session disposal", cancelledCount);

            // Stop command queue
            await m_CommandQueue.StopAsync();

            // Dispose CDB session
            await m_CdbSession.DisposeAsync();

            SetState(SessionState.Closed);
            m_Logger.LogInformation("Debug session {SessionId} disposed successfully", m_SessionId);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error disposing debug session {SessionId}", m_SessionId);
            SetState(SessionState.Faulted);
            throw;
        }
        finally
        {
            m_Disposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (m_Disposed)
            return;

        try
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error during synchronous disposal of session {SessionId}", m_SessionId);
        }
    }

    protected void OnCommandStateChanged(object? sender, CommandStateChangedEventArgs e)
    {
        // Forward the event with session context
        var args = new CommandStateChangedEventArgs
        {
            SessionId = m_SessionId,
            CommandId = e.CommandId,
            OldState = e.OldState,
            NewState = e.NewState,
            Timestamp = e.Timestamp,
            Command = e.Command
        };

        CommandStateChanged?.Invoke(this, args);
    }

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
                SessionId = m_SessionId,
                OldState = oldState,
                NewState = newState,
                Timestamp = DateTime.Now
            };

            SessionStateChanged?.Invoke(this, args);
        }
    }

    protected void ThrowIfDisposed()
    {
        if (m_Disposed)
            throw new ObjectDisposedException(nameof(DebugSession));
    }

    protected void ThrowIfNotActive()
    {
        if (!IsActive)
            throw new InvalidOperationException($"Session {m_SessionId} is not active (current state: {State})");
    }
}
