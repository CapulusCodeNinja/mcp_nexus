using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Caching;

namespace mcp_nexus_tests.Caching
{
    public class IntelligentCacheServiceTests : IDisposable
    {
        private readonly Mock<ILogger<IntelligentCacheService<string, string>>> m_mockLogger;
        private readonly IntelligentCacheService<string, string> m_cacheService;

        public IntelligentCacheServiceTests()
        {
            m_mockLogger = new Mock<ILogger<IntelligentCacheService<string, string>>>();
            m_cacheService = new IntelligentCacheService<string, string>(m_mockLogger.Object);
        }

        public void Dispose()
        {
            m_cacheService?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new IntelligentCacheService<string, string>(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Act
            using var service = new IntelligentCacheService<string, string>(m_mockLogger.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithCustomMaxMemory_InitializesCorrectly()
        {
            // Arrange
            var maxMemoryBytes = 50 * 1024 * 1024; // 50MB

            // Act
            using var service = new IntelligentCacheService<string, string>(m_mockLogger.Object, maxMemoryBytes);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithCustomTtl_InitializesCorrectly()
        {
            // Arrange
            var customTtl = TimeSpan.FromMinutes(60);

            // Act
            using var service = new IntelligentCacheService<string, string>(m_mockLogger.Object, defaultTtl: customTtl);

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region TryGet Tests

        [Fact]
        public void TryGet_WithNonExistentKey_ReturnsFalse()
        {
            // Arrange
            var key = "non-existent";

            // Act
            var result = m_cacheService.TryGet(key, out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryGet_WithExistingKey_ReturnsTrueAndValue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            m_cacheService.Set(key, value);

            // Act
            var result = m_cacheService.TryGet(key, out var retrievedValue);

            // Assert
            Assert.True(result);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public void TryGet_WithExpiredKey_ReturnsFalse()
        {
            // Arrange
            var key = "expired-key";
            var value = "test-value";
            m_cacheService.Set(key, value, TimeSpan.FromMilliseconds(1));
            
            // Wait for expiration
            Thread.Sleep(10);

            // Act
            var result = m_cacheService.TryGet(key, out var retrievedValue);

            // Assert
            Assert.False(result);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void TryGet_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            m_cacheService.Set(key, value);
            m_cacheService.Dispose();

            // Act
            var result = m_cacheService.TryGet(key, out var retrievedValue);

            // Assert
            Assert.False(result);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void TryGet_UpdatesAccessTimeAndCount()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            m_cacheService.Set(key, value);

            // Act
            m_cacheService.TryGet(key, out _);
            m_cacheService.TryGet(key, out _);

            // Assert
            var stats = m_cacheService.GetStatistics();
            Assert.Equal(2, stats.TotalAccesses);
        }

        #endregion

        #region Set Tests

        [Fact]
        public void Set_WithValidKeyValue_StoresCorrectly()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";

            // Act
            m_cacheService.Set(key, value);

            // Assert
            var result = m_cacheService.TryGet(key, out var retrievedValue);
            Assert.True(result);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public void Set_WithCustomTtl_RespectsTtl()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var ttl = TimeSpan.FromMilliseconds(50);

            // Act
            m_cacheService.Set(key, value, ttl);

            // Assert
            var result = m_cacheService.TryGet(key, out var retrievedValue);
            Assert.True(result);
            Assert.Equal(value, retrievedValue);

            // Wait for expiration
            Thread.Sleep(60);

            result = m_cacheService.TryGet(key, out retrievedValue);
            Assert.False(result);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void Set_WithExistingKey_OverwritesValue()
        {
            // Arrange
            var key = "test-key";
            var originalValue = "original-value";
            var newValue = "new-value";

            // Act
            m_cacheService.Set(key, originalValue);
            m_cacheService.Set(key, newValue);

            // Assert
            var result = m_cacheService.TryGet(key, out var retrievedValue);
            Assert.True(result);
            Assert.Equal(newValue, retrievedValue);
        }

        [Fact]
        public void Set_AfterDisposal_DoesNothing()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            m_cacheService.Dispose();

            // Act
            m_cacheService.Set(key, value);

            // Assert
            var result = m_cacheService.TryGet(key, out var retrievedValue);
            Assert.False(result);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void Set_WithNullValue_HandlesCorrectly()
        {
            // Arrange
            var key = "test-key";
            string? value = null;

            // Act
            m_cacheService.Set(key, value!);

            // Assert
            var result = m_cacheService.TryGet(key, out var retrievedValue);
            Assert.True(result);
            Assert.Null(retrievedValue);
        }

        #endregion

        #region TryRemove Tests

        [Fact]
        public void TryRemove_WithNonExistentKey_ReturnsFalse()
        {
            // Arrange
            var key = "non-existent";

            // Act
            var result = m_cacheService.TryRemove(key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryRemove_WithExistingKey_ReturnsTrue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            m_cacheService.Set(key, value);

            // Act
            var result = m_cacheService.TryRemove(key);

            // Assert
            Assert.True(result);
            
            // Verify it's actually removed
            var getResult = m_cacheService.TryGet(key, out _);
            Assert.False(getResult);
        }

        [Fact]
        public void TryRemove_AfterDisposal_ReturnsFalse()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            m_cacheService.Set(key, value);
            m_cacheService.Dispose();

            // Act
            var result = m_cacheService.TryRemove(key);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Clear Tests

        [Fact]
        public void Clear_WithEmptyCache_DoesNothing()
        {
            // Act
            m_cacheService.Clear();

            // Assert
            var stats = m_cacheService.GetStatistics();
            Assert.Equal(0, stats.TotalEntries);
        }

        [Fact]
        public void Clear_WithPopulatedCache_RemovesAllEntries()
        {
            // Arrange
            m_cacheService.Set("key1", "value1");
            m_cacheService.Set("key2", "value2");
            m_cacheService.Set("key3", "value3");

            // Act
            m_cacheService.Clear();

            // Assert
            var stats = m_cacheService.GetStatistics();
            Assert.Equal(0, stats.TotalEntries);
            
            Assert.False(m_cacheService.TryGet("key1", out _));
            Assert.False(m_cacheService.TryGet("key2", out _));
            Assert.False(m_cacheService.TryGet("key3", out _));
        }

        [Fact]
        public void Clear_AfterDisposal_DoesNothing()
        {
            // Arrange
            m_cacheService.Set("key1", "value1");
            m_cacheService.Dispose();

            // Act
            m_cacheService.Clear();

            // Assert - should not throw
        }

        #endregion

        #region GetStatistics Tests

        [Fact]
        public void GetStatistics_WithEmptyCache_ReturnsEmptyStats()
        {
            // Act
            var stats = m_cacheService.GetStatistics();

            // Assert
            Assert.Equal(0, stats.TotalEntries);
            Assert.Equal(0, stats.ExpiredEntries);
            Assert.Equal(0, stats.TotalSizeBytes);
            Assert.Equal(0, stats.TotalAccesses);
            Assert.Equal(0, stats.AverageAccessCount);
            Assert.Equal(0, stats.MemoryUsagePercent);
        }

        [Fact]
        public void GetStatistics_WithPopulatedCache_ReturnsCorrectStats()
        {
            // Arrange
            m_cacheService.Set("key1", "value1");
            m_cacheService.Set("key2", "value2");
            m_cacheService.TryGet("key1", out _); // Access once
            m_cacheService.TryGet("key1", out _); // Access twice

            // Act
            var stats = m_cacheService.GetStatistics();

            // Assert
            Assert.Equal(2, stats.TotalEntries);
            Assert.Equal(0, stats.ExpiredEntries);
            Assert.True(stats.TotalSizeBytes > 0);
            Assert.Equal(2, stats.TotalAccesses);
            Assert.Equal(1.0, stats.AverageAccessCount);
            Assert.True(stats.MemoryUsagePercent >= 0);
        }

        [Fact]
        public void GetStatistics_WithExpiredEntries_CountsExpired()
        {
            // Arrange
            m_cacheService.Set("key1", "value1", TimeSpan.FromMilliseconds(1));
            m_cacheService.Set("key2", "value2", TimeSpan.FromMinutes(30));
            
            // Wait for first key to expire
            Thread.Sleep(10);

            // Act
            var stats = m_cacheService.GetStatistics();

            // Assert
            Assert.Equal(2, stats.TotalEntries);
            Assert.Equal(1, stats.ExpiredEntries);
        }

        [Fact]
        public void GetStatistics_AfterDisposal_ReturnsEmptyStats()
        {
            // Arrange
            m_cacheService.Set("key1", "value1");
            m_cacheService.Dispose();

            // Act
            var stats = m_cacheService.GetStatistics();

            // Assert
            Assert.Equal(0, stats.TotalEntries);
            Assert.Equal(0, stats.ExpiredEntries);
            Assert.Equal(0, stats.TotalSizeBytes);
            Assert.Equal(0, stats.TotalAccesses);
            Assert.Equal(0, stats.AverageAccessCount);
            Assert.Equal(0, stats.MemoryUsagePercent);
        }

        #endregion

        #region Memory Pressure Tests

        [Fact]
        public void Set_WithHighMemoryUsage_TriggersEviction()
        {
            // Arrange - Create a cache with very small memory limit
            using var smallCache = new IntelligentCacheService<string, string>(
                m_mockLogger.Object, 
                maxMemoryBytes: 1000); // 1KB limit

            // Act - Add many entries to trigger memory pressure
            for (int i = 0; i < 100; i++)
            {
                smallCache.Set($"key{i}", $"value{i}");
            }

            // Assert - Some entries should have been evicted
            var stats = smallCache.GetStatistics();
            Assert.True(stats.TotalEntries < 100);
        }

        #endregion

        #region Size Estimation Tests

        [Fact]
        public void Set_WithStringValue_EstimatesSizeCorrectly()
        {
            // Arrange
            var key = "test-key";
            var value = "hello world"; // 11 characters * 2 = 22 bytes

            // Act
            m_cacheService.Set(key, value);

            // Assert
            var stats = m_cacheService.GetStatistics();
            Assert.True(stats.TotalSizeBytes >= 22);
        }

        [Fact]
        public void Set_WithByteArrayValue_EstimatesSizeCorrectly()
        {
            // Arrange
            var byteLogger = new Mock<ILogger<IntelligentCacheService<string, byte[]>>>();
            using var byteCache = new IntelligentCacheService<string, byte[]>(byteLogger.Object);
            var key = "test-key";
            var value = new byte[] { 1, 2, 3, 4, 5 }; // 5 bytes

            // Act
            byteCache.Set(key, value);

            // Assert
            var stats = byteCache.GetStatistics();
            Assert.Equal(5, stats.TotalSizeBytes);
        }

        [Fact]
        public void Set_WithComplexObject_EstimatesDefaultSize()
        {
            // Arrange
            var objectLogger = new Mock<ILogger<IntelligentCacheService<string, object>>>();
            using var objectCache = new IntelligentCacheService<string, object>(objectLogger.Object);
            var key = "test-key";
            var value = new { Name = "test", Value = 123 };

            // Act
            objectCache.Set(key, value);

            // Assert
            var stats = objectCache.GetStatistics();
            Assert.Equal(100, stats.TotalSizeBytes); // Default estimate
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            // Act & Assert
            m_cacheService.Dispose();
            m_cacheService.Dispose(); // Should not throw
        }

        [Fact]
        public void Dispose_ClearsCache()
        {
            // Arrange
            m_cacheService.Set("key1", "value1");
            m_cacheService.Set("key2", "value2");

            // Act
            m_cacheService.Dispose();

            // Assert
            var stats = m_cacheService.GetStatistics();
            Assert.Equal(0, stats.TotalEntries);
        }

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public async Task ConcurrentAccess_IsThreadSafe()
        {
            // Arrange
            var tasks = new List<Task>();
            var random = new Random();

            // Act - Multiple threads accessing cache concurrently
            for (int i = 0; i < 10; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var key = $"key{taskId}_{j}";
                        var value = $"value{taskId}_{j}";
                        
                        if (random.Next(2) == 0)
                        {
                            m_cacheService.Set(key, value);
                        }
                        else
                        {
                            m_cacheService.TryGet(key, out _);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - Should not throw exceptions and cache should be in valid state
            var stats = m_cacheService.GetStatistics();
            Assert.True(stats.TotalEntries >= 0);
        }

        #endregion
    }
}
