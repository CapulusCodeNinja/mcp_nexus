using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.CommandQueue;
using Xunit;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for SessionCommandResultCache
    /// </summary>
    public class SessionCommandResultCacheTests : IDisposable
    {
        private readonly Mock<ILogger<SessionCommandResultCache>> m_MockLogger;
        private readonly SessionCommandResultCache m_Cache;

        public SessionCommandResultCacheTests()
        {
            m_MockLogger = new Mock<ILogger<SessionCommandResultCache>>();
            m_Cache = new SessionCommandResultCache(
                maxMemoryBytes: 1024 * 1024, // 1MB for testing
                maxResults: 10,
                memoryPressureThreshold: 0.8,
                logger: m_MockLogger.Object);
        }

        public void Dispose()
        {
            m_Cache?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            using var cache = new SessionCommandResultCache();

            // Assert
            Assert.NotNull(cache);
        }

        [Fact]
        public void Constructor_WithCustomParameters_UsesProvidedValues()
        {
            // Arrange
            var maxMemory = 2 * 1024 * 1024; // 2MB
            var maxResults = 5;
            var pressureThreshold = 0.7;

            // Act
            using var cache = new SessionCommandResultCache(maxMemory, maxResults, pressureThreshold);

            // Assert
            var stats = cache.GetStatistics();
            Assert.Equal(maxMemory, stats.MaxMemoryBytes);
            Assert.Equal(maxResults, stats.MaxResults);
        }

        [Fact]
        public void Constructor_WithInvalidPressureThreshold_ClampsToValidRange()
        {
            // Act & Assert - Should not throw
            using var cache1 = new SessionCommandResultCache(memoryPressureThreshold: -0.1);
            using var cache2 = new SessionCommandResultCache(memoryPressureThreshold: 1.5);
            
            // Both should initialize successfully with clamped values
            Assert.NotNull(cache1);
            Assert.NotNull(cache2);
        }

        #endregion

        #region StoreResult Tests

        [Fact]
        public void StoreResult_WithValidCommandId_StoresResult()
        {
            // Arrange
            var commandId = "test-command-1";
            var result = CommandResult.Success("Test output");

            // Act
            m_Cache.StoreResult(commandId, result);

            // Assert
            Assert.True(m_Cache.HasResult(commandId));
            var retrievedResult = m_Cache.GetResult(commandId);
            Assert.NotNull(retrievedResult);
            Assert.Equal("Test output", retrievedResult.Output);
            Assert.True(retrievedResult.IsSuccess);
        }

        [Fact]
        public void StoreResult_WithEmptyCommandId_DoesNotStore()
        {
            // Arrange
            var result = CommandResult.Success("Test output");

            // Act
            m_Cache.StoreResult(string.Empty, result);
            m_Cache.StoreResult(null!, result);

            // Assert
            Assert.False(m_Cache.HasResult(string.Empty));
            Assert.False(m_Cache.HasResult(null!));
        }

        [Fact]
        public void StoreResult_WithSameCommandId_OverwritesPrevious()
        {
            // Arrange
            var commandId = "test-command-1";
            var result1 = CommandResult.Success("First output");
            var result2 = CommandResult.Success("Second output");

            // Act
            m_Cache.StoreResult(commandId, result1);
            m_Cache.StoreResult(commandId, result2);

            // Assert
            var retrievedResult = m_Cache.GetResult(commandId);
            Assert.NotNull(retrievedResult);
            Assert.Equal("Second output", retrievedResult.Output);
        }

        [Fact]
        public void StoreResult_WithFailedResult_StoresCorrectly()
        {
            // Arrange
            var commandId = "test-command-1";
            var result = CommandResult.Failure("Test error");

            // Act
            m_Cache.StoreResult(commandId, result);

            // Assert
            var retrievedResult = m_Cache.GetResult(commandId);
            Assert.NotNull(retrievedResult);
            Assert.False(retrievedResult.IsSuccess);
            Assert.Equal("Test error", retrievedResult.ErrorMessage);
        }

        #endregion

        #region GetResult Tests

        [Fact]
        public void GetResult_WithExistingCommandId_ReturnsResult()
        {
            // Arrange
            var commandId = "test-command-1";
            var result = CommandResult.Success("Test output");
            m_Cache.StoreResult(commandId, result);

            // Act
            var retrievedResult = m_Cache.GetResult(commandId);

            // Assert
            Assert.NotNull(retrievedResult);
            Assert.Equal("Test output", retrievedResult.Output);
        }

        [Fact]
        public void GetResult_WithNonExistentCommandId_ReturnsNull()
        {
            // Act
            var result = m_Cache.GetResult("non-existent-command");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetResult_WithEmptyCommandId_ReturnsNull()
        {
            // Act
            var result = m_Cache.GetResult(string.Empty);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region HasResult Tests

        [Fact]
        public void HasResult_WithExistingCommandId_ReturnsTrue()
        {
            // Arrange
            var commandId = "test-command-1";
            var result = CommandResult.Success("Test output");
            m_Cache.StoreResult(commandId, result);

            // Act
            var hasResult = m_Cache.HasResult(commandId);

            // Assert
            Assert.True(hasResult);
        }

        [Fact]
        public void HasResult_WithNonExistentCommandId_ReturnsFalse()
        {
            // Act
            var hasResult = m_Cache.HasResult("non-existent-command");

            // Assert
            Assert.False(hasResult);
        }

        #endregion

        #region RemoveResult Tests

        [Fact]
        public void RemoveResult_WithExistingCommandId_RemovesResult()
        {
            // Arrange
            var commandId = "test-command-1";
            var result = CommandResult.Success("Test output");
            m_Cache.StoreResult(commandId, result);

            // Act
            var removed = m_Cache.RemoveResult(commandId);

            // Assert
            Assert.True(removed);
            Assert.False(m_Cache.HasResult(commandId));
            Assert.Null(m_Cache.GetResult(commandId));
        }

        [Fact]
        public void RemoveResult_WithNonExistentCommandId_ReturnsFalse()
        {
            // Act
            var removed = m_Cache.RemoveResult("non-existent-command");

            // Assert
            Assert.False(removed);
        }

        #endregion

        #region ClearAll Tests

        [Fact]
        public void ClearAll_WithStoredResults_ClearsAllResults()
        {
            // Arrange
            m_Cache.StoreResult("command-1", CommandResult.Success("Output 1"));
            m_Cache.StoreResult("command-2", CommandResult.Success("Output 2"));
            m_Cache.StoreResult("command-3", CommandResult.Success("Output 3"));

            // Act
            m_Cache.ClearAll();

            // Assert
            Assert.False(m_Cache.HasResult("command-1"));
            Assert.False(m_Cache.HasResult("command-2"));
            Assert.False(m_Cache.HasResult("command-3"));
            
            var stats = m_Cache.GetStatistics();
            Assert.Equal(0, stats.TotalResults);
        }

        #endregion

        #region Memory Management Tests

        [Fact]
        public void StoreResult_WhenMemoryPressure_EvictsOldestResults()
        {
            // Arrange - Create a cache with very small memory limit
            using var smallCache = new SessionCommandResultCache(
                maxMemoryBytes: 100, // Very small limit
                maxResults: 5,
                memoryPressureThreshold: 0.5);

            // Act - Store multiple results to trigger memory pressure
            for (int i = 0; i < 10; i++)
            {
                var result = CommandResult.Success($"Output {i} - " + new string('x', 50)); // Large result
                smallCache.StoreResult($"command-{i}", result);
            }

            // Assert - Should have evicted some results
            var stats = smallCache.GetStatistics();
            Assert.True(stats.TotalResults < 10); // Some results should have been evicted
        }

        [Fact]
        public void StoreResult_WhenMaxResultsExceeded_EvictsOldestResults()
        {
            // Arrange - Create a cache with small result limit
            using var smallCache = new SessionCommandResultCache(
                maxMemoryBytes: 10 * 1024 * 1024, // Large memory
                maxResults: 3, // Small result limit
                memoryPressureThreshold: 0.8);

            // Act - Store more results than the limit
            for (int i = 0; i < 5; i++)
            {
                var result = CommandResult.Success($"Output {i}");
                smallCache.StoreResult($"command-{i}", result);
            }

            // Assert - Should have evicted some results
            var stats = smallCache.GetStatistics();
            Assert.True(stats.TotalResults <= 3); // Should not exceed max results
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public void GetStatistics_WithEmptyCache_ReturnsZeroValues()
        {
            // Act
            var stats = m_Cache.GetStatistics();

            // Assert
            Assert.Equal(0, stats.TotalResults);
            Assert.Equal(0, stats.CurrentMemoryUsage);
            Assert.True(stats.MemoryUsagePercentage >= 0);
        }

        [Fact]
        public void GetStatistics_WithStoredResults_ReturnsCorrectValues()
        {
            // Arrange
            m_Cache.StoreResult("command-1", CommandResult.Success("Output 1"));
            m_Cache.StoreResult("command-2", CommandResult.Success("Output 2"));

            // Act
            var stats = m_Cache.GetStatistics();

            // Assert
            Assert.Equal(2, stats.TotalResults);
            Assert.True(stats.CurrentMemoryUsage > 0);
            Assert.True(stats.MemoryUsagePercentage >= 0);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_WhenCalled_ClearsAllResults()
        {
            // Arrange
            m_Cache.StoreResult("command-1", CommandResult.Success("Output 1"));
            m_Cache.StoreResult("command-2", CommandResult.Success("Output 2"));

            // Act
            m_Cache.Dispose();

            // Assert
            Assert.False(m_Cache.HasResult("command-1"));
            Assert.False(m_Cache.HasResult("command-2"));
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_Cache.Dispose();
            m_Cache.Dispose();
        }

        [Fact]
        public void StoreResult_AfterDisposal_DoesNotStore()
        {
            // Arrange
            m_Cache.Dispose();

            // Act
            m_Cache.StoreResult("command-1", CommandResult.Success("Output 1"));

            // Assert
            Assert.False(m_Cache.HasResult("command-1"));
        }

        #endregion

        #region LRU Eviction Tests

        [Fact]
        public void GetResult_UpdatesAccessTime_ForLRUEviction()
        {
            // Arrange - Create a cache with small limits
            using var smallCache = new SessionCommandResultCache(
                maxMemoryBytes: 1000,
                maxResults: 3,
                memoryPressureThreshold: 0.5);

            // Store initial results
            smallCache.StoreResult("command-1", CommandResult.Success("Output 1"));
            smallCache.StoreResult("command-2", CommandResult.Success("Output 2"));
            smallCache.StoreResult("command-3", CommandResult.Success("Output 3"));

            // Access command-1 to make it recently used
            smallCache.GetResult("command-1");

            // Store more results to trigger eviction
            smallCache.StoreResult("command-4", CommandResult.Success("Output 4"));
            smallCache.StoreResult("command-5", CommandResult.Success("Output 5"));

            // Assert - command-1 should still be there (recently accessed)
            Assert.True(smallCache.HasResult("command-1"));
        }

        #endregion
    }
}
