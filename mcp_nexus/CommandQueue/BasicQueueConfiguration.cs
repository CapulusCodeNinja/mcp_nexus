using mcp_nexus.Constants;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Basic configuration for simple command queue operations
    /// </summary>
    public class BasicQueueConfiguration
    {
        public TimeSpan CleanupInterval { get; }
        public TimeSpan CommandRetentionTime { get; }
        public TimeSpan StatsLogInterval { get; }
        
        public BasicQueueConfiguration(
            TimeSpan? cleanupInterval = null,
            TimeSpan? commandRetentionTime = null,
            TimeSpan? statsLogInterval = null)
        {
            CleanupInterval = cleanupInterval ?? ApplicationConstants.CleanupInterval;
            CommandRetentionTime = commandRetentionTime ?? ApplicationConstants.CommandRetentionTime;
            StatsLogInterval = statsLogInterval ?? TimeSpan.FromMinutes(5);
        }
    }
}
