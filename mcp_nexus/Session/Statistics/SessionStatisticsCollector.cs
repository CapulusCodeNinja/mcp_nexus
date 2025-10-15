using System.Collections.Concurrent;
using mcp_nexus.Session.Core.Models;
using mcp_nexus.Session.Lifecycle;
using mcp_nexus.Session.Monitoring;

namespace mcp_nexus.Session.Statistics
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
        private readonly ILogger m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentDictionary<string, SessionInfo> m_Sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        private readonly SessionLifecycleManager m_LifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        private readonly SessionMonitoringService m_MonitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        private readonly DateTime m_StartTime = DateTime.Now;

        // Performance counters
        private long m_TotalCommandsProcessed = 0;
        private int m_PeakConcurrentSessions = 0;

        /// <summary>
        /// Gets comprehensive session statistics
        /// </summary>
        /// <returns>Current session statistics</returns>
        public SessionStatistics GetStatistics()
        {
            try
            {
                var activeSessionCount = m_Sessions.Count;
                var (Created, Closed, Expired) = m_LifecycleManager.GetLifecycleStats();
                var uptime = DateTime.Now - m_StartTime;

                // Update peak concurrent sessions
                if (activeSessionCount > m_PeakConcurrentSessions)
                {
                    Interlocked.Exchange(ref m_PeakConcurrentSessions, activeSessionCount);
                }

                // Calculate total commands processed
                var totalCommands = CalculateTotalCommandsProcessed();
                Interlocked.Exchange(ref m_TotalCommandsProcessed, totalCommands);

                return new SessionStatistics
                {
                    TotalSessionsCreated = Created,
                    TotalSessionsClosed = Closed,
                    TotalSessionsExpired = Expired,
                    ActiveSessions = activeSessionCount,
                    TotalCommandsProcessed = totalCommands,
                    AverageSessionLifetime = m_MonitoringService.CalculateAverageSessionLifetime(),
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
                m_Logger.LogError(ex, "Error calculating session statistics");
                return new SessionStatistics
                {
                    Uptime = DateTime.Now - m_StartTime
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

                foreach (var kvp in m_Sessions)
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
                        m_Logger.LogWarning(ex, "Error creating context for session {SessionId}", kvp.Key);
                    }
                }

                return activeSessions;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting active sessions");
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
                return m_Sessions.Values.ToList(); // Materialize to avoid concurrent modification issues
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting all sessions");
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
                    UsageHints = m_MonitoringService.GenerateUsageHints(sessionInfo, queueStatus)
                };
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error creating detailed context for session {SessionId}", sessionInfo.SessionId);

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
                m_Logger.LogTrace("Command queue disposed for session {SessionId}", sessionInfo.SessionId);
                return [];
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error getting queue status for session {SessionId}", sessionInfo.SessionId);
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
                var timeUntilExpiry = sessionTimeout - (DateTime.Now - sessionInfo.LastActivity);
                return timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : null;
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error calculating expiry time for session {SessionId}", sessionInfo.SessionId);
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

                foreach (var sessionInfo in m_Sessions.Values)
                {
                    try
                    {
                        var queueStatus = GetQueueStatusSafely(sessionInfo);
                        total += queueStatus.Count(q => q.Status == "Completed");
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogTrace(ex, "Error counting commands for session {SessionId}", sessionInfo.SessionId);
                    }
                }

                return total;
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error calculating total commands processed");
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
                m_Logger.LogTrace(ex, "Error getting memory usage");
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
                m_Logger.LogTrace(ex, "Error getting private memory usage");
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

                m_Logger.LogInformation("📊 Session Statistics Summary:");
                m_Logger.LogInformation("   Active Sessions: {Active}", stats.ActiveSessions);
                m_Logger.LogInformation("   Total Created: {Created}, Closed: {Closed}, Expired: {Expired}",
                    stats.TotalSessionsCreated, stats.TotalSessionsClosed, stats.TotalSessionsExpired);
                m_Logger.LogInformation("   Commands Processed: {Commands}", stats.TotalCommandsProcessed);
                m_Logger.LogInformation("   Average Lifetime: {Lifetime:hh\\:mm\\:ss}", stats.AverageSessionLifetime);
                m_Logger.LogInformation("   Uptime: {Uptime:dd\\.hh\\:mm\\:ss}", stats.Uptime);
                m_Logger.LogInformation("   Memory Usage: {Memory:N0} bytes ({MemoryMB:F1} MB)",
                    stats.MemoryUsage.WorkingSetBytes, stats.MemoryUsage.WorkingSetBytes / (1024.0 * 1024.0));
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error logging statistics summary");
            }
        }
    }

    /// <summary>
    /// Session manager statistics.
    /// Contains information about session usage, performance, and resource consumption.
    /// </summary>
    public class SessionStatistics
    {
        /// <summary>
        /// Gets or sets the current number of active sessions.
        /// </summary>
        public int ActiveSessions { get; set; }

        /// <summary>
        /// Gets or sets the total number of sessions created since startup.
        /// </summary>
        public long TotalSessionsCreated { get; set; }

        /// <summary>
        /// Gets or sets the total number of sessions closed.
        /// </summary>
        public long TotalSessionsClosed { get; set; }

        /// <summary>
        /// Gets or sets the total number of sessions that expired.
        /// </summary>
        public long TotalSessionsExpired { get; set; }

        /// <summary>
        /// Gets or sets the total number of commands processed across all sessions.
        /// </summary>
        public long TotalCommandsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the average session lifetime.
        /// </summary>
        public TimeSpan AverageSessionLifetime { get; set; }

        /// <summary>
        /// Gets or sets the session manager uptime.
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Gets or sets the memory usage information.
        /// </summary>
        public MemoryUsageInfo MemoryUsage { get; set; } = new();
    }

    /// <summary>
    /// Memory usage information.
    /// Contains details about memory consumption and garbage collection statistics.
    /// </summary>
    public class MemoryUsageInfo
    {
        /// <summary>
        /// Gets or sets the working set memory in bytes.
        /// </summary>
        public long WorkingSetBytes { get; set; }

        /// <summary>
        /// Gets or sets the private memory in bytes.
        /// </summary>
        public long PrivateMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets the GC total memory in bytes.
        /// </summary>
        public long GCTotalMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of Gen 0 collections.
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// Gets or sets the number of Gen 1 collections.
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// Gets or sets the number of Gen 2 collections.
        /// </summary>
        public int Gen2Collections { get; set; }
    }
}
