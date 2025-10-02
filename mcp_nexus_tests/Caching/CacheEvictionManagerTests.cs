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
        private readonly Mock<ILogger> _mockLogger;
        private readonly CacheConfiguration _config;
        private readonly ConcurrentDictionary<string, CacheEntry<object>> _cache;
        private readonly CacheEvictionManager<string, object> _evictionManager;

        public CacheEvictionManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _config = new CacheConfiguration(
                maxMemoryBytes: 100 * 1024 * 1024, // 100MB
                defaultTtl: TimeSpan.FromMinutes(30),
                cleanupInterval: TimeSpan.FromMinutes(1),
                memoryPressureThreshold: 0.8,
                maxEntriesPerCleanup: 100
            );
            _cache = new ConcurrentDictionary<string, CacheEntry<object>>();

            _evictionManager = new CacheEvictionManager<string, object>(_mockLogger.Object, _config, _cache);
        }

        public void Dispose()
        {
            _evictionManager?.Dispose();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheEvictionManager<string, object>(null!, _config, _cache));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheEvictionManager<string, object>(_mockLogger.Object, null!, _cache));
        }

        [Fact]
        public void Constructor_WithNullCache_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheEvictionManager<string, object>(_mockLogger.Object, _config, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            var manager = new CacheEvictionManager<string, object>(_mockLogger.Object, _config, _cache);
            Assert.NotNull(manager);
            manager.Dispose();
        }

        [Fact]
        public void CheckMemoryPressure_WithLowMemoryUsage_DoesNotTriggerEviction()
        {
            // Add a small entry that won't trigger memory pressure
            var entry = new CacheEntry<object>("test", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 0, 100);
            _cache.TryAdd("key1", entry);

            _evictionManager.CheckMemoryPressure();

            // Should still have the entry since memory pressure wasn't triggered
            Assert.Single(_cache);
        }

        [Fact]
        public void CheckMemoryPressure_WithHighMemoryUsage_TriggersEviction()
        {
            // Add entries that will trigger memory pressure (over 80% of 100MB)
            var entry1 = new CacheEntry<object>(new byte[50 * 1024 * 1024], DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 0, 50 * 1024 * 1024);
            var entry2 = new CacheEntry<object>(new byte[40 * 1024 * 1024], DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 0, 40 * 1024 * 1024);
            _cache.TryAdd("key1", entry1);
            _cache.TryAdd("key2", entry2);

            _evictionManager.CheckMemoryPressure();

            // Should have evicted some entries to get below 60% of max memory
            Assert.True(_cache.Count < 2);
        }

        [Fact]
        public void RemoveExpiredEntries_WithNoExpiredEntries_ReturnsZero()
        {
            var now = DateTime.UtcNow;
            var entry = new CacheEntry<object>(new object(), now, now, now.AddMinutes(5), 1, 1024);
            _cache.TryAdd("key1", entry);

            var result = _evictionManager.RemoveExpiredEntries();
            Assert.Equal(0, result);
        }

        [Fact]
        public void RemoveExpiredEntries_WithExpiredEntries_RemovesThem()
        {
            var now = DateTime.UtcNow;
            var expiredEntry = new CacheEntry<object>(new object(), now.AddMinutes(-10), now.AddMinutes(-10), now.AddMinutes(-5), 1, 1024);
            _cache.TryAdd("expired", expiredEntry);

            var result = _evictionManager.RemoveExpiredEntries();
            Assert.Equal(1, result);
            Assert.Empty(_cache);
        }

        [Fact]
        public void EvictLeastRecentlyUsed_WithLowMemoryUsage_ReturnsZero()
        {
            var result = _evictionManager.EvictLeastRecentlyUsed(100 * 1024 * 1024);
            Assert.Equal(0, result);
        }

        [Fact]
        public void EvictLeastRecentlyUsed_WithHighMemoryUsage_EvictsEntries()
        {
            var now = DateTime.UtcNow;
            var oldEntry = new CacheEntry<object>(new object(), now.AddMinutes(-10), now.AddMinutes(-10), now.AddMinutes(5), 1, 30 * 1024 * 1024);
            _cache.TryAdd("old", oldEntry);

            var result = _evictionManager.EvictLeastRecentlyUsed(10 * 1024 * 1024);
            Assert.Equal(1, result);
        }

        [Fact]
        public void Dispose_DisposesCorrectly()
        {
            _evictionManager.Dispose();
            _mockLogger.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cache eviction manager disposed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }
}