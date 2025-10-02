using System.Collections.Concurrent;
using System.Diagnostics;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Session.Models;
using mcp_nexus.Notifications;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Manages the lifecycle of debugging sessions including creation, closure, and cleanup
    /// </summary>
    public class SessionLifecycleManager
    {
        private readonly ILogger m_logger;
        private readonly IServiceProvider m_serviceProvider;
        private readonly ILoggerFactory m_loggerFactory;
        private readonly IMcpNotificationService m_notificationService;
        private readonly SessionManagerConfiguration m_config;
        private readonly ConcurrentDictionary<string, SessionInfo> m_sessions;

        // Thread-safe counters
        private long m_totalSessionsCreated = 0;
        private long m_totalSessionsClosed = 0;
        private long m_totalSessionsExpired = 0;

        public SessionLifecycleManager(
            ILogger logger,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IMcpNotificationService notificationService,
            SessionManagerConfiguration config,
            ConcurrentDictionary<string, SessionInfo> sessions)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            m_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        }

        /// <summary>
        /// Creates a new debugging session
        /// </summary>
        /// <param name="sessionId">Unique session identifier</param>
        /// <param name="dumpPath">Path to the dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created session info</returns>
        public async Task<SessionInfo> CreateSessionAsync(string sessionId, string dumpPath, string? symbolsPath, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                m_logger.LogInformation("🔧 Creating session {SessionId} for dump: {DumpPath}", sessionId, dumpPath);

                // Create session components
                var sessionLogger = CreateSessionLogger(sessionId);
                var cdbSession = CreateCdbSession(sessionLogger, sessionId);

                m_logger.LogInformation("🔧 Creating command queue for session {SessionId}", sessionId);
                ICommandQueueService? commandQueue = null;
                try
                {
                    commandQueue = CreateCommandQueue(cdbSession, sessionLogger, sessionId);
                    m_logger.LogInformation("✅ Command queue created successfully for session {SessionId}", sessionId);
                    m_logger.LogInformation("🔍 Command queue object: {CommandQueueType}, IsNull: {IsNull}",
                        commandQueue?.GetType().Name ?? "null", commandQueue == null);
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "❌ Failed to create command queue for session {SessionId}", sessionId);
                    throw;
                }

                // Start CDB session asynchronously
                var cdbTarget = m_config.ConstructCdbTarget(dumpPath, symbolsPath);
                var startSuccess = await cdbSession.StartSession(cdbTarget, null);

                if (!startSuccess)
                {
                    // Log detailed CDB detection failure information
                    m_logger.LogError("❌ CDB session startup failed for {DumpPath}", dumpPath);
                    m_logger.LogError("🔍 This typically means CDB.exe is not installed or not found in:");
                    m_logger.LogError("   - PATH environment variable");
                    m_logger.LogError("   - Standard Windows SDK locations");
                    m_logger.LogError("   - Visual Studio installations");
                    m_logger.LogError("💡 SOLUTION: Install Windows SDK with Debugging Tools from:");
                    m_logger.LogError("   https://developer.microsoft.com/windows/downloads/windows-sdk/");

                    // Cleanup on failure
                    if (cdbSession != null && commandQueue != null)
                    {
                        await CleanupSessionComponents(cdbSession, commandQueue);
                    }
                    throw new InvalidOperationException($"Failed to start CDB session for {dumpPath}. CDB.exe not found - please install Windows SDK with Debugging Tools.");
                }

                // Create session info using constructor (CommandQueue is read-only)
                if (commandQueue == null)
                {
                    throw new InvalidOperationException($"Command queue is null for session {sessionId}");
                }

                var sessionInfo = new SessionInfo(
                    sessionId,
                    cdbSession,
                    commandQueue,
                    dumpPath,
                    symbolsPath,
                    GetCdbProcessId(cdbSession)
                );

                // Verify command queue was properly stored
                m_logger.LogInformation("🔍 Before null check - commandQueue: {CommandQueueType}, sessionInfo.CommandQueue: {SessionCommandQueueType}",
                    commandQueue?.GetType().Name ?? "null", sessionInfo.CommandQueue?.GetType().Name ?? "null");

                if (sessionInfo.CommandQueue == null)
                {
                    m_logger.LogError("❌ Command queue is null in session info for {SessionId}", sessionId);
                    throw new InvalidOperationException($"Command queue is null in session info for {sessionId}");
                }

                m_logger.LogInformation("✅ Session info created with command queue for {SessionId}", sessionId);

                // Add to sessions dictionary first
                m_sessions[sessionId] = sessionInfo;
                Interlocked.Increment(ref m_totalSessionsCreated);

                // Wait for command queue to be ready before marking session as Active
                m_logger.LogInformation("⏳ Waiting for command queue to be ready for session {SessionId}", sessionId);
                var maxWaitTime = TimeSpan.FromSeconds(5);
                var waitStart = DateTime.UtcNow;

                while (DateTime.UtcNow - waitStart < maxWaitTime)
                {
                    if (commandQueue is IsolatedCommandQueueService isolatedQueue && isolatedQueue.IsReady())
                    {
                        m_logger.LogInformation("✅ Command queue is ready for session {SessionId}", sessionId);
                        break;
                    }

                    await Task.Delay(100); // Wait 100ms before checking again
                }

                // Session is ready for use now
                sessionInfo.Status = SessionStatus.Active;
                m_logger.LogInformation("✅ Session {SessionId} created and marked as Active", sessionId);

                // NOTE: Extension loading issue - !analyze requires ext.dll to be loaded
                // However, auto-loading .load ext has historically caused output capture issues
                // Modern CDB should auto-load extensions, but some systems may not
                // TODO: Investigate alternative solutions (CDB startup flags, pre-warm extension loading, etc.)

                stopwatch.Stop();
                m_logger.LogInformation("✅ Session {SessionId} created successfully in {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);

                // Send creation notification
                await m_notificationService.NotifySessionEventAsync(sessionId, "created",
                    $"Session created for {Path.GetFileName(dumpPath)}", GetSessionContext(sessionInfo));

                return sessionInfo;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                m_logger.LogError(ex, "❌ Failed to create session {SessionId} after {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Closes a debugging session
        /// </summary>
        /// <param name="sessionId">Session ID to close</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the session was closed successfully</returns>
        public async Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            if (!m_sessions.TryRemove(sessionId, out var sessionInfo))
            {
                m_logger.LogWarning("Session {SessionId} not found for closure", sessionId);
                return false;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                m_logger.LogInformation("🔒 Closing session {SessionId}", sessionId);

                // Cancel all pending commands if command queue exists
                if (sessionInfo?.CommandQueue != null)
                {
                    var cancelledCount = sessionInfo.CommandQueue.CancelAllCommands("Session closing");
                    if (cancelledCount > 0)
                    {
                        m_logger.LogInformation("Cancelled {Count} pending commands for session {SessionId}",
                            cancelledCount, sessionId);
                    }
                }
                else
                {
                    m_logger.LogTrace("No command queue to cancel for session {SessionId}", sessionId);
                }

                // Cleanup components (includes stopping CDB session)
                if (sessionInfo?.CdbSession != null && sessionInfo?.CommandQueue != null)
                {
                    await CleanupSessionComponents(sessionInfo.CdbSession, sessionInfo.CommandQueue);
                }

                Interlocked.Increment(ref m_totalSessionsClosed);
                stopwatch.Stop();

                m_logger.LogInformation("✅ Session {SessionId} closed successfully in {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);

                // Send closure notification
                if (sessionInfo != null)
                {
                    await m_notificationService.NotifySessionEventAsync(sessionId, "closed",
                        "Session closed successfully", GetSessionContext(sessionInfo));
                }
                else
                {
                    m_logger.LogTrace("Skipping notification for null session {SessionId}", sessionId);
                }

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                m_logger.LogError(ex, "❌ Error closing session {SessionId} after {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);
                return false;
            }
        }

        /// <summary>
        /// Cleans up expired sessions
        /// </summary>
        /// <returns>Number of sessions cleaned up</returns>
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = new List<string>();
            var now = DateTime.UtcNow;

            // Find expired sessions
            foreach (var kvp in m_sessions)
            {
                var sessionInfo = kvp.Value;
                if (m_config.IsSessionExpired(sessionInfo.LastActivity))
                {
                    expiredSessions.Add(kvp.Key);
                }
            }

            if (expiredSessions.Count == 0)
                return 0;

            m_logger.LogInformation("🧹 Cleaning up {Count} expired sessions", expiredSessions.Count);

            var cleanedCount = 0;
            foreach (var sessionId in expiredSessions)
            {
                try
                {
                    if (await CloseSessionAsync(sessionId))
                    {
                        cleanedCount++;
                        Interlocked.Increment(ref m_totalSessionsExpired);

                        m_logger.LogInformation("🗑️ Expired session {SessionId} cleaned up", sessionId);

                        // Send expiry notification
                        await m_notificationService.NotifySessionEventAsync(sessionId, "expired",
                            "Session expired due to inactivity", string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error cleaning up expired session {SessionId}", sessionId);
                }
            }

            if (cleanedCount > 0)
            {
                m_logger.LogInformation("✅ Cleaned up {CleanedCount}/{TotalCount} expired sessions",
                    cleanedCount, expiredSessions.Count);
            }

            return cleanedCount;
        }

        /// <summary>
        /// Gets lifecycle statistics
        /// </summary>
        /// <returns>Lifecycle statistics</returns>
        public (long Created, long Closed, long Expired) GetLifecycleStats()
        {
            return (
                Interlocked.Read(ref m_totalSessionsCreated),
                Interlocked.Read(ref m_totalSessionsClosed),
                Interlocked.Read(ref m_totalSessionsExpired)
            );
        }

        /// <summary>
        /// Creates a logger for a specific session
        /// </summary>
        private ILogger CreateSessionLogger(string sessionId)
        {
            // Create a scoped logger with session context
            var loggerFactory = m_serviceProvider.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"Session.{sessionId}");
        }

        /// <summary>
        /// Creates a CDB session for debugging
        /// </summary>
        private ICdbSession CreateCdbSession(ILogger sessionLogger, string sessionId)
        {
            // Diagnostic logging to see what CDB path is actually being used
            m_logger.LogInformation("🔧 Creating CDB session with path: {CdbPath}", m_config.CdbOptions.CustomCdbPath ?? "NULL");

            // Use typed logger so logs from CdbSession appear properly
            var typedCdbLogger = m_loggerFactory.CreateLogger<CdbSession>();
            return new CdbSession(
                typedCdbLogger,
                m_config.CdbOptions.CommandTimeoutMs,
                m_config.CdbOptions.IdleTimeoutMs,
                m_config.CdbOptions.CustomCdbPath,
                m_config.CdbOptions.SymbolServerTimeoutMs,
                m_config.CdbOptions.SymbolServerMaxRetries,
                m_config.CdbOptions.SymbolSearchPath
            );
        }

        /// <summary>
        /// Creates a command queue for the session
        /// </summary>
        private ICommandQueueService CreateCommandQueue(ICdbSession cdbSession, ILogger sessionLogger, string sessionId)
        {
            return new IsolatedCommandQueueService(
                cdbSession,
                m_loggerFactory.CreateLogger<IsolatedCommandQueueService>(),
                m_notificationService,
                sessionId
            );
        }

        /// <summary>
        /// Gets the process ID of a CDB session
        /// </summary>
        private int? GetCdbProcessId(ICdbSession cdbSession)
        {
            try
            {
                // This would need to be implemented based on the CDB session's process tracking
                // For now, return null as a placeholder
                return null;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Could not retrieve CDB process ID");
                return null;
            }
        }

        /// <summary>
        /// Creates a session context for notifications
        /// </summary>
        private SessionContext GetSessionContext(SessionInfo sessionInfo)
        {
            if (sessionInfo == null)
            {
                return new SessionContext
                {
                    SessionId = "unknown",
                    Description = "Session context unavailable",
                    Status = "Unknown"
                };
            }

            return new SessionContext
            {
                SessionId = sessionInfo.SessionId ?? "unknown",
                DumpPath = sessionInfo.DumpPath,
                CreatedAt = sessionInfo.CreatedAt,
                LastActivity = sessionInfo.LastActivity,
                Status = sessionInfo.Status.ToString(),
                Description = $"Session for {Path.GetFileName(sessionInfo.DumpPath ?? "Unknown")}"
            };
        }

        /// <summary>
        /// Cleans up session components safely
        /// </summary>
        private async Task CleanupSessionComponents(ICdbSession cdbSession, ICommandQueueService commandQueue)
        {
            try
            {
                // Dispose command queue first
                commandQueue?.Dispose();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error disposing command queue during cleanup");
            }

            try
            {
                // Stop and dispose CDB session
                if (cdbSession.IsActive)
                {
                    await cdbSession.StopSession();
                }
                cdbSession?.Dispose();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error disposing CDB session during cleanup");
            }
        }
    }
}
