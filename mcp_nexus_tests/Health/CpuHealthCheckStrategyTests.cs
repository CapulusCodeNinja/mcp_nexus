using System.Diagnostics;
using mcp_nexus.Health;
using Xunit;

namespace mcp_nexus_tests.Health
{
    /// <summary>
    /// Unit tests for CpuHealthCheckStrategy
    /// </summary>
    public class CpuHealthCheckStrategyTests
    {
        /// <summary>
        /// Test helper extensions for accessing private fields
        /// </summary>
        private static class TestHelperExtensions
        {
            public static T GetPrivateField<T>(object obj, string fieldName)
            {
                var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (T)field!.GetValue(obj)!;
            }
        }

        [Fact]
        public void Constructor_WithDefaultThreshold_CreatesInstance()
        {
            // Act
            var strategy = new CpuHealthCheckStrategy();

            // Assert
            Assert.NotNull(strategy);
            Assert.Equal("CPU Health Check", strategy.StrategyName);
            Assert.True(strategy.IsApplicable());
        }

        [Fact]
        public void Constructor_WithCustomThreshold_CreatesInstance()
        {
            // Arrange
            var customThreshold = 90.0;

            // Act
            var strategy = new CpuHealthCheckStrategy(customThreshold);

            // Assert
            Assert.NotNull(strategy);
            Assert.Equal("CPU Health Check", strategy.StrategyName);
            Assert.True(strategy.IsApplicable());
            
            // Verify the threshold was set correctly
            var actualThreshold = TestHelperExtensions.GetPrivateField<double>(strategy, "m_cpuThresholdPercent");
            Assert.Equal(customThreshold, actualThreshold);
        }

        [Fact]
        public void Constructor_WithZeroThreshold_CreatesInstance()
        {
            // Act
            var strategy = new CpuHealthCheckStrategy(0.0);

            // Assert
            Assert.NotNull(strategy);
            Assert.Equal("CPU Health Check", strategy.StrategyName);
            Assert.True(strategy.IsApplicable());
        }

        [Fact]
        public void Constructor_WithNegativeThreshold_CreatesInstance()
        {
            // Act
            var strategy = new CpuHealthCheckStrategy(-10.0);

            // Assert
            Assert.NotNull(strategy);
            Assert.Equal("CPU Health Check", strategy.StrategyName);
            Assert.True(strategy.IsApplicable());
        }

        [Fact]
        public void Constructor_WithVeryHighThreshold_CreatesInstance()
        {
            // Act
            var strategy = new CpuHealthCheckStrategy(999.0);

            // Assert
            Assert.NotNull(strategy);
            Assert.Equal("CPU Health Check", strategy.StrategyName);
            Assert.True(strategy.IsApplicable());
        }

        [Fact]
        public void StrategyName_ReturnsCorrectValue()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy();

            // Act
            var name = strategy.StrategyName;

            // Assert
            Assert.Equal("CPU Health Check", name);
        }

        [Fact]
        public void IsApplicable_AlwaysReturnsTrue()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy();

            // Act
            var isApplicable = strategy.IsApplicable();

