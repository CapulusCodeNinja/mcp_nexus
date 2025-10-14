using System.Collections.Concurrent;
using mcp_nexus.Session.Models;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Collects and provides statistics about session management
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SessionStatisticsCollector"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance for recording statistics operations and errors.</param>
    /// <param name="sessions">The concurrent dictionary containing active sessions.</param>
    /// <param name="lifecycleManager">The session lifecycle manager for managing session operations.</param>
    /// <param name="monitoringService">The session monitoring service for tracking session health.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
    public class SessionStatisticsCollector(
        ILogger logger,
        ConcurrentDictionary<string, SessionInfo> sessions,
        SessionLifecycleManager lifecycleManager,
        SessionMonitoringService monitoringService)
    {
        private readonly ILogger m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentDictionary<string, SessionInfo> m_sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        private readonly SessionLifecycleManager m_lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        private readonly SessionMonitoringService m_monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        private readonly DateTime m_startTime = DateTime.UtcNow;

        // Performance counters
        private long m_totalCommandsProcessed = 0;
        private int m_peakConcurrentSessions = 0;

        /// <summary>
        /// Gets comprehensive session statistics
        /// </summary>
        /// <returns>Current session statistics</returns>
        public SessionStatistics GetStatistics()
        {
            try
            {
                var activeSessionCount = m_sessions.Count;
                var (Created, Closed, Expired) = m_lifecycleManager.GetLifecycleStats();
                var uptime = DateTime.UtcNow - m_startTime;

                // Update peak concurrent sessions
                if (activeSessionCount > m_peakConcurrentSessions)
                {
                    Interlocked.Exchange(ref m_peakConcurrentSessions, activeSessionCount);
                }

                // Calculate total commands processed
                var totalCommands = CalculateTotalCommandsProcessed();
                Interlocked.Exchange(ref m_totalCommandsProcessed, totalCommands);

                return new SessionStatistics
                {
                    TotalSessionsCreated = Created,
                    TotalSessionsClosed = Closed,
                    TotalSessionsExpired = Expired,
                    ActiveSessions = activeSessionCount,
                    TotalCommandsProcessed = totalCommands,
                    AverageSessionLifetime = m_monitoringService.CalculateAverageSessionLifetime(),
                    Uptime = uptime,
                    MemoryUsage = new MemoryUsageInfo
                    {
                        WorkingSetBytes = GetMemoryUsage(),
                        PrivateMemoryBytes = GetPrivateMemoryUsage(),
                        GCTotalMemoryBytes = GC.GetTotalMemory(false),
                        Gen0Collections = GC.CollectionCount(0),
                        Gen1Collections = GC.CollectionCount(1),
                        Gen2Collections = GC.CollectionCount(2)
                    }
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error calculating session statistics");
                return new SessionStatistics
                {
                    Uptime = DateTime.UtcNow - m_startTime
                };
            }
        }

        /// <summary>
        /// Gets all active sessions as contexts
        /// </summary>
        /// <returns>Collection of active session contexts</returns>
        public IEnumerable<SessionContext> GetActiveSessions()
        {
            try
            {
                var activeSessions = new List<SessionContext>();

                foreach (var kvp in m_sessions)
                {
                    var sessionInfo = kvp.Value;

                    try
                    {
                        if (sessionInfo.Status == SessionStatus.Active && !sessionInfo.IsDisposed)
                        {
                            var context = CreateSessionContext(sessionInfo);
                            activeSessions.Add(context);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Error creating context for session {SessionId}", kvp.Key);
                    }
                }

                return activeSessions;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error getting active sessions");
                return [];
            }
        }

        /// <summary>
        /// Gets all sessions (active and inactive)
        /// </summary>
        /// <returns>Collection of all session info objects</returns>
        public IEnumerable<SessionInfo> GetAllSessions()
        {
            try
            {
                return m_sessions.Values.ToList(); // Materialize to avoid concurrent modification issues
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error getting all sessions");
                return [];
            }
        }

        /// <summary>
        /// Creates a detailed session context
        /// </summary>
        /// <param name="sessionInfo">Session information</param>
        /// <returns>Session context with detailed information</returns>
        private SessionContext CreateSessionContext(SessionInfo sessionInfo)
        {
            try
            {
                // Get queue status safely
                var queueStatus = GetQueueStatusSafely(sessionInfo);
                var timeUntilExpiry = CalculateTimeUntilExpiry(sessionInfo);

                return new SessionContext
                {
                    SessionId = sessionInfo.SessionId,
                    Description = $"Debugging session for {Path.GetFileName(sessionInfo.DumpPath ?? "Unknown")}",
                    DumpPath = sessionInfo.DumpPath,
                    CreatedAt = sessionInfo.CreatedAt,
                    LastActivity = sessionInfo.LastActivity,
                    Status = sessionInfo.Status.ToString(),
                    CommandsProcessed = queueStatus.Count(q => q.Status == "Completed"),
                    ActiveCommands = queueStatus.Count(q => q.Status is "Queued" or "Executing"),
                    TimeUntilExpiry = timeUntilExpiry,
                    UsageHints = m_monitoringService.GenerateUsageHints(sessionInfo, queueStatus)
                };
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error creating detailed context for session {SessionId}", sessionInfo.SessionId);

                // Return minimal context on error
                return new SessionContext
                {
                    SessionId = sessionInfo.SessionId,
                    Description = "Session context unavailable",
                    Status = sessionInfo.Status.ToString(),
                    CreatedAt = sessionInfo.CreatedAt,
                    LastActivity = sessionInfo.LastActivity
                };
            }
        }

        /// <summary>
        /// Gets queue status safely, handling disposal and errors
        /// </summary>
        /// <param name="sessionInfo">Session information</param>
        /// <returns>Queue status list</returns>
        private List<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatusSafely(SessionInfo sessionInfo)
        {
            try
            {
                return sessionInfo.CommandQueue?.GetQueueStatus()?.ToList() ??
                       [];
            }
            catch (ObjectDisposedException)
            {
                m_logger.LogTrace("Command queue disposed for session {SessionId}", sessionInfo.SessionId);
                return [];
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error getting queue status for session {SessionId}", sessionInfo.SessionId);
                return [];
            }
        }

        /// <summary>
        /// Calculates time until session expiry
        /// </summary>
        /// <param name="sessionInfo">Session information</param>
        /// <returns>Time until expiry, or null if already expired</returns>
        private TimeSpan? CalculateTimeUntilExpiry(SessionInfo sessionInfo)
        {
            try
            {
                // This would need access to session configuration for timeout
                // For now, use a default 30-minute timeout
                var sessionTimeout = TimeSpan.FromMinutes(30);
                var timeUntilExpiry = sessionTimeout - (DateTime.UtcNow - sessionInfo.LastActivity);
                return timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : null;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error calculating expiry time for session {SessionId}", sessionInfo.SessionId);
                return null;
            }
        }

        /// <summary>
        /// Calculates total commands processed across all sessions
        /// </summary>
        /// <returns>Total command count</returns>
        private long CalculateTotalCommandsProcessed()
        {
            try
            {
                long total = 0;

                foreach (var sessionInfo in m_sessions.Values)
                {
                    try
                    {
                        var queueStatus = GetQueueStatusSafely(sessionInfo);
                        total += queueStatus.Count(q => q.Status == "Completed");
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogTrace(ex, "Error counting commands for session {SessionId}", sessionInfo.SessionId);
                    }
                }

                return total;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error calculating total commands processed");
                return 0;
            }
        }

        /// <summary>
        /// Gets current memory usage
        /// </summary>
        /// <returns>Memory usage in bytes</returns>
        private long GetMemoryUsage()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                return process.WorkingSet64;
            }
            catch (Exception ex)
            {
                m_logger.LogTrace(ex, "Error getting memory usage");
                return 0;
            }
        }

        /// <summary>
        /// Gets current private memory usage
        /// </summary>
        /// <returns>Private memory usage in bytes</returns>
        private long GetPrivateMemoryUsage()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                return process.PrivateMemorySize64;
            }
            catch (Exception ex)
            {
                m_logger.LogTrace(ex, "Error getting private memory usage");
                return 0;
            }
        }

        /// <summary>
        /// Logs periodic statistics summary
        /// </summary>
        public void LogStatisticsSummary()
        {
            try
            {
                var stats = GetStatistics();

                m_logger.LogInformation("📊 Session Statistics Summary:");
                m_logger.LogInformation("   Active Sessions: {Active}", stats.ActiveSessions);
                m_logger.LogInformation("   Total Created: {Created}, Closed: {Closed}, Expired: {Expired}",
                    stats.TotalSessionsCreated, stats.TotalSessionsClosed, stats.TotalSessionsExpired);
                m_logger.LogInformation("   Commands Processed: {Commands}", stats.TotalCommandsProcessed);
                m_logger.LogInformation("   Average Lifetime: {Lifetime:hh\\:mm\\:ss}", stats.AverageSessionLifetime);
                m_logger.LogInformation("   Uptime: {Uptime:dd\\.hh\\:mm\\:ss}", stats.Uptime);
                m_logger.LogInformation("   Memory Usage: {Memory:N0} bytes ({MemoryMB:F1} MB)",
                    stats.MemoryUsage.WorkingSetBytes, stats.MemoryUsage.WorkingSetBytes / (1024.0 * 1024.0));
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error logging statistics summary");
            }
        }
    }
}
