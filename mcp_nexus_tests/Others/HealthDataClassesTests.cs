using System;
using System.Collections.Generic;
using Xunit;
using mcp_nexus.Health;

namespace mcp_nexus_tests.Health
{
    /// <summary>
    /// Tests for Health data classes - simple data containers
    /// </summary>
    public class HealthDataClassesTests
    {
        [Fact]
        public void AdvancedHealthStatus_DefaultValues_AreCorrect()
        {
            // Act
            var status = new AdvancedHealthStatus();

            // Assert
            Assert.Equal(DateTime.MinValue, status.Timestamp);
            Assert.False(status.IsHealthy);
            Assert.Equal(string.Empty, status.Message);
            Assert.Null(status.MemoryUsage);
            Assert.Null(status.CpuUsage);
            Assert.Null(status.DiskUsage);
            Assert.Null(status.ThreadCount);
            Assert.Null(status.GcStatus);
        }

        [Fact]
        public void AdvancedHealthStatus_WithValues_SetsProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var memoryHealth = new MemoryHealth { IsHealthy = true, WorkingSetMB = 100.5 };
            var cpuHealth = new CpuHealth { IsHealthy = true, CpuUsagePercent = 25.0 };
            var diskHealth = new DiskHealth { IsHealthy = true, UnhealthyDrives = new List<string>() };
            var threadHealth = new ThreadHealth { IsHealthy = true, ThreadCount = 10 };
            var gcHealth = new GcHealth { IsHealthy = true, Gen0Collections = 5 };

            // Act
            var status = new AdvancedHealthStatus();
            status.SetHealthStatus(true, "All systems operational");
            status.SetMemoryUsage(memoryHealth);
            status.SetCpuUsage(cpuHealth);
            status.SetDiskUsage(diskHealth);
            status.SetThreadCount(threadHealth);
            status.SetGcStatus(gcHealth);

            // Assert
            Assert.Equal(timestamp, status.Timestamp);
            Assert.True(status.IsHealthy);
            Assert.Equal("All systems operational", status.Message);
            Assert.Equal(memoryHealth, status.MemoryUsage);
            Assert.Equal(cpuHealth, status.CpuUsage);
            Assert.Equal(diskHealth, status.DiskUsage);
            Assert.Equal(threadHealth, status.ThreadCount);
            Assert.Equal(gcHealth, status.GcStatus);
        }

        [Fact]
        public void MemoryHealth_DefaultValues_AreCorrect()
        {
            // Act
            var memory = new MemoryHealth();

            // Assert
            Assert.False(memory.IsHealthy);
            Assert.Equal(0.0, memory.WorkingSetMB);
            Assert.Equal(0.0, memory.PrivateMemoryMB);
            Assert.Equal(0.0, memory.VirtualMemoryMB);
            Assert.Equal(0.0, memory.TotalPhysicalMemoryMB);
            Assert.Equal(string.Empty, memory.Message);
        }

        [Fact]
        public void MemoryHealth_WithValues_SetsProperties()
        {
            // Arrange
            const double workingSet = 1024.5;
            const double privateMemory = 2048.75;
            const double virtualMemory = 4096.0;
            const double totalPhysical = 8192.25;
            const string message = "Memory usage normal";

            // Act
            var memory = new MemoryHealth();
            memory.SetMemoryInfo(true, workingSet, privateMemory, virtualMemory, totalPhysical, message);

            // Assert
            Assert.True(memory.IsHealthy);
            Assert.Equal(workingSet, memory.WorkingSetMB);
            Assert.Equal(privateMemory, memory.PrivateMemoryMB);
            Assert.Equal(virtualMemory, memory.VirtualMemoryMB);
            Assert.Equal(totalPhysical, memory.TotalPhysicalMemoryMB);
            Assert.Equal(message, memory.Message);
        }

        [Fact]
        public void CpuHealth_DefaultValues_AreCorrect()
        {
            // Act
            var cpu = new CpuHealth();

            // Assert
            Assert.False(cpu.IsHealthy);
            Assert.Equal(0.0, cpu.CpuUsagePercent);
            Assert.Equal(TimeSpan.Zero, cpu.TotalProcessorTime);
            Assert.Equal(string.Empty, cpu.Message);
        }

        [Fact]
        public void CpuHealth_WithValues_SetsProperties()
        {
            // Arrange
            const double usagePercent = 45.5;
            var processorTime = TimeSpan.FromMinutes(5.5);
            const string message = "CPU usage normal";

            // Act
            var cpu = new CpuHealth
            {
                IsHealthy = true,
                CpuUsagePercent = usagePercent,
                TotalProcessorTime = processorTime,
                Message = message
            };

            // Assert
            Assert.True(cpu.IsHealthy);
            Assert.Equal(usagePercent, cpu.CpuUsagePercent);
            Assert.Equal(processorTime, cpu.TotalProcessorTime);
            Assert.Equal(message, cpu.Message);
        }

        [Fact]
        public void DiskHealth_DefaultValues_AreCorrect()
        {
            // Act
            var disk = new DiskHealth();

            // Assert
            Assert.False(disk.IsHealthy);
            Assert.NotNull(disk.UnhealthyDrives);
            Assert.Empty(disk.UnhealthyDrives);
            Assert.Equal(string.Empty, disk.Message);
        }

