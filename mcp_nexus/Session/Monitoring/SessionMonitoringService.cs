using System.Collections.Concurrent;
using mcp_nexus.Session.Core.Models;
using mcp_nexus.Notifications;
using mcp_nexus.Session.Core;
using mcp_nexus.Session.Lifecycle;

namespace mcp_nexus.Session.Monitoring
{
    /// <summary>
    /// Monitors session health, activity, and provides periodic cleanup
    /// </summary>
    public class SessionMonitoringService
    {
        private readonly ILogger m_Logger;
        private readonly IMcpNotificationService m_NotificationService;
        private readonly SessionManagerConfiguration m_Config;
        private readonly ConcurrentDictionary<string, SessionInfo> m_Sessions;
        private readonly SessionLifecycleManager m_LifecycleManager;

        // Monitoring state
        private readonly Timer m_CleanupTimer;
        private readonly CancellationTokenSource m_ShutdownCts;
        private readonly Task m_MonitoringTask;
        private volatile bool m_Disposed = false;
        private DateTime m_LastHealthLogTime = DateTime.Now;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionMonitoringService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording monitoring operations and errors.</param>
        /// <param name="notificationService">The notification service for publishing monitoring events.</param>
        /// <param name="config">The session manager configuration settings.</param>
        /// <param name="sessions">The concurrent dictionary containing active sessions.</param>
        /// <param name="lifecycleManager">The session lifecycle manager for managing session operations.</param>
        /// <param name="shutdownCts">The cancellation token source for shutdown operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public SessionMonitoringService(
            ILogger logger,
            IMcpNotificationService notificationService,
            SessionManagerConfiguration config,
            ConcurrentDictionary<string, SessionInfo> sessions,
            SessionLifecycleManager lifecycleManager,
            CancellationTokenSource shutdownCts)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            m_Config = config ?? throw new ArgumentNullException(nameof(config));
            m_Sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            m_LifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
            m_ShutdownCts = shutdownCts ?? throw new ArgumentNullException(nameof(shutdownCts));