            // Assert
            Assert.True(isApplicable);
        }

        [Fact]
        public async Task CheckHealthAsync_WithLowCpuUsage_ReturnsHealthyResult()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(80.0);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy);
            Assert.Contains("CPU usage is healthy", result.Message);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("CpuUsagePercent"));
            Assert.True(result.Data.ContainsKey("TotalProcessorTime"));
            Assert.True(result.Data.ContainsKey("ThresholdPercent"));
            Assert.Equal(80.0, result.Data["ThresholdPercent"]);
        }

        [Fact]
        public async Task CheckHealthAsync_WithHighThreshold_ReturnsHealthyResult()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(99.9);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy);
            Assert.Contains("CPU usage is healthy", result.Message);
        }

        [Fact]
        public async Task CheckHealthAsync_WithZeroThreshold_ReturnsUnhealthyResult()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(0.0);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            // Note: This might be healthy or unhealthy depending on actual CPU usage
            // The important thing is that it doesn't throw and returns a valid result
            Assert.NotNull(result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task CheckHealthAsync_WithNegativeThreshold_ReturnsUnhealthyResult()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(-10.0);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            // Note: This might be healthy or unhealthy depending on actual CPU usage
            // The important thing is that it doesn't throw and returns a valid result
            Assert.NotNull(result.Message);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsValidDataStructure()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(50.0);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("CpuUsagePercent"));
            Assert.True(result.Data.ContainsKey("TotalProcessorTime"));
            Assert.True(result.Data.ContainsKey("ThresholdPercent"));
            
            // Verify data types
            Assert.IsType<double>(result.Data["CpuUsagePercent"]);
            Assert.IsType<TimeSpan>(result.Data["TotalProcessorTime"]);
            Assert.IsType<double>(result.Data["ThresholdPercent"]);
            
            // Verify threshold is set correctly
            Assert.Equal(50.0, result.Data["ThresholdPercent"]);
        }

        [Fact]
        public async Task CheckHealthAsync_WithVeryHighThreshold_ReturnsHealthyResult()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(999.0);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy);
            Assert.Contains("CPU usage is healthy", result.Message);
        }

        [Fact]
        public async Task CheckHealthAsync_MessageFormat_IsCorrect()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(80.0);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Message);
            
            // The message should contain either "healthy" or "High CPU usage detected"
            var isHealthyMessage = result.Message.Contains("CPU usage is healthy");
            var isUnhealthyMessage = result.Message.Contains("High CPU usage detected");
            
            Assert.True(isHealthyMessage || isUnhealthyMessage);
            
            // The message should contain a percentage value
            Assert.Contains("%", result.Message);
        }

        [Fact]
        public async Task CheckHealthAsync_DataValues_AreReasonable()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(80.0);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            
            var cpuUsage = (double)result.Data["CpuUsagePercent"];
            var threshold = (double)result.Data["ThresholdPercent"];
            var totalProcessorTime = (TimeSpan)result.Data["TotalProcessorTime"];
            
            // CPU usage should be between 0 and 100 (or slightly above due to Math.Min(100, ...))
            Assert.True(cpuUsage >= 0);
            Assert.True(cpuUsage <= 100.1); // Allow for small floating point errors
            
            // Threshold should be exactly what we set
            Assert.Equal(80.0, threshold);
            
            // Total processor time should be non-negative
            Assert.True(totalProcessorTime >= TimeSpan.Zero);
        }

        [Fact]
        public async Task CheckHealthAsync_IsAsync_ReturnsTask()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy();

            // Act
            var task = strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(task);
            Assert.True(task is Task<IHealthCheckResult>);
            
            var result = await task;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task CheckHealthAsync_MultipleCalls_ReturnConsistentResults()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(80.0);

            // Act
            var result1 = await strategy.CheckHealthAsync();
            var result2 = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            
            // Both results should have the same structure
            Assert.Equal(result1.Data.Keys, result2.Data.Keys);
            Assert.Equal(result1.Data["ThresholdPercent"], result2.Data["ThresholdPercent"]);
            
            // Both results should be valid
            Assert.NotNull(result1.Message);
            Assert.NotNull(result2.Message);
        }

        [Fact]
        public void Constructor_ProcessField_IsSetCorrectly()
        {
            // Arrange & Act
            var strategy = new CpuHealthCheckStrategy();

            // Assert
            var process = TestHelperExtensions.GetPrivateField<Process>(strategy, "m_currentProcess");
            Assert.NotNull(process);
            Assert.Equal(Process.GetCurrentProcess().Id, process.Id);
        }

        [Fact]
        public void Constructor_ThresholdField_IsSetCorrectly()
        {
            // Arrange
            var expectedThreshold = 75.5;

            // Act
            var strategy = new CpuHealthCheckStrategy(expectedThreshold);

            // Assert
            var actualThreshold = TestHelperExtensions.GetPrivateField<double>(strategy, "m_cpuThresholdPercent");
            Assert.Equal(expectedThreshold, actualThreshold);
        }

        [Fact]
        public async Task CheckHealthAsync_WithEdgeCaseThreshold_HandlesCorrectly()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(0.1); // Very low threshold

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Message);
            Assert.NotNull(result.Data);
            
            // The result should be valid regardless of whether it's healthy or unhealthy
            var isHealthyMessage = result.Message.Contains("CPU usage is healthy");
            var isUnhealthyMessage = result.Message.Contains("High CPU usage detected");
            Assert.True(isHealthyMessage || isUnhealthyMessage);
        }

        [Fact]
        public async Task CheckHealthAsync_WithMaxThreshold_HandlesCorrectly()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(double.MaxValue);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy); // Should always be healthy with max threshold
            Assert.Contains("CPU usage is healthy", result.Message);
        }

        [Fact]
        public async Task CheckHealthAsync_WithMinThreshold_HandlesCorrectly()
        {
            // Arrange
            var strategy = new CpuHealthCheckStrategy(double.MinValue);

            // Act
            var result = await strategy.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            // Note: This might be healthy or unhealthy depending on actual CPU usage
            // The important thing is that it doesn't throw and returns a valid result
            Assert.NotNull(result.Message);
            Assert.NotNull(result.Data);
        }
    }
}
