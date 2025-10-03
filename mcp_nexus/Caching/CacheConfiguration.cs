namespace mcp_nexus.Caching
{
    /// <summary>
    /// Configuration settings for intelligent cache operations.
    /// Provides configurable parameters for cache behavior, memory management, and cleanup policies.
    /// </summary>
    public class CacheConfiguration
    {
        /// <summary>
        /// Gets the maximum memory usage in bytes.
        /// </summary>
        public long MaxMemoryBytes { get; }

        /// <summary>
        /// Gets the default time-to-live for cache entries.
        /// </summary>
        public TimeSpan DefaultTtl { get; }

        /// <summary>
        /// Gets the cleanup interval for expired entries.
        /// </summary>
        public TimeSpan CleanupInterval { get; }

        /// <summary>
        /// Gets the memory pressure threshold (0.0 to 1.0).
        /// </summary>
        public double MemoryPressureThreshold { get; }

        /// <summary>
        /// Gets the maximum number of entries to clean up per operation.
        /// </summary>
        public int MaxEntriesPerCleanup { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheConfiguration"/> class.
        /// </summary>
        /// <param name="maxMemoryBytes">The maximum memory usage in bytes. Default is 100MB.</param>
        /// <param name="defaultTtl">The default time-to-live for cache entries. Default is 30 minutes.</param>
        /// <param name="cleanupInterval">The cleanup interval for expired entries. Default is 5 minutes.</param>
        /// <param name="memoryPressureThreshold">The memory pressure threshold (0.0 to 1.0). Default is 0.8.</param>
        /// <param name="maxEntriesPerCleanup">The maximum number of entries to clean up per operation. Default is 1000.</param>
        public CacheConfiguration(
            long maxMemoryBytes = 100 * 1024 * 1024, // 100MB default
            TimeSpan? defaultTtl = null,
            TimeSpan? cleanupInterval = null,
            double memoryPressureThreshold = 0.8,
            int maxEntriesPerCleanup = 1000)
        {
            MaxMemoryBytes = maxMemoryBytes;
            DefaultTtl = defaultTtl ?? TimeSpan.FromMinutes(30);
            CleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);
            MemoryPressureThreshold = memoryPressureThreshold;
            MaxEntriesPerCleanup = maxEntriesPerCleanup;
        }

        /// <summary>
        /// Determines if memory pressure cleanup should be triggered.
        /// </summary>
        /// <param name="currentMemoryUsage">The current memory usage in bytes.</param>
        /// <returns>
        /// <c>true</c> if cleanup should be triggered; otherwise, <c>false</c>.
        /// </returns>
        public bool ShouldTriggerCleanup(long currentMemoryUsage)
        {
            return currentMemoryUsage > (MaxMemoryBytes * MemoryPressureThreshold);
        }

        /// <summary>
        /// Calculates the target memory usage after cleanup.
        /// </summary>
        /// <returns>
        /// The target memory usage in bytes (60% of maximum memory).
        /// </returns>
        public long GetTargetMemoryAfterCleanup()
        {
            return (long)(MaxMemoryBytes * 0.6); // Target 60% of max after cleanup
        }
    }
}
