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
        /// Gets the maximum memory usage for command results per session in bytes.
        /// </summary>
        public long MaxCommandMemoryBytes { get; }

        /// <summary>
        /// Gets the maximum number of command results to store per session.
        /// </summary>
        public int MaxCommandsInMemory { get; }

        /// <summary>
        /// Gets the memory pressure threshold (0.0 to 1.0) for triggering cleanup.
        /// </summary>
        public double MemoryPressureThreshold { get; }

        /// <summary>
        /// Gets whether memory optimization is enabled.
        /// </summary>
        public bool EnableMemoryOptimization { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicQueueConfiguration"/> class.
        /// </summary>
        /// <param name="cleanupInterval">The cleanup interval for expired commands.</param>
        /// <param name="commandRetentionTime">The retention time for completed commands.</param>
        /// <param name="statsLogInterval">The interval for logging statistics.</param>
        /// <param name="maxCommandMemoryBytes">Maximum memory usage for command results per session in bytes.</param>
        /// <param name="maxCommandsInMemory">Maximum number of command results to store per session.</param>
        /// <param name="memoryPressureThreshold">Memory pressure threshold (0.0 to 1.0) for triggering cleanup.</param>
        /// <param name="enableMemoryOptimization">Whether memory optimization is enabled.</param>
        public BasicQueueConfiguration(
            TimeSpan? cleanupInterval = null,
            TimeSpan? commandRetentionTime = null,
            TimeSpan? statsLogInterval = null,
            long? maxCommandMemoryBytes = null,
            int? maxCommandsInMemory = null,
            double? memoryPressureThreshold = null,
            bool? enableMemoryOptimization = null)
        {
            CleanupInterval = cleanupInterval ?? ApplicationConstants.CleanupInterval;
            CommandRetentionTime = commandRetentionTime ?? ApplicationConstants.CommandRetentionTime;
            StatsLogInterval = statsLogInterval ?? TimeSpan.FromMinutes(5);
            MaxCommandMemoryBytes = maxCommandMemoryBytes ?? 100 * 1024 * 1024; // 100MB default
            MaxCommandsInMemory = maxCommandsInMemory ?? 1000;
            MemoryPressureThreshold = memoryPressureThreshold ?? 0.8;
            EnableMemoryOptimization = enableMemoryOptimization ?? true;
        }
    }
}
