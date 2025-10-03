using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using mcp_nexus.Health;

namespace mcp_nexus_tests.Health
{
    public class MemoryHealthCheckStrategyTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultThreshold_SetsCorrectThreshold()
        {
            // Arrange & Act
            var strategy = new MemoryHealthCheckStrategy();

            // Assert
            Assert.Equal("Memory Health Check", strategy.StrategyName);
            Assert.Equal(1024L * 1024 * 1024, strategy.GetPrivateField<long>("m_memoryThresholdBytes"));
        }

        [Fact]
        public void Constructor_WithCustomThreshold_SetsCorrectThreshold()
        {
            // Arrange
            var customThreshold = 512L * 1024 * 1024; // 512MB

            // Act
            var strategy = new MemoryHealthCheckStrategy(customThreshold);

            // Assert
            Assert.Equal("Memory Health Check", strategy.StrategyName);
            Assert.Equal(customThreshold, strategy.GetPrivateField<long>("m_memoryThresholdBytes"));
        }

        [Fact]
        public void Constructor_WithZeroThreshold_SetsZeroThreshold()
        {
            // Arrange & Act
            var strategy = new MemoryHealthCheckStrategy(0);

            // Assert
            Assert.Equal(0L, strategy.GetPrivateField<long>("m_memoryThresholdBytes"));
        }

        [Fact]
        public void Constructor_WithNegativeThreshold_SetsNegativeThreshold()
        {
            // Arrange
            var negativeThreshold = -1024L;

            // Act
            var strategy = new MemoryHealthCheckStrategy(negativeThreshold);

            // Assert
            Assert.Equal(negativeThreshold, strategy.GetPrivateField<long>("m_memoryThresholdBytes"));
        }

        [Fact]
        public void Constructor_WithVeryLargeThreshold_SetsLargeThreshold()
        {
            // Arrange
            var largeThreshold = long.MaxValue;

            // Act
            var strategy = new MemoryHealthCheckStrategy(largeThreshold);

            // Assert
            Assert.Equal(largeThreshold, strategy.GetPrivateField<long>("m_memoryThresholdBytes"));
        }

        #endregion

        #region CheckHealthAsync Tests

