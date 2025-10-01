using System.Diagnostics;
using mcp_nexus.Session;
using mcp_nexus.CommandQueue;

namespace mcp_nexus.Health
{
    /// <summary>
    /// Health check service for monitoring server status and resource usage
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly ILogger<HealthCheckService> m_logger;
        private readonly ISessionManager m_sessionManager;
        private readonly ICommandQueueService? m_commandQueue;
        private readonly DateTime m_startTime = DateTime.UtcNow;
        private readonly Process m_currentProcess = Process.GetCurrentProcess();

        public HealthCheckService(
            ILogger<HealthCheckService> logger,
            ISessionManager sessionManager,
            ICommandQueueService? commandQueue = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            m_commandQueue = commandQueue;
        }

        public HealthStatus GetHealthStatus()
        {
            try
            {
                var uptime = DateTime.UtcNow - m_startTime;
                var memoryUsage = GetMemoryUsage();
                var sessionCount = GetActiveSessionCount();
                var commandQueueStatus = GetCommandQueueStatus();

                var status = new HealthStatus();
                status.SetHealthInfo("healthy", DateTime.UtcNow, uptime, memoryUsage,
                    sessionCount, Environment.ProcessId, Environment.MachineName);
                status.SetCommandQueue(commandQueueStatus);

                // Determine overall health status
                if (memoryUsage > 1024 * 1024 * 1024) // 1GB
                {
                    status.SetHealthInfo("degraded", DateTime.UtcNow, uptime, memoryUsage,
                        sessionCount, Environment.ProcessId, Environment.MachineName);
                    status.AddIssue("High memory usage detected");
                }

                if (sessionCount > 10) // Configurable threshold
                {
                    status.SetHealthInfo("degraded", DateTime.UtcNow, uptime, memoryUsage,
                        sessionCount, Environment.ProcessId, Environment.MachineName);
                    status.AddIssue("High session count detected");
                }

                if (commandQueueStatus?.QueueSize > 100) // Configurable threshold
                {
                    status.SetHealthInfo("degraded", DateTime.UtcNow, uptime, memoryUsage,
                        sessionCount, Environment.ProcessId, Environment.MachineName);
                    status.AddIssue("High command queue size detected");
                }

                return status;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error getting health status");
                var errorStatus = new HealthStatus();
                errorStatus.SetHealthInfo("unhealthy", DateTime.UtcNow, TimeSpan.Zero, 0, 0, 0, "");
                errorStatus.AddIssue($"Health check failed: {ex.Message}");
                return errorStatus;
            }
        }

        private long GetMemoryUsage()
        {
            try
            {
                m_currentProcess.Refresh();
                return m_currentProcess.WorkingSet64;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Failed to get memory usage");
                return 0;
            }
        }

        private int GetActiveSessionCount()
        {
            try
            {
                // This would need to be implemented in ISessionManager
                // For now, return 0 as a placeholder
                return 0;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Failed to get active session count");
                return 0;
            }
        }

