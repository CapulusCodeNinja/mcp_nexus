namespace mcp_nexus.Health
{
    /// <summary>
    /// Builder interface for creating advanced health status objects using Builder Pattern
    /// </summary>
    public interface IAdvancedHealthStatusBuilder
    {
        /// <summary>
        /// Sets the basic health status information
        /// </summary>
        /// <param name="isHealthy">Whether the system is healthy</param>
        /// <param name="message">Health status message</param>
        /// <returns>Builder instance for method chaining</returns>
        IAdvancedHealthStatusBuilder SetHealthStatus(bool isHealthy, string message);

        /// <summary>
        /// Sets the memory usage health information
        /// </summary>
        /// <param name="memoryHealth">Memory health information</param>
        /// <returns>Builder instance for method chaining</returns>
        IAdvancedHealthStatusBuilder WithMemoryUsage(IMemoryHealth? memoryHealth);

        /// <summary>
        /// Sets the CPU usage health information
        /// </summary>
        /// <param name="cpuHealth">CPU health information</param>
        /// <returns>Builder instance for method chaining</returns>
        IAdvancedHealthStatusBuilder WithCpuUsage(ICpuHealth? cpuHealth);

        /// <summary>
        /// Sets the disk usage health information
        /// </summary>
        /// <param name="diskHealth">Disk health information</param>
        /// <returns>Builder instance for method chaining</returns>
        IAdvancedHealthStatusBuilder WithDiskUsage(IDiskHealth? diskHealth);

        /// <summary>
        /// Sets the thread count health information
        /// </summary>
        /// <param name="threadHealth">Thread health information</param>
        /// <returns>Builder instance for method chaining</returns>
        IAdvancedHealthStatusBuilder WithThreadCount(IThreadHealth? threadHealth);

        /// <summary>
        /// Sets the garbage collection health information
        /// </summary>
        /// <param name="gcHealth">GC health information</param>
        /// <returns>Builder instance for method chaining</returns>
        IAdvancedHealthStatusBuilder WithGcStatus(IGcHealth? gcHealth);

        /// <summary>
        /// Builds the final advanced health status object
        /// </summary>
        /// <returns>Completed advanced health status object</returns>
        IAdvancedHealthStatus Build();
    }
}
