using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Caching;
using System.Collections.Concurrent;

namespace mcp_nexus_tests.Caching
{
    /// <summary>
    /// Tests for CacheEvictionManager
    /// </summary>
    public class CacheEvictionManagerTests : IDisposable
    {
        private readonly Mock<ILogger> m_MockLogger;
        private readonly CacheConfiguration m_Config;
        private readonly ConcurrentDictionary<string, CacheEntry<object>> m_Cache;
        private readonly CacheEvictionManager<string, object> m_EvictionManager;

        public CacheEvictionManagerTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_Config = new CacheConfiguration(
                maxMemoryBytes: 100 * 1024 * 1024, // 100MB
                defaultTtl: TimeSpan.FromMinutes(30),
                cleanupInterval: TimeSpan.FromMinutes(1),
                memoryPressureThreshold: 0.8,
                maxEntriesPerCleanup: 100
            );
            m_Cache = new ConcurrentDictionary<string, CacheEntry<object>>();

            m_EvictionManager = new CacheEvictionManager<string, object>(m_MockLogger.Object, m_Config, m_Cache);
        }

        public void Dispose()
        {
            m_EvictionManager?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheEvictionManager<string, object>(null!, m_Config, m_Cache));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheEvictionManager<string, object>(m_MockLogger.Object, null!, m_Cache));
        }

        [Fact]
        public void Constructor_WithNullCache_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheEvictionManager<string, object>(m_MockLogger.Object, m_Config, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            var manager = new CacheEvictionManager<string, object>(m_MockLogger.Object, m_Config, m_Cache);
            Assert.NotNull(manager);
            manager.Dispose();
        }

        [Fact]
        public void CheckMemoryPressure_WithLowMemoryUsage_DoesNotTriggerEviction()
        {
            // Add a small entry that won't trigger memory pressure
            var entry = new CacheEntry<object>
            {
                Value = "test",
                SizeBytes = 100,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            m_Cache.TryAdd("key1", entry);

            m_EvictionManager.CheckMemoryPressure();

            // Should still have the entry since memory pressure wasn't triggered
            Assert.Single(m_Cache);
        }

        [Fact]
        public void CheckMemoryPressure_WithHighMemoryUsage_TriggersEviction()
        {
            // Add entries that will trigger memory pressure (over 80% of 100MB)
            var entry1 = new CacheEntry<object>
            {
                Value = new byte[50 * 1024 * 1024], // 50MB
                SizeBytes = 50 * 1024 * 1024,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            var entry2 = new CacheEntry<object>
            {
                Value = new byte[40 * 1024 * 1024], // 40MB
                SizeBytes = 40 * 1024 * 1024,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            m_Cache.TryAdd("key1", entry1);
            m_Cache.TryAdd("key2", entry2);

            m_EvictionManager.CheckMemoryPressure();

            // Should have evicted some entries to get below 60% of max memory
            Assert.True(m_Cache.Count < 2);
        }

        [Fact]
        public void RemoveExpiredEntries_WithNoExpiredEntries_ReturnsZero()
        {
            var now = DateTime.UtcNow;
            var entry = new CacheEntry<object>
            {
                Value = new object(),
                ExpiresAt = now.AddMinutes(5),
                LastAccessed = now,
                AccessCount = 1,
                SizeBytes = 1024
            };
            m_Cache.TryAdd("key1", entry);

            var result = m_EvictionManager.RemoveExpiredEntries();
            Assert.Equal(0, result);
        }

        [Fact]
        public void RemoveExpiredEntries_WithExpiredEntries_RemovesThem()
        {
            var now = DateTime.UtcNow;
            var expiredEntry = new CacheEntry<object>
            {
                Value = new object(),
                ExpiresAt = now.AddMinutes(-5),
                LastAccessed = now.AddMinutes(-10),
                AccessCount = 1,
                SizeBytes = 1024
            };
            m_Cache.TryAdd("expired", expiredEntry);

            var result = m_EvictionManager.RemoveExpiredEntries();
            Assert.Equal(1, result);
            Assert.Empty(m_Cache);
        }

        [Fact]
        public void EvictLeastRecentlyUsed_WithLowMemoryUsage_ReturnsZero()
        {
            var result = m_EvictionManager.EvictLeastRecentlyUsed(100 * 1024 * 1024);
            Assert.Equal(0, result);
        }

        [Fact]
        public void EvictLeastRecentlyUsed_WithHighMemoryUsage_EvictsEntries()
        {
            var now = DateTime.UtcNow;
            var oldEntry = new CacheEntry<object>
            {
                Value = new object(),
                ExpiresAt = now.AddMinutes(5),
                LastAccessed = now.AddMinutes(-10),
                AccessCount = 1,
                SizeBytes = 30 * 1024 * 1024
            };
            m_Cache.TryAdd("old", oldEntry);

            var result = m_EvictionManager.EvictLeastRecentlyUsed(10 * 1024 * 1024);
            Assert.Equal(1, result);
        }

        [Fact]
        public void Dispose_DisposesCorrectly()
        {
            m_EvictionManager.Dispose();
            m_MockLogger.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cache eviction manager disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }
}