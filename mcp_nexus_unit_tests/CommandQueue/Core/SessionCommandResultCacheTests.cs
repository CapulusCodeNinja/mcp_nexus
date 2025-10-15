using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.CommandQueue.Core;
using Xunit;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for SessionCommandResultCache
    /// </summary>
    public class SessionCommandResultCacheTests : IDisposable
    {
        private readonly Mock<ILogger<SessionCommandResultCache>> m_MockLogger;
        private readonly SessionCommandResultCache m_Cache;
        private sealed class FakeMem : IMemoryPressureProvider { public long MemoryLoadBytes { get; set; } public long HighMemoryLoadThresholdBytes { get; set; } }
        private sealed class FakeProc : IProcessMemoryProvider { public long PrivateBytes { get; set; } }
        private sealed class ThrowMem : IMemoryPressureProvider { public long MemoryLoadBytes => throw new Exception("boom"); public long HighMemoryLoadThresholdBytes => throw new Exception("boom"); }
        private sealed class ThrowProc : IProcessMemoryProvider { public long PrivateBytes => throw new Exception("boom"); }

        public SessionCommandResultCacheTests()
        {
            m_MockLogger = new Mock<ILogger<SessionCommandResultCache>>();
            // Use fake memory providers to prevent GC-based eviction during tests
            var fakeMemPressure = new FakeMem { HighMemoryLoadThresholdBytes = long.MaxValue, MemoryLoadBytes = 0 };
            var fakeProcessMem = new FakeProc { PrivateBytes = 0 };
            m_Cache = new SessionCommandResultCache(
                maxMemoryBytes: 1024 * 1024, // 1MB for testing
                maxResults: 10,
                memoryPressureThreshold: 0.8,
                logger: m_MockLogger.Object,
                memoryPressureProvider: fakeMemPressure,
                processMemoryProvider: fakeProcessMem);
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
            // Arrange - Create a cache with adaptive pressure forcing eviction
            var mem = new FakeMem { HighMemoryLoadThresholdBytes = 1000, MemoryLoadBytes = 900 }; // 90% > 85%
            var proc = new FakeProc { PrivateBytes = 0 };
            using var smallCache = new SessionCommandResultCache(
                maxMemoryBytes: long.MaxValue, // disable guardrail
                maxResults: int.MaxValue,      // disable guardrail
                memoryPressureThreshold: 0.99,
                logger: null,
                memoryPressureProvider: mem,
                processMemoryProvider: proc);

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
        public void StoreResult_WhenProcessPressureOnly_EvictsOldestResults()
        {
            // Arrange - Process pressure > 75%, system pressure < 85%
            var mem = new FakeMem { HighMemoryLoadThresholdBytes = 1000, MemoryLoadBytes = 600 };
            var proc = new FakeProc { PrivateBytes = 800 }; // 80% > 75%
            using var cache = new SessionCommandResultCache(
                maxMemoryBytes: long.MaxValue,
                maxResults: int.MaxValue,
                memoryPressureThreshold: 0.99,
                logger: null,
                memoryPressureProvider: mem,
                processMemoryProvider: proc);

            for (int i = 0; i < 20; i++)
                cache.StoreResult($"cmd-{i}", CommandResult.Success("x"));

            var stats = cache.GetStatistics();
            Assert.True(stats.TotalResults < 20);
        }

        [Fact]
        public void StoreResult_WhenAdaptiveDisabled_UsesGuardrails()
        {
            // Arrange - Adaptive disabled (no threshold), force guardrail eviction by small caps
            var mem = new FakeMem { HighMemoryLoadThresholdBytes = 0, MemoryLoadBytes = 0 };
            var proc = new FakeProc { PrivateBytes = 0 };
            using var cache = new SessionCommandResultCache(
                maxMemoryBytes: 100, // tiny
                maxResults: 5,
                memoryPressureThreshold: 0.5,
                logger: null,
                memoryPressureProvider: mem,
                processMemoryProvider: proc);

            for (int i = 0; i < 20; i++)
                cache.StoreResult($"cmd-{i}", CommandResult.Success(new string('a', 50)));

            var stats = cache.GetStatistics();
            Assert.True(stats.TotalResults <= 5 || stats.CurrentMemoryUsage <= 100);
        }

        [Fact]
        public void StoreResult_WhenAdaptiveThrows_FallsBackToGuardrails()
        {
            // Arrange - Providers throw; ensure no crash and guardrails apply
            using var cache = new SessionCommandResultCache(
                maxMemoryBytes: 100, // tiny
                maxResults: 5,
                memoryPressureThreshold: 0.5,
                logger: null,
                memoryPressureProvider: new ThrowMem(),
                processMemoryProvider: new ThrowProc());

            for (int i = 0; i < 20; i++)
                cache.StoreResult($"cmd-{i}", CommandResult.Success(new string('b', 50)));

            var stats = cache.GetStatistics();
            Assert.True(stats.TotalResults <= 5 || stats.CurrentMemoryUsage <= 100);
        }

        [Fact]
        public void StoreResult_NoEvictionUnderThresholds()
        {
            // Arrange - No pressure and large guardrails
            var mem = new FakeMem { HighMemoryLoadThresholdBytes = 1000, MemoryLoadBytes = 100 };
            var proc = new FakeProc { PrivateBytes = 100 };
            using var cache = new SessionCommandResultCache(
                maxMemoryBytes: 10 * 1024 * 1024,
                maxResults: 10000,
                memoryPressureThreshold: 0.95,
                logger: null,
                memoryPressureProvider: mem,
                processMemoryProvider: proc);

            for (int i = 0; i < 200; i++)
                cache.StoreResult($"cmd-{i}", CommandResult.Success("x"));

            var stats = cache.GetStatistics();
            Assert.Equal(200, stats.TotalResults);
        }

        [Fact]
        public void StoreResult_PriorityQueueKeepsRecentlyAccessed()
        {
            // Arrange - Small guardrails to force eviction path deterministically
            // Use large memory limit so count-based eviction triggers, not memory-based
            // Use fake memory providers to prevent GC-based eviction
            var fakeMemPressure = new FakeMem { HighMemoryLoadThresholdBytes = long.MaxValue, MemoryLoadBytes = 0 };
            var fakeProcessMem = new FakeProc { PrivateBytes = 0 };
            using var cache = new SessionCommandResultCache(
                maxMemoryBytes: 10000000, // 10MB - large enough to avoid memory-based eviction
                maxResults: 4,
                memoryPressureThreshold: 0.99, // Very high threshold
                logger: null,
                memoryPressureProvider: fakeMemPressure,
                processMemoryProvider: fakeProcessMem);

            // Add 4 entries with delays to ensure distinct creation times
            cache.StoreResult("a", CommandResult.Success("1"));
            var statsAfterA = cache.GetStatistics();
            Assert.Equal(1, statsAfterA.TotalResults); // Should have 1 item after storing "a"
            Thread.Sleep(20);
            cache.StoreResult("b", CommandResult.Success("2"));
            var statsAfterB = cache.GetStatistics();
            Assert.Equal(2, statsAfterB.TotalResults); // Should have 2 items after storing "b"
            Thread.Sleep(20);
            cache.StoreResult("c", CommandResult.Success("3"));
            var statsAfterC = cache.GetStatistics();
            Assert.Equal(3, statsAfterC.TotalResults); // Should have 3 items after storing "c"
            Thread.Sleep(20);
            cache.StoreResult("d", CommandResult.Success("4"));

            // Verify all 4 items are present
            var stats = cache.GetStatistics();
            Assert.Equal(4, stats.TotalResults); // Should have 4 items
            Assert.True(cache.HasResult("a"), "a should exist after initial storage");
            Assert.True(cache.HasResult("b"), "b should exist after initial storage");
            Assert.True(cache.HasResult("c"), "c should exist after initial storage");
            Assert.True(cache.HasResult("d"), "d should exist after initial storage");

            // Add delay to ensure different LastAccessTime values
            Thread.Sleep(50);

            // Refresh access for b and d (making them more recently accessed)
            var resultB = cache.GetResult("b");
            Assert.NotNull(resultB);

            Thread.Sleep(50);

            var resultD = cache.GetResult("d");
            Assert.NotNull(resultD);

            // Add delay before triggering eviction
            Thread.Sleep(50);

            // Trigger eviction by exceeding max results
            cache.StoreResult("e", CommandResult.Success("5"));

            // After adding "e", we should have 4 items (one evicted)
            // The evicted item should be "a" or "c" (oldest LastAccessTime)
            stats = cache.GetStatistics();
            Assert.Equal(4, stats.TotalResults);

            cache.StoreResult("f", CommandResult.Success("6"));

            // After adding "f", we should still have 4 items (another one evicted)
            stats = cache.GetStatistics();
            Assert.Equal(4, stats.TotalResults);

            // Recently accessed ones (b and d) should be retained
            // Oldest ones (a and c) should be evicted
            Assert.True(cache.HasResult("b"), "b should be retained (recently accessed)");
            Assert.True(cache.HasResult("d"), "d should be retained (recently accessed)");
        }

        [Fact]
        public void AdaptiveThresholdBoundary_DoesNotEvictAtExactThreshold()
        {
            // Arrange - Exactly at 85% system, 75% process → no eviction (strict >)
            var mem = new FakeMem { HighMemoryLoadThresholdBytes = 1000, MemoryLoadBytes = 850 };
            var proc = new FakeProc { PrivateBytes = 750 };
            using var cache = new SessionCommandResultCache(
                maxMemoryBytes: long.MaxValue,
                maxResults: int.MaxValue,
                memoryPressureThreshold: 0.99,
                logger: null,
                memoryPressureProvider: mem,
                processMemoryProvider: proc);

            for (int i = 0; i < 50; i++)
                cache.StoreResult($"cmd-{i}", CommandResult.Success("x"));

            var stats = cache.GetStatistics();
            Assert.Equal(50, stats.TotalResults);
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
            // Use fake memory providers to prevent GC-based eviction
            var fakeMemPressure = new FakeMem { HighMemoryLoadThresholdBytes = long.MaxValue, MemoryLoadBytes = 0 };
            var fakeProcessMem = new FakeProc { PrivateBytes = 0 };
            using var smallCache = new SessionCommandResultCache(
                maxMemoryBytes: 10000000, // Large enough to avoid memory-based eviction
                maxResults: 3,
                memoryPressureThreshold: 0.99,
                logger: null,
                memoryPressureProvider: fakeMemPressure,
                processMemoryProvider: fakeProcessMem);

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
