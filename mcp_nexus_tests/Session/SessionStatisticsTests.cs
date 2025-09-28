using System;
using Xunit;
using mcp_nexus.Session;

namespace mcp_nexus_tests.Session
{
    /// <summary>
    /// Tests for SessionStatistics and MemoryUsageInfo data classes - simple data containers
    /// </summary>
    public class SessionStatisticsTests
    {
        [Fact]
        public void SessionStatistics_DefaultValues_AreCorrect()
        {
            // Act
            var statistics = new SessionStatistics();

            // Assert
            Assert.Equal(0, statistics.ActiveSessions);
            Assert.Equal(0, statistics.TotalSessionsCreated);
            Assert.Equal(0, statistics.TotalSessionsClosed);
            Assert.Equal(0, statistics.TotalSessionsExpired);
            Assert.Equal(0, statistics.TotalCommandsProcessed);
            Assert.Equal(TimeSpan.Zero, statistics.AverageSessionLifetime);
            Assert.Equal(TimeSpan.Zero, statistics.Uptime);
            Assert.NotNull(statistics.MemoryUsage);
        }

        [Fact]
        public void SessionStatistics_WithValues_SetsProperties()
        {
            // Arrange
            var averageLifetime = TimeSpan.FromMinutes(30);
            var uptime = TimeSpan.FromHours(2);
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = 1024 * 1024,
                PrivateMemoryBytes = 512 * 1024,
                GCTotalMemoryBytes = 256 * 1024
            };

            // Act
            var statistics = new SessionStatistics
            {
                ActiveSessions = 5,
                TotalSessionsCreated = 100,
                TotalSessionsClosed = 95,
                TotalSessionsExpired = 10,
                TotalCommandsProcessed = 1000,
                AverageSessionLifetime = averageLifetime,
                Uptime = uptime,
                MemoryUsage = memoryUsage
            };

            // Assert
            Assert.Equal(5, statistics.ActiveSessions);
            Assert.Equal(100, statistics.TotalSessionsCreated);
            Assert.Equal(95, statistics.TotalSessionsClosed);
            Assert.Equal(10, statistics.TotalSessionsExpired);
            Assert.Equal(1000, statistics.TotalCommandsProcessed);
            Assert.Equal(averageLifetime, statistics.AverageSessionLifetime);
            Assert.Equal(uptime, statistics.Uptime);
            Assert.Equal(memoryUsage, statistics.MemoryUsage);
        }

        [Fact]
        public void SessionStatistics_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var statistics = new SessionStatistics
            {
                ActiveSessions = -1,
                TotalSessionsCreated = -100,
                TotalSessionsClosed = -95,
                TotalSessionsExpired = -10,
                TotalCommandsProcessed = -1000
            };

            // Assert
            Assert.Equal(-1, statistics.ActiveSessions);
            Assert.Equal(-100, statistics.TotalSessionsCreated);
            Assert.Equal(-95, statistics.TotalSessionsClosed);
            Assert.Equal(-10, statistics.TotalSessionsExpired);
            Assert.Equal(-1000, statistics.TotalCommandsProcessed);
        }

        [Fact]
        public void SessionStatistics_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var statistics = new SessionStatistics
            {
                ActiveSessions = int.MaxValue,
                TotalSessionsCreated = long.MaxValue,
                TotalSessionsClosed = long.MaxValue,
                TotalSessionsExpired = long.MaxValue,
                TotalCommandsProcessed = long.MaxValue,
                AverageSessionLifetime = TimeSpan.MaxValue,
                Uptime = TimeSpan.MaxValue
            };

            // Assert
            Assert.Equal(int.MaxValue, statistics.ActiveSessions);
            Assert.Equal(long.MaxValue, statistics.TotalSessionsCreated);
            Assert.Equal(long.MaxValue, statistics.TotalSessionsClosed);
            Assert.Equal(long.MaxValue, statistics.TotalSessionsExpired);
            Assert.Equal(long.MaxValue, statistics.TotalCommandsProcessed);
            Assert.Equal(TimeSpan.MaxValue, statistics.AverageSessionLifetime);
            Assert.Equal(TimeSpan.MaxValue, statistics.Uptime);
        }

        [Fact]
        public void MemoryUsageInfo_DefaultValues_AreCorrect()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo();

            // Assert
            Assert.Equal(0, memoryUsage.WorkingSetBytes);
            Assert.Equal(0, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(0, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithValues_SetsProperties()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = 1024 * 1024 * 100, // 100 MB
                PrivateMemoryBytes = 1024 * 1024 * 50, // 50 MB
                GCTotalMemoryBytes = 1024 * 1024 * 25 // 25 MB
            };

            // Assert
            Assert.Equal(1024 * 1024 * 100, memoryUsage.WorkingSetBytes);
            Assert.Equal(1024 * 1024 * 50, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(1024 * 1024 * 25, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = -1024,
                PrivateMemoryBytes = -2048,
                GCTotalMemoryBytes = -4096
            };

            // Assert
            Assert.Equal(-1024, memoryUsage.WorkingSetBytes);
            Assert.Equal(-2048, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(-4096, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithMaxValues_HandlesCorrectly()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = long.MaxValue,
                PrivateMemoryBytes = long.MaxValue,
                GCTotalMemoryBytes = long.MaxValue
            };

            // Assert
            Assert.Equal(long.MaxValue, memoryUsage.WorkingSetBytes);
            Assert.Equal(long.MaxValue, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(long.MaxValue, memoryUsage.GCTotalMemoryBytes);
        }

        [Fact]
        public void MemoryUsageInfo_WithZeroValues_HandlesCorrectly()
        {
            // Act
            var memoryUsage = new MemoryUsageInfo
            {
                WorkingSetBytes = 0,
                PrivateMemoryBytes = 0,
                GCTotalMemoryBytes = 0
            };

            // Assert
            Assert.Equal(0, memoryUsage.WorkingSetBytes);
            Assert.Equal(0, memoryUsage.PrivateMemoryBytes);
            Assert.Equal(0, memoryUsage.GCTotalMemoryBytes);
        }
    }
}
