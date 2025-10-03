using mcp_nexus.Health;
using System;
using Xunit;

namespace mcp_nexus_tests.Health
{
    public class AdvancedHealthStatusBuilderTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_CreatesNewInstance()
        {
            // Act
            var builder = new AdvancedHealthStatusBuilder();

            // Assert
            Assert.NotNull(builder);
        }

        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            // Act
            var builder = new AdvancedHealthStatusBuilder();
            var healthStatus = builder.Build();

            // Assert
            Assert.NotNull(healthStatus);
            Assert.False(healthStatus.IsHealthy);
            Assert.Equal(string.Empty, healthStatus.Message);
            Assert.True(healthStatus.Timestamp <= DateTime.UtcNow);
            Assert.Null(healthStatus.MemoryUsage);
            Assert.Null(healthStatus.CpuUsage);
            Assert.Null(healthStatus.DiskUsage);
            Assert.Null(healthStatus.ThreadCount);
            Assert.Null(healthStatus.GcStatus);
        }

        #endregion

        #region SetHealthStatus Tests

        [Fact]
        public void SetHealthStatus_WithHealthyTrue_SetsCorrectValues()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var isHealthy = true;
            var message = "System is healthy";

            // Act
            var result = builder.SetHealthStatus(isHealthy, message);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.True(healthStatus.IsHealthy);
            Assert.Equal(message, healthStatus.Message);
        }

        [Fact]
        public void SetHealthStatus_WithHealthyFalse_SetsCorrectValues()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var isHealthy = false;
            var message = "System is unhealthy";

            // Act
            var result = builder.SetHealthStatus(isHealthy, message);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.False(healthStatus.IsHealthy);
            Assert.Equal(message, healthStatus.Message);
        }

        [Fact]
        public void SetHealthStatus_WithNullMessage_SetsEmptyString()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var isHealthy = true;
            string? message = null;

            // Act
            var result = builder.SetHealthStatus(isHealthy, message!);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.True(healthStatus.IsHealthy);
            Assert.Equal(string.Empty, healthStatus.Message);
        }

        [Fact]
        public void SetHealthStatus_WithEmptyMessage_SetsEmptyString()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var isHealthy = true;
            var message = string.Empty;

            // Act
            var result = builder.SetHealthStatus(isHealthy, message);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.True(healthStatus.IsHealthy);
            Assert.Equal(string.Empty, healthStatus.Message);
        }

        [Fact]
        public void SetHealthStatus_WithVeryLongMessage_HandlesCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var isHealthy = true;
            var message = new string('A', 10000);

            // Act
            var result = builder.SetHealthStatus(isHealthy, message);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.True(healthStatus.IsHealthy);
            Assert.Equal(message, healthStatus.Message);
        }

        [Fact]
        public void SetHealthStatus_WithUnicodeMessage_HandlesCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var isHealthy = true;
            var message = "系统健康状态正常 ✅";

            // Act
            var result = builder.SetHealthStatus(isHealthy, message);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.True(healthStatus.IsHealthy);
            Assert.Equal(message, healthStatus.Message);
        }

        #endregion

        #region WithMemoryUsage Tests

        [Fact]
        public void WithMemoryUsage_WithValidMemoryHealth_SetsCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var memoryHealth = new MemoryHealth();

            // Act
            var result = builder.WithMemoryUsage(memoryHealth);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Same(memoryHealth, healthStatus.MemoryUsage);
        }

        [Fact]
        public void WithMemoryUsage_WithNull_SetsNull()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result = builder.WithMemoryUsage(null);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Null(healthStatus.MemoryUsage);
        }

        #endregion

        #region WithCpuUsage Tests

        [Fact]
        public void WithCpuUsage_WithValidCpuHealth_SetsCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var cpuHealth = new CpuHealth();

            // Act
            var result = builder.WithCpuUsage(cpuHealth);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Same(cpuHealth, healthStatus.CpuUsage);
        }

        [Fact]
        public void WithCpuUsage_WithNull_SetsNull()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result = builder.WithCpuUsage(null);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Null(healthStatus.CpuUsage);
        }

        #endregion

        #region WithDiskUsage Tests

        [Fact]
        public void WithDiskUsage_WithValidDiskHealth_SetsCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var diskHealth = new DiskHealth();

            // Act
            var result = builder.WithDiskUsage(diskHealth);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Same(diskHealth, healthStatus.DiskUsage);
        }

        [Fact]
        public void WithDiskUsage_WithNull_SetsNull()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result = builder.WithDiskUsage(null);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Null(healthStatus.DiskUsage);
        }

        #endregion

        #region WithThreadCount Tests

        [Fact]
        public void WithThreadCount_WithValidThreadHealth_SetsCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var threadHealth = new ThreadHealth();

            // Act
            var result = builder.WithThreadCount(threadHealth);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Same(threadHealth, healthStatus.ThreadCount);
        }

        [Fact]
        public void WithThreadCount_WithNull_SetsNull()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result = builder.WithThreadCount(null);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Null(healthStatus.ThreadCount);
        }

        #endregion

        #region WithGcStatus Tests

        [Fact]
        public void WithGcStatus_WithValidGcHealth_SetsCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var gcHealth = new GcHealth();

            // Act
            var result = builder.WithGcStatus(gcHealth);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Same(gcHealth, healthStatus.GcStatus);
        }

        [Fact]
        public void WithGcStatus_WithNull_SetsNull()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result = builder.WithGcStatus(null);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result); // Method chaining
            Assert.Null(healthStatus.GcStatus);
        }

        #endregion

        #region Build Tests

        [Fact]
        public void Build_ReturnsAdvancedHealthStatus()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result = builder.Build();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<AdvancedHealthStatus>(result);
        }

        [Fact]
        public void Build_ReturnsSameInstance()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result1 = builder.Build();
            var result2 = builder.Build();

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public void Build_AfterMultipleOperations_ReturnsCorrectState()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var memoryHealth = new MemoryHealth();
            var cpuHealth = new CpuHealth();
            var diskHealth = new DiskHealth();
            var threadHealth = new ThreadHealth();
            var gcHealth = new GcHealth();

            // Act
            builder.SetHealthStatus(true, "All systems operational")
                   .WithMemoryUsage(memoryHealth)
                   .WithCpuUsage(cpuHealth)
                   .WithDiskUsage(diskHealth)
                   .WithThreadCount(threadHealth)
                   .WithGcStatus(gcHealth);

            var result = builder.Build();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal("All systems operational", result.Message);
            Assert.Same(memoryHealth, result.MemoryUsage);
            Assert.Same(cpuHealth, result.CpuUsage);
            Assert.Same(diskHealth, result.DiskUsage);
            Assert.Same(threadHealth, result.ThreadCount);
            Assert.Same(gcHealth, result.GcStatus);
        }

        #endregion

        #region Method Chaining Tests

        [Fact]
        public void MethodChaining_AllMethods_ReturnBuilderInstance()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var memoryHealth = new MemoryHealth();
            var cpuHealth = new CpuHealth();
            var diskHealth = new DiskHealth();
            var threadHealth = new ThreadHealth();
            var gcHealth = new GcHealth();

            // Act & Assert
            var result1 = builder.SetHealthStatus(true, "Test");
            Assert.Same(builder, result1);

            var result2 = builder.WithMemoryUsage(memoryHealth);
            Assert.Same(builder, result2);

            var result3 = builder.WithCpuUsage(cpuHealth);
            Assert.Same(builder, result3);

            var result4 = builder.WithDiskUsage(diskHealth);
            Assert.Same(builder, result4);

            var result5 = builder.WithThreadCount(threadHealth);
            Assert.Same(builder, result5);

            var result6 = builder.WithGcStatus(gcHealth);
            Assert.Same(builder, result6);
        }

        [Fact]
        public void MethodChaining_CanBeCalledMultipleTimes()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var memoryHealth1 = new MemoryHealth();
            var memoryHealth2 = new MemoryHealth();

            // Act
            var result = builder.WithMemoryUsage(memoryHealth1)
                               .WithMemoryUsage(memoryHealth2)
                               .Build();

            // Assert
            Assert.Same(memoryHealth2, result.MemoryUsage);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void SetHealthStatus_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var message = "System health: !@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            var result = builder.SetHealthStatus(true, message);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result);
            Assert.Equal(message, healthStatus.Message);
        }

        [Fact]
        public void SetHealthStatus_WithWhitespaceMessage_HandlesCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var message = "   \t\n   ";

            // Act
            var result = builder.SetHealthStatus(true, message);
            var healthStatus = builder.Build();

            // Assert
            Assert.Same(builder, result);
            Assert.Equal(message, healthStatus.Message);
        }

        [Fact]
        public void Builder_CanBeReusedAfterBuild()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();
            var memoryHealth1 = new MemoryHealth();
            var memoryHealth2 = new MemoryHealth();

            // Act
            builder.WithMemoryUsage(memoryHealth1);
            var result1 = builder.Build();

            builder.WithMemoryUsage(memoryHealth2);
            var result2 = builder.Build();

            // Assert
            Assert.Same(result1, result2); // Same instance
            Assert.Same(memoryHealth2, result2.MemoryUsage);
        }

        [Fact]
        public void Builder_MultipleInstances_WorkIndependently()
        {
            // Arrange
            var builder1 = new AdvancedHealthStatusBuilder();
            var builder2 = new AdvancedHealthStatusBuilder();
            var memoryHealth1 = new MemoryHealth();
            var memoryHealth2 = new MemoryHealth();

            // Act
            builder1.WithMemoryUsage(memoryHealth1);
            builder2.WithMemoryUsage(memoryHealth2);

            var result1 = builder1.Build();
            var result2 = builder2.Build();

            // Assert
            Assert.NotSame(result1, result2);
            Assert.Same(memoryHealth1, result1.MemoryUsage);
            Assert.Same(memoryHealth2, result2.MemoryUsage);
        }

        [Fact]
        public void Builder_WithAllNullValues_HandlesCorrectly()
        {
            // Arrange
            var builder = new AdvancedHealthStatusBuilder();

            // Act
            var result = builder.SetHealthStatus(false, "System degraded")
                               .WithMemoryUsage(null)
                               .WithCpuUsage(null)
                               .WithDiskUsage(null)
                               .WithThreadCount(null)
                               .WithGcStatus(null)
                               .Build();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("System degraded", result.Message);
            Assert.Null(result.MemoryUsage);
            Assert.Null(result.CpuUsage);
            Assert.Null(result.DiskUsage);
            Assert.Null(result.ThreadCount);
            Assert.Null(result.GcStatus);
        }

        #endregion

        #region Builder Pattern Tests

        [Fact]
        public void BuilderPattern_CanCreateComplexHealthStatus()
        {
            // Arrange
            var memoryHealth = new MemoryHealth();
            var cpuHealth = new CpuHealth();
            var diskHealth = new DiskHealth();
            var threadHealth = new ThreadHealth();
            var gcHealth = new GcHealth();

            // Act
            var healthStatus = new AdvancedHealthStatusBuilder()
                .SetHealthStatus(true, "All systems operational")
                .WithMemoryUsage(memoryHealth)
                .WithCpuUsage(cpuHealth)
                .WithDiskUsage(diskHealth)
                .WithThreadCount(threadHealth)
                .WithGcStatus(gcHealth)
                .Build();

            // Assert
            Assert.True(healthStatus.IsHealthy);
            Assert.Equal("All systems operational", healthStatus.Message);
            Assert.Same(memoryHealth, healthStatus.MemoryUsage);
            Assert.Same(cpuHealth, healthStatus.CpuUsage);
            Assert.Same(diskHealth, healthStatus.DiskUsage);
            Assert.Same(threadHealth, healthStatus.ThreadCount);
            Assert.Same(gcHealth, healthStatus.GcStatus);
        }

        [Fact]
        public void BuilderPattern_CanCreateMinimalHealthStatus()
        {
            // Act
            var healthStatus = new AdvancedHealthStatusBuilder()
                .SetHealthStatus(false, "System offline")
                .Build();

            // Assert
            Assert.False(healthStatus.IsHealthy);
            Assert.Equal("System offline", healthStatus.Message);
            Assert.Null(healthStatus.MemoryUsage);
            Assert.Null(healthStatus.CpuUsage);
            Assert.Null(healthStatus.DiskUsage);
            Assert.Null(healthStatus.ThreadCount);
            Assert.Null(healthStatus.GcStatus);
        }

        #endregion
    }
}
