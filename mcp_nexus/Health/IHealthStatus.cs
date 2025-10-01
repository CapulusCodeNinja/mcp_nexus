namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for health status with proper encapsulation
    /// </summary>
    public interface IHealthStatus
    {
        /// <summary>Gets the health status</summary>
        string Status { get; }

        /// <summary>Gets the timestamp</summary>
        DateTime Timestamp { get; }

        /// <summary>Gets the uptime</summary>
        TimeSpan Uptime { get; }

        /// <summary>Gets the memory usage</summary>
        long MemoryUsage { get; }

        /// <summary>Gets the active sessions count</summary>
        int ActiveSessions { get; }

        /// <summary>Gets the command queue status</summary>
        ICommandQueueHealthStatus? CommandQueue { get; }

        /// <summary>Gets the process ID</summary>
        int ProcessId { get; }

        /// <summary>Gets the machine name</summary>
        string MachineName { get; }

        /// <summary>Gets the issues list</summary>
        IReadOnlyList<string> Issues { get; }

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
        void SetHealthInfo(string status, DateTime timestamp, TimeSpan uptime,
            long memoryUsage, int activeSessions, int processId, string machineName);

        /// <summary>
        /// Sets the command queue status
        /// </summary>
        /// <param name="commandQueue">Command queue status</param>
        void SetCommandQueue(ICommandQueueHealthStatus? commandQueue);

        /// <summary>
        /// Adds an issue to the issues list
        /// </summary>
        /// <param name="issue">Issue to add</param>
        void AddIssue(string issue);

        /// <summary>
        /// Clears all issues
        /// </summary>
        void ClearIssues();
    }
}
