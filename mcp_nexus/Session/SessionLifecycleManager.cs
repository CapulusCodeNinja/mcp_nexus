using System.Collections.Concurrent;
using System.Diagnostics;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Session.Models;
using mcp_nexus.Notifications;
using mcp_nexus.Metrics;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Manages the lifecycle of debugging sessions including creation, closure, and cleanup.
    /// Provides thread-safe session management with proper resource cleanup and notification support.
    /// </summary>
    public class SessionLifecycleManager
    {
        private readonly ILogger m_Logger;
        private readonly IServiceProvider m_ServiceProvider;
        private readonly ILoggerFactory m_LoggerFactory;
        private readonly IMcpNotificationService m_NotificationService;
        private readonly SessionManagerConfiguration m_Config;
        private readonly ConcurrentDictionary<string, SessionInfo> m_Sessions;
        private readonly ConcurrentDictionary<string, SessionCommandResultCache> m_SessionCaches;
        private readonly AdvancedMetricsService? m_MetricsService;

        // Thread-safe counters
        private long m_TotalSessionsCreated = 0;
        private long m_TotalSessionsClosed = 0;
        private long m_TotalSessionsExpired = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLifecycleManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording lifecycle operations and errors.</param>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="loggerFactory">The logger factory for creating session-specific loggers.</param>
        /// <param name="notificationService">The notification service for sending session events.</param>
        /// <param name="config">The session manager configuration.</param>
        /// <param name="sessions">The thread-safe dictionary for storing session information.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public SessionLifecycleManager(
            ILogger logger,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IMcpNotificationService notificationService,
            SessionManagerConfiguration config,
            ConcurrentDictionary<string, SessionInfo> sessions)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            m_LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            m_NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_Sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            m_SessionCaches = new ConcurrentDictionary<string, SessionCommandResultCache>();

            // Try to get metrics service (may not be available in all configurations)
            try
            {
                m_MetricsService = m_ServiceProvider.GetService<AdvancedMetricsService>();
            }
            catch
            {
                m_MetricsService = null;
            }
        }

        /// <summary>
        /// Creates a new debugging session asynchronously.
        /// This method initializes all session components including CDB session, command queue, and logging.
        /// </summary>
        /// <param name="sessionId">The unique session identifier.</param>
        /// <param name="dumpPath">The path to the dump file.</param>
        /// <param name="symbolsPath">The optional symbols path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the created session info.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sessionId"/> or <paramref name="dumpPath"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when CDB session startup fails or command queue creation fails.</exception>
        public async Task<SessionInfo> CreateSessionAsync(string sessionId, string dumpPath, string? symbolsPath, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                m_Logger.LogInformation("🔧 Creating session {SessionId} for dump: {DumpPath}", sessionId, dumpPath);

                // Create session components
                var sessionLogger = CreateSessionLogger(sessionId);
                var cdbSession = CreateCdbSession(sessionLogger, sessionId);

                m_Logger.LogInformation("🔧 Creating command queue for session {SessionId}", sessionId);
                ICommandQueueService? commandQueue = null;
                try
                {
                    commandQueue = CreateCommandQueue(cdbSession, sessionLogger, sessionId);
                    m_Logger.LogInformation("✅ Command queue created successfully for session {SessionId}", sessionId);
                    m_Logger.LogInformation("🔍 Command queue object: {CommandQueueType}, IsNull: {IsNull}",
                        commandQueue?.GetType().Name ?? "null", commandQueue == null);
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "❌ Failed to create command queue for session {SessionId}", sessionId);
                    throw;
                }

                // Start CDB session asynchronously
                var cdbTarget = m_Config.ConstructCdbTarget(dumpPath, symbolsPath);
                var startSuccess = await cdbSession.StartSession(cdbTarget, null);

                if (!startSuccess)
                {
                    // Log detailed CDB detection failure information
                    m_Logger.LogError("❌ CDB session startup failed for {DumpPath}", dumpPath);
                    m_Logger.LogError("🔍 This typically means CDB.exe is not installed or not found in:");
                    m_Logger.LogError("   - PATH environment variable");
                    m_Logger.LogError("   - Standard Windows SDK locations");
                    m_Logger.LogError("   - Visual Studio installations");
                    m_Logger.LogError("💡 SOLUTION: Install Windows SDK with Debugging Tools from:");
                    m_Logger.LogError("   https://developer.microsoft.com/windows/downloads/windows-sdk/");

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
                    cdbSession.ProcessId
                );

                // Verify command queue was properly stored
                m_Logger.LogInformation("🔍 Before null check - commandQueue: {CommandQueueType}, sessionInfo.CommandQueue: {SessionCommandQueueType}",
                    commandQueue?.GetType().Name ?? "null", sessionInfo.CommandQueue?.GetType().Name ?? "null");

                if (sessionInfo.CommandQueue == null)
                {
                    m_Logger.LogError("❌ Command queue is null in session info for {SessionId}", sessionId);
                    throw new InvalidOperationException($"Command queue is null in session info for {sessionId}");
                }

                m_Logger.LogInformation("✅ Session info created with command queue for {SessionId}", sessionId);

                // Add to sessions dictionary first
                m_Sessions[sessionId] = sessionInfo;
                Interlocked.Increment(ref m_TotalSessionsCreated);

                // Wait for command queue to be ready before marking session as Active
                m_Logger.LogInformation("⏳ Waiting for command queue to be ready for session {SessionId}", sessionId);
                var maxWaitTime = TimeSpan.FromSeconds(5);
                var waitStart = DateTime.UtcNow;

                while (DateTime.UtcNow - waitStart < maxWaitTime)
                {
                    if (commandQueue is IsolatedCommandQueueService isolatedQueue && isolatedQueue.IsReady())
                    {
                        m_Logger.LogInformation("✅ Command queue is ready for session {SessionId}", sessionId);
                        break;
                    }

                    await Task.Delay(100); // Wait 100ms before checking again
                }

                // Session is ready for use now
                sessionInfo.Status = SessionStatus.Active;
                m_Logger.LogInformation("✅ Session {SessionId} created and marked as Active", sessionId);

                // NOTE: Extension loading issue - !analyze requires ext.dll to be loaded
                // However, auto-loading .load ext has historically caused output capture issues
                // Modern CDB should auto-load extensions, but some systems may not

                stopwatch.Stop();
                m_Logger.LogInformation("✅ Session {SessionId} created successfully in {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);

                // Send creation notification
                await m_NotificationService.NotifySessionEventAsync(sessionId, "created",
                    $"Session created for {Path.GetFileName(dumpPath)}", GetSessionContext(sessionInfo));

                return sessionInfo;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                m_Logger.LogError(ex, "❌ Failed to create session {SessionId} after {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Closes a debugging session asynchronously.
        /// This method cancels pending commands, cleans up resources, and removes the session from the dictionary.
        /// </summary>
        /// <param name="sessionId">The session ID to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the session was closed successfully; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            if (!m_Sessions.TryRemove(sessionId, out var sessionInfo))
            {
                m_Logger.LogWarning("Session {SessionId} not found for closure", sessionId);
                return false;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                m_Logger.LogInformation("🔒 Closing session {SessionId}", sessionId);

                // Cancel all pending commands if command queue exists
                if (sessionInfo?.CommandQueue != null)
                {
                    var cancelledCount = sessionInfo.CommandQueue.CancelAllCommands("Session closing");
                    if (cancelledCount > 0)
                    {
                        m_Logger.LogInformation("Cancelled {Count} pending commands for session {SessionId}",
                            cancelledCount, sessionId);
                    }
                }
                else
                {
                    m_Logger.LogTrace("No command queue to cancel for session {SessionId}", sessionId);
                }

                // Cleanup components (includes stopping CDB session)
                if (sessionInfo?.CdbSession != null && sessionInfo?.CommandQueue != null)
                {
                    await CleanupSessionComponents(sessionInfo.CdbSession, sessionInfo.CommandQueue);
                }

                // Cleanup session command result cache
                RemoveSessionCache(sessionId);

                Interlocked.Increment(ref m_TotalSessionsClosed);
                stopwatch.Stop();

                m_Logger.LogInformation("✅ Session {SessionId} closed successfully in {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);

                // Send closure notification
                if (sessionInfo != null)
                {
                    await m_NotificationService.NotifySessionEventAsync(sessionId, "closed",
                        "Session closed successfully", GetSessionContext(sessionInfo));
                }
                else
                {
                    m_Logger.LogTrace("Skipping notification for null session {SessionId}", sessionId);
                }

                // Clean up session-specific metrics
                m_MetricsService?.CleanupSessionMetrics(sessionId);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                m_Logger.LogError(ex, "❌ Error closing session {SessionId} after {Elapsed}ms",
                    sessionId, stopwatch.ElapsedMilliseconds);
                return false;
            }
        }

        /// <summary>
        /// Cleans up expired sessions asynchronously.
        /// This method identifies and closes sessions that have exceeded their idle timeout.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the number of sessions cleaned up.
        /// </returns>
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = new List<string>();
            var now = DateTime.UtcNow;

            // Find expired sessions
            foreach (var kvp in m_Sessions)
            {
                var sessionInfo = kvp.Value;
                if (m_Config.IsSessionExpired(sessionInfo.LastActivity))
                {
                    expiredSessions.Add(kvp.Key);
                }
            }

            if (expiredSessions.Count == 0)
                return 0;

            m_Logger.LogInformation("🧹 Cleaning up {Count} expired sessions", expiredSessions.Count);

            var cleanedCount = 0;
            foreach (var sessionId in expiredSessions)
            {
                try
                {
                    if (await CloseSessionAsync(sessionId))
                    {
                        cleanedCount++;
                        Interlocked.Increment(ref m_TotalSessionsExpired);

                        m_Logger.LogInformation("🗑️ Expired session {SessionId} cleaned up", sessionId);

                        // Send expiry notification
                        await m_NotificationService.NotifySessionEventAsync(sessionId, "expired",
                            "Session expired due to inactivity", string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error cleaning up expired session {SessionId}", sessionId);
                }
            }

            if (cleanedCount > 0)
            {
                m_Logger.LogInformation("✅ Cleaned up {CleanedCount}/{TotalCount} expired sessions",
                    cleanedCount, expiredSessions.Count);
            }

            return cleanedCount;
        }

        /// <summary>
        /// Gets lifecycle statistics.
        /// This method returns thread-safe counters for session creation, closure, and expiration.
        /// </summary>
        /// <returns>
        /// A tuple containing the number of sessions created, closed, and expired.
        /// </returns>
        public (long Created, long Closed, long Expired) GetLifecycleStats()
        {
            return (
                Interlocked.Read(ref m_TotalSessionsCreated),
                Interlocked.Read(ref m_TotalSessionsClosed),
                Interlocked.Read(ref m_TotalSessionsExpired)
            );
        }

        /// <summary>
        /// Creates a logger for a specific session.
        /// </summary>
        /// <param name="sessionId">The session ID to create a logger for.</param>
        /// <returns>A logger instance configured for the session.</returns>
        private ILogger CreateSessionLogger(string sessionId)
        {
            // Create a scoped logger with session context
            var loggerFactory = m_ServiceProvider.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger($"Session.{sessionId}");
        }

        /// <summary>
        /// Creates a CDB session for debugging.
        /// </summary>
        /// <param name="sessionLogger">The logger for the session.</param>
        /// <param name="sessionId">The session ID.</param>
        /// <returns>A configured CDB session instance.</returns>
        private ICdbSession CreateCdbSession(ILogger sessionLogger, string sessionId)
        {
            // Diagnostic logging to see what CDB path is actually being used
            m_Logger.LogInformation("🔧 Creating CDB session with path: {CdbPath}", m_Config.CdbOptions.CustomCdbPath ?? "NULL");

            // Use typed logger so logs from CdbSession appear properly
            var typedCdbLogger = m_LoggerFactory.CreateLogger<CdbSession>();
            return new CdbSession(
                typedCdbLogger,
                m_Config.CdbOptions.CommandTimeoutMs,
                m_Config.CdbOptions.IdleTimeoutMs,
                m_Config.CdbOptions.CustomCdbPath,
                m_Config.CdbOptions.SymbolServerTimeoutMs,
                m_Config.CdbOptions.SymbolServerMaxRetries,
                m_Config.CdbOptions.SymbolSearchPath
            );
        }

        /// <summary>
        /// Creates a command queue for the session.
        /// </summary>
        /// <param name="cdbSession">The CDB session instance.</param>
        /// <param name="sessionLogger">The logger for the session.</param>
        /// <param name="sessionId">The session ID.</param>
        /// <returns>A configured command queue service instance.</returns>
        private ICommandQueueService CreateCommandQueue(ICdbSession cdbSession, ILogger sessionLogger, string sessionId)
        {
            // Get or create the session cache
            var sessionCache = GetOrCreateSessionCache(sessionId);
            
            return new IsolatedCommandQueueService(
                cdbSession,
                m_LoggerFactory.CreateLogger<IsolatedCommandQueueService>(),
                m_NotificationService,
                sessionId,
                sessionCache
            );
        }


        /// <summary>
        /// Creates a session context for notifications.
        /// </summary>
        /// <param name="sessionInfo">The session information.</param>
        /// <returns>A session context for notifications.</returns>
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
                m_Logger.LogWarning(ex, "Error disposing command queue during cleanup");
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
                m_Logger.LogWarning(ex, "Error disposing CDB session during cleanup");
            }
        }

        /// <summary>
        /// Gets or creates a command result cache for the specified session
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        /// <returns>The session's command result cache</returns>
        public SessionCommandResultCache GetOrCreateSessionCache(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            return m_SessionCaches.GetOrAdd(sessionId, _ =>
            {
                m_Logger.LogDebug("📦 Creating command result cache for session {SessionId}", sessionId);
                return new SessionCommandResultCache(
                    maxMemoryBytes: 100 * 1024 * 1024, // 100MB default
                    maxResults: 1000,
                    memoryPressureThreshold: 0.8,
                    logger: null); // Use null logger to avoid type mismatch
            });
        }

        /// <summary>
        /// Gets the command result cache for the specified session if it exists
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        /// <returns>The session's command result cache, or null if not found</returns>
        public SessionCommandResultCache? GetSessionCache(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return null;

            return m_SessionCaches.TryGetValue(sessionId, out var cache) ? cache : null;
        }

        /// <summary>
        /// Removes and disposes the command result cache for the specified session
        /// </summary>
        /// <param name="sessionId">The session identifier</param>
        public void RemoveSessionCache(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return;

            if (m_SessionCaches.TryRemove(sessionId, out var cache))
            {
                try
                {
                    cache.Dispose();
                    m_Logger.LogDebug("🗑️ Disposed command result cache for session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Error disposing command result cache for session {SessionId}", sessionId);
                }
            }
        }
    }
}
