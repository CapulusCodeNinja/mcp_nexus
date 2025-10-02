using System.Collections.Concurrent;

namespace mcp_nexus.Caching
{
    /// <summary>
    /// Statistics about cache performance
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public long TotalSizeBytes { get; set; }
        public long TotalAccesses { get; set; }
        public double AverageAccessCount { get; set; }
        public double HitRatio { get; set; }
        public DateTime OldestEntry { get; set; }
        public DateTime NewestEntry { get; set; }
        public TimeSpan AverageAge { get; set; }
        public long MemoryPressureBytes { get; set; }
        public double MemoryUtilizationPercent { get; set; }
        public double MemoryUsagePercent { get; set; }

        // Additional properties for test compatibility
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRate { get; set; }
        public long EvictionCount { get; set; }
        public long ExpirationCount { get; set; }
    }

    /// <summary>
    /// Collects and provides statistics about cache performance and usage
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key</typeparam>
    /// <typeparam name="TValue">The type of the cache value</typeparam>
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
        /// Records a cache hit
        /// </summary>
        public void RecordHit()
        {
            Interlocked.Increment(ref m_totalHits);
        }

        /// <summary>
        /// Records a cache miss
        /// </summary>
        public void RecordMiss()
        {
            Interlocked.Increment(ref m_totalMisses);
        }

        /// <summary>
        /// Records a cache set operation
        /// </summary>
        public void RecordSet()
        {
            Interlocked.Increment(ref m_totalSets);
        }

        /// <summary>
        /// Records cache evictions
        /// </summary>
        /// <param name="count">Number of entries evicted</param>
        public void RecordEvictions(int count)
        {
            Interlocked.Add(ref m_totalEvictions, count);
        }

        /// <summary>
        /// Gets comprehensive cache statistics
        /// </summary>
        /// <returns>Current cache statistics</returns>
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
        /// Gets performance counters
        /// </summary>
        /// <returns>Performance counter values</returns>
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
        /// Logs a summary of cache statistics
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
        /// Resets performance counters
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