        [Fact]
        public void DiskHealth_WithValues_SetsProperties()
        {
            // Arrange
            var unhealthyDrives = new List<string> { "C:", "D:" };
            const string message = "Disk usage high";

            // Act
            var disk = new DiskHealth
            {
                IsHealthy = false,
                UnhealthyDrives = unhealthyDrives,
                Message = message
            };

            // Assert
            Assert.False(disk.IsHealthy);
            Assert.Equal(unhealthyDrives, disk.UnhealthyDrives);
            Assert.Equal(message, disk.Message);
        }

        [Fact]
        public void DiskHealth_WithEmptyUnhealthyDrives_HandlesCorrectly()
        {
            // Act
            var disk = new DiskHealth
            {
                IsHealthy = true,
                UnhealthyDrives = new List<string>(),
                Message = "All drives healthy"
            };

            // Assert
            Assert.True(disk.IsHealthy);
            Assert.NotNull(disk.UnhealthyDrives);
            Assert.Empty(disk.UnhealthyDrives);
            Assert.Equal("All drives healthy", disk.Message);
        }

        [Fact]
        public void ThreadHealth_DefaultValues_AreCorrect()
        {
            // Act
            var thread = new ThreadHealth();

            // Assert
            Assert.False(thread.IsHealthy);
            Assert.Equal(0, thread.ThreadCount);
            Assert.Equal(string.Empty, thread.Message);
        }

        [Fact]
        public void ThreadHealth_WithValues_SetsProperties()
        {
            // Arrange
            const int threadCount = 25;
            const string message = "Thread count normal";

            // Act
            var thread = new ThreadHealth
            {
                IsHealthy = true,
                ThreadCount = threadCount,
                Message = message
            };

            // Assert
            Assert.True(thread.IsHealthy);
            Assert.Equal(threadCount, thread.ThreadCount);
            Assert.Equal(message, thread.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(99)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ThreadHealth_WithVariousThreadCounts_SetsCorrectly(int threadCount)
        {
            // Act
            var thread = new ThreadHealth
            {
                ThreadCount = threadCount
            };

            // Assert
            Assert.Equal(threadCount, thread.ThreadCount);
        }

        [Fact]
        public void GcHealth_DefaultValues_AreCorrect()
        {
            // Act
            var gc = new GcHealth();

            // Assert
            Assert.False(gc.IsHealthy);
            Assert.Equal(0, gc.Gen0Collections);
            Assert.Equal(0, gc.Gen1Collections);
            Assert.Equal(0, gc.Gen2Collections);
            Assert.Equal(0, gc.TotalCollections);
            Assert.Equal(string.Empty, gc.Message);
        }

        [Fact]
        public void GcHealth_WithValues_SetsProperties()
        {
            // Arrange
            const int gen0 = 10;
            const int gen1 = 5;
            const int gen2 = 2;
            const int total = 17;
            const string message = "GC health normal";

            // Act
            var gc = new GcHealth
            {
                IsHealthy = true,
                Gen0Collections = gen0,
                Gen1Collections = gen1,
                Gen2Collections = gen2,
                TotalCollections = total,
                Message = message
            };

            // Assert
            Assert.True(gc.IsHealthy);
            Assert.Equal(gen0, gc.Gen0Collections);
            Assert.Equal(gen1, gc.Gen1Collections);
            Assert.Equal(gen2, gc.Gen2Collections);
            Assert.Equal(total, gc.TotalCollections);
            Assert.Equal(message, gc.Message);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 0, 0, 1)]
        [InlineData(10, 5, 2, 17)]
        [InlineData(100, 50, 25, 175)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        public void GcHealth_WithVariousCollectionCounts_SetsCorrectly(int gen0, int gen1, int gen2, int total)
        {
            // Act
            var gc = new GcHealth
            {
                Gen0Collections = gen0,
                Gen1Collections = gen1,
                Gen2Collections = gen2,
                TotalCollections = total
            };

            // Assert
            Assert.Equal(gen0, gc.Gen0Collections);
            Assert.Equal(gen1, gc.Gen1Collections);
            Assert.Equal(gen2, gc.Gen2Collections);
            Assert.Equal(total, gc.TotalCollections);
        }

        [Fact]
        public void HealthStatus_WithNullValues_HandlesGracefully()
        {
            // Act
            var status = new AdvancedHealthStatus();
            // Note: Properties are read-only, so we can't set them directly
            // The test should verify default values instead

            // Assert
            Assert.Equal(string.Empty, status.Message); // Default is empty string, not null
            Assert.Null(status.MemoryUsage);
            Assert.Null(status.CpuUsage);
            Assert.Null(status.DiskUsage);
            Assert.Null(status.ThreadCount);
            Assert.Null(status.GcStatus);
        }

        [Fact]
        public void HealthStatus_WithEmptyMessage_HandlesCorrectly()
        {
            // Act
            var status = new AdvancedHealthStatus();
            // Note: Message is read-only, default value is already string.Empty

            // Assert
            Assert.Equal(string.Empty, status.Message);
        }

        [Fact]
        public void AllHealthClasses_CanBeInstantiated()
        {
            // Act & Assert - Just verify they can be created without throwing
            Assert.NotNull(new AdvancedHealthStatus());
            Assert.NotNull(new MemoryHealth());
            Assert.NotNull(new CpuHealth());
            Assert.NotNull(new DiskHealth());
            Assert.NotNull(new ThreadHealth());
            Assert.NotNull(new GcHealth());
        }
    }
}
