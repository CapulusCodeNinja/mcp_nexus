namespace mcp_nexus.Health
{
    /// <summary>
    /// Builder for creating advanced health status objects using Builder Pattern
    /// </summary>
    public class AdvancedHealthStatusBuilder
    {
        private readonly AdvancedHealthStatus m_healthStatus;

        /// <summary>
        /// Initializes a new instance of the AdvancedHealthStatusBuilder
        /// </summary>
        public AdvancedHealthStatusBuilder()
        {
            m_healthStatus = new AdvancedHealthStatus();
        }

        /// <summary>
        /// Sets the basic health status information
        /// </summary>
        /// <param name="isHealthy">Whether the system is healthy</param>
        /// <param name="message">Health status message</param>
        /// <returns>Builder instance for method chaining</returns>
        public AdvancedHealthStatusBuilder SetHealthStatus(bool isHealthy, string message)
        {
            m_healthStatus.SetHealthStatus(isHealthy, message);
            return this;
        }

        /// <summary>
        /// Sets the memory usage health information
        /// </summary>
        /// <param name="memoryHealth">Memory health information</param>
        /// <returns>Builder instance for method chaining</returns>
        public AdvancedHealthStatusBuilder WithMemoryUsage(MemoryHealth? memoryHealth)
        {
            m_healthStatus.SetMemoryUsage(memoryHealth);
            return this;
        }

        /// <summary>
        /// Sets the CPU usage health information
        /// </summary>
        /// <param name="cpuHealth">CPU health information</param>
        /// <returns>Builder instance for method chaining</returns>
        public AdvancedHealthStatusBuilder WithCpuUsage(CpuHealth? cpuHealth)
        {
            m_healthStatus.SetCpuUsage(cpuHealth);
            return this;
        }

        /// <summary>
        /// Sets the disk usage health information
        /// </summary>
        /// <param name="diskHealth">Disk health information</param>
        /// <returns>Builder instance for method chaining</returns>
        public AdvancedHealthStatusBuilder WithDiskUsage(DiskHealth? diskHealth)
        {
            m_healthStatus.SetDiskUsage(diskHealth);
            return this;
        }

        /// <summary>
        /// Sets the thread count health information
        /// </summary>
        /// <param name="threadHealth">Thread health information</param>
        /// <returns>Builder instance for method chaining</returns>
        public AdvancedHealthStatusBuilder WithThreadCount(ThreadHealth? threadHealth)
        {
            m_healthStatus.SetThreadCount(threadHealth);
            return this;
        }

        /// <summary>
        /// Sets the garbage collection health information
        /// </summary>
        /// <param name="gcHealth">GC health information</param>
        /// <returns>Builder instance for method chaining</returns>
        public AdvancedHealthStatusBuilder WithGcStatus(GcHealth? gcHealth)
        {
            m_healthStatus.SetGcStatus(gcHealth);
            return this;
        }

        /// <summary>
        /// Builds the final advanced health status object
        /// </summary>
        /// <returns>Completed advanced health status object</returns>
        public AdvancedHealthStatus Build()
        {
            return m_healthStatus;
        }
    }
}
