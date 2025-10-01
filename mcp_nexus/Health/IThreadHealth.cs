namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for thread health information with proper encapsulation
    /// </summary>
    public interface IThreadHealth
    {
        /// <summary>Gets whether thread count is healthy</summary>
        bool IsHealthy { get; }

        /// <summary>Gets current thread count</summary>
        int ThreadCount { get; }

        /// <summary>Gets a message describing the thread health</summary>
        string Message { get; }

        /// <summary>
        /// Sets the thread health information
        /// </summary>
        /// <param name="isHealthy">Whether thread count is healthy</param>
        /// <param name="threadCount">Current thread count</param>
        /// <param name="message">Health message</param>
        void SetThreadInfo(bool isHealthy, int threadCount, string message);
    }
}
