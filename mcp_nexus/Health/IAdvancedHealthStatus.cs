namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for advanced health status with proper encapsulation
    /// </summary>
    public interface IAdvancedHealthStatus
    {
        /// <summary>Gets the timestamp when the health status was checked</summary>
        DateTime Timestamp { get; }

        /// <summary>Gets whether the system is healthy</summary>
        bool IsHealthy { get; }

        /// <summary>Gets a message describing the health status</summary>
        string Message { get; }

        /// <summary>Gets memory usage health information</summary>
        IMemoryHealth? MemoryUsage { get; }

        /// <summary>Gets CPU usage health information</summary>
        ICpuHealth? CpuUsage { get; }

        /// <summary>Gets disk usage health information</summary>
        IDiskHealth? DiskUsage { get; }

        /// <summary>Gets thread count health information</summary>
        IThreadHealth? ThreadCount { get; }

        /// <summary>Gets garbage collection health information</summary>
        IGcHealth? GcStatus { get; }

        /// <summary>
        /// Sets the health status information
        /// </summary>
        /// <param name="isHealthy">Whether the system is healthy</param>
        /// <param name="message">Health status message</param>
        void SetHealthStatus(bool isHealthy, string message);

        /// <summary>
        /// Sets the memory usage health information
        /// </summary>
        /// <param name="memoryHealth">Memory health information</param>
        void SetMemoryUsage(IMemoryHealth? memoryHealth);

        /// <summary>
        /// Sets the CPU usage health information
        /// </summary>
        /// <param name="cpuHealth">CPU health information</param>
        void SetCpuUsage(ICpuHealth? cpuHealth);

        /// <summary>
        /// Sets the disk usage health information
        /// </summary>
        /// <param name="diskHealth">Disk health information</param>
        void SetDiskUsage(IDiskHealth? diskHealth);

        /// <summary>
        /// Sets the thread count health information
        /// </summary>
        /// <param name="threadHealth">Thread health information</param>
        void SetThreadCount(IThreadHealth? threadHealth);

        /// <summary>
        /// Sets the garbage collection health information
        /// </summary>
        /// <param name="gcHealth">GC health information</param>
        void SetGcStatus(IGcHealth? gcHealth);
    }
}
