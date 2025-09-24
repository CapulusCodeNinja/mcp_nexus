using Xunit;
using System;
using System.Text.Json;
using mcp_nexus.Models;

namespace mcp_nexus_tests.Models
{
    public class McpNotificationModelsTests
    {
        [Fact]
        public void McpNotification_Serialization_IncludesAllProperties()
        {
            // Arrange
            var notification = new McpNotification
            {
                Method = "notifications/test",
                Params = new { message = "test", value = 42 }
            };

            // Act
            var json = JsonSerializer.Serialize(notification);

            // Assert
            Assert.Contains("\"jsonrpc\":\"2.0\"", json);
            Assert.Contains("\"method\":\"notifications/test\"", json);
            Assert.Contains("\"params\":", json);
            Assert.Contains("\"message\":\"test\"", json);
            Assert.Contains("\"value\":42", json);
        }

        [Fact]
        public void McpNotification_Deserialization_RestoresAllProperties()
        {
            // Arrange
            var json = """
                {
                    "jsonrpc": "2.0",
                    "method": "notifications/status",
                    "params": {
                        "commandId": "cmd123",
                        "status": "executing"
                    }
                }
                """;

            // Act
            var notification = JsonSerializer.Deserialize<McpNotification>(json);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("2.0", notification.JsonRpc);
            Assert.Equal("notifications/status", notification.Method);
            Assert.NotNull(notification.Params);
        }

        [Fact]
        public void McpCommandStatusNotification_Serialization_IncludesAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var notification = new McpCommandStatusNotification
            {
                CommandId = "cmd789",
                Command = "!analyze -v",
                Status = "completed",
                Progress = 100,
                Message = "Analysis complete",
                Result = "Thread analysis results...",
                Error = null,
                Timestamp = timestamp
            };

            // Act
            var json = JsonSerializer.Serialize(notification);

            // Assert
            Assert.Contains("\"commandId\":\"cmd789\"", json);
            Assert.Contains("\"command\":\"!analyze -v\"", json);
            Assert.Contains("\"status\":\"completed\"", json);
            Assert.Contains("\"progress\":100", json);
            Assert.Contains("\"message\":\"Analysis complete\"", json);
            Assert.Contains("\"result\":\"Thread analysis results...\"", json);
            Assert.Contains("\"error\":null", json);
            Assert.Contains("\"timestamp\":", json);
        }

