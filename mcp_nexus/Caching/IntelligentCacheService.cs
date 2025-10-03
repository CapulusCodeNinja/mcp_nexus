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

        /// <summary>Gets or sets the cached value</summary>
        public TValue Value { get => m_value; set => m_value = value; }

        /// <summary>Gets or sets the creation timestamp</summary>
        public DateTime CreatedAt { get => m_createdAt; set => m_createdAt = value; }

        /// <summary>Gets or sets the last accessed timestamp</summary>
        public DateTime LastAccessed { get => m_lastAccessed; set => m_lastAccessed = value; }

        /// <summary>Gets or sets the expiration timestamp</summary>
        public DateTime ExpiresAt { get => m_expiresAt; set => m_expiresAt = value; }

        /// <summary>Gets or sets the access count</summary>
        public long AccessCount { get => m_accessCount; set => m_accessCount = value; }

        /// <summary>Gets or sets the size in bytes</summary>
        public long SizeBytes { get => m_sizeBytes; set => m_sizeBytes = value; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new cache entry with default values
        /// </summary>
        public CacheEntry()
        {
            m_value = default(TValue)!;
            m_createdAt = DateTime.UtcNow;
            m_lastAccessed = DateTime.UtcNow;
            m_expiresAt = DateTime.UtcNow.AddHours(1);
            m_accessCount = 0;
            m_sizeBytes = 0;
        }

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
        /// Updates the last accessed time and increments access count.
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
        /// Checks if the entry is expired.
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
        /// Estimates the size of a value in bytes using a hybrid approach
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
                    // Primitive types - accurate estimates
                    string str => str.Length * 2, // Unicode characters are 2 bytes
                    byte[] bytes => bytes.Length,
                    int => 4,
                    long => 8,
                    double => 8,
                    float => 4,
                    bool => 1,
                    DateTime => 8,
                    Guid => 16,
                    decimal => 16,
                    char => 2,
                    short => 2,
                    ushort => 2,
                    uint => 4,
                    ulong => 8,
                    sbyte => 1,
                    byte => 1,
                    
                    // Common collection types - optimized handlers (order matters!)
                    Array array => EstimateArraySize(array),
                    System.Collections.IDictionary dictionary => EstimateDictionarySize(dictionary),
                    System.Collections.ICollection collection => EstimateCollectionSize(collection),
                    
                    // Fallback to reflection-based estimation
                    _ => EstimateComplexObjectSize(value)
                };
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error estimating size for value of type {Type}", typeof(TValue).Name);
                return 64; // Default fallback
            }
        }

        /// <summary>
        /// Estimates the size of a collection using optimized calculation
        /// </summary>
        /// <param name="collection">The collection to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateCollectionSize(System.Collections.ICollection collection)
        {
            try
            {
                var baseSize = 24; // Collection object overhead
                var elementSize = 0L;
                var count = collection.Count;
                
                if (count == 0) return baseSize;
                
                // Sample first few elements to estimate element size
                var sampleSize = Math.Min(count, 10);
                var sampleCount = 0;
                
                foreach (var item in collection)
                {
                    if (sampleCount >= sampleSize) break;
                    
                    elementSize += EstimateElementSize(item);
                    sampleCount++;
                }
                
                // Calculate average element size and multiply by count
                var avgElementSize = sampleCount > 0 ? elementSize / sampleCount : 8;
                return baseSize + (avgElementSize * count);
            }
            catch
            {
                // Fallback: estimate based on count
                return 24 + (collection.Count * 8);
            }
        }

        /// <summary>
        /// Estimates the size of a dictionary using optimized calculation
        /// </summary>
        /// <param name="dictionary">The dictionary to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateDictionarySize(System.Collections.IDictionary dictionary)
        {
            try
            {
                var baseSize = 32; // Dictionary object overhead
                var keyValueSize = 0L;
                var count = dictionary.Count;
                
                if (count == 0) return baseSize;
                
                // Sample first few key-value pairs
                var sampleSize = Math.Min(count, 10);
                var sampleCount = 0;
                
                foreach (System.Collections.DictionaryEntry entry in dictionary)
                {
                    if (sampleCount >= sampleSize) break;
                    
                    keyValueSize += EstimateElementSize(entry.Key);
                    keyValueSize += EstimateElementSize(entry.Value);
                    sampleCount++;
                }
                
                // Calculate average key-value size and multiply by count
                var avgKeyValueSize = sampleCount > 0 ? keyValueSize / sampleCount : 16;
                return baseSize + (avgKeyValueSize * count);
            }
            catch
            {
                // Fallback: estimate based on count
                return 32 + (dictionary.Count * 16);
            }
        }

        /// <summary>
        /// Estimates the size of an array using optimized calculation
        /// </summary>
        /// <param name="array">The array to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateArraySize(Array array)
        {
            try
            {
                var baseSize = 24; // Array object overhead
                var elementSize = 0L;
                var length = array.Length;
                
                if (length == 0) return baseSize;
                
                // Sample first few elements
                var sampleSize = Math.Min(length, 10);
                var sampleCount = 0;
                
                for (int i = 0; i < sampleSize; i++)
                {
                    var item = array.GetValue(i);
                    elementSize += EstimateElementSize(item);
                    sampleCount++;
                }
                
                // Calculate average element size and multiply by length
                var avgElementSize = sampleCount > 0 ? elementSize / sampleCount : 8;
                return baseSize + (avgElementSize * length);
            }
            catch
            {
                // Fallback: estimate based on length
                return 24 + (array.Length * 8);
            }
        }

        /// <summary>
        /// Estimates the size of a single element (recursive)
        /// </summary>
        /// <param name="element">The element to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateElementSize(object? element)
        {
            if (element == null) return 0;
            
            return element switch
            {
                string str => str.Length * 2,
                byte[] bytes => bytes.Length,
                int => 4,
                long => 8,
                double => 8,
                float => 4,
                bool => 1,
                DateTime => 8,
                Guid => 16,
                decimal => 16,
                char => 2,
                short => 2,
                ushort => 2,
                uint => 4,
                ulong => 8,
                sbyte => 1,
                byte => 1,
                _ => EstimateComplexObjectSize(element)
            };
        }

        /// <summary>
        /// Estimates the size of a complex object using reflection
        /// </summary>
        /// <param name="obj">The object to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateComplexObjectSize(object obj)
        {
            try
            {
                var type = obj.GetType();
                var totalSize = 0L;
                
                // Object overhead (header + type info)
                totalSize += 24;
                
                // Get all fields (including private ones)
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | 
                                          System.Reflection.BindingFlags.NonPublic | 
                                          System.Reflection.BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    try
                    {
                        var fieldValue = field.GetValue(obj);
                        if (fieldValue != null)
                        {
                            totalSize += EstimateElementSize(fieldValue);
                        }
                    }
                    catch
                    {
                        // Skip fields that can't be accessed
                        totalSize += 8; // Conservative estimate
                    }
                }
                
                // Get all properties
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | 
                                                  System.Reflection.BindingFlags.Instance);
                
                foreach (var property in properties)
                {
                    try
                    {
                        if (property.CanRead && property.GetIndexParameters().Length == 0)
                        {
                            var propertyValue = property.GetValue(obj);
                            if (propertyValue != null)
                            {
                                totalSize += EstimateElementSize(propertyValue);
                            }
                        }
                    }
                    catch
                    {
                        // Skip properties that can't be accessed
                        totalSize += 8; // Conservative estimate
                    }
                }
                
                return totalSize;
            }
            catch
            {
                // Fallback: return a reasonable estimate based on type
                return GetTypeBasedEstimate(obj.GetType());
            }
        }

        /// <summary>
        /// Gets a type-based size estimate as fallback
        /// </summary>
        /// <param name="type">The type to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long GetTypeBasedEstimate(Type type)
        {
            // Return reasonable estimates based on common patterns
            return type.Name switch
            {
                var name when name.Contains("Result") => 512,      // Command results
                var name when name.Contains("Data") => 1024,       // Data objects
                var name when name.Contains("Config") => 256,      // Configuration objects
                var name when name.Contains("Info") => 128,        // Info objects
                var name when name.Contains("Metadata") => 256,    // Metadata objects
                _ => 200 // Generic complex object estimate
            };
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