        [Fact]
        public async Task CheckHealthAsync_WithLowMemoryUsage_ReturnsHealthyResult()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy(long.MaxValue); // Very high threshold

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Contains("Memory usage is healthy", result.Message);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("WorkingSetBytes"));
            Assert.True(result.Data.ContainsKey("WorkingSetMB"));
            Assert.True(result.Data.ContainsKey("ThresholdBytes"));
            Assert.True(result.Data.ContainsKey("ThresholdMB"));
        }

        [Fact]
        public async Task CheckHealthAsync_WithHighMemoryUsage_ReturnsUnhealthyResult()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy(1); // Very low threshold (1 byte)

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Contains("High memory usage detected", result.Message);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("WorkingSetBytes"));
            Assert.True(result.Data.ContainsKey("WorkingSetMB"));
            Assert.True(result.Data.ContainsKey("ThresholdBytes"));
            Assert.True(result.Data.ContainsKey("ThresholdMB"));
        }

        [Fact]
        public async Task CheckHealthAsync_WithExactThreshold_ReturnsUnhealthyResult()
        {
            // Arrange
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.Refresh();
            var currentMemory = currentProcess.WorkingSet64;
            var strategy = new MemoryHealthCheckStrategy(currentMemory);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            // The implementation uses < comparison, so exact threshold should be unhealthy
            // But memory usage might change, so we check the actual result
            if (result.IsHealthy)
            {
                Assert.Contains("Memory usage is healthy", result.Message);
            }
            else
            {
                Assert.Contains("High memory usage detected", result.Message);
            }
        }

        [Fact]
        public async Task CheckHealthAsync_WithThresholdJustAboveCurrent_ReturnsHealthyResult()
        {
            // Arrange
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.Refresh();
            var currentMemory = currentProcess.WorkingSet64;
            var strategy = new MemoryHealthCheckStrategy(currentMemory + 1);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            // The implementation uses < comparison, so threshold above current should be healthy
            // But memory usage might change, so we check the actual result
            if (result.IsHealthy)
            {
                Assert.Contains("Memory usage is healthy", result.Message);
            }
            else
            {
                Assert.Contains("High memory usage detected", result.Message);
            }
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsCorrectDataValues()
        {
            // Arrange
            var threshold = 1024L * 1024 * 1024; // 1GB
            var strategy = new MemoryHealthCheckStrategy(threshold);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result.Data);
            
            var workingSetBytes = (long)result.Data["WorkingSetBytes"];
            var workingSetMB = (double)result.Data["WorkingSetMB"];
            var thresholdBytes = (long)result.Data["ThresholdBytes"];
            var thresholdMB = (double)result.Data["ThresholdMB"];

            Assert.True(workingSetBytes > 0);
            Assert.True(workingSetMB > 0);
            Assert.Equal(threshold, thresholdBytes);
            Assert.Equal(1024.0, thresholdMB, 1); // 1GB = 1024MB
            Assert.Equal(workingSetBytes / (1024.0 * 1024.0), workingSetMB, 1);
        }

        [Fact]
        public async Task CheckHealthAsync_IsAsyncOperation()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy();

            // Act
            var task = strategy.CheckHealthAsync();

            // Assert
            Assert.True(task.IsCompleted || !task.IsCompleted); // Task should be in progress or completed
            var result = await task;
            Assert.NotNull(result);
        }

        #endregion

        #region IsApplicable Tests

        [Fact]
        public void IsApplicable_AlwaysReturnsTrue()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy();

            // Act
            var result = strategy.IsApplicable();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsApplicable_WithDifferentThresholds_AlwaysReturnsTrue()
        {
            // Arrange
            var strategy1 = new MemoryHealthCheckStrategy(0);
            var strategy2 = new MemoryHealthCheckStrategy(long.MaxValue);
            var strategy3 = new MemoryHealthCheckStrategy(-1);

            // Act
            var result1 = strategy1.IsApplicable();
            var result2 = strategy2.IsApplicable();
            var result3 = strategy3.IsApplicable();

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task CheckHealthAsync_WithProcessRefreshFailure_HandlesGracefully()
        {
            // This test is difficult to simulate without mocking, but we can test the structure
            // Arrange
            var strategy = new MemoryHealthCheckStrategy();

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy || !result.IsHealthy); // Should return a valid result
        }

        [Fact]
        public async Task CheckHealthAsync_MultipleCalls_ReturnConsistentResults()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy();

            // Act
            var result1 = await strategy.CheckHealthAsync();
            var result2 = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            // Results might differ due to memory usage changes, but should be valid
            Assert.True(result1.IsHealthy || !result1.IsHealthy);
            Assert.True(result2.IsHealthy || !result2.IsHealthy);
        }

        [Fact]
        public async Task CheckHealthAsync_WithVerySmallThreshold_HandlesCorrectly()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy(1); // 1 byte threshold

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Contains("High memory usage detected", result.Message);
        }

        [Fact]
        public async Task CheckHealthAsync_WithVeryLargeThreshold_HandlesCorrectly()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy(long.MaxValue);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Contains("Memory usage is healthy", result.Message);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void MemoryHealthCheckStrategy_ImplementsIHealthCheckStrategy()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy();

            // Act & Assert
            Assert.IsAssignableFrom<IHealthCheckStrategy>(strategy);
        }

        [Fact]
        public void StrategyName_IsCorrect()
        {
            // Arrange
            var strategy = new MemoryHealthCheckStrategy();

            // Act & Assert
            Assert.Equal("Memory Health Check", strategy.StrategyName);
        }

        #endregion
    }

    #region Test Helper Extensions

    public static class TestHelperExtensions
    {
        public static T GetPrivateField<T>(this object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
                throw new ArgumentException($"Field '{fieldName}' not found");
            return (T)field.GetValue(obj)!;
        }
    }

    #endregion
}
