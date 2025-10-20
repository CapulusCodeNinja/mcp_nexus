using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.CommandQueue.Batching;
using Xunit;

namespace mcp_nexus_unit_tests.CommandQueue.Batching
{
    /// <summary>
    /// Tests for BatchingConfiguration validation and edge cases
    /// </summary>
    public class BatchingConfigurationTests
    {
        [Fact]
        public void Constructor_WithDefaultValues_ShouldSetCorrectDefaults()
        {
            // Act
            var config = new BatchingConfiguration();

            // Assert
            Assert.True(config.Enabled);
            Assert.Equal(5, config.MaxBatchSize);
            Assert.Equal(2000, config.BatchWaitTimeoutMs);
            Assert.Equal(1.0, config.BatchTimeoutMultiplier);
            Assert.Equal(30, config.MaxBatchTimeoutMinutes);
            Assert.NotNull(config.ExcludedCommands);
            Assert.NotEmpty(config.ExcludedCommands);
            Assert.Contains("!analyze", config.ExcludedCommands);
            Assert.Contains("!dump", config.ExcludedCommands);
            Assert.Contains("!heap", config.ExcludedCommands);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void MaxBatchSize_WithInvalidValues_ShouldAcceptButMayCauseIssues(int invalidSize)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.MaxBatchSize = invalidSize;

            // Assert
            Assert.Equal(invalidSize, config.MaxBatchSize);
            // Note: The configuration itself doesn't validate, but the BatchCommandProcessor should handle this
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        public void MaxBatchSize_WithValidValues_ShouldAccept(int validSize)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.MaxBatchSize = validSize;

            // Assert
            Assert.Equal(validSize, config.MaxBatchSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-1000)]
        public void BatchWaitTimeoutMs_WithInvalidValues_ShouldAcceptButMayCauseIssues(int invalidTimeout)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.BatchWaitTimeoutMs = invalidTimeout;

            // Assert
            Assert.Equal(invalidTimeout, config.BatchWaitTimeoutMs);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(30000)]
        public void BatchWaitTimeoutMs_WithValidValues_ShouldAccept(int validTimeout)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.BatchWaitTimeoutMs = validTimeout;

            // Assert
            Assert.Equal(validTimeout, config.BatchWaitTimeoutMs);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(2.0)]
        [InlineData(10.0)]
        public void BatchTimeoutMultiplier_WithValidValues_ShouldAccept(double validMultiplier)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.BatchTimeoutMultiplier = validMultiplier;

            // Assert
            Assert.Equal(validMultiplier, config.BatchTimeoutMultiplier);
        }

        [Theory]
        [InlineData(-1.0)]
        [InlineData(-0.5)]
        [InlineData(-10.0)]
        public void BatchTimeoutMultiplier_WithNegativeValues_ShouldAcceptButMayCauseIssues(double negativeMultiplier)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.BatchTimeoutMultiplier = negativeMultiplier;

            // Assert
            Assert.Equal(negativeMultiplier, config.BatchTimeoutMultiplier);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void MaxBatchTimeoutMinutes_WithInvalidValues_ShouldAcceptButMayCauseIssues(int invalidMinutes)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.MaxBatchTimeoutMinutes = invalidMinutes;

            // Assert
            Assert.Equal(invalidMinutes, config.MaxBatchTimeoutMinutes);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(30)]
        [InlineData(60)]
        [InlineData(120)]
        public void MaxBatchTimeoutMinutes_WithValidValues_ShouldAccept(int validMinutes)
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.MaxBatchTimeoutMinutes = validMinutes;

            // Assert
            Assert.Equal(validMinutes, config.MaxBatchTimeoutMinutes);
        }

        [Fact]
        public void ExcludedCommands_WithNullList_ShouldSetToNull()
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.ExcludedCommands = null!;

            // Assert
            Assert.Null(config.ExcludedCommands);
        }

        [Fact]
        public void ExcludedCommands_WithEmptyList_ShouldAccept()
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.ExcludedCommands = new string[0];

            // Assert
            Assert.NotNull(config.ExcludedCommands);
            Assert.Empty(config.ExcludedCommands);
        }

        [Fact]
        public void ExcludedCommands_WithCustomList_ShouldAccept()
        {
            // Arrange
            var config = new BatchingConfiguration();
            var customExclusions = new[] { "custom1", "custom2", "!custom3" };

            // Act
            config.ExcludedCommands = customExclusions;

            // Assert
            Assert.NotNull(config.ExcludedCommands);
            Assert.Equal(customExclusions, config.ExcludedCommands);
        }

        [Fact]
        public void ExcludedCommands_WithDuplicateEntries_ShouldAccept()
        {
            // Arrange
            var config = new BatchingConfiguration();
            var exclusionsWithDuplicates = new[] { "!analyze", "!analyze", "!dump" };

            // Act
            config.ExcludedCommands = exclusionsWithDuplicates;

            // Assert
            Assert.NotNull(config.ExcludedCommands);
            Assert.Equal(exclusionsWithDuplicates, config.ExcludedCommands);
        }

        [Fact]
        public void ExcludedCommands_WithWhitespaceEntries_ShouldAccept()
        {
            // Arrange
            var config = new BatchingConfiguration();
            var exclusionsWithWhitespace = new[] { "!analyze", "  ", "!dump", "" };

            // Act
            config.ExcludedCommands = exclusionsWithWhitespace;

            // Assert
            Assert.NotNull(config.ExcludedCommands);
            Assert.Equal(exclusionsWithWhitespace, config.ExcludedCommands);
        }

        [Fact]
        public void AllProperties_CanBeSetIndependently()
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.Enabled = false;
            config.MaxBatchSize = 10;
            config.BatchWaitTimeoutMs = 5000;
            config.BatchTimeoutMultiplier = 2.5;
            config.MaxBatchTimeoutMinutes = 60;
            config.ExcludedCommands = new[] { "test1", "test2" };

            // Assert
            Assert.False(config.Enabled);
            Assert.Equal(10, config.MaxBatchSize);
            Assert.Equal(5000, config.BatchWaitTimeoutMs);
            Assert.Equal(2.5, config.BatchTimeoutMultiplier);
            Assert.Equal(60, config.MaxBatchTimeoutMinutes);
            Assert.Equal(new[] { "test1", "test2" }, config.ExcludedCommands);
        }

        [Fact]
        public void Configuration_WithExtremeValues_ShouldAccept()
        {
            // Arrange
            var config = new BatchingConfiguration();

            // Act
            config.MaxBatchSize = int.MaxValue;
            config.BatchWaitTimeoutMs = int.MaxValue;
            config.BatchTimeoutMultiplier = double.MaxValue;
            config.MaxBatchTimeoutMinutes = int.MaxValue;

            // Assert
            Assert.Equal(int.MaxValue, config.MaxBatchSize);
            Assert.Equal(int.MaxValue, config.BatchWaitTimeoutMs);
            Assert.Equal(double.MaxValue, config.BatchTimeoutMultiplier);
            Assert.Equal(int.MaxValue, config.MaxBatchTimeoutMinutes);
        }
    }
}
