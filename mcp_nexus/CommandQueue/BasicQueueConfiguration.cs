using mcp_nexus.Constants;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Basic configuration for simple command queue operations
    /// </summary>
    public class BasicQueueConfiguration
    {
        /// <summary>
        /// Gets the cleanup interval for expired commands.
        /// </summary>
        public TimeSpan CleanupInterval { get; }

        /// <summary>
        /// Gets the retention time for completed commands.
        /// </summary>
        public TimeSpan CommandRetentionTime { get; }

        /// <summary>
        /// Gets the interval for logging statistics.
        /// </summary>
        public TimeSpan StatsLogInterval { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicQueueConfiguration"/> class.
        /// </summary>
        /// <param name="cleanupInterval">The cleanup interval for expired commands.</param>
        /// <param name="commandRetentionTime">The retention time for completed commands.</param>
        /// <param name="statsLogInterval">The interval for logging statistics.</param>
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
