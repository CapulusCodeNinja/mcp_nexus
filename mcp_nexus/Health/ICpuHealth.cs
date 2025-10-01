namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for CPU health information with proper encapsulation
    /// </summary>
    public interface ICpuHealth
    {
        /// <summary>Gets whether CPU usage is healthy</summary>
        bool IsHealthy { get; }

        /// <summary>Gets CPU usage percentage</summary>
        double CpuUsagePercent { get; }

        /// <summary>Gets total processor time</summary>
        TimeSpan TotalProcessorTime { get; }

        /// <summary>Gets a message describing the CPU health</summary>
        string Message { get; }

        /// <summary>
        /// Sets the CPU health information
        /// </summary>
        /// <param name="isHealthy">Whether CPU usage is healthy</param>
        /// <param name="cpuUsagePercent">CPU usage percentage</param>
        /// <param name="totalProcessorTime">Total processor time</param>
        /// <param name="message">Health message</param>
        void SetCpuInfo(bool isHealthy, double cpuUsagePercent, TimeSpan totalProcessorTime, string message);
    }
}
