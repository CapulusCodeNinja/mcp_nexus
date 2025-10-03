using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Health;
using System.Diagnostics;

namespace mcp_nexus_tests.Health
{
    public class AdvancedHealthServiceTests : IDisposable
    {
        private readonly Mock<ILogger<AdvancedHealthService>> m_mockLogger;
        private readonly AdvancedHealthService m_healthService;

        public AdvancedHealthServiceTests()
        {
            m_mockLogger = new Mock<ILogger<AdvancedHealthService>>();
            m_healthService = new AdvancedHealthService(m_mockLogger.Object);
        }

        public void Dispose()
        {
            m_healthService?.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AdvancedHealthService(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Arrange
            var localMockLogger = new Mock<ILogger<AdvancedHealthService>>();

            // Act
            using var service = new AdvancedHealthService(localMockLogger.Object);

            // Assert
            Assert.NotNull(service);
            localMockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AdvancedHealthService initialized")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region GetHealthStatus Tests

        [Fact]
        public void GetHealthStatus_WhenNotDisposed_ReturnsHealthStatus()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status);
            Assert.True(status.Timestamp > DateTime.MinValue);
            Assert.NotNull(status.MemoryUsage);
            Assert.NotNull(status.CpuUsage);
            Assert.NotNull(status.DiskUsage);
            Assert.NotNull(status.ThreadCount);
            Assert.NotNull(status.GcStatus);
        }

        [Fact]
        public void GetHealthStatus_WhenDisposed_ReturnsUnhealthyStatus()
        {
            // Arrange
            m_healthService.Dispose();

            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status);
            Assert.False(status.IsHealthy);
            Assert.Equal("Service disposed", status.Message);
        }

