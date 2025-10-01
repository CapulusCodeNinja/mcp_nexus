namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for memory health information with proper encapsulation
    /// </summary>
    public interface IMemoryHealth
    {
        /// <summary>Gets whether memory usage is healthy</summary>
        bool IsHealthy { get; }

        /// <summary>Gets working set memory in MB</summary>
        double WorkingSetMB { get; }

        /// <summary>Gets private memory in MB</summary>
        double PrivateMemoryMB { get; }

        /// <summary>Gets virtual memory in MB</summary>
        double VirtualMemoryMB { get; }

        /// <summary>Gets total physical memory in MB</summary>
        double TotalPhysicalMemoryMB { get; }

        /// <summary>Gets a message describing the memory health</summary>
        string Message { get; }

        /// <summary>
        /// Sets the memory health information
        /// </summary>
        /// <param name="isHealthy">Whether memory usage is healthy</param>
        /// <param name="workingSetMB">Working set memory in MB</param>
        /// <param name="privateMemoryMB">Private memory in MB</param>
        /// <param name="virtualMemoryMB">Virtual memory in MB</param>
        /// <param name="totalPhysicalMemoryMB">Total physical memory in MB</param>
        /// <param name="message">Health message</param>
        void SetMemoryInfo(bool isHealthy, double workingSetMB, double privateMemoryMB, 
            double virtualMemoryMB, double totalPhysicalMemoryMB, string message);
    }
}
