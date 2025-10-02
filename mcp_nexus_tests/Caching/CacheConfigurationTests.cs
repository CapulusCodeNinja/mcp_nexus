using System;
using Xunit;
using mcp_nexus.Caching;

namespace mcp_nexus_tests.Caching
{
    /// <summary>
    /// Tests for CacheEntry<T> and CacheStatistics data classes
    /// </summary>
    public class CacheConfigurationTests
    {
        #region CacheEntry<T> Tests

        [Fact]
        public void CacheEntry_DefaultValues_AreCorrect()
        {
            // Act
            var entry = new CacheEntry<string>(null!, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, 0, 0);

            // Assert
            Assert.Null(entry.Value);
            Assert.Equal(DateTime.MinValue, entry.CreatedAt);
            Assert.Equal(DateTime.MinValue, entry.LastAccessed);
            Assert.Equal(DateTime.MinValue, entry.ExpiresAt);
            Assert.Equal(0, entry.AccessCount);
            Assert.Equal(0, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithStringValue_SetsProperties()
        {
            // Arrange
            var createdAt = DateTime.UtcNow.AddMinutes(-10);
            var lastAccessed = DateTime.UtcNow.AddMinutes(-5);
            var expiresAt = DateTime.UtcNow.AddMinutes(20);
            const string value = "test value";
            const int accessCount = 5;
            const long sizeBytes = 1024;

            // Act
            var entry = new CacheEntry<string>(value, createdAt, lastAccessed, expiresAt, accessCount, sizeBytes);

            // Assert
            Assert.Equal(value, entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithIntValue_SetsProperties()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var lastAccessed = DateTime.UtcNow;
            var expiresAt = DateTime.UtcNow.AddHours(1);
            const int value = 42;
            const int accessCount = 1;
            const long sizeBytes = 4;

            // Act
            var entry = new CacheEntry<int>(value, createdAt, lastAccessed, expiresAt, accessCount, sizeBytes);

            // Assert
            Assert.Equal(value, entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithNullValue_HandlesCorrectly()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var lastAccessed = DateTime.UtcNow;
            var expiresAt = DateTime.UtcNow.AddHours(1);
            const int accessCount = 0;
            const long sizeBytes = 0;

            // Act
            var entry = new CacheEntry<string>(null!, createdAt, lastAccessed, expiresAt, accessCount, sizeBytes);

            // Assert
            Assert.Null(entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithMaxValues_HandlesCorrectly()
        {
            // Arrange
            var createdAt = DateTime.MaxValue;
            var lastAccessed = DateTime.MaxValue;
            var expiresAt = DateTime.MaxValue;
            const string value = "max value";
            const int accessCount = int.MaxValue;
            const long sizeBytes = long.MaxValue;

            // Act
            var entry = new CacheEntry<string>(value, createdAt, lastAccessed, expiresAt, accessCount, sizeBytes);

            // Assert
            Assert.Equal(value, entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithMinValues_HandlesCorrectly()
        {
            // Arrange
            var createdAt = DateTime.MinValue;
            var lastAccessed = DateTime.MinValue;
            var expiresAt = DateTime.MinValue;
            const string value = "min value";
            const int accessCount = 0;
            const long sizeBytes = 0;

            // Act
            var entry = new CacheEntry<string>(value, createdAt, lastAccessed, expiresAt, accessCount, sizeBytes);

            // Assert
            Assert.Equal(value, entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithNegativeValues_HandlesCorrectly()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var lastAccessed = DateTime.UtcNow;
            var expiresAt = DateTime.UtcNow.AddHours(-1); // Negative expiration
            const string value = "negative value";
            const int accessCount = -1;
            const long sizeBytes = -1;

            // Act
            var entry = new CacheEntry<string>(value, createdAt, lastAccessed, expiresAt, accessCount, sizeBytes);

            // Assert
            Assert.Equal(value, entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        #endregion

        #region CacheStatistics Tests

        [Fact]
        public void CacheStatistics_DefaultValues_AreCorrect()
        {
            // Act
            var statistics = new CacheStatistics();

            // Assert
            Assert.Equal(0, statistics.TotalEntries);
            Assert.Equal(0, statistics.HitCount);
            Assert.Equal(0, statistics.MissCount);
            Assert.Equal(0.0, statistics.HitRate);
            Assert.Equal(0, statistics.TotalSizeBytes);
            Assert.Equal(0, statistics.EvictionCount);
            Assert.Equal(0, statistics.ExpirationCount);
        }

        [Fact]
        public void CacheStatistics_WithValues_SetsProperties()
        {
            // Arrange
            const int totalEntries = 100;
            const long hitCount = 80;
            const long missCount = 20;
            const double hitRate = 0.8;
            const long totalSizeBytes = 1024000;
            const long evictionCount = 5;
            const long expirationCount = 3;

            // Act
            var statistics = new CacheStatistics
            {
                TotalEntries = totalEntries,
                HitCount = hitCount,
                MissCount = missCount,
                HitRate = hitRate,
                TotalSizeBytes = totalSizeBytes,
                EvictionCount = evictionCount,
                ExpirationCount = expirationCount
            };

            // Assert
            Assert.Equal(totalEntries, statistics.TotalEntries);
            Assert.Equal(hitCount, statistics.HitCount);
            Assert.Equal(missCount, statistics.MissCount);
            Assert.Equal(hitRate, statistics.HitRate);
            Assert.Equal(totalSizeBytes, statistics.TotalSizeBytes);
            Assert.Equal(evictionCount, statistics.EvictionCount);
            Assert.Equal(expirationCount, statistics.ExpirationCount);
        }

        [Fact]
        public void CacheStatistics_WithMaxValues_HandlesCorrectly()
        {
            // Arrange
            const int totalEntries = int.MaxValue;
            const long hitCount = long.MaxValue;
            const long missCount = long.MaxValue;
            const double hitRate = double.MaxValue;
            const long totalSizeBytes = long.MaxValue;
            const long evictionCount = long.MaxValue;
            const long expirationCount = long.MaxValue;

            // Act
            var statistics = new CacheStatistics
            {
                TotalEntries = totalEntries,
                HitCount = hitCount,
                MissCount = missCount,
                HitRate = hitRate,
                TotalSizeBytes = totalSizeBytes,
                EvictionCount = evictionCount,
                ExpirationCount = expirationCount
            };

            // Assert
            Assert.Equal(totalEntries, statistics.TotalEntries);
            Assert.Equal(hitCount, statistics.HitCount);
            Assert.Equal(missCount, statistics.MissCount);
            Assert.Equal(hitRate, statistics.HitRate);
            Assert.Equal(totalSizeBytes, statistics.TotalSizeBytes);
            Assert.Equal(evictionCount, statistics.EvictionCount);
            Assert.Equal(expirationCount, statistics.ExpirationCount);
        }

        [Fact]
        public void CacheStatistics_WithMinValues_HandlesCorrectly()
        {
            // Arrange
            const int totalEntries = 0;
            const long hitCount = 0;
            const long missCount = 0;
            const double hitRate = 0.0;
            const long totalSizeBytes = 0;
            const long evictionCount = 0;
            const long expirationCount = 0;

            // Act
            var statistics = new CacheStatistics
            {
                TotalEntries = totalEntries,
                HitCount = hitCount,
                MissCount = missCount,
                HitRate = hitRate,
                TotalSizeBytes = totalSizeBytes,
                EvictionCount = evictionCount,
                ExpirationCount = expirationCount
            };

            // Assert
            Assert.Equal(totalEntries, statistics.TotalEntries);
            Assert.Equal(hitCount, statistics.HitCount);
            Assert.Equal(missCount, statistics.MissCount);
            Assert.Equal(hitRate, statistics.HitRate);
            Assert.Equal(totalSizeBytes, statistics.TotalSizeBytes);
            Assert.Equal(evictionCount, statistics.EvictionCount);
            Assert.Equal(expirationCount, statistics.ExpirationCount);
        }

        [Fact]
        public void CacheStatistics_WithNegativeValues_HandlesCorrectly()
        {
            // Arrange
            const int totalEntries = -1;
            const long hitCount = -1;
            const long missCount = -1;
            const double hitRate = -1.0;
            const long totalSizeBytes = -1;
            const long evictionCount = -1;
            const long expirationCount = -1;

            // Act
            var statistics = new CacheStatistics
            {
                TotalEntries = totalEntries,
                HitCount = hitCount,
                MissCount = missCount,
                HitRate = hitRate,
                TotalSizeBytes = totalSizeBytes,
                EvictionCount = evictionCount,
                ExpirationCount = expirationCount
            };

            // Assert
            Assert.Equal(totalEntries, statistics.TotalEntries);
            Assert.Equal(hitCount, statistics.HitCount);
            Assert.Equal(missCount, statistics.MissCount);
            Assert.Equal(hitRate, statistics.HitRate);
            Assert.Equal(totalSizeBytes, statistics.TotalSizeBytes);
            Assert.Equal(evictionCount, statistics.EvictionCount);
            Assert.Equal(expirationCount, statistics.ExpirationCount);
        }

        #endregion
    }
}