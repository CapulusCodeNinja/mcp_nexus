using mcp_nexus.Constants;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Basic configuration for simple command queue operations
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="BasicQueueConfiguration"/> class.
    /// </remarks>
    /// <param name="cleanupInterval">The cleanup interval for expired commands.</param>
    /// <param name="commandRetentionTime">The retention time for completed commands.</param>
    /// <param name="statsLogInterval">The interval for logging statistics.</param>
    /// <param name="maxCommandMemoryBytes">Maximum memory usage for command results per session in bytes.</param>
    /// <param name="maxCommandsInMemory">Maximum number of command results to store per session.</param>
    /// <param name="memoryPressureThreshold">Memory pressure threshold (0.0 to 1.0) for triggering cleanup.</param>
    /// <param name="enableMemoryOptimization">Whether memory optimization is enabled.</param>
    public class BasicQueueConfiguration(
        TimeSpan? cleanupInterval = null,
        TimeSpan? commandRetentionTime = null,
        TimeSpan? statsLogInterval = null,
        long? maxCommandMemoryBytes = null,
        int? maxCommandsInMemory = null,
        double? memoryPressureThreshold = null,
        bool? enableMemoryOptimization = null)
    {
        /// <summary>
        /// Gets the cleanup interval for expired commands.
        /// </summary>
        public TimeSpan CleanupInterval { get; } = cleanupInterval ?? ApplicationConstants.CleanupInterval;

        /// <summary>
        /// Gets the retention time for completed commands.
        /// </summary>
        public TimeSpan CommandRetentionTime { get; } = commandRetentionTime ?? ApplicationConstants.CommandRetentionTime;

        /// <summary>
        /// Gets the interval for logging statistics.
        /// </summary>
        public TimeSpan StatsLogInterval { get; } = statsLogInterval ?? TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the maximum memory usage for command results per session in bytes.
        /// </summary>
        public long MaxCommandMemoryBytes { get; } = maxCommandMemoryBytes ?? 100 * 1024 * 1024; // 100MB default

        /// <summary>
        /// Gets the maximum number of command results to store per session.
        /// </summary>
        public int MaxCommandsInMemory { get; } = maxCommandsInMemory ?? 1000;

        /// <summary>
        /// Gets the memory pressure threshold (0.0 to 1.0) for triggering cleanup.
        /// </summary>
        public double MemoryPressureThreshold { get; } = memoryPressureThreshold ?? 0.8;

        /// <summary>
        /// Gets whether memory optimization is enabled.
        /// </summary>
        public bool EnableMemoryOptimization { get; } = enableMemoryOptimization ?? true;
    }
}
