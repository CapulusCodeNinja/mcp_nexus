using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.CommandQueue.Core
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
        private readonly object m_MemoryLock = new();
        private volatile bool m_Disposed = false;

        // Providers for testability
        private readonly IMemoryPressureProvider m_MemoryPressureProvider;
        private readonly IProcessMemoryProvider m_ProcessMemoryProvider;

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
            ILogger<SessionCommandResultCache>? logger = null,
            IMemoryPressureProvider? memoryPressureProvider = null,
            IProcessMemoryProvider? processMemoryProvider = null)
        {
            m_MaxMemoryBytes = maxMemoryBytes;
            m_MaxResults = maxResults;
            m_MemoryPressureThreshold = Math.Clamp(memoryPressureThreshold, 0.1, 1.0);
            m_Logger = logger;
            m_Results = new ConcurrentDictionary<string, CachedCommandResult>();
            m_MemoryPressureProvider = memoryPressureProvider ?? new SystemMemoryPressureProvider();
            m_ProcessMemoryProvider = processMemoryProvider ?? new DefaultProcessMemoryProvider();

            m_Logger?.LogDebug("📦 SessionCommandResultCache initialized - MaxMemory: {MaxMB}MB, MaxResults: {MaxResults}, PressureThreshold: {Threshold:P0}",
                maxMemoryBytes / (1024.0 * 1024.0), maxResults, memoryPressureThreshold);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Stores a command result in the cache with complete metadata
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <param name="result">The command result to store</param>
        /// <param name="originalCommand">The original command text</param>
        /// <param name="queueTime">When the command was queued</param>
        /// <param name="startTime">When the command started executing</param>
        /// <param name="endTime">When the command finished</param>
        public void StoreResult(string commandId, ICommandResult result,
            string? originalCommand = null, DateTime? queueTime = null,
            DateTime? startTime = null, DateTime? endTime = null)
        {
            if (m_Disposed) return;
            if (string.IsNullOrEmpty(commandId)) return;

            try
            {
                var cachedResult = new CachedCommandResult(result, DateTime.Now,
                    originalCommand, queueTime, startTime, endTime);
                var resultSize = EstimateResultSize(cachedResult);

                lock (m_MemoryLock)
                {
                    // Check if we need to evict old results due to memory pressure
                    if (ShouldEvictForMemory(resultSize))
                    {
                        EvictOldestResults();
                    }

                    // Store the new result with metadata
                    m_Results[commandId] = cachedResult;
                    m_CurrentMemoryUsage += resultSize;

                    m_Logger?.LogDebug("Cache StoreResult: Stored command {CommandId}, Output length: {Length}, Output: '{Output}' - Size: {SizeKB}KB, TotalMemory: {TotalMB}MB",
                        commandId, result.Output?.Length ?? 0, result.Output, resultSize / 1024.0, m_CurrentMemoryUsage / (1024.0 * 1024.0));
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
                    m_Logger?.LogDebug("Cache GetResult: Found command {CommandId}, Output length: {Length}, Output: '{Output}'",
                        commandId, cachedResult.Result.Output?.Length ?? 0, cachedResult.Result.Output);
                    return cachedResult.Result;
                }
                else
                {
                    m_Logger?.LogDebug("Cache GetResult: Command {CommandId} not found in cache", commandId);
                }
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Failed to retrieve result for command {CommandId}", commandId);
            }

            return null;
        }

        /// <summary>
        /// Retrieves a cached result with metadata from the cache
        /// </summary>
        /// <param name="commandId">The command identifier</param>
        /// <returns>The cached result with metadata, or null if not found</returns>
        public CachedCommandResult? GetCachedResultWithMetadata(string commandId)
        {
            if (m_Disposed) return null;
            if (string.IsNullOrEmpty(commandId)) return null;

            try
            {
                if (m_Results.TryGetValue(commandId, out var cachedResult))
                {
                    // Update access time for LRU tracking
                    cachedResult.UpdateAccessTime();
                    return cachedResult;
                }
            }
            catch (Exception ex)
            {
                m_Logger?.LogError(ex, "Failed to retrieve result with metadata for command {CommandId}", commandId);
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
            // Adaptive: system/process pressure first
            try
            {
                var gcHigh = m_MemoryPressureProvider.HighMemoryLoadThresholdBytes;
                if (gcHigh > 0)
                {
                    var systemPressure = m_MemoryPressureProvider.MemoryLoadBytes > gcHigh * 0.85;
                    if (systemPressure)
                        return true;

                    var privateBytes = m_ProcessMemoryProvider.PrivateBytes;
                    var processPressure = privateBytes > gcHigh * 0.75;
                    if (processPressure)
                        return true;
                }
            }
            catch
            {
                // Ignore adaptive errors; fall back to guardrails
            }

            // Guardrails: configured caps
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
                var total = m_Results.Count;
                if (total == 0)
                    return;

                var targetCount = Math.Max(1, total / 4); // Remove ~25% oldest

                // Create a priority queue to find the oldest items
                // Use a min-heap by default, so oldest items (smaller DateTime) have highest priority
                var pq = new PriorityQueue<KeyValuePair<string, CachedCommandResult>, DateTime>();

                // Add all items to the priority queue (min-heap by default, so oldest first)
                foreach (var kv in m_Results)
                {
                    pq.Enqueue(kv, kv.Value.LastAccessTime);
                }

                var evictedCount = 0;
                var freedMemory = 0L;
                var evictedKeys = new List<string>();

                // Remove the oldest items (up to targetCount)
                var itemsToRemove = Math.Min(targetCount, pq.Count);
                for (int i = 0; i < itemsToRemove; i++)
                {
                    if (pq.Count > 0)
                    {
                        var kv = pq.Dequeue();
                        evictedKeys.Add(kv.Key);
                        if (m_Results.TryRemove(kv.Key, out var removedResult))
                        {
                            freedMemory += EstimateResultSize(removedResult);
                            evictedCount++;
                        }
                    }
                }

                m_CurrentMemoryUsage -= freedMemory;

                m_Logger?.LogDebug("🧹 Evicted {Count} oldest results ({EvictedKeys}), freed {FreedMB}MB, current usage: {CurrentMB}MB",
                    evictedCount, string.Join(", ", evictedKeys), freedMemory / (1024.0 * 1024.0), m_CurrentMemoryUsage / (1024.0 * 1024.0));
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
        private static long EstimateResultSize(CachedCommandResult cachedResult)
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

    public interface IMemoryPressureProvider
    {
        long MemoryLoadBytes { get; }
        long HighMemoryLoadThresholdBytes { get; }
    }

    public sealed class SystemMemoryPressureProvider : IMemoryPressureProvider
    {
        public long MemoryLoadBytes => GC.GetGCMemoryInfo().MemoryLoadBytes;
        public long HighMemoryLoadThresholdBytes => GC.GetGCMemoryInfo().HighMemoryLoadThresholdBytes;
    }

    public interface IProcessMemoryProvider
    {
        long PrivateBytes { get; }
    }

    public sealed class DefaultProcessMemoryProvider : IProcessMemoryProvider
    {
        public long PrivateBytes => System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;
    }

    /// <summary>
    /// Represents a cached command result with access tracking
    /// </summary>
    public class CachedCommandResult(ICommandResult result, DateTime createdTime,
        string? originalCommand = null, DateTime? queueTime = null,
        DateTime? startTime = null, DateTime? endTime = null)
    {
        public ICommandResult Result { get; } = result ?? throw new ArgumentNullException(nameof(result));
        public DateTime CreatedTime { get; } = createdTime;
        public DateTime LastAccessTime { get; private set; } = createdTime;
        public string? OriginalCommand { get; } = originalCommand;
        public DateTime QueueTime { get; } = queueTime ?? createdTime.AddMinutes(-1);
        public DateTime StartTime { get; } = startTime ?? createdTime.Add(-result.Duration);
        public DateTime EndTime { get; } = endTime ?? createdTime;

        public void UpdateAccessTime()
        {
            LastAccessTime = DateTime.Now;
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
