using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Refactored thread-safe, deadlock-free multi-session manager
    /// Uses focused components for better maintainability and testability
    /// </summary>
    public class ThreadSafeSessionManager : ISessionManager, IDisposable
    {
        private readonly ILogger<ThreadSafeSessionManager> m_logger;
        private readonly ConcurrentDictionary<string, SessionInfo> m_sessions = new();
        private readonly SemaphoreSlim m_sessionCreationSemaphore = new(1, 1);
        private readonly CancellationTokenSource m_shutdownCts = new();

        // Focused components
        private readonly SessionManagerConfiguration m_config;
        private readonly SessionLifecycleManager m_lifecycleManager;
        private readonly SessionMonitoringService m_monitoringService;
        private readonly SessionStatisticsCollector m_statisticsCollector;

        // Thread-safe counters
        private long m_sessionCounter = 0;
        private volatile bool m_disposed = false;

        public ThreadSafeSessionManager(
            ILogger<ThreadSafeSessionManager> logger,
            IServiceProvider serviceProvider,
            IMcpNotificationService notificationService,
            IOptions<SessionConfiguration>? config = null,
            IOptions<CdbSessionOptions>? cdbOptions = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create focused components
            m_config = new SessionManagerConfiguration(config, cdbOptions);
            m_lifecycleManager = new SessionLifecycleManager(
                logger, serviceProvider, notificationService, m_config, m_sessions);
            m_monitoringService = new SessionMonitoringService(
                logger, notificationService, m_config, m_sessions, m_lifecycleManager, m_shutdownCts);
            m_statisticsCollector = new SessionStatisticsCollector(
                logger, m_sessions, m_lifecycleManager, m_monitoringService);

            m_logger.LogInformation("🚀 ThreadSafeSessionManager initializing with config: MaxSessions={MaxSessions}, Timeout={Timeout}",
                m_config.Config.MaxConcurrentSessions, m_config.Config.SessionTimeout);

            m_logger.LogInformation("✅ ThreadSafeSessionManager initialized successfully with focused components");
        }

        /// <summary>
        /// Creates a new debugging session
        /// </summary>
        /// <param name="dumpPath">Path to the dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The unique session ID</returns>
        public virtual async Task<string> CreateSessionAsync(string dumpPath, string? symbolsPath = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // Validate parameters
            if (dumpPath == null)
                throw new ArgumentNullException(nameof(dumpPath));

            var validation = m_config.ValidateSessionCreation(dumpPath, symbolsPath);
            if (!validation.IsValid)
            {
                if (validation.ErrorMessage?.Contains("not found") == true)
                    throw new FileNotFoundException(validation.ErrorMessage);
                throw new ArgumentException(validation.ErrorMessage);
            }

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, m_shutdownCts.Token);

            // Generate unique session ID
            var sessionNumber = Interlocked.Increment(ref m_sessionCounter);
            var sessionId = SessionManagerConfiguration.GenerateSessionId(sessionNumber);

            try
            {
                // Hold semaphore during entire session creation to prevent race conditions
                await m_sessionCreationSemaphore.WaitAsync(combinedCts.Token);
                try
                {
                    // Check if we're shutting down
                    if (m_shutdownCts.Token.IsCancellationRequested)
                        throw new OperationCanceledException("Session manager is shutting down");

                    // Check session count limits
                    if (m_config.WouldExceedSessionLimit(m_sessions.Count))
                    {
                        throw new SessionLimitExceededException(m_sessions.Count, m_config.Config.MaxConcurrentSessions);
                    }

                    // Create session using lifecycle manager
                    var sessionInfo = await m_lifecycleManager.CreateSessionAsync(sessionId, dumpPath, symbolsPath, combinedCts.Token);

                    m_logger.LogInformation("✅ Session {SessionId} created successfully", sessionId);
                    return sessionId;
                }
                finally
                {
                    m_sessionCreationSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Failed to create session {SessionId}", sessionId);
                throw;
            }
        }

        /// <summary>
        /// Closes a debugging session
        /// </summary>
        /// <param name="sessionId">Session ID to close</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the session was closed successfully</returns>
        public virtual async Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            if (sessionId == null)
                throw new ArgumentNullException(nameof(sessionId));

            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be empty or whitespace", nameof(sessionId));

            return await m_lifecycleManager.CloseSessionAsync(sessionId, cancellationToken);
        }

        /// <summary>
        /// Checks if a session exists and is active
        /// </summary>
        /// <param name="sessionId">Session ID to check</param>
        /// <returns>True if the session exists and is active</returns>
        public virtual bool SessionExists(string sessionId)
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeSessionManager));

            if (sessionId == null)
                throw new ArgumentNullException(nameof(sessionId));

            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be empty or whitespace", nameof(sessionId));

            if (!m_sessions.TryGetValue(sessionId, out var session))
                return false;

            // Check if session is expired and schedule cleanup if needed
            if (m_config.IsSessionExpired(session.LastActivity))
            {
                // Schedule async cleanup without blocking caller
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_lifecycleManager.CloseSessionAsync(sessionId);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Error during async cleanup of session {SessionId}", sessionId);
                    }
                }, CancellationToken.None);
                return false;
            }

            return session.Status == SessionStatus.Active && !session.IsDisposed;
        }

        /// <summary>
        /// Gets the command queue for a session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>The command queue service</returns>
        public ICommandQueueService GetCommandQueue(string sessionId)
        {
            if (m_sessions.TryGetValue(sessionId, out var session) &&
                session.Status == SessionStatus.Active && !session.IsDisposed)
            {
                UpdateActivity(sessionId);
                return session.CommandQueue;
            }

            throw new SessionNotFoundException(sessionId);
        }

        /// <summary>
        /// Gets detailed context information about a session
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns>Session context with detailed information</returns>
        public virtual SessionContext GetSessionContext(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            if (!m_sessions.TryGetValue(sessionId, out var session))
                throw new SessionNotFoundException(sessionId);

            if (session == null || session.IsDisposed)
                throw new SessionNotFoundException(sessionId, "Session has been disposed");

            // Use statistics collector to create detailed context
            var activeSessions = m_statisticsCollector.GetActiveSessions();
            var sessionContext = activeSessions.FirstOrDefault(s => s.SessionId == sessionId);

            if (sessionContext == null)
                throw new SessionNotFoundException(sessionId, "Session context not available");

            return sessionContext;
        }

        /// <summary>
        /// Updates the last activity time for a session
        /// </summary>
        /// <param name="sessionId">Session ID to update</param>
        public virtual void UpdateActivity(string sessionId)
        {
            m_monitoringService.UpdateActivity(sessionId);
        }

        /// <summary>
        /// Gets all active sessions
        /// </summary>
        /// <returns>Collection of active session contexts</returns>
        public virtual IEnumerable<SessionContext> GetActiveSessions()
        {
            if (m_disposed)
                return Enumerable.Empty<SessionContext>();

            return m_statisticsCollector.GetActiveSessions();
        }

        /// <summary>
        /// Gets all sessions (active and inactive)
        /// </summary>
        /// <returns>Collection of all session info objects</returns>
        public virtual IEnumerable<SessionInfo> GetAllSessions()
        {
            if (m_disposed)
                return Enumerable.Empty<SessionInfo>();

            return m_statisticsCollector.GetAllSessions();
        }

        /// <summary>
        /// Gets comprehensive session statistics
        /// </summary>
        /// <returns>Session management statistics</returns>
        public SessionStatistics GetStatistics()
        {
            if (m_disposed)
                return new SessionStatistics();

            return m_statisticsCollector.GetStatistics();
        }

        /// <summary>
        /// Cleans up expired sessions
        /// </summary>
        /// <returns>Number of sessions cleaned up</returns>
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            if (m_disposed)
                return 0;

            return await m_lifecycleManager.CleanupExpiredSessionsAsync();
        }

        /// <summary>
        /// Throws if the session manager is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeSessionManager));
        }

        /// <summary>
        /// Disposes the session manager and all resources
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            try
            {
                m_logger.LogInformation("🛑 Shutting down ThreadSafeSessionManager");

                // Signal shutdown to all components
                m_shutdownCts.Cancel();

                // Close all active sessions
                var sessionIds = m_sessions.Keys.ToList();
                var closeTasks = sessionIds.Select(id => m_lifecycleManager.CloseSessionAsync(id)).ToArray();

                try
                {
                    Task.WaitAll(closeTasks, TimeSpan.FromSeconds(30));
                }
                catch (AggregateException ex)
                {
                    m_logger.LogWarning("Some sessions failed to close cleanly: {Errors}",
                        string.Join(", ", ex.InnerExceptions.Select(e => e.Message)));
                }

                // Dispose components
                m_monitoringService?.Dispose();
                m_sessionCreationSemaphore?.Dispose();
                m_shutdownCts?.Dispose();

                // Log final statistics
                m_statisticsCollector?.LogStatisticsSummary();

                m_logger.LogInformation("✅ ThreadSafeSessionManager shutdown complete");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Error during ThreadSafeSessionManager disposal");
            }
        }
    }
}
