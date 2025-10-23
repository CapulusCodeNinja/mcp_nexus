using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using nexus.engine.Configuration;
using nexus.engine.Events;
using nexus.engine.Models;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;
using nexus.engine.batch;

namespace nexus.engine;

/// <summary>
/// Main implementation of the debug engine that manages CDB sessions and command execution.
/// </summary>
public class DebugEngine : IDebugEngine
{
    private readonly ILogger<DebugEngine> m_Logger;
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly IFileSystem m_FileSystem;
    private readonly IProcessManager m_ProcessManager;
    private readonly IBatchProcessor? m_BatchProcessor;
    private readonly ConcurrentDictionary<string, Internal.DebugSession> m_Sessions = new();
    private volatile bool m_Disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEngine"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="configuration">The engine configuration.</param>
    /// <param name="fileSystem">The file system interface.</param>
    /// <param name="processManager">The process manager interface.</param>
    /// <param name="batchProcessor">Optional batch processor for command batching.</param>
    /// <exception cref="ArgumentNullException">Thrown when loggerFactory or configuration is null.</exception>
    public DebugEngine(ILoggerFactory loggerFactory, DebugEngineConfiguration configuration, IFileSystem fileSystem, IProcessManager processManager, IBatchProcessor? batchProcessor = null)
    {
        m_LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        m_Logger = loggerFactory.CreateLogger<DebugEngine>();
        m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        m_BatchProcessor = batchProcessor;

        m_Logger.LogInformation("DebugEngine initialized with max {MaxSessions} concurrent sessions", m_Configuration.MaxConcurrentSessions);
    }

    /// <inheritdoc />
    public event EventHandler<CommandStateChangedEventArgs>? CommandStateChanged;

    /// <inheritdoc />
    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

    /// <inheritdoc />
    public async Task<string> CreateSessionAsync(string dumpFilePath, string? symbolPath = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateSessionId(dumpFilePath, nameof(dumpFilePath));

        if (!m_FileSystem.FileExists(dumpFilePath))
            throw new FileNotFoundException($"Dump file not found: {dumpFilePath}", dumpFilePath);

        if (m_Sessions.Count >= m_Configuration.MaxConcurrentSessions)
            throw new InvalidOperationException($"Maximum number of concurrent sessions ({m_Configuration.MaxConcurrentSessions}) reached");

        var sessionId = GenerateSessionId();
        m_Logger.LogInformation("Creating debug session {SessionId} for dump file: {DumpFilePath}", sessionId, dumpFilePath);

        try
        {
            var session = new Internal.DebugSession(sessionId, dumpFilePath, symbolPath, m_Configuration, m_LoggerFactory, m_FileSystem, m_ProcessManager, m_BatchProcessor);

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

            m_Logger.LogInformation("Debug session {SessionId} created successfully", sessionId);
            return sessionId;
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to create debug session {SessionId}", sessionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task CloseSessionAsync(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        m_Logger.LogInformation("Closing debug session {SessionId}", sessionId);

        if (m_Sessions.TryRemove(sessionId, out var session))
        {
            try
            {
                // Unsubscribe from events
                session.CommandStateChanged -= OnSessionCommandStateChanged;
                session.SessionStateChanged -= OnSessionStateChanged;

                await session.DisposeAsync();
                m_Logger.LogInformation("Debug session {SessionId} closed successfully", sessionId);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error closing debug session {SessionId}", sessionId);
                throw;
            }
        }
        else
        {
            m_Logger.LogWarning("Session {SessionId} not found for closing", sessionId);
        }
    }

    /// <inheritdoc />
    public bool IsSessionActive(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        return m_Sessions.TryGetValue(sessionId, out var session) && session.IsActive;
    }

    /// <inheritdoc />
    public string EnqueueCommand(string sessionId, string command)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommand(command, nameof(command));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
            throw new InvalidOperationException($"Session {sessionId} not found");

        if (!session.IsActive)
            throw new InvalidOperationException($"Session {sessionId} is not active");

        var commandId = session.EnqueueCommand(command);
        m_Logger.LogDebug("Command {CommandId} enqueued in session {SessionId}: {Command}", commandId, sessionId, command);
        return commandId;
    }

    /// <inheritdoc />
    public async Task<CommandInfo> GetCommandInfoAsync(string sessionId, string commandId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommandId(commandId, nameof(commandId));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
            throw new InvalidOperationException($"Session {sessionId} not found");

        if (!session.IsActive)
            throw new InvalidOperationException($"Session {sessionId} is not active");

        return await session.GetCommandInfoAsync(commandId, cancellationToken);
    }

    /// <inheritdoc />
    public CommandInfo? GetCommandInfo(string sessionId, string commandId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommandId(commandId, nameof(commandId));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
            return null;

        return session.GetCommandInfo(commandId);
    }

    /// <inheritdoc />
    public Dictionary<string, CommandInfo> GetAllCommandInfos(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
            return new Dictionary<string, CommandInfo>();

        return session.GetAllCommandInfos();
    }

    /// <inheritdoc />
    public bool CancelCommand(string sessionId, string commandId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));
        ValidateCommandId(commandId, nameof(commandId));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
            return false;

        return session.CancelCommand(commandId);
    }

    /// <inheritdoc />
    public int CancelAllCommands(string sessionId, string? reason = null)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
            return 0;

        return session.CancelAllCommands(reason);
    }

    /// <inheritdoc />
    public SessionState? GetSessionState(string sessionId)
    {
        ThrowIfDisposed();
        ValidateSessionId(sessionId, nameof(sessionId));

        if (!m_Sessions.TryGetValue(sessionId, out var session))
            return null;

        return session.State;
    }


    /// <inheritdoc />
    public void Dispose()
    {
        if (m_Disposed)
            return;

        m_Logger.LogInformation("Disposing DebugEngine with {SessionCount} active sessions", m_Sessions.Count);

        // Close all sessions
        var closeTasks = m_Sessions.Values.Select(async session =>
        {
            try
            {
                session.CommandStateChanged -= OnSessionCommandStateChanged;
                session.SessionStateChanged -= OnSessionStateChanged;
                await session.DisposeAsync();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error disposing session {SessionId}", session.SessionId);
            }
        });

        try
        {
            Task.WaitAll(closeTasks.ToArray(), TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error waiting for sessions to close during disposal");
        }

        m_Sessions.Clear();
        m_Disposed = true;
        m_Logger.LogInformation("DebugEngine disposed");
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

    private static string GenerateSessionId()
    {
        return $"sess-{Guid.NewGuid():N}";
    }

    private static void ValidateSessionId(string sessionId, string paramName)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", paramName);
    }

    private static void ValidateCommandId(string commandId, string paramName)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            throw new ArgumentException("Command ID cannot be null or empty", paramName);
    }

    private static void ValidateCommand(string command, string paramName)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty", paramName);
    }

    /// <summary>
    /// Throws an exception if the engine has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the engine has been disposed.</exception>
    protected void ThrowIfDisposed()
    {
        if (m_Disposed)
            throw new ObjectDisposedException(nameof(DebugEngine));
    }
}
