using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Session-scoped cache for command results with memory management and LRU eviction
    /// </summary>
    public class SessionCommandResultCache : IDisposable
    {
        #region Private Fields

        private readonly ConcurrentDictionary<string, CachedCommandResult> m_Results;
        private readonly long m_MaxMemoryBytes;
        private readonly int m_MaxResults;
        private readonly double m_MemoryPressureThreshold;
        private readonly ILogger<SessionCommandResultCache>? m_Logger;
        private long m_CurrentMemoryUsage = 0;
        private readonly object m_MemoryLock = new object();
        private volatile bool m_Disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the SessionCommandResultCache class
        /// </summary>
        /// <param name="maxMemoryBytes">Maximum memory usage in bytes (default: 100MB)</param>
        /// <param name="maxResults">Maximum number of results to store (default: 1000)</param>
        /// <param name="memoryPressureThreshold">Memory pressure threshold (0.0 to 1.0, default: 0.8)</param>
        /// <param name="logger">Optional logger for cache operations</param>
        public SessionCommandResultCache(
            long maxMemoryBytes = 100 * 1024 * 1024, // 100MB
            int maxResults = 1000,
            double memoryPressureThreshold = 0.8,
            ILogger<SessionCommandResultCache>? logger = null)
        {
            m_MaxMemoryBytes = maxMemoryBytes;
            m_MaxResults = maxResults;
            m_MemoryPressureThreshold = Math.Clamp(memoryPressureThreshold, 0.1, 1.0);
            m_Logger = logger;
            m_Results = new ConcurrentDictionary<string, CachedCommandResult>();

            m_Logger?.LogDebug("📦 SessionCommandResultCache initialized - MaxMemory: {MaxMB}MB, MaxResults: {MaxResults}, PressureThreshold: {Threshold:P0}",
                maxMemoryBytes / (1024.0 * 1024.0), maxResults, memoryPressureThreshold);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Stores a command result in the cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <param name="result">The command result to store</param>
        public void StoreResult(string commandId, ICommandResult result)
        {
            if (m_Disposed) return;
            if (string.IsNullOrEmpty(commandId)) return;

            try
            {
                var cachedResult = new CachedCommandResult(result, DateTime.UtcNow);
                var resultSize = EstimateResultSize(cachedResult);

                lock (m_MemoryLock)
                {
                    // Check if we need to evict old results due to memory pressure
                    if (ShouldEvictForMemory(resultSize))
                    {
                        EvictOldestResults();
                    }

                    // Store the new result
                    m_Results[commandId] = cachedResult;
                    m_CurrentMemoryUsage += resultSize;

                    m_Logger?.LogTrace("💾 Stored result for command {CommandId} - Size: {SizeKB}KB, TotalMemory: {TotalMB}MB",
                        commandId, resultSize / 1024.0, m_CurrentMemoryUsage / (1024.0 * 1024.0));
                }
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Failed to store result for command {CommandId}", commandId);
            }
        }

        /// <summary>
        /// Retrieves a command result from the cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <returns>The cached command result, or null if not found</returns>
        public ICommandResult? GetResult(string commandId)
        {
            if (m_Disposed) return null;
            if (string.IsNullOrEmpty(commandId)) return null;

            try
            {
                if (m_Results.TryGetValue(commandId, out var cachedResult))
                {
                    // Update access time for LRU tracking
                    cachedResult.UpdateAccessTime();
                    return cachedResult.Result;
                }
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Failed to retrieve result for command {CommandId}", commandId);
            }

            return null;
        }

        /// <summary>
        /// Checks if a command result exists in the cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <returns>True if the result exists, false otherwise</returns>
        public bool HasResult(string commandId)
        {
            if (m_Disposed) return false;
            return !string.IsNullOrEmpty(commandId) && m_Results.ContainsKey(commandId);
        }

        /// <summary>
        /// Removes a specific command result from the cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <returns>True if the result was removed, false if not found</returns>
        public bool RemoveResult(string commandId)
        {
            if (m_Disposed) return false;
            if (string.IsNullOrEmpty(commandId)) return false;

            try
            {
                if (m_Results.TryRemove(commandId, out var removedResult))
                {
                    lock (m_MemoryLock)
                    {
                        m_CurrentMemoryUsage -= EstimateResultSize(removedResult);
                    }

                    m_Logger?.LogTrace("🗑️ Removed result for command {CommandId}", commandId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Failed to remove result for command {CommandId}", commandId);
            }

            return false;
        }

        /// <summary>
        /// Clears all cached results
        /// </summary>
        public void ClearAll()
        {
            if (m_Disposed) return;

            try
            {
                var count = m_Results.Count;
                m_Results.Clear();

                lock (m_MemoryLock)
                {
                    m_CurrentMemoryUsage = 0;
                }

                m_Logger?.LogDebug("🧹 Cleared all {Count} cached results", count);
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Failed to clear all results");
            }
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        /// <returns>Cache statistics</returns>
        public CacheStatistics GetStatistics()
        {
            lock (m_MemoryLock)
            {
                return new CacheStatistics
                {
                    TotalResults = m_Results.Count,
                    CurrentMemoryUsage = m_CurrentMemoryUsage,
                    MaxMemoryBytes = m_MaxMemoryBytes,
                    MaxResults = m_MaxResults,
                    MemoryUsagePercentage = (double)m_CurrentMemoryUsage / m_MaxMemoryBytes * 100.0
                };
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines if memory eviction is needed
        /// </summary>
        /// <param name="newResultSize">Size of the new result being added</param>
        /// <returns>True if eviction is needed</returns>
        private bool ShouldEvictForMemory(long newResultSize)
        {
            return m_CurrentMemoryUsage + newResultSize > m_MaxMemoryBytes * m_MemoryPressureThreshold ||
                   m_Results.Count >= m_MaxResults;
        }

        /// <summary>
        /// Evicts the oldest results to free up memory
        /// </summary>
        private void EvictOldestResults()
        {
            try
            {
                var resultsToEvict = m_Results
                    .OrderBy(kvp => kvp.Value.LastAccessTime) // LRU eviction
                    .Take(Math.Max(1, m_Results.Count / 4)) // Remove 25% of oldest results
                    .ToList();

                var evictedCount = 0;
                var freedMemory = 0L;

                foreach (var kvp in resultsToEvict)
                {
                    if (m_Results.TryRemove(kvp.Key, out var removedResult))
                    {
                        freedMemory += EstimateResultSize(removedResult);
                        evictedCount++;
                    }
                }

                m_CurrentMemoryUsage -= freedMemory;

                m_Logger?.LogDebug("🧹 Evicted {Count} oldest results, freed {FreedMB}MB, current usage: {CurrentMB}MB",
                    evictedCount, freedMemory / (1024.0 * 1024.0), m_CurrentMemoryUsage / (1024.0 * 1024.0));
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Failed to evict oldest results");
            }
        }

        /// <summary>
        /// Estimates the memory size of a cached command result
        /// </summary>
        /// <param name="cachedResult">The cached result to estimate</param>
        /// <returns>Estimated size in bytes</returns>
        private long EstimateResultSize(CachedCommandResult cachedResult)
        {
            try
            {
                var result = cachedResult.Result;
                var baseSize = 100; // Base overhead for the object
                var outputSize = result.Output?.Length * 2 ?? 0; // UTF-16 encoding
                var errorSize = result.ErrorMessage?.Length * 2 ?? 0;
                var dataSize = result.Data?.Count * 50 ?? 0; // Rough estimate for dictionary overhead
                
                return baseSize + outputSize + errorSize + dataSize;
            }
            catch
            {
                return 1024; // Fallback estimate
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes the cache and clears all results
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed) return;

            try
            {
                ClearAll();
                m_Disposed = true;
                m_Logger?.LogDebug("♻️ SessionCommandResultCache disposed");
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Error disposing SessionCommandResultCache");
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a cached command result with access tracking
    /// </summary>
    internal class CachedCommandResult
    {
        public ICommandResult Result { get; }
        public DateTime CreatedTime { get; }
        public DateTime LastAccessTime { get; private set; }

        public CachedCommandResult(ICommandResult result, DateTime createdTime)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            CreatedTime = createdTime;
            LastAccessTime = createdTime;
        }

        public void UpdateAccessTime()
        {
            LastAccessTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStatistics
    {
        public int TotalResults { get; set; }
        public long CurrentMemoryUsage { get; set; }
        public long MaxMemoryBytes { get; set; }
        public int MaxResults { get; set; }
        public double MemoryUsagePercentage { get; set; }
    }
}