        private CommandQueueHealthStatus? GetCommandQueueStatus()
        {
            try
            {
                if (m_commandQueue == null)
                    return null;

                // This would need to be implemented in ICommandQueueService
                // For now, return a placeholder
                var commandQueueStatus = new CommandQueueHealthStatus();
                commandQueueStatus.SetStatus(0, 0, 0, 0);
                return commandQueueStatus;
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Failed to get command queue status");
                return null;
            }
        }
    }

    public interface IHealthCheckService
    {
        HealthStatus GetHealthStatus();
    }

    /// <summary>
    /// Represents the health status of the system - properly encapsulated
    /// </summary>
    public class HealthStatus
    {
        #region Private Fields

        private string m_status = "unknown";
        private DateTime m_timestamp;
        private TimeSpan m_uptime;
        private long m_memoryUsage;
        private int m_activeSessions;
        private CommandQueueHealthStatus? m_commandQueue;
        private int m_processId;
        private string m_machineName = string.Empty;
        private readonly List<string> m_issues = new();

        #endregion

        #region Public Properties

        /// <summary>Gets the health status</summary>
        public string Status => m_status;

        /// <summary>Gets the timestamp</summary>
        public DateTime Timestamp => m_timestamp;

        /// <summary>Gets the uptime</summary>
        public TimeSpan Uptime => m_uptime;

        /// <summary>Gets the memory usage</summary>
        public long MemoryUsage => m_memoryUsage;

        /// <summary>Gets the active sessions count</summary>
        public int ActiveSessions => m_activeSessions;

        /// <summary>Gets the command queue status</summary>
        public CommandQueueHealthStatus? CommandQueue => m_commandQueue;

        /// <summary>Gets the process ID</summary>
        public int ProcessId => m_processId;

        /// <summary>Gets the machine name</summary>
        public string MachineName => m_machineName;

        /// <summary>Gets the issues list</summary>
        public IReadOnlyList<string> Issues => m_issues.AsReadOnly();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new health status
        /// </summary>
        public HealthStatus()
        {
            m_timestamp = DateTime.UtcNow;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the health status information
        /// </summary>
        /// <param name="status">Health status</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="uptime">Uptime</param>
        /// <param name="memoryUsage">Memory usage</param>
        /// <param name="activeSessions">Active sessions count</param>
        /// <param name="processId">Process ID</param>
        /// <param name="machineName">Machine name</param>
        public void SetHealthInfo(string status, DateTime timestamp, TimeSpan uptime,
            long memoryUsage, int activeSessions, int processId, string machineName)
        {
            m_status = status ?? "unknown";
            m_timestamp = timestamp;
            m_uptime = uptime;
            m_memoryUsage = memoryUsage;
            m_activeSessions = activeSessions;
            m_processId = processId;
            m_machineName = machineName ?? string.Empty;
        }

        /// <summary>
        /// Sets the command queue status
        /// </summary>
        /// <param name="commandQueue">Command queue status</param>
        public void SetCommandQueue(CommandQueueHealthStatus? commandQueue)
        {
            m_commandQueue = commandQueue;
        }

        /// <summary>
        /// Adds an issue to the issues list
        /// </summary>
        /// <param name="issue">Issue to add</param>
        public void AddIssue(string issue)
        {
            if (!string.IsNullOrEmpty(issue))
                m_issues.Add(issue);
        }

        /// <summary>
        /// Clears all issues
        /// </summary>
        public void ClearIssues()
        {
            m_issues.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Represents command queue health status - properly encapsulated
    /// </summary>
    public class CommandQueueHealthStatus
    {
        #region Private Fields

        private int m_queueSize;
        private int m_activeCommands;
        private long m_processedCommands;
        private long m_failedCommands;

        #endregion

        #region Public Properties

        /// <summary>Gets the queue size</summary>
        public int QueueSize => m_queueSize;

        /// <summary>Gets the active commands count</summary>
        public int ActiveCommands => m_activeCommands;

        /// <summary>Gets the processed commands count</summary>
        public long ProcessedCommands => m_processedCommands;

        /// <summary>Gets the failed commands count</summary>
        public long FailedCommands => m_failedCommands;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new command queue health status
        /// </summary>
        public CommandQueueHealthStatus()
        {
            m_queueSize = 0;
            m_activeCommands = 0;
            m_processedCommands = 0;
            m_failedCommands = 0;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the command queue status information
        /// </summary>
        /// <param name="queueSize">Queue size</param>
        /// <param name="activeCommands">Active commands count</param>
        /// <param name="processedCommands">Processed commands count</param>
        /// <param name="failedCommands">Failed commands count</param>
        public void SetStatus(int queueSize, int activeCommands, long processedCommands, long failedCommands)
        {
            m_queueSize = queueSize;
            m_activeCommands = activeCommands;
            m_processedCommands = processedCommands;
            m_failedCommands = failedCommands;
        }

        #endregion
    }
}
