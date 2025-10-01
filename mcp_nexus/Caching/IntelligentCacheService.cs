using System.Collections.Concurrent;

namespace mcp_nexus.Caching
{
    /// <summary>
    /// Cache entry with metadata for intelligent eviction - properly encapsulated
    /// </summary>
    /// <typeparam name="TValue">The type of the cached value</typeparam>
    public class CacheEntry<TValue>
    {
        #region Private Fields

        private TValue m_value;
        private DateTime m_createdAt;
        private DateTime m_lastAccessed;
        private DateTime m_expiresAt;
        private long m_accessCount;
        private long m_sizeBytes;

        #endregion

        #region Public Properties

        /// <summary>Gets the cached value</summary>
        public TValue Value => m_value;

        /// <summary>Gets the creation timestamp</summary>
        public DateTime CreatedAt => m_createdAt;

        /// <summary>Gets the last accessed timestamp</summary>
        public DateTime LastAccessed => m_lastAccessed;

        /// <summary>Gets the expiration timestamp</summary>
        public DateTime ExpiresAt => m_expiresAt;

        /// <summary>Gets the access count</summary>
        public long AccessCount => m_accessCount;

        /// <summary>Gets the size in bytes</summary>
        public long SizeBytes => m_sizeBytes;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new cache entry
        /// </summary>
        /// <param name="value">The value to cache</param>
        /// <param name="createdAt">Creation timestamp</param>
        /// <param name="lastAccessed">Last accessed timestamp</param>
        /// <param name="expiresAt">Expiration timestamp</param>
        /// <param name="accessCount">Access count</param>
        /// <param name="sizeBytes">Size in bytes</param>
        public CacheEntry(TValue value, DateTime createdAt, DateTime lastAccessed, DateTime expiresAt,
            long accessCount, long sizeBytes)
        {
            m_value = value;
            m_createdAt = createdAt;
            m_lastAccessed = lastAccessed;
            m_expiresAt = expiresAt;
            m_accessCount = accessCount;
            m_sizeBytes = sizeBytes;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the last accessed time and increments access count
        /// </summary>
        public void UpdateAccess()
        {
            m_lastAccessed = DateTime.UtcNow;
            m_accessCount++;
        }

        /// <summary>
        /// Updates the value and size
        /// </summary>
        /// <param name="value">New value</param>
        /// <param name="sizeBytes">New size in bytes</param>
        public void UpdateValue(TValue value, long sizeBytes)
        {
            m_value = value;
            m_sizeBytes = sizeBytes;
        }

        /// <summary>
        /// Checks if the entry is expired
        /// </summary>
        /// <returns>True if expired</returns>
        public bool IsExpired()
        {
            return DateTime.UtcNow > m_expiresAt;
        }

        #endregion
    }

    /// <summary>
    /// Refactored intelligent caching service with LRU eviction and memory pressure handling
    /// Uses focused components for better maintainability and testability
    /// </summary>
    /// <typeparam name="TKey">The type of the cache key</typeparam>
    /// <typeparam name="TValue">The type of the cache value</typeparam>
    public class IntelligentCacheService<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly ILogger<IntelligentCacheService<TKey, TValue>> m_logger;
        private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> m_cache = new();
        private volatile bool m_disposed = false;

        // Focused components
        private readonly CacheConfiguration m_config;
        private readonly CacheEvictionManager<TKey, TValue> m_evictionManager;
        private readonly CacheStatisticsCollector<TKey, TValue> m_statisticsCollector;

        /// <summary>
        /// Initializes a new instance of the IntelligentCacheService class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="maxMemoryBytes">Maximum memory usage in bytes (default: 100MB)</param>
        /// <param name="defaultTtl">Default time-to-live for cache entries (default: 30 minutes)</param>
        /// <param name="cleanupInterval">Interval for periodic cleanup (default: 5 minutes)</param>
        public IntelligentCacheService(
            ILogger<IntelligentCacheService<TKey, TValue>> logger,
            long maxMemoryBytes = 100 * 1024 * 1024,
            TimeSpan? defaultTtl = null,
            TimeSpan? cleanupInterval = null)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Create focused components
            m_config = new CacheConfiguration(maxMemoryBytes, defaultTtl, cleanupInterval);
            m_evictionManager = new CacheEvictionManager<TKey, TValue>(logger, m_config, m_cache);
            m_statisticsCollector = new CacheStatisticsCollector<TKey, TValue>(logger, m_config, m_cache);

