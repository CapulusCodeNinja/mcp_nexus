using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Constants;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for BasicQueueConfiguration
    /// </summary>
    public class BasicQueueConfigurationTests
    {
        [Fact]
        public void Constructor_WithDefaultParameters_UsesApplicationConstants()
        {
            // Act
            var config = new BasicQueueConfiguration();

            // Assert
            Assert.Equal(ApplicationConstants.CleanupInterval, config.CleanupInterval);
            Assert.Equal(ApplicationConstants.CommandRetentionTime, config.CommandRetentionTime);
            Assert.Equal(TimeSpan.FromMinutes(5), config.StatsLogInterval);
        }

        [Fact]
        public void Constructor_WithCustomCleanupInterval_UsesCustomValue()
        {
            // Arrange
            var customInterval = TimeSpan.FromMinutes(10);

            // Act
            var config = new BasicQueueConfiguration(cleanupInterval: customInterval);

            // Assert
            Assert.Equal(customInterval, config.CleanupInterval);
            Assert.Equal(ApplicationConstants.CommandRetentionTime, config.CommandRetentionTime);
            Assert.Equal(TimeSpan.FromMinutes(5), config.StatsLogInterval);
        }

        [Fact]
        public void Constructor_WithCustomCommandRetentionTime_UsesCustomValue()
        {
            // Arrange
            var customRetentionTime = TimeSpan.FromHours(2);

            // Act
            var config = new BasicQueueConfiguration(commandRetentionTime: customRetentionTime);

            // Assert
            Assert.Equal(ApplicationConstants.CleanupInterval, config.CleanupInterval);
            Assert.Equal(customRetentionTime, config.CommandRetentionTime);
            Assert.Equal(TimeSpan.FromMinutes(5), config.StatsLogInterval);
        }

        [Fact]
        public void Constructor_WithCustomStatsLogInterval_UsesCustomValue()
        {
            // Arrange
            var customStatsInterval = TimeSpan.FromMinutes(10);

            // Act
            var config = new BasicQueueConfiguration(statsLogInterval: customStatsInterval);

            // Assert
            Assert.Equal(ApplicationConstants.CleanupInterval, config.CleanupInterval);
            Assert.Equal(ApplicationConstants.CommandRetentionTime, config.CommandRetentionTime);
            Assert.Equal(customStatsInterval, config.StatsLogInterval);
        }

        [Fact]
        public void Constructor_WithAllCustomParameters_UsesAllCustomValues()
        {
            // Arrange
            var customCleanupInterval = TimeSpan.FromMinutes(15);
            var customRetentionTime = TimeSpan.FromHours(3);
            var customStatsInterval = TimeSpan.FromMinutes(20);

            // Act
            var config = new BasicQueueConfiguration(customCleanupInterval, customRetentionTime, customStatsInterval);

            // Assert
            Assert.Equal(customCleanupInterval, config.CleanupInterval);
            Assert.Equal(customRetentionTime, config.CommandRetentionTime);
            Assert.Equal(customStatsInterval, config.StatsLogInterval);
        }

        [Fact]
        public void Constructor_WithNullCleanupInterval_UsesApplicationConstants()
        {
            // Act
            var config = new BasicQueueConfiguration(cleanupInterval: null);

            // Assert
            Assert.Equal(ApplicationConstants.CleanupInterval, config.CleanupInterval);
        }

        [Fact]
        public void Constructor_WithNullCommandRetentionTime_UsesApplicationConstants()
        {
            // Act
            var config = new BasicQueueConfiguration(commandRetentionTime: null);

            // Assert
            Assert.Equal(ApplicationConstants.CommandRetentionTime, config.CommandRetentionTime);
        }

        [Fact]
        public void Constructor_WithNullStatsLogInterval_UsesDefaultValue()
        {
            // Act
            var config = new BasicQueueConfiguration(statsLogInterval: null);

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), config.StatsLogInterval);
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(5, 10, 15)]
        [InlineData(0, 0, 0)]
        public void Constructor_WithVariousTimeSpans_UsesCorrectValues(int cleanupMinutes, int retentionHours, int statsMinutes)
        {
            // Arrange
            var cleanupInterval = TimeSpan.FromMinutes(cleanupMinutes);
            var retentionTime = TimeSpan.FromHours(retentionHours);
            var statsInterval = TimeSpan.FromMinutes(statsMinutes);

            // Act
            var config = new BasicQueueConfiguration(cleanupInterval, retentionTime, statsInterval);

            // Assert
            Assert.Equal(cleanupInterval, config.CleanupInterval);
            Assert.Equal(retentionTime, config.CommandRetentionTime);
            Assert.Equal(statsInterval, config.StatsLogInterval);
        }

        [Fact]
        public void Properties_AreReadOnly()
        {
            // Arrange
            _ = new BasicQueueConfiguration();

            // Act & Assert
            // Properties should be read-only (no setters)
            var properties = typeof(BasicQueueConfiguration).GetProperties();
            foreach (var property in properties)
            {
                Assert.False(property.CanWrite, $"Property {property.Name} should be read-only");
            }
        }

        [Fact]
        public void Properties_ReturnExpectedTypes()
        {
            // Arrange
            var config = new BasicQueueConfiguration();

            // Act & Assert
            Assert.IsType<TimeSpan>(config.CleanupInterval);
            Assert.IsType<TimeSpan>(config.CommandRetentionTime);
            Assert.IsType<TimeSpan>(config.StatsLogInterval);
        }

        [Fact]
        public void Properties_ReturnPositiveValues()
        {
            // Arrange
            var config = new BasicQueueConfiguration();

            // Act & Assert
            Assert.True(config.CleanupInterval > TimeSpan.Zero);
            Assert.True(config.CommandRetentionTime > TimeSpan.Zero);
            Assert.True(config.StatsLogInterval > TimeSpan.Zero);
        }
    }
}