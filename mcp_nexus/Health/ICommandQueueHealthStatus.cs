namespace mcp_nexus.Health
{
    /// <summary>
    /// Interface for command queue health status with proper encapsulation
    /// </summary>
    public interface ICommandQueueHealthStatus
    {
        /// <summary>Gets the queue size</summary>
        int QueueSize { get; }

        /// <summary>Gets the active commands count</summary>
        int ActiveCommands { get; }

        /// <summary>Gets the processed commands count</summary>
        long ProcessedCommands { get; }

        /// <summary>Gets the failed commands count</summary>
        long FailedCommands { get; }

        /// <summary>
        /// Sets the command queue status information
        /// </summary>
        /// <param name="queueSize">Queue size</param>
        /// <param name="activeCommands">Active commands count</param>
        /// <param name="processedCommands">Processed commands count</param>
        /// <param name="failedCommands">Failed commands count</param>
        void SetStatus(int queueSize, int activeCommands, long processedCommands, long failedCommands);
    }
}
