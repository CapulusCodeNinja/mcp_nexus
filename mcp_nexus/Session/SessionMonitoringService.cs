using System.Collections.Concurrent;
using mcp_nexus.Session.Models;
using mcp_nexus.Notifications;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Monitors session health, activity, and provides periodic cleanup
    /// </summary>
    public class SessionMonitoringService
    {
        private readonly ILogger m_logger;
        private readonly IMcpNotificationService m_notificationService;
        private readonly SessionManagerConfiguration m_config;
        private readonly ConcurrentDictionary<string, SessionInfo> m_sessions;
        private readonly SessionLifecycleManager m_lifecycleManager;

        // Monitoring state
        private readonly Timer m_cleanupTimer;
        private readonly CancellationTokenSource m_shutdownCts;
        private readonly Task m_monitoringTask;
        private volatile bool m_disposed = false;

        public SessionMonitoringService(
            ILogger logger,
            IMcpNotificationService notificationService,
            SessionManagerConfiguration config,
            ConcurrentDictionary<string, SessionInfo> sessions,
            SessionLifecycleManager lifecycleManager,
            CancellationTokenSource shutdownCts)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            m_lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
            m_shutdownCts = shutdownCts ?? throw new ArgumentNullException(nameof(shutdownCts));

            // Initialize cleanup timer
            m_cleanupTimer = new Timer(_ =>
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
                        m_logger.LogError(ex, "Error during scheduled session cleanup");
                    }
                }, CancellationToken.None);
            }, null, m_config.GetCleanupInterval(), m_config.GetCleanupInterval());

            // Start session health monitoring
            m_monitoringTask = Task.Run(MonitorSessionHealthAsync, m_shutdownCts.Token);

            m_logger.LogDebug("✅ Session monitoring service initialized");
        }

        /// <summary>
        /// Updates the last activity time for a session
        /// </summary>
        /// <param name="sessionId">Session ID to update</param>
        public void UpdateActivity(string sessionId)
        {
            if (m_disposed || string.IsNullOrWhiteSpace(sessionId))
                return;

            if (m_sessions.TryGetValue(sessionId, out var sessionInfo))
            {
                sessionInfo.LastActivity = DateTime.UtcNow;
                m_logger.LogTrace("📝 Updated activity for session {SessionId}", sessionId);
            }
            else
            {
                m_logger.LogWarning("Cannot update activity for unknown session {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Performs safe cleanup of expired sessions
        /// </summary>
        private async Task SafeCleanupExpiredSessions()
        {
            if (m_disposed)
                return;

            try
            {
                var cleanedCount = await m_lifecycleManager.CleanupExpiredSessionsAsync();
                if (cleanedCount > 0)
                {
                    m_logger.LogInformation("🧹 Periodic cleanup removed {Count} expired sessions", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during safe session cleanup");
            }
        }

        /// <summary>
        /// Monitors session health continuously
        /// </summary>
        private async Task MonitorSessionHealthAsync()
        {
            m_logger.LogInformation("🔍 Starting session health monitoring");

            try
            {
                while (!m_shutdownCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), m_shutdownCts.Token);

                    if (m_disposed)
                        break;

                    await PerformHealthCheck();
                }
            }
            catch (OperationCanceledException)
            {
                m_logger.LogInformation("🛑 Session health monitoring stopped due to cancellation");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Fatal error in session health monitoring");
            }
            finally
            {
                m_logger.LogInformation("🏁 Session health monitoring shutdown complete");
            }
        }

        /// <summary>
        /// Performs a comprehensive health check of all sessions
        /// </summary>
        private async Task PerformHealthCheck()
        {
            try
            {
                var sessionCount = m_sessions.Count;
                var activeCount = 0;
                var inactiveCount = 0;
                var unhealthyCount = 0;

                foreach (var kvp in m_sessions)
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
                            m_logger.LogWarning("⚠️ Session {SessionId} has inactive CDB session", kvp.Key);
                        }

                        // Check for sessions that might be stuck
                        var timeSinceActivity = DateTime.UtcNow - sessionInfo.LastActivity;
                        if (timeSinceActivity > TimeSpan.FromHours(1))
                        {
                            m_logger.LogWarning("⏰ Session {SessionId} has been inactive for {Duration}",
                                kvp.Key, timeSinceActivity);
                        }
                    }
                    catch (Exception ex)
                    {
                        unhealthyCount++;
                        m_logger.LogError(ex, "💥 Health check failed for session {SessionId}", kvp.Key);
                    }
                }

                // Log health summary
                m_logger.LogInformation("💊 Health check: {Total} sessions ({Active} active, {Inactive} inactive, {Unhealthy} unhealthy)",
                    sessionCount, activeCount, inactiveCount, unhealthyCount);

                // Send health notification
                await m_notificationService.NotifyServerHealthAsync(
                    status: unhealthyCount > 0 ? "Warning" : "Healthy",
                    cdbSessionActive: activeCount > 0,
                    queueSize: GetTotalQueueSize(),
                    activeCommands: GetTotalActiveCommands()
                );
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during health check");
            }
        }

        /// <summary>
        /// Gets the total queue size across all sessions
        /// </summary>
        private int GetTotalQueueSize()
        {
            try
            {
                return m_sessions.Values
                    .Select(s => s.CommandQueue.GetQueueStatus().Count())
                    .Sum();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error calculating total queue size");
                return 0;
            }
        }

        /// <summary>
        /// Gets the total number of active commands across all sessions
        /// </summary>
        private int GetTotalActiveCommands()
        {
            try
            {
                return m_sessions.Values
                    .Select(s => s.CommandQueue.GetCurrentCommand() != null ? 1 : 0)
                    .Sum();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error calculating total active commands");
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
                var sessionAge = DateTime.UtcNow - session.CreatedAt;
                var timeSinceActivity = DateTime.UtcNow - session.LastActivity;

                // Age-based hints
                if (sessionAge < TimeSpan.FromMinutes(5))
                {
                    hints.Add("💡 New session - try basic commands like 'k' (stack) or 'lm' (loaded modules)");
                }

                // Activity-based hints
                if (timeSinceActivity > TimeSpan.FromMinutes(30))
                {
                    hints.Add("⏰ Session has been idle - consider running '!analyze -v' for crash analysis");
                }

                // Queue-based hints
                if (queueStatus.Count == 0)
                {
                    hints.Add("🎯 Queue is empty - ready for new commands");
                }
                else if (queueStatus.Count > 5)
                {
                    hints.Add("📋 Queue is busy - consider waiting for current commands to complete");
                }

                // Session health hints
                if (!session.CdbSession.IsActive)
                {
                    hints.Add("⚠️ CDB session is not active - session may need to be recreated");
                }

                // File-based hints
                if (session.DumpPath.Contains("crash", StringComparison.OrdinalIgnoreCase))
                {
                    hints.Add("🔍 Crash dump detected - use '!analyze -v' for detailed crash analysis");
                }

                if (string.IsNullOrEmpty(session.SymbolsPath))
                {
                    hints.Add("🔣 No symbols path specified - consider setting symbol path for better analysis");
                }
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error generating usage hints for session {SessionId}", session.SessionId);
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
                var now = DateTime.UtcNow;
                var lifetimes = m_sessions.Values
                    .Select(s => now - s.CreatedAt)
                    .ToList();

                if (lifetimes.Count == 0)
                    return TimeSpan.Zero;

                var averageTicks = lifetimes.Select(t => t.Ticks).Average();
                return new TimeSpan((long)averageTicks);
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error calculating average session lifetime");
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Disposes the monitoring service
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            try
            {
                m_logger.LogInformation("🛑 Shutting down session monitoring service");

                // Stop cleanup timer
                m_cleanupTimer?.Dispose();

                // Wait for monitoring task to complete
                if (m_monitoringTask != null && !m_monitoringTask.IsCompleted)
                {
                    try
                    {
                        m_monitoringTask.Wait(TimeSpan.FromSeconds(5));
                    }
                    catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                    {
                        // Expected during shutdown
                        m_logger.LogDebug("Monitoring task cancelled during shutdown (expected)");
                    }
                }

                m_logger.LogInformation("✅ Session monitoring service shutdown complete");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "💥 Error during session monitoring service disposal");
            }
        }
    }
}
