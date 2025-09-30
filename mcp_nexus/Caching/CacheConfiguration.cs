namespace mcp_nexus.Caching
{
    /// <summary>
    /// Configuration settings for intelligent cache operations
    /// </summary>
    public class CacheConfiguration
    {
        public long MaxMemoryBytes { get; }
        public TimeSpan DefaultTtl { get; }
        public TimeSpan CleanupInterval { get; }
        public double MemoryPressureThreshold { get; }
        public int MaxEntriesPerCleanup { get; }
        
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
        /// Determines if memory pressure cleanup should be triggered
        /// </summary>
        /// <param name="currentMemoryUsage">Current memory usage in bytes</param>
        /// <returns>True if cleanup should be triggered</returns>
        public bool ShouldTriggerCleanup(long currentMemoryUsage)
        {
            return currentMemoryUsage > (MaxMemoryBytes * MemoryPressureThreshold);
        }
        
        /// <summary>
        /// Calculates the target memory usage after cleanup
        /// </summary>
        /// <returns>Target memory usage in bytes</returns>
        public long GetTargetMemoryAfterCleanup()
        {
            return (long)(MaxMemoryBytes * 0.6); // Target 60% of max after cleanup
        }
    }
}
