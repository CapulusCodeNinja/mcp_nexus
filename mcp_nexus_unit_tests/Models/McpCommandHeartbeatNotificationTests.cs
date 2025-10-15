using Xunit;
using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus_unit_tests.Models
{
    /// <summary>
    /// Tests for McpCommandHeartbeatNotification model.
    /// </summary>
    public class McpCommandHeartbeatNotificationTests
    {
        [Fact]
        public void McpCommandHeartbeatNotification_DefaultConstructor_InitializesDefaults()
        {
            // Act
            var notification = new McpCommandHeartbeatNotification();

            // Assert
            Assert.Null(notification.SessionId);
            Assert.Equal(string.Empty, notification.CommandId);
            Assert.Equal(string.Empty, notification.Command);
            Assert.Equal(0, notification.ElapsedSeconds);
            Assert.Equal(string.Empty, notification.ElapsedDisplay);
            Assert.Null(notification.Details);
            Assert.NotEqual(default(DateTimeOffset), notification.Timestamp);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_SessionId_CanBeSet()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification();

            // Act
            notification.SessionId = "session-123";

            // Assert
            Assert.Equal("session-123", notification.SessionId);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_CommandId_CanBeSet()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification();

            // Act
            notification.CommandId = "cmd-456";

            // Assert
            Assert.Equal("cmd-456", notification.CommandId);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_Command_CanBeSet()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification();

            // Act
            notification.Command = "!analyze -v";

            // Assert
            Assert.Equal("!analyze -v", notification.Command);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_ElapsedSeconds_CanBeSet()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification();

            // Act
            notification.ElapsedSeconds = 125.3;

            // Assert
            Assert.Equal(125.3, notification.ElapsedSeconds);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_ElapsedDisplay_CanBeSet()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification();

            // Act
            notification.ElapsedDisplay = "2m 5s";

            // Assert
            Assert.Equal("2m 5s", notification.ElapsedDisplay);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_Details_CanBeSet()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification();

            // Act
            notification.Details = "Still analyzing heap corruption...";

            // Assert
            Assert.Equal("Still analyzing heap corruption...", notification.Details);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_Timestamp_CanBeSet()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification();
            var timestamp = DateTimeOffset.Now;

            // Act
            notification.Timestamp = timestamp;

            // Assert
            Assert.Equal(timestamp, notification.Timestamp);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_AllProperties_CanBeSetTogether()
        {
            // Arrange & Act
            var timestamp = DateTimeOffset.Now;
            var notification = new McpCommandHeartbeatNotification
            {
                SessionId = "session-789",
                CommandId = "cmd-789",
                Command = "lm",
                ElapsedSeconds = 60.0,
                ElapsedDisplay = "1m",
                Details = "Loading modules...",
                Timestamp = timestamp
            };

            // Assert
            Assert.Equal("session-789", notification.SessionId);
            Assert.Equal("cmd-789", notification.CommandId);
            Assert.Equal("lm", notification.Command);
            Assert.Equal(60.0, notification.ElapsedSeconds);
            Assert.Equal("1m", notification.ElapsedDisplay);
            Assert.Equal("Loading modules...", notification.Details);
            Assert.Equal(timestamp, notification.Timestamp);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_Serialization_PreservesAllProperties()
        {
            // Arrange
            var timestamp = new DateTimeOffset(2025, 10, 17, 10, 30, 0, TimeSpan.Zero);
            var notification = new McpCommandHeartbeatNotification
            {
                SessionId = "session-111",
                CommandId = "cmd-111",
                Command = "k",
                ElapsedSeconds = 90.5,
                ElapsedDisplay = "1m 30s",
                Details = "Analyzing stack trace...",
                Timestamp = timestamp
            };

            // Act
            var json = JsonSerializer.Serialize(notification);
            var deserialized = JsonSerializer.Deserialize<McpCommandHeartbeatNotification>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("session-111", deserialized.SessionId);
            Assert.Equal("cmd-111", deserialized.CommandId);
            Assert.Equal("k", deserialized.Command);
            Assert.Equal(90.5, deserialized.ElapsedSeconds);
            Assert.Equal("1m 30s", deserialized.ElapsedDisplay);
            Assert.Equal("Analyzing stack trace...", deserialized.Details);
            Assert.Equal(timestamp, deserialized.Timestamp);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_WithNullSessionId_SerializesCorrectly()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification
            {
                CommandId = "cmd-222",
                Command = "!threads",
                ElapsedSeconds = 30.0,
                ElapsedDisplay = "30s"
            };

            // Act
            var json = JsonSerializer.Serialize(notification);
            var deserialized = JsonSerializer.Deserialize<McpCommandHeartbeatNotification>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.SessionId);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_WithNullDetails_SerializesCorrectly()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification
            {
                CommandId = "cmd-333",
                Command = "!peb",
                ElapsedSeconds = 15.0,
                ElapsedDisplay = "15s"
            };

            // Act
            var json = JsonSerializer.Serialize(notification);
            var deserialized = JsonSerializer.Deserialize<McpCommandHeartbeatNotification>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.Details);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_WithZeroElapsedSeconds_SerializesCorrectly()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification
            {
                CommandId = "cmd-000",
                Command = "version",
                ElapsedSeconds = 0.0,
                ElapsedDisplay = "0s"
            };

            // Act
            var json = JsonSerializer.Serialize(notification);
            var deserialized = JsonSerializer.Deserialize<McpCommandHeartbeatNotification>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(0.0, deserialized.ElapsedSeconds);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_WithLargeElapsedSeconds_SerializesCorrectly()
        {
            // Arrange
            var notification = new McpCommandHeartbeatNotification
            {
                CommandId = "cmd-max",
                Command = "long_command",
                ElapsedSeconds = 3600.0, // 1 hour
                ElapsedDisplay = "1h"
            };

            // Act
            var json = JsonSerializer.Serialize(notification);
            var deserialized = JsonSerializer.Deserialize<McpCommandHeartbeatNotification>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3600.0, deserialized.ElapsedSeconds);
        }
    }
}