            m_logger.LogInformation("💾 IntelligentCacheService initialized with focused components - Max: {MaxMB:F1}MB, TTL: {TTL}",
                maxMemoryBytes / (1024.0 * 1024.0), m_config.DefaultTtl);
        }

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
                if (entry.IsExpired())
                {
                    m_cache.TryRemove(key, out _);
                    value = default;
                    m_statisticsCollector.RecordMiss();
                    return false;
                }

                // Update access time for LRU
                entry.UpdateAccess();
                value = entry.Value;

                m_logger.LogTrace("💾 Cache HIT for key: {Key}", key);
                m_statisticsCollector.RecordHit();
                return true;
            }

            m_logger.LogTrace("💾 Cache MISS for key: {Key}", key);
            value = default;
            m_statisticsCollector.RecordMiss();
            return false;
        }

        /// <summary>
        /// Sets a value in the cache
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="value">The value to cache</param>
        /// <param name="ttl">Time-to-live for this entry (optional)</param>
        public void Set(TKey key, TValue value, TimeSpan? ttl = null)
        {
            if (m_disposed) return;

            var now = DateTime.UtcNow;
            var expiresAt = now.Add(ttl ?? m_config.DefaultTtl);
            var sizeBytes = EstimateSize(value);

            var entry = new CacheEntry<TValue>(value, now, now, expiresAt, 0, sizeBytes);

            m_cache.AddOrUpdate(key, entry, (k, existing) => entry);
            m_statisticsCollector.RecordSet();

            // Check memory pressure and evict if necessary
            m_evictionManager.CheckMemoryPressure();

            m_logger.LogTrace("💾 Cached key: {Key}, expires at: {ExpiresAt}", key, expiresAt);
        }

        /// <summary>
        /// Attempts to remove a value from the cache
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <returns>True if the key was found and removed</returns>
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

        /// <summary>
        /// Clears all entries from the cache
        /// </summary>
        public void Clear()
        {
            if (m_disposed) return;

            var count = m_cache.Count;
            m_cache.Clear();
            m_logger.LogInformation("💾 Cleared {Count} cache entries", count);
        }

        /// <summary>
        /// Gets comprehensive cache statistics
        /// </summary>
        /// <returns>Current cache statistics</returns>
        public CacheStatistics GetStatistics()
        {
            if (m_disposed) return new CacheStatistics();

            return m_statisticsCollector.GetStatistics();
        }

        /// <summary>
        /// Forces cleanup of expired entries
        /// </summary>
        /// <returns>Number of entries removed</returns>
        public int ForceCleanup()
        {
            if (m_disposed) return 0;

            var removedCount = m_evictionManager.RemoveExpiredEntries();
            m_logger.LogDebug("💾 Force cleanup removed {Count} expired entries", removedCount);
            return removedCount;
        }

        /// <summary>
        /// Gets the current number of entries in the cache
        /// </summary>
        public int Count => m_disposed ? 0 : m_cache.Count;

        /// <summary>
        /// Checks if the cache contains the specified key
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists and is not expired</returns>
        public bool ContainsKey(TKey key)
        {
            if (m_disposed) return false;

            if (m_cache.TryGetValue(key, out var entry))
            {
                // Check if expired
                if (entry.IsExpired())
                {
                    m_cache.TryRemove(key, out _);
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Estimates the size of a value in bytes
        /// </summary>
        /// <param name="value">The value to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateSize(TValue value)
        {
            try
            {
                if (value == null) return 0;

                return value switch
                {
                    string str => str.Length * 2, // Unicode characters are 2 bytes
                    byte[] bytes => bytes.Length,
                    int => 4,
                    long => 8,
                    double => 8,
                    float => 4,
                    bool => 1,
                    DateTime => 8,
                    Guid => 16,
                    _ => 100 // Default estimate for complex objects
                };
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error estimating size for value of type {Type}", typeof(TValue).Name);
                return 64; // Default fallback
            }
        }

        /// <summary>
        /// Disposes the cache service and all resources
        /// </summary>
        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            try
            {
                m_logger.LogInformation("💾 Shutting down IntelligentCacheService");

                // Log final statistics
                m_statisticsCollector.LogStatisticsSummary();

                // Dispose components
                m_evictionManager?.Dispose();

                // Clear cache
                var count = m_cache.Count;
                m_cache.Clear();

                m_logger.LogInformation("💾 IntelligentCacheService disposed - cleared {Count} entries", count);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error disposing IntelligentCacheService");
            }
        }
    }
}
