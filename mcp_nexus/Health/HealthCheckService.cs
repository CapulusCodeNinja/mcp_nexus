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

                var status = new HealthStatus
                {
                    Status = "healthy",
                    Timestamp = DateTime.UtcNow,
                    Uptime = uptime,
                    MemoryUsage = memoryUsage,
                    ActiveSessions = sessionCount,
                    CommandQueue = commandQueueStatus,
                    ProcessId = Environment.ProcessId,
                    MachineName = Environment.MachineName
                };

                // Determine overall health status
                if (memoryUsage > 1024 * 1024 * 1024) // 1GB
                {
                    status.Status = "degraded";
                    status.Issues.Add("High memory usage detected");
                }

                if (sessionCount > 10) // Configurable threshold
                {
                    status.Status = "degraded";
                    status.Issues.Add("High session count detected");
                }

                if (commandQueueStatus?.QueueSize > 100) // Configurable threshold
                {
                    status.Status = "degraded";
                    status.Issues.Add("High command queue size detected");
                }

                return status;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error getting health status");
                return new HealthStatus
                {
                    Status = "unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Issues = { $"Health check failed: {ex.Message}" }
                };
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
                return new CommandQueueHealthStatus
                {
                    QueueSize = 0,
                    ActiveCommands = 0,
                    ProcessedCommands = 0,
                    FailedCommands = 0
                };
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

    public class HealthStatus
    {
        public string Status { get; set; } = "unknown";
        public DateTime Timestamp { get; set; }
        public TimeSpan Uptime { get; set; }
        public long MemoryUsage { get; set; }
        public int ActiveSessions { get; set; }
        public CommandQueueHealthStatus? CommandQueue { get; set; }
        public int ProcessId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
    }

    public class CommandQueueHealthStatus
    {
        public int QueueSize { get; set; }
        public int ActiveCommands { get; set; }
        public long ProcessedCommands { get; set; }
        public long FailedCommands { get; set; }
    }
}