        [Fact]
        public void McpCommandStatusNotification_Deserialization_RestoresAllProperties()
        {
            // Arrange
            var json = """
                {
                    "commandId": "cmd456",
                    "command": "!process",
                    "status": "executing",
                    "progress": 50,
                    "message": "Processing...",
                    "result": null,
                    "error": null,
                    "timestamp": "2023-01-01T12:00:00Z"
                }
                """;

            // Act
            var notification = JsonSerializer.Deserialize<McpCommandStatusNotification>(json);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("cmd456", notification.CommandId);
            Assert.Equal("!process", notification.Command);
            Assert.Equal("executing", notification.Status);
            Assert.Equal(50, notification.Progress);
            Assert.Equal("Processing...", notification.Message);
            Assert.Null(notification.Result);
            Assert.Null(notification.Error);
            Assert.Equal(new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc), notification.Timestamp);
        }

        [Fact]
        public void McpSessionRecoveryNotification_Serialization_IncludesAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var notification = new McpSessionRecoveryNotification
            {
                Reason = "command timeout",
                RecoveryStep = "restart session",
                Success = true,
                Message = "Session restarted successfully",
                AffectedCommands = new[] { "cmd1", "cmd2", "cmd3" },
                Timestamp = timestamp
            };

            // Act
            var json = JsonSerializer.Serialize(notification);

            // Assert
            Assert.Contains("\"reason\":\"command timeout\"", json);
            Assert.Contains("\"recoveryStep\":\"restart session\"", json);
            Assert.Contains("\"success\":true", json);
            Assert.Contains("\"message\":\"Session restarted successfully\"", json);
            Assert.Contains("\"affectedCommands\":[\"cmd1\",\"cmd2\",\"cmd3\"]", json);
            Assert.Contains("\"timestamp\":", json);
        }

        [Fact]
        public void McpSessionRecoveryNotification_Deserialization_RestoresAllProperties()
        {
            // Arrange
            var json = """
                {
                    "reason": "session hang",
                    "recoveryStep": "cancel operations",
                    "success": false,
                    "message": "Recovery failed",
                    "affectedCommands": ["cmd5", "cmd6"],
                    "timestamp": "2023-01-01T15:30:00Z"
                }
                """;

            // Act
            var notification = JsonSerializer.Deserialize<McpSessionRecoveryNotification>(json);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("session hang", notification.Reason);
            Assert.Equal("cancel operations", notification.RecoveryStep);
            Assert.False(notification.Success);
            Assert.Equal("Recovery failed", notification.Message);
            Assert.Equal(new[] { "cmd5", "cmd6" }, notification.AffectedCommands);
            Assert.Equal(new DateTime(2023, 1, 1, 15, 30, 0, DateTimeKind.Utc), notification.Timestamp);
        }

        [Fact]
        public void McpServerHealthNotification_Serialization_IncludesAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var uptime = TimeSpan.FromHours(2.5);
            var notification = new McpServerHealthNotification
            {
                Status = "healthy",
                CdbSessionActive = true,
                QueueSize = 3,
                ActiveCommands = 1,
                Uptime = uptime,
                Timestamp = timestamp
            };

            // Act
            var json = JsonSerializer.Serialize(notification);

            // Assert
            Assert.Contains("\"status\":\"healthy\"", json);
            Assert.Contains("\"cdbSessionActive\":true", json);
            Assert.Contains("\"queueSize\":3", json);
            Assert.Contains("\"activeCommands\":1", json);
            Assert.Contains("\"uptime\":", json);
            Assert.Contains("\"timestamp\":", json);
        }

        [Fact]
        public void McpServerHealthNotification_Deserialization_RestoresAllProperties()
        {
            // Arrange
            var json = """
                {
                    "status": "degraded",
                    "cdbSessionActive": false,
                    "queueSize": 10,
                    "activeCommands": 0,
                    "uptime": "01:30:00",
                    "timestamp": "2023-01-01T18:45:00Z"
                }
                """;

            // Act
            var notification = JsonSerializer.Deserialize<McpServerHealthNotification>(json);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("degraded", notification.Status);
            Assert.False(notification.CdbSessionActive);
            Assert.Equal(10, notification.QueueSize);
            Assert.Equal(0, notification.ActiveCommands);
            Assert.Equal(TimeSpan.FromHours(1.5), notification.Uptime);
            Assert.Equal(new DateTime(2023, 1, 1, 18, 45, 0, DateTimeKind.Utc), notification.Timestamp);
        }

        [Fact]
        public void McpCommandStatusNotification_DefaultValues_AreCorrect()
        {
            // Act
            var notification = new McpCommandStatusNotification();

            // Assert
            Assert.Equal(string.Empty, notification.CommandId);
            Assert.Equal(string.Empty, notification.Command);
            Assert.Equal(string.Empty, notification.Status);
            Assert.Null(notification.Progress);
            Assert.Null(notification.Message);
            Assert.Null(notification.Result);
            Assert.Null(notification.Error);
            Assert.True(notification.Timestamp <= DateTime.UtcNow);
            Assert.True(notification.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be very recent
        }

        [Fact]
        public void McpSessionRecoveryNotification_DefaultValues_AreCorrect()
        {
            // Act
            var notification = new McpSessionRecoveryNotification();

            // Assert
            Assert.Equal(string.Empty, notification.Reason);
            Assert.Equal(string.Empty, notification.RecoveryStep);
            Assert.False(notification.Success);
            Assert.Equal(string.Empty, notification.Message);
            Assert.Null(notification.AffectedCommands);
            Assert.True(notification.Timestamp <= DateTime.UtcNow);
            Assert.True(notification.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be very recent
        }

        [Fact]
        public void McpServerHealthNotification_DefaultValues_AreCorrect()
        {
            // Act
            var notification = new McpServerHealthNotification();

            // Assert
            Assert.Equal(string.Empty, notification.Status);
            Assert.False(notification.CdbSessionActive);
            Assert.Equal(0, notification.QueueSize);
            Assert.Equal(0, notification.ActiveCommands);
            Assert.Null(notification.Uptime);
            Assert.True(notification.Timestamp <= DateTime.UtcNow);
            Assert.True(notification.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be very recent
        }

        [Fact]
        public void McpNotification_DefaultValues_AreCorrect()
        {
            // Act
            var notification = new McpNotification();

            // Assert
            Assert.Equal("2.0", notification.JsonRpc);
            Assert.Equal(string.Empty, notification.Method);
            Assert.Null(notification.Params);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_Serialization_IncludesAllProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var notification = new McpCommandHeartbeatNotification
            {
                CommandId = "cmd123",
                Command = "!analyze -v",
                ElapsedSeconds = 150.5,
                ElapsedDisplay = "2.5m",
                Details = "Analyzing crash dump...",
                Timestamp = timestamp
            };

            // Act
            var json = JsonSerializer.Serialize(notification);

            // Assert
            Assert.Contains("\"commandId\":\"cmd123\"", json);
            Assert.Contains("\"command\":\"!analyze -v\"", json);
            Assert.Contains("\"elapsedSeconds\":150.5", json);
            Assert.Contains("\"elapsedDisplay\":\"2.5m\"", json);
            Assert.Contains("\"details\":\"Analyzing crash dump...\"", json);
            Assert.Contains("\"timestamp\":", json);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_Deserialization_RestoresAllProperties()
        {
            // Arrange
            var json = """
                {
                    "commandId": "cmd456",
                    "command": "!heap",
                    "elapsedSeconds": 90.0,
                    "elapsedDisplay": "1.5m",
                    "details": "Processing heap data...",
                    "timestamp": "2023-01-01T14:30:00Z"
                }
                """;

            // Act
            var notification = JsonSerializer.Deserialize<McpCommandHeartbeatNotification>(json);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("cmd456", notification.CommandId);
            Assert.Equal("!heap", notification.Command);
            Assert.Equal(90.0, notification.ElapsedSeconds);
            Assert.Equal("1.5m", notification.ElapsedDisplay);
            Assert.Equal("Processing heap data...", notification.Details);
            Assert.Equal(new DateTime(2023, 1, 1, 14, 30, 0, DateTimeKind.Utc), notification.Timestamp);
        }

        [Fact]
        public void McpCommandHeartbeatNotification_DefaultValues_AreCorrect()
        {
            // Act
            var notification = new McpCommandHeartbeatNotification();

            // Assert
            Assert.Equal(string.Empty, notification.CommandId);
            Assert.Equal(string.Empty, notification.Command);
            Assert.Equal(0.0, notification.ElapsedSeconds);
            Assert.Equal(string.Empty, notification.ElapsedDisplay);
            Assert.Null(notification.Details);
            Assert.True(notification.Timestamp <= DateTime.UtcNow);
            Assert.True(notification.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be very recent
        }

        [Fact]
        public void McpCapabilities_IncludesNotificationSupport()
        {
            // Act
            var capabilities = new McpCapabilities();

            // Assert
            Assert.NotNull(capabilities.Notifications);
            
            var json = JsonSerializer.Serialize(capabilities, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            Assert.Contains("\"notifications\":", json);
            Assert.Contains("\"commandStatus\":true", json);
            Assert.Contains("\"sessionRecovery\":true", json);
            Assert.Contains("\"serverHealth\":true", json);
        }
    }
}
