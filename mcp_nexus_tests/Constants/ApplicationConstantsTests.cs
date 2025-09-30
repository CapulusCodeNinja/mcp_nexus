using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Constants;

namespace mcp_nexus_tests.Constants
{
    /// <summary>
    /// Tests for ApplicationConstants
    /// </summary>
    public class ApplicationConstantsTests
    {
        [Fact]
        public void CommandTimeoutConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(TimeSpan.FromMinutes(2), ApplicationConstants.SimpleCommandTimeout);
            Assert.Equal(TimeSpan.FromMinutes(5), ApplicationConstants.DefaultCommandTimeout);
            Assert.Equal(TimeSpan.FromMinutes(30), ApplicationConstants.MaxCommandTimeout);
            Assert.Equal(TimeSpan.FromHours(1), ApplicationConstants.LongRunningCommandTimeout);
        }

        [Fact]
        public void CleanupAndRetentionConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), ApplicationConstants.CleanupInterval);
            Assert.Equal(TimeSpan.FromHours(1), ApplicationConstants.CommandRetentionTime);
        }

        [Fact]
        public void PollingConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(100), ApplicationConstants.InitialPollInterval);
            Assert.Equal(TimeSpan.FromSeconds(2), ApplicationConstants.MaxPollInterval);
            Assert.Equal(1.5, ApplicationConstants.PollBackoffMultiplier);
        }

        [Fact]
        public void ServerConfigurationConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(5000, ApplicationConstants.DefaultHttpPort);
            Assert.Equal(5117, ApplicationConstants.DefaultDevPort);
            Assert.Equal(5511, ApplicationConstants.DefaultServicePort);
        }

        [Fact]
        public void ProcessManagementConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(TimeSpan.FromSeconds(5), ApplicationConstants.ProcessWaitTimeout);
            Assert.Equal(TimeSpan.FromSeconds(5), ApplicationConstants.ServiceShutdownTimeout);
        }

        [Fact]
        public void LoggingConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), ApplicationConstants.StatsLogInterval);
        }

        [Fact]
        public void FilePathConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(50, ApplicationConstants.MaxPathDisplayLength);
            Assert.Equal(3, ApplicationConstants.PathTruncationPrefix);
        }

        [Fact]
        public void HttpConfigurationConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(TimeSpan.FromMinutes(15), ApplicationConstants.HttpRequestTimeout);
            Assert.Equal(TimeSpan.FromMinutes(15), ApplicationConstants.HttpKeepAliveTimeout);
        }

        [Fact]
        public void RecoveryConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(3, ApplicationConstants.MaxRecoveryAttempts);
            Assert.Equal(TimeSpan.FromSeconds(5), ApplicationConstants.RecoveryDelay);
            Assert.Equal(TimeSpan.FromSeconds(30), ApplicationConstants.HealthCheckInterval);
        }

        [Fact]
        public void PerformanceOptimizationSettings_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(10, ApplicationConstants.MaxConcurrentNotifications);
            Assert.Equal(100, ApplicationConstants.MaxCleanupBatchSize);
            Assert.Equal(1000, ApplicationConstants.MaxLogMessageLength);
        }

        [Fact]
        public void CdbSessionTimingConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(1000), ApplicationConstants.CdbInterruptDelay);
            Assert.Equal(TimeSpan.FromMilliseconds(2000), ApplicationConstants.CdbPromptDelay);
            Assert.Equal(TimeSpan.FromMilliseconds(200), ApplicationConstants.CdbStartupDelay);
            Assert.Equal(TimeSpan.FromMilliseconds(500), ApplicationConstants.CdbOutputDelay);
            Assert.Equal(TimeSpan.FromMilliseconds(1000), ApplicationConstants.CdbCommandDelay);
            Assert.Equal(TimeSpan.FromMilliseconds(5000), ApplicationConstants.CdbOutputTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(5000), ApplicationConstants.CdbProcessWaitTimeout);
        }

        [Fact]
        public void MemoryDisplayConstants_HaveExpectedValues()
        {
            // Assert
            Assert.Equal(1024.0 * 1024.0, ApplicationConstants.BytesPerMB);
        }

        [Fact]
        public void TimeoutConstants_AreInAscendingOrder()
        {
            // Assert
            Assert.True(ApplicationConstants.SimpleCommandTimeout < ApplicationConstants.DefaultCommandTimeout);
            Assert.True(ApplicationConstants.DefaultCommandTimeout < ApplicationConstants.MaxCommandTimeout);
            Assert.True(ApplicationConstants.MaxCommandTimeout < ApplicationConstants.LongRunningCommandTimeout);
        }

        [Fact]
        public void PollingConstants_AreInAscendingOrder()
        {
            // Assert
            Assert.True(ApplicationConstants.InitialPollInterval < ApplicationConstants.MaxPollInterval);
            Assert.True(ApplicationConstants.PollBackoffMultiplier > 1.0);
        }

        [Fact]
        public void PortConstants_AreDifferent()
        {
            // Assert
            Assert.NotEqual(ApplicationConstants.DefaultHttpPort, ApplicationConstants.DefaultDevPort);
            Assert.NotEqual(ApplicationConstants.DefaultDevPort, ApplicationConstants.DefaultServicePort);
            Assert.NotEqual(ApplicationConstants.DefaultHttpPort, ApplicationConstants.DefaultServicePort);
        }

        [Fact]
        public void TimeoutConstants_ArePositive()
        {
            // Assert
            Assert.True(ApplicationConstants.SimpleCommandTimeout > TimeSpan.Zero);
            Assert.True(ApplicationConstants.DefaultCommandTimeout > TimeSpan.Zero);
            Assert.True(ApplicationConstants.MaxCommandTimeout > TimeSpan.Zero);
            Assert.True(ApplicationConstants.LongRunningCommandTimeout > TimeSpan.Zero);
            Assert.True(ApplicationConstants.CleanupInterval > TimeSpan.Zero);
            Assert.True(ApplicationConstants.CommandRetentionTime > TimeSpan.Zero);
        }

        [Fact]
        public void NumericConstants_ArePositive()
        {
            // Assert
            Assert.True(ApplicationConstants.DefaultHttpPort > 0);
            Assert.True(ApplicationConstants.DefaultDevPort > 0);
            Assert.True(ApplicationConstants.DefaultServicePort > 0);
            Assert.True(ApplicationConstants.MaxRecoveryAttempts > 0);
            Assert.True(ApplicationConstants.MaxConcurrentNotifications > 0);
            Assert.True(ApplicationConstants.MaxCleanupBatchSize > 0);
            Assert.True(ApplicationConstants.MaxLogMessageLength > 0);
            Assert.True(ApplicationConstants.MaxPathDisplayLength > 0);
            Assert.True(ApplicationConstants.PathTruncationPrefix > 0);
        }

        [Fact]
        public void BytesPerMB_IsCorrect()
        {
            // Assert
            Assert.Equal(1048576.0, ApplicationConstants.BytesPerMB);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        [InlineData(4096)]
        public void BytesPerMB_CanBeUsedForConversion(int bytes)
        {
            // Act
            var mb = bytes / ApplicationConstants.BytesPerMB;

            // Assert
            Assert.True(mb > 0);
            Assert.True(mb < 1); // 1024, 2048, 4096 bytes are all less than 1 MB
        }
    }
}