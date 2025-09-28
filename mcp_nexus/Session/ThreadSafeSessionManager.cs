using System.Collections.Concurrent;
using System.Diagnostics;
using mcp_nexus.Debugger;
using mcp_nexus.Models;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using Microsoft.Extensions.Options;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Thread-safe, deadlock-free multi-session manager with automatic cleanup
    /// </summary>
    public class ThreadSafeSessionManager : ISessionManager, IDisposable
    {
        private readonly ILogger<ThreadSafeSessionManager> m_logger;
        private readonly IServiceProvider m_serviceProvider;
        private readonly IMcpNotificationService m_notificationService;
        private readonly SessionConfiguration m_config;
        private readonly CdbSessionOptions m_cdbOptions;

        // CONCURRENCY: Thread-safe session storage
        private readonly ConcurrentDictionary<string, SessionInfo> m_sessions = new();

        // LOCKING: Single semaphore for session creation to prevent race conditions
        private readonly SemaphoreSlim m_sessionCreationSemaphore = new(1, 1);

        // CANCELLATION: Global shutdown coordination
        private readonly CancellationTokenSource m_shutdownCts = new();

        // CLEANUP: Automatic session cleanup
        private readonly Timer m_cleanupTimer;
        private readonly Task m_monitoringTask;

        // METRICS: Thread-safe performance counters
        private long m_sessionCounter = 0;
        private long m_totalSessionsCreated = 0;
        private long m_totalSessionsClosed = 0;
        private long m_totalSessionsExpired = 0;
        private long m_totalCommandsProcessed = 0;
        private readonly DateTime m_startTime = DateTime.UtcNow;

        private volatile bool m_disposed = false;

        public ThreadSafeSessionManager(
            ILogger<ThreadSafeSessionManager> logger,
            IServiceProvider serviceProvider,
            IMcpNotificationService notificationService,
            IOptions<SessionConfiguration>? config = null,
            IOptions<CdbSessionOptions>? cdbOptions = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            m_config = config?.Value ?? new SessionConfiguration();
            m_cdbOptions = cdbOptions?.Value ?? new CdbSessionOptions();

            m_logger.LogInformation("🚀 ThreadSafeSessionManager initializing with config: MaxSessions={MaxSessions}, Timeout={Timeout}",
                m_config.MaxConcurrentSessions, m_config.SessionTimeout);

            // SAFETY: Initialize cleanup timer with thread-safe callback
            m_cleanupTimer = new Timer(_ =>
            {
                // SAFETY: Fire-and-forget with proper exception handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SafeCleanupExpiredSessions();
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Error during scheduled session cleanup");
                    }
                }, CancellationToken.None);
            }, null, m_config.CleanupInterval, m_config.CleanupInterval);

            // MONITORING: Start session health monitoring
            m_monitoringTask = Task.Run(MonitorSessionHealthAsync, m_shutdownCts.Token);

            m_logger.LogInformation("✅ ThreadSafeSessionManager initialized successfully");
        }

        public async Task<string> CreateSessionAsync(string dumpPath, string? symbolsPath = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // VALIDATION: Early parameter validation (no locks needed)
            if (string.IsNullOrWhiteSpace(dumpPath))
                throw new ArgumentException("Dump path cannot be null or empty", nameof(dumpPath));

            if (!File.Exists(dumpPath))
                throw new FileNotFoundException($"Dump file not found: {dumpPath}");

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, m_shutdownCts.Token);

            // CONCURRENCY: Generate unique session ID atomically with enhanced entropy
            var sessionNumber = Interlocked.Increment(ref m_sessionCounter);
            var guid = Guid.NewGuid().ToString("N");
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var processId = Environment.ProcessId;
            var sessionId = $"sess-{sessionNumber:D6}-{guid[..8]}-{timestamp:X8}-{processId:X4}";

            SessionInfo? newSession = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // CRITICAL FIX: Hold semaphore during entire session creation to prevent TOCTTOU race
                await m_sessionCreationSemaphore.WaitAsync(combinedCts.Token);
                try
                {
                    // SAFETY: Check if we're shutting down
                    if (m_shutdownCts.Token.IsCancellationRequested)
                        throw new OperationCanceledException("Session manager is shutting down");

                    // LIMITS: Check session count limits
                    if (m_sessions.Count >= m_config.MaxConcurrentSessions)
                    {
                        throw new SessionLimitExceededException(m_sessions.Count, m_config.MaxConcurrentSessions);
                    }

                    m_logger.LogInformation("🔧 Creating session {SessionId} for dump: {DumpPath}", sessionId, dumpPath);

                    // ISOLATION: Create session components INSIDE lock to prevent race
                    var sessionLogger = CreateSessionLogger(sessionId);
                    var cdbSession = CreateCdbSession(sessionLogger, sessionId);
                    var commandQueue = CreateCommandQueue(cdbSession, sessionLogger, sessionId);

                    // ASYNC: Start CDB session asynchronously to prevent deadlocks
                    var cdbTarget = ConstructCdbTarget(dumpPath, symbolsPath);
                    var startSuccess = await Task.Run(() => cdbSession.StartSession(cdbTarget, null), combinedCts.Token);

                    if (!startSuccess)
                    {
                        // CLEANUP: Synchronous cleanup inside lock
                        try { cdbSession.Dispose(); } catch { }
                        try { commandQueue.Dispose(); } catch { }
                        throw new InvalidOperationException($"Failed to start CDB session for {dumpPath}");
                    }

                    // IMMUTABLE: Create immutable session info
                    newSession = new SessionInfo
                    {
                        SessionId = sessionId,
                        CdbSession = cdbSession,
                        CommandQueue = commandQueue,
                        CreatedAt = DateTime.UtcNow,
                        DumpPath = dumpPath,
                        SymbolsPath = symbolsPath,
                        Status = SessionStatus.Active,
                        ProcessId = GetCdbProcessId(cdbSession)
                    };

                    // ATOMIC: Add to concurrent dictionary (thread-safe) - still inside lock
                    if (!m_sessions.TryAdd(sessionId, newSession))
                    {
                        // CLEANUP: Rare race condition - cleanup and retry
                        try { newSession.CdbSession.Dispose(); } catch { }
                        try { newSession.CommandQueue.Dispose(); } catch { }
                        throw new InvalidOperationException($"Session ID conflict: {sessionId}");
                    }
                }
                finally
                {
                    m_sessionCreationSemaphore.Release();
                }

                // METRICS: Update counters atomically
                Interlocked.Increment(ref m_totalSessionsCreated);

                // NOTIFICATION: Notify session creation (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService.NotifySessionEventAsync(sessionId,
                            "SESSION_CREATED", $"Session created for {Path.GetFileName(dumpPath)}",
                            GetSessionContext(sessionId));
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send session creation notification for {SessionId}", sessionId);
                    }
                }, CancellationToken.None);

                m_logger.LogInformation("✅ Session {SessionId} created successfully in {ElapsedMs}ms for {DumpPath}",
                    sessionId, stopwatch.ElapsedMilliseconds, dumpPath);

                return sessionId;
            }
            catch
            {
                // CLEANUP: Ensure cleanup on any failure
                if (newSession != null)
                {
                    await SafeCleanupSession(newSession);
                }
                throw;
            }
        }

        public async Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(sessionId))
                return false;

            if (!m_sessions.TryRemove(sessionId, out var session))
            {
                m_logger.LogDebug("Session {SessionId} not found for closure", sessionId);
                return false;
            }

            m_logger.LogInformation("🛑 Closing session {SessionId}", sessionId);

            try
            {
                await SafeCleanupSession(session);
                Interlocked.Increment(ref m_totalSessionsClosed);

                // NOTIFICATION: Notify session closure
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService.NotifySessionEventAsync(sessionId,
                            "SESSION_CLOSED", "Session closed by user request");
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send session closure notification for {SessionId}", sessionId);
                    }
                }, CancellationToken.None);

                m_logger.LogInformation("✅ Session {SessionId} closed successfully", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Error closing session {SessionId}", sessionId);
                return false;
            }
        }

        /// <summary>
        /// Internal method to close a session without disposal checks (used during shutdown)
        /// </summary>
        private async Task<bool> CloseSessionInternalAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sessionId))
                return false;

            if (!m_sessions.TryRemove(sessionId, out var session))
            {
                m_logger.LogDebug("Session {SessionId} not found for closure during shutdown", sessionId);
                return false;
            }

            m_logger.LogInformation("🛑 Closing session {SessionId} during shutdown", sessionId);

            try
            {
                await SafeCleanupSession(session);
                Interlocked.Increment(ref m_totalSessionsClosed);

                // Skip notifications during shutdown to avoid potential issues
                m_logger.LogDebug("Session {SessionId} closed successfully during shutdown", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Error closing session {SessionId} during shutdown", sessionId);
                return false;
            }
        }

        public bool SessionExists(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return false;

            // FAST PATH: Check existence without locks
            if (!m_sessions.TryGetValue(sessionId, out var session))
                return false;

            // ACTIVITY: Check expiration atomically
            var lastActivity = session.LastActivity;
            if (DateTime.UtcNow - lastActivity > m_config.SessionTimeout)
            {
                // ASYNC CLEANUP: Schedule cleanup without blocking caller
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CleanupExpiredSession(sessionId);
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

        public SessionContext GetSessionContext(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            if (!m_sessions.TryGetValue(sessionId, out var session))
                throw new SessionNotFoundException(sessionId);

            // SAFETY: Check if session is disposed or null
            if (session == null || session.IsDisposed)
                throw new SessionNotFoundException(sessionId, "Session has been disposed");

            List<(string Id, string Command, DateTime QueueTime, string Status)> queueStatus;
            try
            {
                queueStatus = session.CommandQueue?.GetQueueStatus()?.ToList() ?? new List<(string, string, DateTime, string)>();
            }
            catch (ObjectDisposedException)
            {
                throw new SessionNotFoundException(sessionId, "Session command queue has been disposed");
            }

            var timeUntilExpiry = m_config.SessionTimeout - (DateTime.UtcNow - session.LastActivity);

            return new SessionContext
            {
                SessionId = sessionId,
                Description = $"Debugging session for {Path.GetFileName(session.DumpPath ?? "Unknown")}",
                DumpPath = session.DumpPath,
                CreatedAt = session.CreatedAt,
                LastActivity = session.LastActivity,
                Status = session.Status.ToString(),
                CommandsProcessed = queueStatus.Count(q => q.Status == "Completed"),
                ActiveCommands = queueStatus.Count(q => q.Status is "Queued" or "Executing"),
                TimeUntilExpiry = timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : null,
                UsageHints = GenerateUsageHints(session, queueStatus)
            };
        }

        public void UpdateActivity(string sessionId)
        {
            if (m_sessions.TryGetValue(sessionId, out var session))
            {
                session.LastActivity = DateTime.UtcNow;
            }
        }

        public IEnumerable<SessionContext> GetActiveSessions()
        {
            return m_sessions.Values
                .Where(s => s.Status == SessionStatus.Active && !s.IsDisposed)
                .Select(s => GetSessionContext(s.SessionId))
                .ToList(); // Materialize to avoid deferred execution issues
        }

        public IEnumerable<SessionInfo> GetAllSessions()
        {
            return m_sessions.Values
                .Where(s => !s.IsDisposed)
                .ToList(); // Materialize to avoid deferred execution issues
        }

        public SessionStatistics GetStatistics()
        {
            var process = Process.GetCurrentProcess();

            return new SessionStatistics
            {
                ActiveSessions = m_sessions.Count(kvp => kvp.Value.Status == SessionStatus.Active),
                TotalSessionsCreated = Volatile.Read(ref m_totalSessionsCreated),
                TotalSessionsClosed = Volatile.Read(ref m_totalSessionsClosed),
                TotalSessionsExpired = Volatile.Read(ref m_totalSessionsExpired),
                TotalCommandsProcessed = Volatile.Read(ref m_totalCommandsProcessed),
                AverageSessionLifetime = CalculateAverageSessionLifetime(),
                Uptime = DateTime.UtcNow - m_startTime,
                MemoryUsage = new MemoryUsageInfo
                {
                    WorkingSetBytes = process.WorkingSet64,
                    PrivateMemoryBytes = process.PrivateMemorySize64,
                    GCTotalMemoryBytes = GC.GetTotalMemory(false),
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                }
            };
        }

        public async Task<int> CleanupExpiredSessionsAsync()
        {
            return await SafeCleanupExpiredSessions();
        }

        #region Private Helper Methods

        private ILogger CreateSessionLogger(string sessionId)
        {
            return m_serviceProvider.GetService<ILoggerFactory>()?.CreateLogger($"Session-{sessionId}")
                ?? m_logger;
        }

        private ICdbSession CreateCdbSession(ILogger sessionLogger, string sessionId)
        {
            // Create CdbSession with session-specific configuration
            // Cast logger to the correct generic type
            var typedLogger = sessionLogger as ILogger<CdbSession> ??
                m_serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<CdbSession>();

            return new CdbSession(
                typedLogger,
                m_cdbOptions.CommandTimeoutMs,
                m_cdbOptions.CustomCdbPath,
                m_cdbOptions.SymbolServerTimeoutMs,
                m_cdbOptions.SymbolServerMaxRetries,
                m_cdbOptions.SymbolSearchPath);
        }

        private ICommandQueueService CreateCommandQueue(ICdbSession cdbSession, ILogger sessionLogger, string sessionId)
        {
            // Create isolated command queue for this session
            return new IsolatedCommandQueueService(cdbSession, sessionLogger, m_notificationService, sessionId);
        }

        private string ConstructCdbTarget(string dumpPath, string? symbolsPath)
        {
            // Construct CDB target command line
            var target = $"-z \"{dumpPath}\"";

            if (!string.IsNullOrEmpty(symbolsPath))
            {
                target += $" -y \"{symbolsPath}\"";
            }

            return target;
        }

        private int? GetCdbProcessId(ICdbSession cdbSession)
        {
            // Try to get process ID from CdbSession if it exposes it
            return cdbSession.GetType().GetProperty("ProcessId")?.GetValue(cdbSession) as int?;
        }

        private List<string> GenerateUsageHints(SessionInfo session, List<(string Id, string Command, DateTime QueueTime, string Status)> queueStatus)
        {
            var hints = new List<string>();

            if (queueStatus.Any(q => q.Status == "Queued"))
            {
                hints.Add("🔄 Commands are queued - they execute sequentially");
            }

            if (session.LastActivity < DateTime.UtcNow.AddMinutes(-5))
            {
                hints.Add("⏰ Session has been inactive - will auto-expire if no activity");
            }

            hints.Add($"📊 Use 'nexus_read_dump_analyze_command_result' tool to check command results");
            hints.Add($"🎯 Always include sessionId='{session.SessionId}' in your requests");

            return hints;
        }

        private TimeSpan CalculateAverageSessionLifetime()
        {
            var totalSessions = Volatile.Read(ref m_totalSessionsClosed);
            if (totalSessions == 0) return TimeSpan.Zero;

            // Simple approximation - in real implementation, we'd track actual lifetimes
            return TimeSpan.FromMinutes(15); // Placeholder
        }

        private async Task<int> SafeCleanupExpiredSessions()
        {
            if (m_disposed) return 0;

            var cleanedCount = 0;
            var expiredSessions = new List<string>();

            // PHASE 1: Identify expired sessions (no locks)
            var cutoffTime = DateTime.UtcNow - m_config.SessionTimeout;

            foreach (var kvp in m_sessions)
            {
                var session = kvp.Value;
                if (session.LastActivity < cutoffTime || session.IsDisposed)
                {
                    expiredSessions.Add(kvp.Key);
                }
            }

            // PHASE 2: Clean up expired sessions
            foreach (var sessionId in expiredSessions)
            {
                try
                {
                    if (await CleanupExpiredSession(sessionId))
                    {
                        cleanedCount++;
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error cleaning up expired session {SessionId}", sessionId);
                }
            }

            if (cleanedCount > 0)
            {
                m_logger.LogInformation("🧹 Cleaned up {Count} expired sessions", cleanedCount);
            }

            return cleanedCount;
        }

        private async Task<bool> CleanupExpiredSession(string sessionId)
        {
            if (!m_sessions.TryRemove(sessionId, out var session))
                return false;

            m_logger.LogInformation("⏰ Cleaning up expired session {SessionId}", sessionId);

            try
            {
                await SafeCleanupSession(session);
                Interlocked.Increment(ref m_totalSessionsExpired);

                // Notification for expired session
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService.NotifySessionEventAsync(sessionId,
                            "SESSION_EXPIRED", "Session expired due to inactivity");
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send session expiry notification for {SessionId}", sessionId);
                    }
                }, CancellationToken.None);

                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error cleaning up expired session {SessionId}", sessionId);
                return false;
            }
        }

        private async Task SafeCleanupSession(SessionInfo session)
        {
            using var timeoutCts = new CancellationTokenSource(m_config.DisposalTimeout);

            try
            {
                // Dispose in correct order: CommandQueue first, then CdbSession
                await Task.Run(() =>
                {
                    try
                    {
                        session.CommandQueue?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Error disposing command queue for session {SessionId}", session.SessionId);
                    }
                }, timeoutCts.Token);

                await SafeDisposeCdbSession(session.CdbSession);

                session.Dispose();
            }
            catch (OperationCanceledException)
            {
                m_logger.LogWarning("Session cleanup timed out for {SessionId}", session.SessionId);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during session cleanup for {SessionId}", session.SessionId);
            }
        }

        private async Task SafeDisposeCdbSession(ICdbSession cdbSession)
        {
            try
            {
                if (cdbSession.IsActive)
                {
                    await cdbSession.StopSession();
                }
                cdbSession.Dispose();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error disposing CDB session");
            }
        }

        private async Task MonitorSessionHealthAsync()
        {
            m_logger.LogDebug("🔍 Session health monitor started");

            try
            {
                while (!m_shutdownCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), m_shutdownCts.Token);

                        var stats = GetStatistics();
                        m_logger.LogDebug("📊 Session Stats: Active={Active}, Total={Total}, Memory={MemoryMB}MB",
                            stats.ActiveSessions, stats.TotalSessionsCreated,
                            stats.MemoryUsage.WorkingSetBytes / 1024 / 1024);

                        // Check for memory pressure and cleanup if needed
                        if (stats.MemoryUsage.WorkingSetBytes > m_config.MemoryCleanupThresholdBytes)
                        {
                            m_logger.LogWarning("⚠️ High memory usage detected, forcing cleanup");
                            await SafeCleanupExpiredSessions();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Error in session health monitor");
                    }
                }
            }
            finally
            {
                m_logger.LogDebug("🔍 Session health monitor stopped");
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeSessionManager));
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (m_disposed) return;

            m_logger.LogInformation("🛑 ThreadSafeSessionManager disposing...");

            // MARK AS DISPOSED FIRST to prevent new operations
            m_disposed = true;

            try
            {
                // SHUTDOWN: Signal shutdown
                m_shutdownCts.Cancel();

                // CLEANUP: Stop timers
                m_cleanupTimer?.Dispose();

                // WAIT: Wait for monitoring task
                if (!m_monitoringTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    m_logger.LogWarning("⚠️ Session monitor did not stop within timeout");
                }

                // CLEANUP: Close all sessions using internal method (no disposal checks)
                var sessionIds = m_sessions.Keys.ToList();
                var cleanupTasks = sessionIds.Select(id => CloseSessionInternalAsync(id, CancellationToken.None));

                try
                {
                    Task.WaitAll(cleanupTasks.ToArray(), TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error during bulk session cleanup");
                }

                // CLEANUP: Dispose resources
                m_shutdownCts.Dispose();
                m_sessionCreationSemaphore.Dispose();

                m_logger.LogInformation("✅ ThreadSafeSessionManager disposed successfully");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Error disposing ThreadSafeSessionManager");
            }
        }

        #endregion
    }
}