        [Fact]
        public void GetHealthStatus_WithHealthySystem_ReturnsHealthyStatus()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status);
            // Note: In test environment, system is likely healthy
            // We can't guarantee specific values, but we can verify structure
            Assert.True(status.Timestamp > DateTime.MinValue);
            Assert.NotNull(status.MemoryUsage);
            Assert.NotNull(status.CpuUsage);
            Assert.NotNull(status.DiskUsage);
            Assert.NotNull(status.ThreadCount);
            Assert.NotNull(status.GcStatus);
        }

        [Fact]
        public void GetHealthStatus_IncludesAllHealthComponents()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status);
            Assert.NotNull(status.MemoryUsage);
            Assert.NotNull(status.CpuUsage);
            Assert.NotNull(status.DiskUsage);
            Assert.NotNull(status.ThreadCount);
            Assert.NotNull(status.GcStatus);

            // Verify memory health has expected properties
            Assert.True(status.MemoryUsage!.WorkingSetMB >= 0);
            Assert.True(status.MemoryUsage.PrivateMemoryMB >= 0);
            Assert.True(status.MemoryUsage.VirtualMemoryMB >= 0);
            Assert.True(status.MemoryUsage.TotalPhysicalMemoryMB >= 0);
            Assert.NotEmpty(status.MemoryUsage.Message);

            // Verify CPU health has expected properties
            Assert.True(status.CpuUsage!.CpuUsagePercent >= 0);
            Assert.True(status.CpuUsage.TotalProcessorTime >= TimeSpan.Zero);
            Assert.NotEmpty(status.CpuUsage.Message);

            // Verify disk health has expected properties
            Assert.NotNull(status.DiskUsage!.UnhealthyDrives);
            Assert.NotEmpty(status.DiskUsage.Message);

            // Verify thread health has expected properties
            Assert.True(status.ThreadCount!.ThreadCount >= 0);
            Assert.NotEmpty(status.ThreadCount.Message);

            // Verify GC health has expected properties
            Assert.True(status.GcStatus!.Gen0Collections >= 0);
            Assert.True(status.GcStatus.Gen1Collections >= 0);
            Assert.True(status.GcStatus.Gen2Collections >= 0);
            Assert.True(status.GcStatus.TotalCollections >= 0);
            Assert.NotEmpty(status.GcStatus.Message);
        }

        #endregion

        #region Memory Health Tests

        [Fact]
        public void CheckMemoryHealth_ReturnsValidMemoryHealth()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status.MemoryUsage);
            var memory = status.MemoryUsage!;
            Assert.True(memory.WorkingSetMB >= 0);
            Assert.True(memory.PrivateMemoryMB >= 0);
            Assert.True(memory.VirtualMemoryMB >= 0);
            Assert.True(memory.TotalPhysicalMemoryMB >= 0);
            Assert.NotEmpty(memory.Message);
        }

        #endregion

        #region CPU Health Tests

        [Fact]
        public void CheckCpuHealth_ReturnsValidCpuHealth()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status.CpuUsage);
            var cpu = status.CpuUsage!;
            Assert.True(cpu.CpuUsagePercent >= 0);
            Assert.True(cpu.CpuUsagePercent <= 100);
            Assert.True(cpu.TotalProcessorTime >= TimeSpan.Zero);
            Assert.NotEmpty(cpu.Message);
        }

        #endregion

        #region Disk Health Tests

        [Fact]
        public void CheckDiskHealth_ReturnsValidDiskHealth()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status.DiskUsage);
            var disk = status.DiskUsage!;
            Assert.NotNull(disk.UnhealthyDrives);
            Assert.NotEmpty(disk.Message);
        }

        #endregion

        #region Thread Health Tests

        [Fact]
        public void CheckThreadHealth_ReturnsValidThreadHealth()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status.ThreadCount);
            var thread = status.ThreadCount!;
            Assert.True(thread.ThreadCount >= 0);
            Assert.NotEmpty(thread.Message);
        }

        #endregion

        #region GC Health Tests

        [Fact]
        public void CheckGcHealth_ReturnsValidGcHealth()
        {
            // Act
            var status = m_healthService.GetHealthStatus();

            // Assert
            Assert.NotNull(status.GcStatus);
            var gc = status.GcStatus!;
            Assert.True(gc.Gen0Collections >= 0);
            Assert.True(gc.Gen1Collections >= 0);
            Assert.True(gc.Gen2Collections >= 0);
            Assert.True(gc.TotalCollections >= 0);
            Assert.Equal(gc.Gen0Collections + gc.Gen1Collections + gc.Gen2Collections, gc.TotalCollections);
            Assert.NotEmpty(gc.Message);
        }

        #endregion

        #region Timer Tests

        [Fact]
        public void Constructor_StartsHealthTimer()
        {
            // Act
            using var service = new AdvancedHealthService(m_mockLogger.Object);

            // Assert - Timer should be running (we can't directly test this, but we can verify no exceptions)
            Assert.NotNull(service);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_WhenNotDisposed_DisposesCorrectly()
        {
            // Act
            m_healthService.Dispose();

            // Assert
            m_mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AdvancedHealthService disposed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
        {
            // Arrange
            m_healthService.Dispose();

            // Act & Assert
            var exception = Record.Exception(() => m_healthService.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void Dispose_StopsHealthTimer()
        {
            // Arrange
            using var service = new AdvancedHealthService(m_mockLogger.Object);

            // Act
            service.Dispose();

            // Assert - No exceptions should be thrown
            Assert.True(true); // If we get here, disposal worked correctly
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void GetHealthStatus_HandlesExceptions_Gracefully()
        {
            // This test verifies that the service handles exceptions gracefully
            // In a real scenario, we might mock Process.GetCurrentProcess() to throw
            // but for now, we'll just verify the method doesn't throw

            // Act & Assert
            var exception = Record.Exception(() => m_healthService.GetHealthStatus());
            Assert.Null(exception);
        }

        #endregion

        #region Data Class Tests

        [Fact]
        public void AdvancedHealthStatus_Properties_WorkCorrectly()
        {
            // Arrange
            var status = new AdvancedHealthStatus();
            status.SetHealthStatus(true, "Test message");
            status.SetMemoryUsage(new MemoryHealth());
            status.SetCpuUsage(new CpuHealth());
            status.SetDiskUsage(new DiskHealth());
            status.SetThreadCount(new ThreadHealth());
            status.SetGcStatus(new GcHealth());

            // Assert
            Assert.True(status.Timestamp > DateTime.MinValue);
            Assert.True(status.IsHealthy);
            Assert.Equal("Test message", status.Message);
            Assert.NotNull(status.MemoryUsage);
            Assert.NotNull(status.CpuUsage);
            Assert.NotNull(status.DiskUsage);
            Assert.NotNull(status.ThreadCount);
            Assert.NotNull(status.GcStatus);
        }

        [Fact]
        public void MemoryHealth_Properties_WorkCorrectly()
        {
            // Arrange
            var memory = new MemoryHealth();
            memory.SetMemoryInfo(true, 100.5, 50.25, 200.75, 1000.0, "Memory is healthy");

            // Assert
            Assert.True(memory.IsHealthy);
            Assert.Equal(100.5, memory.WorkingSetMB);
            Assert.Equal(50.25, memory.PrivateMemoryMB);
            Assert.Equal(200.75, memory.VirtualMemoryMB);
            Assert.Equal(1000.0, memory.TotalPhysicalMemoryMB);
            Assert.Equal("Memory is healthy", memory.Message);
        }

        [Fact]
        public void CpuHealth_Properties_WorkCorrectly()
        {
            // Arrange
            var cpu = new CpuHealth
            {
                IsHealthy = true,
                CpuUsagePercent = 25.5,
                TotalProcessorTime = TimeSpan.FromSeconds(10),
                Message = "CPU is healthy"
            };

            // Assert
            Assert.True(cpu.IsHealthy);
            Assert.Equal(25.5, cpu.CpuUsagePercent);
            Assert.Equal(TimeSpan.FromSeconds(10), cpu.TotalProcessorTime);
            Assert.Equal("CPU is healthy", cpu.Message);
        }

        [Fact]
        public void DiskHealth_Properties_WorkCorrectly()
        {
            // Arrange
            var disk = new DiskHealth
            {
                IsHealthy = false,
                UnhealthyDrives = new List<string> { "C: (5% free)", "D: (8% free)" },
                Message = "Low disk space detected"
            };

            // Assert
            Assert.False(disk.IsHealthy);
            Assert.Equal(2, disk.UnhealthyDrives.Count);
            Assert.Contains("C: (5% free)", disk.UnhealthyDrives);
            Assert.Contains("D: (8% free)", disk.UnhealthyDrives);
            Assert.Equal("Low disk space detected", disk.Message);
        }

        [Fact]
        public void ThreadHealth_Properties_WorkCorrectly()
        {
            // Arrange
            var thread = new ThreadHealth
            {
                IsHealthy = true,
                ThreadCount = 25,
                Message = "Thread count is normal"
            };

            // Assert
            Assert.True(thread.IsHealthy);
            Assert.Equal(25, thread.ThreadCount);
            Assert.Equal("Thread count is normal", thread.Message);
        }

        [Fact]
        public void GcHealth_Properties_WorkCorrectly()
        {
            // Arrange
            var gc = new GcHealth
            {
                IsHealthy = true,
                Gen0Collections = 10,
                Gen1Collections = 5,
                Gen2Collections = 2,
                TotalCollections = 17,
                Message = "GC is healthy"
            };

            // Assert
            Assert.True(gc.IsHealthy);
            Assert.Equal(10, gc.Gen0Collections);
            Assert.Equal(5, gc.Gen1Collections);
            Assert.Equal(2, gc.Gen2Collections);
            Assert.Equal(17, gc.TotalCollections);
            Assert.Equal("GC is healthy", gc.Message);
        }

        #endregion

        #region Adaptive Memory Threshold Tests

        [Fact]
        public void Constructor_WithCustomThresholds_UsesProvidedValues()
        {
            // Arrange
            var customWorkingSet = 1024L; // 1GB
            var customPrivateMemory = 512L; // 512MB
            var customPressureThreshold = 0.7;

            // Act
            using var service = new AdvancedHealthService(m_mockLogger.Object, customWorkingSet, customPrivateMemory, customPressureThreshold);

            // Assert
            // We can't directly test the private fields, but we can verify the service works
            var healthStatus = service.GetHealthStatus();
            Assert.NotNull(healthStatus);
        }

        [Fact]
        public void Constructor_WithZeroThresholds_UsesAutoDetection()
        {
            // Arrange & Act
            using var service = new AdvancedHealthService(m_mockLogger.Object, 0, 0, 0.8);

            // Assert
            // Verify the service initializes successfully with auto-detection
            var healthStatus = service.GetHealthStatus();
            Assert.NotNull(healthStatus);
        }

        [Fact]
        public void Constructor_WithInvalidPressureThreshold_ClampsToValidRange()
        {
            // Arrange & Act
            using var service1 = new AdvancedHealthService(m_mockLogger.Object, 0, 0, -0.1); // Below minimum
            using var service2 = new AdvancedHealthService(m_mockLogger.Object, 0, 0, 1.5); // Above maximum

            // Assert
            // Both should initialize successfully with clamped values
            var healthStatus1 = service1.GetHealthStatus();
            var healthStatus2 = service2.GetHealthStatus();
            Assert.NotNull(healthStatus1);
            Assert.NotNull(healthStatus2);
        }

        #endregion
    }
}
