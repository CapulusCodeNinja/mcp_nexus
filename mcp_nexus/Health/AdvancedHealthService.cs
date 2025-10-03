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
            if (m_disposed)
            {
                var disposedStatus = new AdvancedHealthStatus();
                disposedStatus.SetHealthStatus(false, "Service disposed");
                return disposedStatus;
            }

            try
            {
                var status = new AdvancedHealthStatus();
                status.SetHealthStatus(true, "All systems operational");

                // Check memory usage
                var memoryStatus = CheckMemoryHealth();
                status.SetMemoryUsage(memoryStatus);
                if (!memoryStatus.IsHealthy) status.SetHealthStatus(false, "Health issues detected - check individual components");

                // Check CPU usage
                var cpuStatus = CheckCpuHealth();
                status.SetCpuUsage(cpuStatus);
                if (!cpuStatus.IsHealthy) status.SetHealthStatus(false, "Health issues detected - check individual components");

                // Check disk space
                var diskStatus = CheckDiskHealth();
                status.SetDiskUsage(diskStatus);
                if (!diskStatus.IsHealthy) status.SetHealthStatus(false, "Health issues detected - check individual components");

                // Check thread count
                var threadStatus = CheckThreadHealth();
                status.SetThreadCount(threadStatus);
                if (!threadStatus.IsHealthy) status.SetHealthStatus(false, "Health issues detected - check individual components");

                // Check GC health
                var gcStatus = CheckGcHealth();
                status.SetGcStatus(gcStatus);
                if (!gcStatus.IsHealthy) status.SetHealthStatus(false, "Health issues detected - check individual components");

                return status;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error getting health status");
                var errorStatus = new AdvancedHealthStatus();
                errorStatus.SetHealthStatus(false, $"Health check failed: {ex.Message}");
                return errorStatus;
            }
        }

        /// <summary>
        /// Checks the current memory health status.
        /// </summary>
        /// <returns>A MemoryHealth object containing memory usage information.</returns>
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

                var memoryHealth = new MemoryHealth();
                memoryHealth.SetMemoryInfo(isHealthy, workingSetMB, privateMemoryMB,
                    virtualMemoryMB, totalPhysicalMemoryMB,
                    isHealthy ? "Memory usage normal" : "High memory usage detected");
                return memoryHealth;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error checking memory health");
                var errorMemoryHealth = new MemoryHealth();
                errorMemoryHealth.SetMemoryInfo(false, 0, 0, 0, 0, $"Memory check failed: {ex.Message}");
                return errorMemoryHealth;
            }
        }

        /// <summary>
        /// Checks the current CPU health status.
        /// </summary>
        /// <returns>A CpuHealth object containing CPU usage information.</returns>
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

        /// <summary>
        /// Checks the current disk health status.
        /// </summary>
        /// <returns>A DiskHealth object containing disk usage information.</returns>
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

        /// <summary>
        /// Checks the current thread health status.
        /// </summary>
        /// <returns>A ThreadHealth object containing thread count information.</returns>
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

        /// <summary>
        /// Checks the current garbage collection health status.
        /// </summary>
        /// <returns>A GcHealth object containing GC information.</returns>
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

        /// <summary>
        /// Performs a comprehensive health check of all system components.
        /// </summary>
        /// <param name="state">The timer state (unused).</param>
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
    /// Represents the health status of the system - properly encapsulated
    /// </summary>
    public class AdvancedHealthStatus
    {
        #region Private Fields

        private DateTime m_timestamp;
        private bool m_isHealthy;
        private string m_message = string.Empty;
        private MemoryHealth? m_memoryUsage;
        private CpuHealth? m_cpuUsage;
        private DiskHealth? m_diskUsage;
        private ThreadHealth? m_threadCount;
        private GcHealth? m_gcStatus;

        #endregion

        #region Public Properties

        /// <summary>Gets the timestamp when the health status was checked</summary>
        public DateTime Timestamp => m_timestamp;

        /// <summary>Gets whether the system is healthy</summary>
        public bool IsHealthy => m_isHealthy;

        /// <summary>Gets a message describing the health status</summary>
        public string Message => m_message;

        /// <summary>Gets memory usage health information</summary>
        public MemoryHealth? MemoryUsage => m_memoryUsage;

        /// <summary>Gets CPU usage health information</summary>
        public CpuHealth? CpuUsage => m_cpuUsage;

        /// <summary>Gets disk usage health information</summary>
        public DiskHealth? DiskUsage => m_diskUsage;

        /// <summary>Gets thread count health information</summary>
        public ThreadHealth? ThreadCount => m_threadCount;

        /// <summary>Gets garbage collection health information</summary>
        public GcHealth? GcStatus => m_gcStatus;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new health status
        /// </summary>
        public AdvancedHealthStatus()
        {
            m_timestamp = DateTime.UtcNow;
            m_isHealthy = false;
            m_message = string.Empty;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the health status information
        /// </summary>
        /// <param name="isHealthy">Whether the system is healthy</param>
        /// <param name="message">Health status message</param>
        public void SetHealthStatus(bool isHealthy, string message)
        {
            m_isHealthy = isHealthy;
            m_message = message ?? string.Empty;
        }

        /// <summary>
        /// Sets the memory usage health information
        /// </summary>
        /// <param name="memoryHealth">Memory health information</param>
        public void SetMemoryUsage(MemoryHealth? memoryHealth)
        {
            m_memoryUsage = memoryHealth;
        }

        /// <summary>
        /// Sets the CPU usage health information
        /// </summary>
        /// <param name="cpuHealth">CPU health information</param>
        public void SetCpuUsage(CpuHealth? cpuHealth)
        {
            m_cpuUsage = cpuHealth;
        }

        /// <summary>
        /// Sets the disk usage health information
        /// </summary>
        /// <param name="diskHealth">Disk health information</param>
        public void SetDiskUsage(DiskHealth? diskHealth)
        {
            m_diskUsage = diskHealth;
        }

        /// <summary>
        /// Sets the thread count health information
        /// </summary>
        /// <param name="threadHealth">Thread health information</param>
        public void SetThreadCount(ThreadHealth? threadHealth)
        {
            m_threadCount = threadHealth;
        }

        /// <summary>
        /// Sets the garbage collection health information
        /// </summary>
        /// <param name="gcHealth">GC health information</param>
        public void SetGcStatus(GcHealth? gcHealth)
        {
            m_gcStatus = gcHealth;
        }

        #endregion
    }

    /// <summary>
    /// Represents memory health information - properly encapsulated
    /// </summary>
    public class MemoryHealth
    {
        #region Private Fields

        private bool m_isHealthy;
        private double m_workingSetMB;
        private double m_privateMemoryMB;
        private double m_virtualMemoryMB;
        private double m_totalPhysicalMemoryMB;
        private string m_message = string.Empty;

        #endregion

        #region Public Properties

        /// <summary>Gets or sets whether memory usage is healthy</summary>
        public bool IsHealthy { get => m_isHealthy; set => m_isHealthy = value; }

        /// <summary>Gets or sets working set memory in MB</summary>
        public double WorkingSetMB { get => m_workingSetMB; set => m_workingSetMB = value; }

        /// <summary>Gets or sets private memory in MB</summary>
        public double PrivateMemoryMB { get => m_privateMemoryMB; set => m_privateMemoryMB = value; }

        /// <summary>Gets or sets virtual memory in MB</summary>
        public double VirtualMemoryMB { get => m_virtualMemoryMB; set => m_virtualMemoryMB = value; }

        /// <summary>Gets or sets total physical memory in MB</summary>
        public double TotalPhysicalMemoryMB { get => m_totalPhysicalMemoryMB; set => m_totalPhysicalMemoryMB = value; }

        /// <summary>Gets a message describing the memory health</summary>
        public string Message => m_message;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new memory health instance
        /// </summary>
        public MemoryHealth()
        {
            m_isHealthy = false;
            m_workingSetMB = 0;
            m_privateMemoryMB = 0;
            m_virtualMemoryMB = 0;
            m_totalPhysicalMemoryMB = 0;
            m_message = string.Empty;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the memory health information
        /// </summary>
        /// <param name="isHealthy">Whether memory usage is healthy</param>
        /// <param name="workingSetMB">Working set memory in MB</param>
        /// <param name="privateMemoryMB">Private memory in MB</param>
        /// <param name="virtualMemoryMB">Virtual memory in MB</param>
        /// <param name="totalPhysicalMemoryMB">Total physical memory in MB</param>
        /// <param name="message">Health message</param>
        public void SetMemoryInfo(bool isHealthy, double workingSetMB, double privateMemoryMB,
            double virtualMemoryMB, double totalPhysicalMemoryMB, string message)
        {
            m_isHealthy = isHealthy;
            m_workingSetMB = workingSetMB;
            m_privateMemoryMB = privateMemoryMB;
            m_virtualMemoryMB = virtualMemoryMB;
            m_totalPhysicalMemoryMB = totalPhysicalMemoryMB;
            m_message = message ?? string.Empty;
        }

        #endregion
    }

    /// <summary>
    /// Represents CPU health information - properly encapsulated
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
    /// Represents disk health information - properly encapsulated
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
    /// Represents thread health information - properly encapsulated
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
    /// Represents garbage collection health information - properly encapsulated
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
