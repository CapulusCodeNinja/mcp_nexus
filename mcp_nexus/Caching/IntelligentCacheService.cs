using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Caching
{
    /// <summary>
    /// Intelligent caching service with LRU eviction and memory pressure handling
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key</typeparam>
    /// <typeparam name="TValue">The type of the cache value</typeparam>
    public class IntelligentCacheService<TKey, TValue> : IDisposable where TKey : notnull
    {
        #region Private Fields

        private readonly ILogger<IntelligentCacheService<TKey, TValue>> m_logger;
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> m_cache = new();
        private readonly Timer m_cleanupTimer;
        private readonly long m_maxMemoryBytes;
        private readonly TimeSpan m_defaultTtl;
        private volatile bool m_disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the IntelligentCacheService class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="maxMemoryBytes">Maximum memory usage in bytes (default: 100MB)</param>
        /// <param name="defaultTtl">Default time-to-live for cache entries (default: 30 minutes)</param>
        public IntelligentCacheService(ILogger<IntelligentCacheService<TKey, TValue>> logger,
            long maxMemoryBytes = 100 * 1024 * 1024, // 100MB default
            TimeSpan? defaultTtl = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_maxMemoryBytes = maxMemoryBytes;
            m_defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(30);

            // Cleanup expired entries every 5 minutes
            m_cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            m_logger.LogInformation("💾 IntelligentCacheService initialized with {MaxMemory}MB limit", maxMemoryBytes / (1024 * 1024));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to get a value from the cache
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="value">The cached value, if found</param>
        /// <returns>True if the value was found and not expired</returns>
        public bool TryGet(TKey key, out TValue? value)
        {
            if (m_disposed)
            {
                value = default;
                return false;
            }

            if (m_cache.TryGetValue(key, out var entry))
            {
                // Check if expired
                if (DateTime.UtcNow > entry.ExpiresAt)
                {
                    m_cache.TryRemove(key, out _);
                    value = default;
                    return false;
                }

                // Update access time for LRU
                entry.LastAccessed = DateTime.UtcNow;
                entry.AccessCount++;
                value = entry.Value;

                m_logger.LogTrace("💾 Cache HIT for key: {Key}", key);
                return true;
            }

            m_logger.LogTrace("💾 Cache MISS for key: {Key}", key);
            value = default;
            return false;
        }

        public void Set(TKey key, TValue value, TimeSpan? ttl = null)
        {
            if (m_disposed) return;

            var expiresAt = DateTime.UtcNow.Add(ttl ?? m_defaultTtl);
            var entry = new CacheEntry<TValue>
            {
                Value = value,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                AccessCount = 0,
                SizeBytes = EstimateSize(value)
            };

            m_cache.AddOrUpdate(key, entry, (k, existing) => entry);

            // Check memory pressure and evict if necessary
            CheckMemoryPressure();

            m_logger.LogTrace("💾 Cached key: {Key}, expires at: {ExpiresAt}", key, expiresAt);
        }

        public bool TryRemove(TKey key)
        {
            if (m_disposed) return false;

            var removed = m_cache.TryRemove(key, out _);
            if (removed)
            {
                m_logger.LogTrace("💾 Removed key: {Key}", key);
            }
            return removed;
        }

        public void Clear()
        {
            if (m_disposed) return;

            var count = m_cache.Count;
            m_cache.Clear();
            m_logger.LogInformation("💾 Cleared {Count} cache entries", count);
        }

        public CacheStatistics GetStatistics()
        {
            if (m_disposed) return new CacheStatistics();

            var now = DateTime.UtcNow;
            var entries = m_cache.Values.ToList();

            var totalSize = entries.Sum(e => e.SizeBytes);
            var expiredCount = entries.Count(e => now > e.ExpiresAt);
            var totalAccesses = entries.Sum(e => e.AccessCount);
            var avgAccessCount = entries.Count > 0 ? (double)totalAccesses / entries.Count : 0;

            return new CacheStatistics
            {
                TotalEntries = m_cache.Count,
                ExpiredEntries = expiredCount,
                TotalSizeBytes = totalSize,
                TotalAccesses = totalAccesses,
                AverageAccessCount = avgAccessCount,
                MemoryUsagePercent = (double)totalSize / m_maxMemoryBytes * 100
            };
        }

        private void CheckMemoryPressure()
        {
            var stats = GetStatistics();

            if (stats.MemoryUsagePercent > 90)
            {
                m_logger.LogWarning("💾 High memory usage: {Percent:F1}%, evicting least recently used entries", stats.MemoryUsagePercent);
                EvictLeastRecentlyUsed(0.2); // Evict 20% of entries
            }
            else if (stats.MemoryUsagePercent > 80)
            {
                m_logger.LogInformation("💾 Memory usage: {Percent:F1}%, evicting least recently used entries", stats.MemoryUsagePercent);
                EvictLeastRecentlyUsed(0.1); // Evict 10% of entries
            }
        }

        private void EvictLeastRecentlyUsed(double percentage)
        {
            var entriesToEvict = (int)(m_cache.Count * percentage);
            if (entriesToEvict <= 0) return;

            var sortedEntries = m_cache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .ThenBy(kvp => kvp.Value.AccessCount)
                .Take(entriesToEvict)
                .ToList();

            foreach (var kvp in sortedEntries)
            {
                m_cache.TryRemove(kvp.Key, out _);
            }

            m_logger.LogInformation("💾 Evicted {Count} least recently used entries", entriesToEvict);
        }

        private void CleanupExpiredEntries(object? state)
        {
            if (m_disposed) return;

            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = m_cache
                    .Where(kvp => now > kvp.Value.ExpiresAt)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    m_cache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    m_logger.LogDebug("💾 Cleaned up {Count} expired cache entries", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error cleaning up expired cache entries");
            }
        }

        private static long EstimateSize(TValue value)
        {
            if (value == null) return 0;

            // Simple size estimation - in production, you might want more sophisticated estimation
            return value switch
            {
                string str => str.Length * 2, // UTF-16 characters
                byte[] bytes => bytes.Length,
                _ => 100 // Default estimate for complex objects
            };
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the cache service
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            m_cleanupTimer?.Dispose();
            m_cache.Clear();
            m_logger.LogInformation("💾 IntelligentCacheService disposed");
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Represents a cache entry
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    public class CacheEntry<T>
    {
        /// <summary>
        /// The cached value
        /// </summary>
        public T Value { get; set; } = default!;

        /// <summary>
        /// When the entry was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the entry was last accessed
        /// </summary>
        public DateTime LastAccessed { get; set; }

        /// <summary>
        /// When the entry expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Number of times the entry has been accessed
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Estimated size of the entry in bytes
        /// </summary>
        public long SizeBytes { get; set; }
    }

    /// <summary>
    /// Represents cache statistics
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Total number of entries in the cache
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Number of expired entries
        /// </summary>
        public int ExpiredEntries { get; set; }

        /// <summary>
        /// Total size of all entries in bytes
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Total number of cache accesses
        /// </summary>
        public int TotalAccesses { get; set; }

        /// <summary>
        /// Average number of accesses per entry
        /// </summary>
        public double AverageAccessCount { get; set; }

        /// <summary>
        /// Memory usage as a percentage of the maximum
        /// </summary>
        public double MemoryUsagePercent { get; set; }
    }

    #endregion
}
