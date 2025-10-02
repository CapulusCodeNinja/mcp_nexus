using System;
using Xunit;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for CommandInfo data class - simple data container
    /// </summary>
    public class CommandInfoTests
    {
        [Fact]
        public void CommandInfo_DefaultValues_AreCorrect()
        {
            // Act
            var commandInfo = new CommandInfo();

            // Assert
            Assert.Equal(string.Empty, commandInfo.CommandId);
            Assert.Equal(string.Empty, commandInfo.Command);
            Assert.Equal(CommandState.Queued, commandInfo.State);
            Assert.True(commandInfo.QueueTime > DateTime.MinValue);
            Assert.Equal(TimeSpan.Zero, commandInfo.Elapsed);
            Assert.Equal(TimeSpan.Zero, commandInfo.Remaining);
            Assert.Equal(0, commandInfo.QueuePosition);
            Assert.False(commandInfo.IsCompleted);
        }

        [Fact]
        public void CommandInfo_WithValues_SetsProperties()
        {
            // Arrange
            var queueTime = DateTime.UtcNow;
            var elapsed = TimeSpan.FromSeconds(5);
            var remaining = TimeSpan.FromSeconds(10);

            // Act
            var commandInfo = new CommandInfo("cmd-123", "!analyze -v", CommandState.Executing, queueTime, 3)
            {
                Elapsed = elapsed,
                Remaining = remaining,
                IsCompleted = true
            };

            // Assert
            Assert.Equal("cmd-123", commandInfo.CommandId);
            Assert.Equal("!analyze -v", commandInfo.Command);
            Assert.Equal(CommandState.Executing, commandInfo.State);
            Assert.Equal(queueTime, commandInfo.QueueTime);
            Assert.Equal(elapsed, commandInfo.Elapsed);
            Assert.Equal(remaining, commandInfo.Remaining);
            Assert.Equal(3, commandInfo.QueuePosition);
            Assert.True(commandInfo.IsCompleted);
        }

        [Theory]
        [InlineData(CommandState.Queued)]
        [InlineData(CommandState.Executing)]
        [InlineData(CommandState.Completed)]
        [InlineData(CommandState.Failed)]
        [InlineData(CommandState.Cancelled)]
        public void CommandInfo_State_CanBeSet(CommandState state)
        {
            // Act
            var commandInfo = new CommandInfo { State = state };

            // Assert
            Assert.Equal(state, commandInfo.State);
        }

        [Fact]
        public void CommandInfo_WithNullValues_HandlesGracefully()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommandInfo(null!, "test", CommandState.Queued, DateTime.UtcNow));
            Assert.Throws<ArgumentNullException>(() => new CommandInfo("test", null!, CommandState.Queued, DateTime.UtcNow));
        }

        [Fact]
        public void CommandInfo_WithNegativeValues_HandlesCorrectly()
        {
            // Act
            var commandInfo = new CommandInfo
            {
                QueuePosition = -1,
                Elapsed = TimeSpan.FromSeconds(-5),
                Remaining = TimeSpan.FromSeconds(-10)
            };

            // Assert
            Assert.Equal(-1, commandInfo.QueuePosition);
            Assert.Equal(TimeSpan.FromSeconds(-5), commandInfo.Elapsed);
            Assert.Equal(TimeSpan.FromSeconds(-10), commandInfo.Remaining);
        }

        [Fact]
        public void CommandInfo_WithMaxValues_HandlesCorrectly()
        {
            // Arrange
            var maxDateTime = DateTime.MaxValue;
            var maxTimeSpan = TimeSpan.MaxValue;

            // Act
            var commandInfo = new CommandInfo("test", "test", CommandState.Queued, maxDateTime, int.MaxValue)
            {
                Elapsed = maxTimeSpan,
                Remaining = maxTimeSpan
            };

            // Assert
            Assert.Equal(maxDateTime, commandInfo.QueueTime);
            Assert.Equal(maxTimeSpan, commandInfo.Elapsed);
            Assert.Equal(maxTimeSpan, commandInfo.Remaining);
            Assert.Equal(int.MaxValue, commandInfo.QueuePosition);
        }
    }
}
