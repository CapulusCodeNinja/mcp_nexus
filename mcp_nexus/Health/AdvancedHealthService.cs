using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Health
{
    /// <summary>
    /// Advanced health monitoring service with comprehensive system checks
    /// </summary>
    public class AdvancedHealthService : IDisposable
    {
        #region Private Fields

        private readonly ILogger<AdvancedHealthService> m_logger;
        private readonly Timer m_healthTimer;
        private readonly Process m_currentProcess;
        private volatile bool m_disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AdvancedHealthService class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public AdvancedHealthService(ILogger<AdvancedHealthService> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_currentProcess = Process.GetCurrentProcess();

            // Health check every 30 seconds
            m_healthTimer = new Timer(PerformHealthCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            m_logger.LogInformation("🏥 AdvancedHealthService initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current health status of the system
        /// </summary>
        /// <returns>The current health status</returns>
        public AdvancedHealthStatus GetHealthStatus()
        {
            if (m_disposed) return new AdvancedHealthStatus { IsHealthy = false, Message = "Service disposed" };

            try
            {
                var status = new AdvancedHealthStatus
                {
                    Timestamp = DateTime.UtcNow,
                    IsHealthy = true,
                    Message = "All systems operational"
                };

                // Check memory usage
                var memoryStatus = CheckMemoryHealth();
                status.MemoryUsage = memoryStatus;
                if (!memoryStatus.IsHealthy) status.IsHealthy = false;

                // Check CPU usage
                var cpuStatus = CheckCpuHealth();
                status.CpuUsage = cpuStatus;
                if (!cpuStatus.IsHealthy) status.IsHealthy = false;

                // Check disk space
                var diskStatus = CheckDiskHealth();
                status.DiskUsage = diskStatus;
                if (!diskStatus.IsHealthy) status.IsHealthy = false;

                // Check thread count
                var threadStatus = CheckThreadHealth();
                status.ThreadCount = threadStatus;
                if (!threadStatus.IsHealthy) status.IsHealthy = false;

                // Check GC health
                var gcStatus = CheckGcHealth();
                status.GcStatus = gcStatus;
                if (!gcStatus.IsHealthy) status.IsHealthy = false;

                if (!status.IsHealthy)
                {
                    status.Message = "Health issues detected - check individual components";
                }

                return status;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error getting health status");
                return new AdvancedHealthStatus
                {
                    IsHealthy = false,
                    Message = $"Health check failed: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private MemoryHealth CheckMemoryHealth()
        {
            try
            {
                var workingSet = m_currentProcess.WorkingSet64;
                var privateMemory = m_currentProcess.PrivateMemorySize64;
                var virtualMemory = m_currentProcess.VirtualMemorySize64;
                var totalPhysicalMemory = GC.GetTotalMemory(false);

                var workingSetMB = workingSet / (1024.0 * 1024.0);
                var privateMemoryMB = privateMemory / (1024.0 * 1024.0);
                var virtualMemoryMB = virtualMemory / (1024.0 * 1024.0);
                var totalPhysicalMemoryMB = totalPhysicalMemory / (1024.0 * 1024.0);

                var isHealthy = workingSetMB < 2048 && privateMemoryMB < 1024; // 2GB working set, 1GB private

                return new MemoryHealth
                {
                    IsHealthy = isHealthy,
                    WorkingSetMB = workingSetMB,
                    PrivateMemoryMB = privateMemoryMB,
                    VirtualMemoryMB = virtualMemoryMB,
                    TotalPhysicalMemoryMB = totalPhysicalMemoryMB,
                    Message = isHealthy ? "Memory usage normal" : "High memory usage detected"
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error checking memory health");
                return new MemoryHealth { IsHealthy = false, Message = $"Memory check failed: {ex.Message}" };
            }
        }

        private CpuHealth CheckCpuHealth()
        {
            try
            {
                var totalProcessorTime = m_currentProcess.TotalProcessorTime;
                var cpuUsage = totalProcessorTime.TotalMilliseconds / Environment.TickCount;

                var isHealthy = cpuUsage < 0.8; // Less than 80% CPU usage

                return new CpuHealth
                {
                    IsHealthy = isHealthy,
                    CpuUsagePercent = Math.Min(100, cpuUsage * 100),
                    TotalProcessorTime = totalProcessorTime,
                    Message = isHealthy ? "CPU usage normal" : "High CPU usage detected"
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error checking CPU health");
                return new CpuHealth { IsHealthy = false, Message = $"CPU check failed: {ex.Message}" };
            }
        }

        private DiskHealth CheckDiskHealth()
        {
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                var unhealthyDrives = new List<string>();

                foreach (var drive in drives)
                {
                    var freeSpacePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                    if (freeSpacePercent < 10) // Less than 10% free space
                    {
                        unhealthyDrives.Add($"{drive.Name} ({freeSpacePercent:F1}% free)");
                    }
                }

                var isHealthy = unhealthyDrives.Count == 0;

                return new DiskHealth
                {
                    IsHealthy = isHealthy,
                    UnhealthyDrives = unhealthyDrives,
                    Message = isHealthy ? "Disk space normal" : $"Low disk space: {string.Join(", ", unhealthyDrives)}"
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error checking disk health");
                return new DiskHealth { IsHealthy = false, Message = $"Disk check failed: {ex.Message}" };
            }
        }

        private ThreadHealth CheckThreadHealth()
        {
            try
            {
                var threadCount = m_currentProcess.Threads.Count;
                var isHealthy = threadCount < 100; // Less than 100 threads

                return new ThreadHealth
                {
                    IsHealthy = isHealthy,
                    ThreadCount = threadCount,
                    Message = isHealthy ? "Thread count normal" : "High thread count detected"
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error checking thread health");
                return new ThreadHealth { IsHealthy = false, Message = $"Thread check failed: {ex.Message}" };
            }
        }

        private GcHealth CheckGcHealth()
        {
            try
            {
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);
                var totalCollections = gen0Collections + gen1Collections + gen2Collections;

                var isHealthy = gen2Collections < 10; // Less than 10 Gen2 collections

                return new GcHealth
                {
                    IsHealthy = isHealthy,
                    Gen0Collections = gen0Collections,
                    Gen1Collections = gen1Collections,
                    Gen2Collections = gen2Collections,
                    TotalCollections = totalCollections,
                    Message = isHealthy ? "GC health normal" : "Frequent GC collections detected"
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error checking GC health");
                return new GcHealth { IsHealthy = false, Message = $"GC check failed: {ex.Message}" };
            }
        }

        private void PerformHealthCheck(object? state)
        {
            if (m_disposed) return;

            try
            {
                var status = GetHealthStatus();
                if (!status.IsHealthy)
                {
                    m_logger.LogWarning("🏥 Health check failed: {Message}", status.Message);
                }
                else
                {
                    m_logger.LogDebug("🏥 Health check passed");
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during health check");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the health service
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            m_healthTimer?.Dispose();
            m_logger.LogInformation("🏥 AdvancedHealthService disposed");
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Represents the health status of the system
    /// </summary>
    public class AdvancedHealthStatus
    {
        /// <summary>
        /// The timestamp when the health status was checked
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether the system is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// A message describing the health status
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Memory usage health information
        /// </summary>
        public MemoryHealth? MemoryUsage { get; set; }

        /// <summary>
        /// CPU usage health information
        /// </summary>
        public CpuHealth? CpuUsage { get; set; }

        /// <summary>
        /// Disk usage health information
        /// </summary>
        public DiskHealth? DiskUsage { get; set; }

        /// <summary>
        /// Thread count health information
        /// </summary>
        public ThreadHealth? ThreadCount { get; set; }

        /// <summary>
        /// Garbage collection health information
        /// </summary>
        public GcHealth? GcStatus { get; set; }
    }

    /// <summary>
    /// Represents memory health information
    /// </summary>
    public class MemoryHealth
    {
        /// <summary>
        /// Whether memory usage is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Working set memory in MB
        /// </summary>
        public double WorkingSetMB { get; set; }

        /// <summary>
        /// Private memory in MB
        /// </summary>
        public double PrivateMemoryMB { get; set; }

        /// <summary>
        /// Virtual memory in MB
        /// </summary>
        public double VirtualMemoryMB { get; set; }

        /// <summary>
        /// Total physical memory in MB
        /// </summary>
        public double TotalPhysicalMemoryMB { get; set; }

        /// <summary>
        /// A message describing the memory health
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents CPU health information
    /// </summary>
    public class CpuHealth
    {
        /// <summary>
        /// Whether CPU usage is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// Total processor time
        /// </summary>
        public TimeSpan TotalProcessorTime { get; set; }

        /// <summary>
        /// A message describing the CPU health
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents disk health information
    /// </summary>
    public class DiskHealth
    {
        /// <summary>
        /// Whether disk usage is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// List of unhealthy drives
        /// </summary>
        public List<string> UnhealthyDrives { get; set; } = new();

        /// <summary>
        /// A message describing the disk health
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents thread health information
    /// </summary>
    public class ThreadHealth
    {
        /// <summary>
        /// Whether thread count is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Current thread count
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// A message describing the thread health
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents garbage collection health information
    /// </summary>
    public class GcHealth
    {
        /// <summary>
        /// Whether garbage collection is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Number of Gen0 collections
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// Number of Gen1 collections
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// Number of Gen2 collections
        /// </summary>
        public int Gen2Collections { get; set; }

        /// <summary>
        /// Total number of collections
        /// </summary>
        public int TotalCollections { get; set; }

        /// <summary>
        /// A message describing the GC health
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}
