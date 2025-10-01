namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for disk health information with proper encapsulation
    /// </summary>
    public interface IDiskHealth
    {
        /// <summary>Gets whether disk usage is healthy</summary>
        bool IsHealthy { get; }

        /// <summary>Gets the list of unhealthy drives</summary>
        IReadOnlyList<string> UnhealthyDrives { get; }

        /// <summary>Gets a message describing the disk health</summary>
        string Message { get; }

        /// <summary>
        /// Sets the disk health information
        /// </summary>
        /// <param name="isHealthy">Whether disk usage is healthy</param>
        /// <param name="unhealthyDrives">List of unhealthy drives</param>
        /// <param name="message">Health message</param>
        void SetDiskInfo(bool isHealthy, IReadOnlyList<string> unhealthyDrives, string message);
    }
}
