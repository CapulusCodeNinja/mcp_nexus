using System.Collections.Concurrent;

namespace mcp_nexus.Caching
{
    /// <summary>
    /// Statistics about cache performance and usage.
    /// Contains comprehensive metrics for monitoring cache behavior and efficiency.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Gets or sets the total number of cache entries.
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Gets or sets the number of expired entries.
        /// </summary>
        public int ExpiredEntries { get; set; }

        /// <summary>
        /// Gets or sets the total size of all cache entries in bytes.
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the total number of cache accesses.
        /// </summary>
        public long TotalAccesses { get; set; }

        /// <summary>
        /// Gets or sets the average access count per entry.
        /// </summary>
        public double AverageAccessCount { get; set; }

        /// <summary>
        /// Gets or sets the cache hit ratio (0.0 to 1.0).
        /// </summary>
        public double HitRatio { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the oldest entry.
        /// </summary>
        public DateTime OldestEntry { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the newest entry.
        /// </summary>
        public DateTime NewestEntry { get; set; }

        /// <summary>
        /// Gets or sets the average age of cache entries.
        /// </summary>
        public TimeSpan AverageAge { get; set; }

        /// <summary>
        /// Gets or sets the memory pressure in bytes (excess over limit).
        /// </summary>
        public long MemoryPressureBytes { get; set; }

        /// <summary>
        /// Gets or sets the memory utilization percentage (0.0 to 100.0).
        /// </summary>
        public double MemoryUtilizationPercent { get; set; }

        /// <summary>
        /// Gets or sets the memory usage percentage (0.0 to 100.0).
        /// </summary>
        public double MemoryUsagePercent { get; set; }

        // Additional properties for test compatibility
        /// <summary>
        /// Gets or sets the total number of cache hits.
        /// </summary>
        public long HitCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of cache misses.
        /// </summary>
        public long MissCount { get; set; }

        /// <summary>
        /// Gets or sets the cache hit rate (0.0 to 1.0).
        /// </summary>
        public double HitRate { get; set; }

        /// <summary>
        /// Gets or sets the total number of evictions.
        /// </summary>
        public long EvictionCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of expirations.
        /// </summary>
        public long ExpirationCount { get; set; }
    }

    /// <summary>
    /// Collects and provides statistics about cache performance and usage.
    /// Monitors cache operations, memory usage, and performance metrics.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public class CacheStatisticsCollector<TKey, TValue> where TKey : notnull
    {
        private readonly ILogger m_logger;
        private readonly CacheConfiguration m_config;
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> m_cache;

        // Performance counters
        private long m_totalHits = 0;
        private long m_totalMisses = 0;
        private long m_totalSets = 0;
        private long m_totalEvictions = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheStatisticsCollector{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording statistics and errors.</param>
        /// <param name="config">The cache configuration settings.</param>
        /// <param name="cache">The thread-safe cache dictionary to monitor.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public CacheStatisticsCollector(
            ILogger logger,
            CacheConfiguration config,
            ConcurrentDictionary<TKey, CacheEntry<TValue>> cache)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Records a cache hit.
        /// This method increments the hit counter in a thread-safe manner.
        /// </summary>
        public void RecordHit()
        {
            Interlocked.Increment(ref m_totalHits);
        }

        /// <summary>
        /// Records a cache miss.
        /// This method increments the miss counter in a thread-safe manner.
        /// </summary>
        public void RecordMiss()
        {
            Interlocked.Increment(ref m_totalMisses);
        }

        /// <summary>
        /// Records a cache set operation.
        /// This method increments the set counter in a thread-safe manner.
        /// </summary>
        public void RecordSet()
        {
            Interlocked.Increment(ref m_totalSets);
        }

        /// <summary>
        /// Records cache evictions.
        /// This method adds the specified count to the eviction counter in a thread-safe manner.
        /// </summary>
        /// <param name="count">The number of entries evicted.</param>
        public void RecordEvictions(int count)
        {
            Interlocked.Add(ref m_totalEvictions, count);
        }

        /// <summary>
        /// Gets comprehensive cache statistics.
        /// This method calculates and returns detailed statistics about cache performance and usage.
        /// </summary>
        /// <returns>
        /// A <see cref="CacheStatistics"/> object containing current cache statistics.
        /// </returns>
        public CacheStatistics GetStatistics()
        {
            try
            {
                var now = DateTime.UtcNow;
                var entries = m_cache.Values.ToList();

                if (entries.Count == 0)
                {
                    return new CacheStatistics
                    {
                        TotalEntries = 0,
                        HitRatio = 0.0,
                        MemoryUtilizationPercent = 0.0,
                        MemoryUsagePercent = 0.0
                    };
                }

                var totalSize = entries.Sum(e => e.SizeBytes);
                var expiredCount = entries.Count(e => now > e.ExpiresAt);
                var totalAccesses = entries.Sum(e => e.AccessCount);
                var avgAccessCount = (double)totalAccesses / entries.Count;

                var oldestEntry = entries.Min(e => e.CreatedAt);
                var newestEntry = entries.Max(e => e.CreatedAt);
                var avgAge = TimeSpan.FromTicks((long)entries.Average(e => (now - e.CreatedAt).Ticks));

                var totalOperations = Interlocked.Read(ref m_totalHits) + Interlocked.Read(ref m_totalMisses);
                var hitRatio = totalOperations > 0 ? (double)Interlocked.Read(ref m_totalHits) / totalOperations : 0.0;

                var memoryUtilization = m_config.MaxMemoryBytes > 0 ?
                    (double)totalSize / m_config.MaxMemoryBytes * 100.0 : 0.0;

                return new CacheStatistics
                {
                    TotalEntries = m_cache.Count,
                    ExpiredEntries = expiredCount,
                    TotalSizeBytes = totalSize,
                    TotalAccesses = totalAccesses,
                    AverageAccessCount = avgAccessCount,
                    HitRatio = hitRatio,
                    OldestEntry = oldestEntry,
                    NewestEntry = newestEntry,
                    AverageAge = avgAge,
                    MemoryPressureBytes = Math.Max(0, totalSize - m_config.MaxMemoryBytes),
                    MemoryUtilizationPercent = memoryUtilization,
                    MemoryUsagePercent = memoryUtilization
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error calculating cache statistics");
                return new CacheStatistics();
            }
        }

        /// <summary>
        /// Gets performance counters.
        /// This method returns the current values of all performance counters in a thread-safe manner.
        /// </summary>
        /// <returns>
        /// A tuple containing the current hit count, miss count, set count, and eviction count.
        /// </returns>
        public (long Hits, long Misses, long Sets, long Evictions) GetPerformanceCounters()
        {
            return (
                Interlocked.Read(ref m_totalHits),
                Interlocked.Read(ref m_totalMisses),
                Interlocked.Read(ref m_totalSets),
                Interlocked.Read(ref m_totalEvictions)
            );
        }

        /// <summary>
        /// Logs a summary of cache statistics.
        /// This method writes detailed cache statistics to the logger for monitoring and debugging.
        /// </summary>
        public void LogStatisticsSummary()
        {
            try
            {
                var stats = GetStatistics();
                var counters = GetPerformanceCounters();

                m_logger.LogInformation("💾 Cache Statistics Summary:");
                m_logger.LogInformation("   Entries: {Total} ({Expired} expired)", stats.TotalEntries, stats.ExpiredEntries);
                m_logger.LogInformation("   Memory: {MemoryMB:F1}MB ({Utilization:F1}% of {MaxMB:F1}MB)",
                    stats.TotalSizeBytes / (1024.0 * 1024.0),
                    stats.MemoryUtilizationPercent,
                    m_config.MaxMemoryBytes / (1024.0 * 1024.0));
                m_logger.LogInformation("   Performance: {Hits} hits, {Misses} misses (hit ratio: {HitRatio:P1})",
                    counters.Hits, counters.Misses, stats.HitRatio);
                m_logger.LogInformation("   Operations: {Sets} sets, {Evictions} evictions",
                    counters.Sets, counters.Evictions);
                m_logger.LogInformation("   Access: {TotalAccesses} total, {AvgAccess:F1} avg per entry",
                    stats.TotalAccesses, stats.AverageAccessCount);
                m_logger.LogInformation("   Age: {AvgAge:hh\\:mm\\:ss} average, {OldestAge:hh\\:mm\\:ss} oldest",
                    stats.AverageAge, DateTime.UtcNow - stats.OldestEntry);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error logging cache statistics summary");
            }
        }

        /// <summary>
        /// Resets performance counters.
        /// This method resets all performance counters to zero in a thread-safe manner.
        /// </summary>
        public void ResetCounters()
        {
            Interlocked.Exchange(ref m_totalHits, 0);
            Interlocked.Exchange(ref m_totalMisses, 0);
            Interlocked.Exchange(ref m_totalSets, 0);
            Interlocked.Exchange(ref m_totalEvictions, 0);

            m_logger.LogDebug("💾 Cache performance counters reset");
        }
    }
}
