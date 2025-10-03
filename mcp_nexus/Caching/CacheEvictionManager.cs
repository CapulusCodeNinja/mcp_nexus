using System.Collections.Concurrent;

namespace mcp_nexus.Caching
{
    /// <summary>
    /// Manages cache eviction policies including LRU and memory pressure handling.
    /// Provides intelligent cache eviction based on memory usage and access patterns.
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key.</typeparam>
    /// <typeparam name="TValue">The type of the cache value.</typeparam>
    public class CacheEvictionManager<TKey, TValue> where TKey : notnull
    {
        private readonly ILogger m_logger;
        private readonly CacheConfiguration m_config;
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> m_cache;
        private readonly Timer m_cleanupTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheEvictionManager{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording eviction operations and errors.</param>
        /// <param name="config">The cache configuration settings.</param>
        /// <param name="cache">The thread-safe cache dictionary to manage.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public CacheEvictionManager(
            ILogger logger,
            CacheConfiguration config,
            ConcurrentDictionary<TKey, CacheEntry<TValue>> cache)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
            m_cache = cache ?? throw new ArgumentNullException(nameof(cache));

            // Start periodic cleanup timer
            m_cleanupTimer = new Timer(PeriodicCleanup, null, m_config.CleanupInterval, m_config.CleanupInterval);
        }

        /// <summary>
        /// Checks memory pressure and triggers eviction if necessary.
        /// This method monitors current memory usage and initiates cleanup when thresholds are exceeded.
        /// </summary>
        public void CheckMemoryPressure()
        {
            try
            {
                var currentMemoryUsage = CalculateCurrentMemoryUsage();

                if (m_config.ShouldTriggerCleanup(currentMemoryUsage))
                {
                    m_logger.LogDebug("💾 Memory pressure detected: {CurrentMB:F1}MB / {MaxMB:F1}MB - triggering eviction",
                        currentMemoryUsage / (1024.0 * 1024.0),
                        m_config.MaxMemoryBytes / (1024.0 * 1024.0));

                    EvictLeastRecentlyUsed(m_config.GetTargetMemoryAfterCleanup());
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during memory pressure check");
            }
        }

        /// <summary>
        /// Removes expired entries from the cache.
        /// This method identifies and removes entries that have exceeded their time-to-live.
        /// </summary>
        /// <returns>
        /// <summary>
        /// Removes expired entries from the cache.
        /// </summary>
        /// <returns>
        /// The number of entries removed.
        /// </returns>
        public int RemoveExpiredEntries()
        {
            var removedCount = 0;
            var now = DateTime.UtcNow;
            var keysToRemove = new List<TKey>();

            try
            {
                // Find expired entries
                foreach (var kvp in m_cache)
                {
                    if (now > kvp.Value.ExpiresAt)
                    {
                        keysToRemove.Add(kvp.Key);
                    }

                    // Limit the number of entries processed in one cleanup cycle
                    if (keysToRemove.Count >= m_config.MaxEntriesPerCleanup)
                        break;
                }

                // Remove expired entries
                foreach (var key in keysToRemove)
                {
                    if (m_cache.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    m_logger.LogDebug("💾 Removed {Count} expired cache entries", removedCount);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error removing expired entries");
            }

            return removedCount;
        }

        /// <summary>
        /// Evicts least recently used entries until target memory is reached.
        /// This method implements LRU eviction policy based on last accessed time and access count.
        /// </summary>
        /// <param name="targetMemoryBytes">The target memory usage in bytes.</param>
        /// <returns>
        /// <summary>
        /// Evicts the least recently used entries to free up memory.
        /// </summary>
        /// <param name="targetMemoryBytes">The target memory usage in bytes.</param>
        /// <returns>
        /// The number of entries evicted.
        /// </returns>
        public int EvictLeastRecentlyUsed(long targetMemoryBytes)
        {
            var evictedCount = 0;

            try
            {
                var currentMemory = CalculateCurrentMemoryUsage();
                if (currentMemory <= targetMemoryBytes)
                    return 0;

                // Get entries sorted by last accessed time (LRU first)
                var entriesByLru = m_cache.ToList()
                    .OrderBy(kvp => kvp.Value.LastAccessed)
                    .ThenBy(kvp => kvp.Value.AccessCount) // Secondary sort by access count
                    .ToList();

                foreach (var kvp in entriesByLru)
                {
                    if (m_cache.TryRemove(kvp.Key, out _))
                    {
                        evictedCount++;
                        currentMemory -= kvp.Value.SizeBytes;

                        if (currentMemory <= targetMemoryBytes)
                            break;
                    }

                    // Safety limit to prevent excessive eviction
                    if (evictedCount >= m_config.MaxEntriesPerCleanup)
                        break;
                }

                if (evictedCount > 0)
                {
                    m_logger.LogInformation("💾 Evicted {Count} LRU entries, memory reduced to {MemoryMB:F1}MB",
                        evictedCount, currentMemory / (1024.0 * 1024.0));
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during LRU eviction");
            }

            return evictedCount;
        }

        /// <summary>
        /// Calculates the current memory usage of the cache.
        /// </summary>
        /// <returns>
        /// <summary>
        /// Calculates the current memory usage of the cache.
        /// </summary>
        /// <returns>
        /// The current memory usage in bytes.
        /// </returns>
        private long CalculateCurrentMemoryUsage()
        {
            try
            {
                return m_cache.Values.Sum(entry => entry.SizeBytes);
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error calculating memory usage");
                return 0;
            }
        }

        /// <summary>
        /// Periodic cleanup callback.
        /// This method is called by the timer to perform regular cache maintenance.
        /// </summary>
        /// <param name="state">The timer state (unused).</param>
        private void PeriodicCleanup(object? state)
        {
            try
            {
                var expiredRemoved = RemoveExpiredEntries();

                // Check memory pressure after removing expired entries
                CheckMemoryPressure();

                // Log cleanup summary if any work was done
                if (expiredRemoved > 0)
                {
                    var currentMemory = CalculateCurrentMemoryUsage();
                    m_logger.LogTrace("💾 Periodic cleanup: {ExpiredRemoved} expired entries removed, {MemoryMB:F1}MB in use",
                        expiredRemoved, currentMemory / (1024.0 * 1024.0));
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error during periodic cleanup");
            }
        }

        /// <summary>
        /// Disposes the eviction manager and cleans up resources.
        /// This method stops the periodic cleanup timer and releases associated resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                m_cleanupTimer?.Dispose();
                m_logger.LogDebug("💾 Cache eviction manager disposed");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error disposing cache eviction manager");
            }
        }
    }
}