            // Initialize cleanup timer
            m_CleanupTimer = new Timer(_ =>
            {
                // Fire-and-forget with proper exception handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SafeCleanupExpiredSessions();
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogError(ex, "Error during scheduled session cleanup");
                    }
                }, CancellationToken.None);
            }, null, m_Config.GetCleanupInterval(), m_Config.GetCleanupInterval());

            // Start session health monitoring
            m_MonitoringTask = Task.Run(MonitorSessionHealthAsync, m_ShutdownCts.Token);

            m_Logger.LogDebug("✅ Session monitoring service initialized");
        }

        /// <summary>
        /// Updates the last activity time for a session
        /// </summary>
        /// <param name="sessionId">Session ID to update</param>
        public void UpdateActivity(string sessionId)
        {
            if (m_Disposed || string.IsNullOrWhiteSpace(sessionId))
                return;

            if (m_Sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                sessionInfo.LastActivity = DateTime.Now;
                m_Logger.LogTrace("📝 Updated activity for session {SessionId}", sessionId);
            }
            else
            {
                m_Logger.LogWarning("Cannot update activity for unknown session {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Performs safe cleanup of expired sessions
        /// </summary>
        private async Task SafeCleanupExpiredSessions()
        {
            if (m_Disposed)
                return;

            try
            {
                var cleanedCount = await m_LifecycleManager.CleanupExpiredSessionsAsync();
                if (cleanedCount > 0)
                {
                    m_Logger.LogInformation("🧹 Periodic cleanup removed {Count} expired sessions", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during safe session cleanup");
            }
        }

        /// <summary>
        /// Monitors session health continuously
        /// </summary>
        private async Task MonitorSessionHealthAsync()
        {

            try
            {
                while (!m_ShutdownCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), m_ShutdownCts.Token);

                    if (m_Disposed)
                        break;

                    await PerformHealthCheck();
                }
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogInformation("🛑 Session health monitoring stopped due to cancellation");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Fatal error in session health monitoring");
            }
            finally
            {
                m_Logger.LogInformation("🏁 Session health monitoring shutdown complete");
            }
        }

        /// <summary>
        /// Performs a comprehensive health check of all sessions
        /// </summary>
        private async Task PerformHealthCheck()
        {
            try
            {
                var sessionCount = m_Sessions.Count;
                var activeCount = 0;
                var inactiveCount = 0;
                var unhealthyCount = 0;

                foreach (var kvp in m_Sessions)
                {
                    var sessionInfo = kvp.Value;

                    try
                    {
                        if (sessionInfo.CdbSession.IsActive)
                        {
                            activeCount++;
                        }
                        else
                        {
                            inactiveCount++;
                            m_Logger.LogWarning("⚠️ Session {SessionId} has inactive CDB session", kvp.Key);
                        }

                        // Check for sessions that might be stuck
                        var timeSinceActivity = DateTime.Now - sessionInfo.LastActivity;
                        if (timeSinceActivity > TimeSpan.FromHours(1))
                        {
                            m_Logger.LogWarning("⏰ Session {SessionId} has been inactive for {Duration}",
                                kvp.Key, timeSinceActivity);
                        }
                    }
                    catch (Exception ex)
                    {
                        unhealthyCount++;
                        m_Logger.LogError(ex, "💥 Health check failed for session {SessionId}", kvp.Key);
                    }
                }

                // Determine if we should log this health check
                bool needsLog = false;
                if (unhealthyCount > 0 || inactiveCount > 0)
                {
                    needsLog = true;
                }
                else if ((DateTime.Now - m_LastHealthLogTime) > TimeSpan.FromMinutes(15))
                {
                    needsLog = true;
                }

                // Log health summary based on session count and need
                if (needsLog && sessionCount > 0)
                {
                    m_Logger.LogInformation("💊 Health check: {Total} sessions ({Active} active, {Inactive} inactive, {Unhealthy} unhealthy)",
                        sessionCount, activeCount, inactiveCount, unhealthyCount);
                    m_LastHealthLogTime = DateTime.Now;
                }
                else if (needsLog && sessionCount <= 0)
                {
                    m_Logger.LogInformation("💊 Health check: Server idle (0 sessions)");
                    m_LastHealthLogTime = DateTime.Now;
                }

                // Send health notification (automatically skipped in HTTP mode by notification service)
                await m_NotificationService.NotifyServerHealthAsync(
                    status: unhealthyCount > 0 ? "Warning" : "Healthy",
                    cdbSessionActive: activeCount > 0,
                    queueSize: GetTotalQueueSize(),
                    activeCommands: GetTotalActiveCommands()
                );
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error during health check");
            }
        }

        /// <summary>
        /// Gets the total queue size across all sessions.
        /// </summary>
        /// <returns>The total number of queued commands across all sessions.</returns>
        private int GetTotalQueueSize()
        {
            try
            {
                var total = 0;
                foreach (var s in m_Sessions.Values)
                {
                    var qs = s.CommandQueue.GetQueueStatus();
                    // Avoid materialization: iterate and count
                    var count = 0;
                    foreach (var _ in qs) { count++; }
                    total += count;
                }
                return total;
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error calculating total queue size");
                return 0;
            }
        }

        /// <summary>
        /// Gets the total number of active commands across all sessions.
        /// </summary>
        /// <returns>The total number of currently executing commands.</returns>
        private int GetTotalActiveCommands()
        {
            try
            {
                var total = 0;
                foreach (var s in m_Sessions.Values)
                {
                    if (s.CommandQueue.GetCurrentCommand() != null)
                    {
                        total += 1;
                    }
                }
                return total;
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error calculating total active commands");
                return 0;
            }
        }

        /// <summary>
        /// Generates usage hints for a session
        /// </summary>
        /// <param name="session">Session information</param>
        /// <param name="queueStatus">Current queue status</param>
        /// <returns>List of usage hints</returns>
        public List<string> GenerateUsageHints(SessionInfo session, List<(string Id, string Command, DateTime QueueTime, string Status)> queueStatus)
        {
            var hints = new List<string>();

            try
            {
                var sessionAge = DateTime.Now - session.CreatedAt;
                var timeSinceActivity = DateTime.Now - session.LastActivity;

                // Age-based hints
                if (sessionAge < TimeSpan.FromMinutes(5))
                {
                    hints.Add("💡 New session - try basic commands like 'k' (stack) or 'lm' (loaded modules)");
                }

                // Activity-based hints
                if (timeSinceActivity > TimeSpan.FromMinutes(30))
                {
                    hints.Add("idle");
                }

                // Queue-based hints
                if (queueStatus.Count == 0)
                {
                    hints.Add("Queue is empty");
                }
                else if (queueStatus.Count > 5)
                {
                    hints.Add("Queue is busy");
                }

                // Session health hints
                if (session.CdbSession != null && !session.CdbSession.IsActive)
                {
                    hints.Add("⚠️ CDB session is not active - session may need to be recreated");
                }
                else if (session.CdbSession == null)
                {
                    hints.Add("⚠️ CDB session is not initialized - session may need to be recreated");
                }

                // File-based hints
                if (session.DumpPath.Contains("crash", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add("crash");
                }

                if (string.IsNullOrEmpty(session.SymbolsPath))
                {
                    hints.Add("🔣 No symbols path specified - consider setting symbol path for better analysis");
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error generating usage hints for session {SessionId}", session.SessionId);
                hints.Add("💡 Use standard WinDbg commands for debugging");
            }

            return hints;
        }

        /// <summary>
        /// Calculates the average session lifetime
        /// </summary>
        /// <returns>Average session lifetime</returns>
        public TimeSpan CalculateAverageSessionLifetime()
        {
            try
            {
                var now = DateTime.Now;
                long totalTicks = 0;
                int n = 0;
                foreach (var s in m_Sessions.Values)
                {
                    totalTicks += (now - s.CreatedAt).Ticks;
                    n++;
                }
                if (n == 0) return TimeSpan.Zero;
                return new TimeSpan(totalTicks / n);
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error calculating average session lifetime");
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Disposes the monitoring service.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            try
            {
                m_Logger.LogInformation("🛑 Shutting down session monitoring service");

                // Stop cleanup timer
                m_CleanupTimer?.Dispose();

                // Wait for monitoring task to complete
                if (m_MonitoringTask != null && !m_MonitoringTask.IsCompleted)
                {
                    try
                    {
                        m_MonitoringTask.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                    {
                        // Expected during shutdown
                        m_Logger.LogDebug("Monitoring task cancelled during shutdown (expected)");
                    }
                }

                m_Logger.LogInformation("✅ Session monitoring service shutdown complete");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "💥 Error during session monitoring service disposal");
            }
        }
    }
}
