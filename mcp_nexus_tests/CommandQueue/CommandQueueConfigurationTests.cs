using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for CommandQueueConfiguration
    /// </summary>
    public class CommandQueueConfigurationTests
    {
        [Fact]
        public void Constructor_WithNullSessionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CommandQueueConfiguration(null!));
        }

        [Fact]
        public void Constructor_WithEmptySessionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CommandQueueConfiguration(""));
        }

        [Fact]
        public void Constructor_WithWhitespaceSessionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CommandQueueConfiguration("   "));
        }

        [Fact]
        public void Constructor_WithValidSessionId_UsesDefaultValues()
        {
            var config = new CommandQueueConfiguration("session-123");

            Assert.Equal("session-123", config.SessionId);
            Assert.Equal(TimeSpan.FromMinutes(10), config.DefaultCommandTimeout);
            Assert.Equal(TimeSpan.FromSeconds(30), config.HeartbeatInterval);
            Assert.Equal(TimeSpan.FromSeconds(5), config.ShutdownTimeout);
            Assert.Equal(TimeSpan.FromSeconds(2), config.ForceShutdownTimeout);
        }

        [Fact]
        public void Constructor_WithCustomValues_UsesProvidedValues()
        {
            var customTimeout = TimeSpan.FromMinutes(15);
            var customHeartbeat = TimeSpan.FromSeconds(60);
            var customShutdown = TimeSpan.FromSeconds(10);
            var customForceShutdown = TimeSpan.FromSeconds(3);

            var config = new CommandQueueConfiguration(
                "session-456",
                customTimeout,
                customHeartbeat,
                customShutdown,
                customForceShutdown);

            Assert.Equal("session-456", config.SessionId);
            Assert.Equal(customTimeout, config.DefaultCommandTimeout);
            Assert.Equal(customHeartbeat, config.HeartbeatInterval);
            Assert.Equal(customShutdown, config.ShutdownTimeout);
            Assert.Equal(customForceShutdown, config.ForceShutdownTimeout);
        }

        [Fact]
        public void Constructor_WithZeroCommandTimeout_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CommandQueueConfiguration("session-123", TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithNegativeCommandTimeout_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CommandQueueConfiguration("session-123", TimeSpan.FromMinutes(-1)));
        }

        [Fact]
        public void Constructor_WithZeroHeartbeatInterval_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CommandQueueConfiguration("session-123", heartbeatInterval: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithZeroShutdownTimeout_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CommandQueueConfiguration("session-123", shutdownTimeout: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithZeroForceShutdownTimeout_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CommandQueueConfiguration("session-123", forceShutdownTimeout: TimeSpan.Zero));
        }

        [Fact]
        public void Constructor_WithForceShutdownGreaterThanShutdown_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CommandQueueConfiguration("session-123",
                    shutdownTimeout: TimeSpan.FromSeconds(5),
                    forceShutdownTimeout: TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void Constructor_WithForceShutdownEqualToShutdown_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CommandQueueConfiguration("session-123",
                    shutdownTimeout: TimeSpan.FromSeconds(5),
                    forceShutdownTimeout: TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public void GenerateCommandId_WithValidCommandNumber_ReturnsFormattedId()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GenerateCommandId(42);

            Assert.Equal("cmd-session-123-0042", result);
        }

        [Fact]
        public void GenerateCommandId_WithZeroCommandNumber_ReturnsFormattedId()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GenerateCommandId(0);

            Assert.Equal("cmd-session-123-0000", result);
        }

        [Fact]
        public void GenerateCommandId_WithLargeCommandNumber_ReturnsFormattedId()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GenerateCommandId(9999);

            Assert.Equal("cmd-session-123-9999", result);
        }

        [Fact]
        public void CalculateProgressPercentage_WithZeroQueuePosition_Returns95()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.CalculateProgressPercentage(0, TimeSpan.FromMinutes(5));

            Assert.Equal(95, result);
        }

        [Fact]
        public void CalculateProgressPercentage_WithNegativeQueuePosition_Returns95()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.CalculateProgressPercentage(-1, TimeSpan.FromMinutes(5));

            Assert.Equal(95, result);
        }

        [Fact]
        public void CalculateProgressPercentage_WithQueuePosition1_Returns85()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.CalculateProgressPercentage(1, TimeSpan.Zero);

            Assert.Equal(85, result);
        }

        [Fact]
        public void CalculateProgressPercentage_WithTimeBonus_IncludesTimeBonus()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.CalculateProgressPercentage(1, TimeSpan.FromMinutes(3));

            Assert.Equal(90, result); // 85 + 6 (3 minutes * 2) capped at 90
        }

        [Fact]
        public void CalculateProgressPercentage_WithHighQueuePosition_ReturnsMinimum5()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.CalculateProgressPercentage(10, TimeSpan.Zero);

            Assert.Equal(5, result);
        }

        [Fact]
        public void CalculateProgressPercentage_WithTimeBonus_CapsAt90()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.CalculateProgressPercentage(1, TimeSpan.FromMinutes(10));

            Assert.Equal(90, result); // Capped at 90
        }

        [Fact]
        public void GetBaseMessage_WithZeroQueuePosition_ReturnsExecutingMessage()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetBaseMessage(0, TimeSpan.FromMinutes(5));

            Assert.Equal("Executing command...", result);
        }

        [Fact]
        public void GetBaseMessage_WithNegativeQueuePosition_ReturnsExecutingMessage()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetBaseMessage(-1, TimeSpan.FromMinutes(5));

            Assert.Equal("Executing command...", result);
        }

        [Fact]
        public void GetBaseMessage_WithQueuePosition1_ReturnsNextInQueue()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetBaseMessage(1, TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(30)));

            Assert.Equal("Next in queue (waited 2m 30s)", result);
        }

        [Fact]
        public void GetBaseMessage_WithQueuePosition2_ReturnsSecondInQueue()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetBaseMessage(2, TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(45)));

            Assert.Equal("2nd in queue (waited 1m 45s)", result);
        }

        [Fact]
        public void GetBaseMessage_WithQueuePosition3_ReturnsThirdInQueue()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetBaseMessage(3, TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(15)));

            Assert.Equal("3rd in queue (waited 3m 15s)", result);
        }

        [Fact]
        public void GetBaseMessage_WithQueuePosition4_ReturnsFourthInQueue()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetBaseMessage(4, TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(0)));

            Assert.Equal("4th in queue (waited 5m 0s)", result);
        }

        [Fact]
        public void GetQueuedStatusMessage_WithZeroQueuePosition_ReturnsBaseMessage()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetQueuedStatusMessage(0, TimeSpan.FromMinutes(5), 2, 30);

            Assert.Equal("Executing command...", result);
        }

        [Fact]
        public void GetQueuedStatusMessage_WithQueuePosition1_ReturnsDetailedMessage()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetQueuedStatusMessage(1, TimeSpan.FromMinutes(2), 3, 45);

            Assert.Equal("Next in queue (waited 2m 0s) - Check again in 3-45 seconds (next in queue)", result);
        }

        [Fact]
        public void GetQueuedStatusMessage_WithHighQueuePosition_ReturnsDetailedMessage()
        {
            var config = new CommandQueueConfiguration("session-123");

            var result = config.GetQueuedStatusMessage(5, TimeSpan.FromMinutes(10), 1, 30);

            Assert.Equal("5th in queue (waited 10m 0s) - Check again in 1-30 seconds (next in queue)", result);
        }
    }
}