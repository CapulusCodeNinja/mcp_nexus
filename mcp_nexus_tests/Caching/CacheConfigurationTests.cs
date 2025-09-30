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
            var entry = new CacheEntry<string>();

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
            var entry = new CacheEntry<string>
            {
                Value = value,
                CreatedAt = createdAt,
                LastAccessed = lastAccessed,
                ExpiresAt = expiresAt,
                AccessCount = accessCount,
                SizeBytes = sizeBytes
            };

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
            var entry = new CacheEntry<int>
            {
                Value = value,
                CreatedAt = createdAt,
                LastAccessed = lastAccessed,
                ExpiresAt = expiresAt,
                AccessCount = accessCount,
                SizeBytes = sizeBytes
            };

            // Assert
            Assert.Equal(value, entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithObjectValue_SetsProperties()
        {
            // Arrange
            var testObject = new { Name = "Test", Id = 123 };
            var createdAt = DateTime.UtcNow.AddSeconds(-30);
            var lastAccessed = DateTime.UtcNow.AddSeconds(-10);
            var expiresAt = DateTime.UtcNow.AddMinutes(5);
            const int accessCount = 3;
            const long sizeBytes = 200;

            // Act
            var entry = new CacheEntry<object>
            {
                Value = testObject,
                CreatedAt = createdAt,
                LastAccessed = lastAccessed,
                ExpiresAt = expiresAt,
                AccessCount = accessCount,
                SizeBytes = sizeBytes
            };

            // Assert
            Assert.Equal(testObject, entry.Value);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(lastAccessed, entry.LastAccessed);
            Assert.Equal(expiresAt, entry.ExpiresAt);
            Assert.Equal(accessCount, entry.AccessCount);
            Assert.Equal(sizeBytes, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithNullValue_HandlesCorrectly()
        {
            // Act
            var entry = new CacheEntry<string>
            {
                Value = null!,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(1),
                AccessCount = 0,
                SizeBytes = 0
            };

            // Assert
            Assert.Null(entry.Value);
            Assert.True(entry.CreatedAt > DateTime.MinValue);
            Assert.True(entry.LastAccessed > DateTime.MinValue);
            Assert.True(entry.ExpiresAt > DateTime.MinValue);
            Assert.Equal(0, entry.AccessCount);
            Assert.Equal(0, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var entry = new CacheEntry<string>
            {
                Value = "max value",
                CreatedAt = DateTime.MaxValue,
                LastAccessed = DateTime.MaxValue,
                ExpiresAt = DateTime.MaxValue,
                AccessCount = int.MaxValue,
                SizeBytes = long.MaxValue
            };

            // Assert
            Assert.Equal("max value", entry.Value);
            Assert.Equal(DateTime.MaxValue, entry.CreatedAt);
            Assert.Equal(DateTime.MaxValue, entry.LastAccessed);
            Assert.Equal(DateTime.MaxValue, entry.ExpiresAt);
            Assert.Equal(int.MaxValue, entry.AccessCount);
            Assert.Equal(long.MaxValue, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithMinValues_HandlesCorrectly()
        {
            // Act
            var entry = new CacheEntry<string>
            {
                Value = "min value",
                CreatedAt = DateTime.MinValue,
                LastAccessed = DateTime.MinValue,
                ExpiresAt = DateTime.MinValue,
                AccessCount = int.MinValue,
                SizeBytes = long.MinValue
            };

            // Assert
            Assert.Equal("min value", entry.Value);
            Assert.Equal(DateTime.MinValue, entry.CreatedAt);
            Assert.Equal(DateTime.MinValue, entry.LastAccessed);
            Assert.Equal(DateTime.MinValue, entry.ExpiresAt);
            Assert.Equal(int.MinValue, entry.AccessCount);
            Assert.Equal(long.MinValue, entry.SizeBytes);
        }

        [Fact]
        public void CacheEntry_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var entry = new CacheEntry<string>
            {
                Value = "negative test",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LastAccessed = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
                AccessCount = -5,
                SizeBytes = -100
            };

            // Assert
            Assert.Equal("negative test", entry.Value);
            Assert.True(entry.CreatedAt < DateTime.UtcNow);
            Assert.True(entry.LastAccessed < DateTime.UtcNow);
            Assert.True(entry.ExpiresAt < DateTime.UtcNow); // Expired
            Assert.Equal(-5, entry.AccessCount);
            Assert.Equal(-100, entry.SizeBytes);
        }

        #endregion

        #region CacheStatistics Tests

        [Fact]
        public void CacheStatistics_DefaultValues_AreCorrect()
        {
            // Act
            var stats = new CacheStatistics();

            // Assert
            Assert.Equal(0, stats.TotalEntries);
            Assert.Equal(0, stats.ExpiredEntries);
            Assert.Equal(0, stats.TotalSizeBytes);
            Assert.Equal(0, stats.TotalAccesses);
            Assert.Equal(0.0, stats.AverageAccessCount);
            Assert.Equal(0.0, stats.MemoryUsagePercent);
        }

        [Fact]
        public void CacheStatistics_WithValues_SetsProperties()
        {
            // Arrange
            const int totalEntries = 100;
            const int expiredEntries = 10;
            const long totalSizeBytes = 1024 * 1024; // 1MB
            const int totalAccesses = 500;
            const double averageAccessCount = 5.0;
            const double memoryUsagePercent = 75.5;

            // Act
            var stats = new CacheStatistics
            {
                TotalEntries = totalEntries,
                ExpiredEntries = expiredEntries,
                TotalSizeBytes = totalSizeBytes,
                TotalAccesses = totalAccesses,
                AverageAccessCount = averageAccessCount,
                MemoryUsagePercent = memoryUsagePercent
            };

            // Assert
            Assert.Equal(totalEntries, stats.TotalEntries);
            Assert.Equal(expiredEntries, stats.ExpiredEntries);
            Assert.Equal(totalSizeBytes, stats.TotalSizeBytes);
            Assert.Equal(totalAccesses, stats.TotalAccesses);
            Assert.Equal(averageAccessCount, stats.AverageAccessCount);
            Assert.Equal(memoryUsagePercent, stats.MemoryUsagePercent);
        }

        [Fact]
        public void CacheStatistics_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var stats = new CacheStatistics
            {
                TotalEntries = int.MaxValue,
                ExpiredEntries = int.MaxValue,
                TotalSizeBytes = long.MaxValue,
                TotalAccesses = int.MaxValue,
                AverageAccessCount = double.MaxValue,
                MemoryUsagePercent = double.MaxValue
            };

            // Assert
            Assert.Equal(int.MaxValue, stats.TotalEntries);
            Assert.Equal(int.MaxValue, stats.ExpiredEntries);
            Assert.Equal(long.MaxValue, stats.TotalSizeBytes);
            Assert.Equal(int.MaxValue, stats.TotalAccesses);
            Assert.Equal(double.MaxValue, stats.AverageAccessCount);
            Assert.Equal(double.MaxValue, stats.MemoryUsagePercent);
        }

        [Fact]
        public void CacheStatistics_WithMinValues_HandlesCorrectly()
        {
            // Act
            var stats = new CacheStatistics
            {
                TotalEntries = int.MinValue,
                ExpiredEntries = int.MinValue,
                TotalSizeBytes = long.MinValue,
                TotalAccesses = int.MinValue,
                AverageAccessCount = double.MinValue,
                MemoryUsagePercent = double.MinValue
            };

            // Assert
            Assert.Equal(int.MinValue, stats.TotalEntries);
            Assert.Equal(int.MinValue, stats.ExpiredEntries);
            Assert.Equal(long.MinValue, stats.TotalSizeBytes);
            Assert.Equal(int.MinValue, stats.TotalAccesses);
            Assert.Equal(double.MinValue, stats.AverageAccessCount);
            Assert.Equal(double.MinValue, stats.MemoryUsagePercent);
        }

        [Fact]
        public void CacheStatistics_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var stats = new CacheStatistics
            {
                TotalEntries = -10,
                ExpiredEntries = -5,
                TotalSizeBytes = -1000,
                TotalAccesses = -50,
                AverageAccessCount = -2.5,
                MemoryUsagePercent = -10.0
            };

            // Assert
            Assert.Equal(-10, stats.TotalEntries);
            Assert.Equal(-5, stats.ExpiredEntries);
            Assert.Equal(-1000, stats.TotalSizeBytes);
            Assert.Equal(-50, stats.TotalAccesses);
            Assert.Equal(-2.5, stats.AverageAccessCount);
            Assert.Equal(-10.0, stats.MemoryUsagePercent);
        }

        [Fact]
        public void CacheStatistics_WithZeroValues_HandlesCorrectly()
        {
            // Act
            var stats = new CacheStatistics
            {
                TotalEntries = 0,
                ExpiredEntries = 0,
                TotalSizeBytes = 0,
                TotalAccesses = 0,
                AverageAccessCount = 0.0,
                MemoryUsagePercent = 0.0
            };

            // Assert
            Assert.Equal(0, stats.TotalEntries);
            Assert.Equal(0, stats.ExpiredEntries);
            Assert.Equal(0, stats.TotalSizeBytes);
            Assert.Equal(0, stats.TotalAccesses);
            Assert.Equal(0.0, stats.AverageAccessCount);
            Assert.Equal(0.0, stats.MemoryUsagePercent);
        }

        [Fact]
        public void CacheStatistics_WithDecimalValues_HandlesCorrectly()
        {
            // Act
            var stats = new CacheStatistics
            {
                TotalEntries = 42,
                ExpiredEntries = 7,
                TotalSizeBytes = 123456789,
                TotalAccesses = 999,
                AverageAccessCount = 23.456789,
                MemoryUsagePercent = 87.123456
            };

            // Assert
            Assert.Equal(42, stats.TotalEntries);
            Assert.Equal(7, stats.ExpiredEntries);
            Assert.Equal(123456789, stats.TotalSizeBytes);
            Assert.Equal(999, stats.TotalAccesses);
            Assert.Equal(23.456789, stats.AverageAccessCount);
            Assert.Equal(87.123456, stats.MemoryUsagePercent);
        }

        [Fact]
        public void CacheStatistics_WithVerySmallValues_HandlesCorrectly()
        {
            // Act
            var stats = new CacheStatistics
            {
                TotalEntries = 1,
                ExpiredEntries = 0,
                TotalSizeBytes = 1,
                TotalAccesses = 1,
                AverageAccessCount = 0.000001,
                MemoryUsagePercent = 0.000001
            };

            // Assert
            Assert.Equal(1, stats.TotalEntries);
            Assert.Equal(0, stats.ExpiredEntries);
            Assert.Equal(1, stats.TotalSizeBytes);
            Assert.Equal(1, stats.TotalAccesses);
            Assert.Equal(0.000001, stats.AverageAccessCount);
            Assert.Equal(0.000001, stats.MemoryUsagePercent);
        }

        #endregion
    }
}
