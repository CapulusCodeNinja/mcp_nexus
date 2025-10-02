using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Notifications;
using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus_tests.Notifications
{
    /// <summary>
    /// Tests for NotificationFactory
    /// </summary>
    public class NotificationFactoryTests
    {
        [Fact]
        public void CreateCommandStatusNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var status = "running";

            // Act
            var notification = NotificationFactory.CreateCommandStatusNotification(sessionId, commandId, status);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_status", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(status, paramsObj!["status"]!.ToString());
        }

        [Fact]
        public void CreateCommandStatusNotification_WithOptionalParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var status = "running";
            var progress = 50;
            var message = "Processing...";

            // Act
            var notification = NotificationFactory.CreateCommandStatusNotification(sessionId, commandId, status, progress, message);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_status", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(status, paramsObj!["status"]!.ToString());
            Assert.Equal(progress, JsonSerializer.Deserialize<int>(paramsObj!["progress"]!.ToString()!));
            Assert.Equal(message, paramsObj["message"]!.ToString());
        }

        [Fact]
        public void CreateCommandStatusNotification_WithNullMessage_ExcludesMessageFromParams()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var status = "running";

            // Act
            var notification = NotificationFactory.CreateCommandStatusNotification(sessionId, commandId, status, message: null);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_status", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(status, paramsObj!["status"]!.ToString());
            Assert.False(paramsObj.ContainsKey("message"));
        }

        [Fact]
        public void CreateCommandCompletionNotification_WithValidParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var result = new { output = "Command completed successfully" };

            // Act
            var notification = NotificationFactory.CreateCommandCompletionNotification(sessionId, commandId, result);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_completion", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.NotNull(paramsObj["result"]);
        }

        [Fact]
        public void CreateCommandFailureNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var error = "Command failed";

            // Act
            var notification = NotificationFactory.CreateCommandFailureNotification(sessionId, commandId, error);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_failure", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(error, paramsObj!["error"]!.ToString());
        }

        [Fact]
        public void CreateCommandFailureNotification_WithDetails_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var error = "Command failed";
            var details = "Stack trace here";

            // Act
            var notification = NotificationFactory.CreateCommandFailureNotification(sessionId, commandId, error, details);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_failure", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(error, paramsObj!["error"]!.ToString());
            Assert.Equal(details, paramsObj["details"]!.ToString());
        }

        [Fact]
        public void CreateCommandFailureNotification_WithNullDetails_ExcludesDetailsFromParams()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var error = "Command failed";

            // Act
            var notification = NotificationFactory.CreateCommandFailureNotification(sessionId, commandId, error, details: null);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_failure", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(error, paramsObj!["error"]!.ToString());
            Assert.False(paramsObj!.ContainsKey("details"));
        }

        [Fact]
        public void CreateCommandHeartbeatNotification_WithValidParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var status = "running";
            var progress = 75;

            // Act
            var notification = NotificationFactory.CreateCommandHeartbeatNotification(sessionId, commandId, status, progress);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_heartbeat", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(status, paramsObj!["status"]!.ToString());
            Assert.Equal(progress, JsonSerializer.Deserialize<int>(paramsObj!["progress"]!.ToString()!));
        }

        [Fact]
        public void CreateSessionEventNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "session_started";

            // Act
            var notification = NotificationFactory.CreateSessionEventNotification(sessionId, eventType);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/session_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(eventType, paramsObj!["eventType"]!.ToString());
        }

        [Fact]
        public void CreateSessionEventNotification_WithEventData_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "session_started";
            var eventData = new { timestamp = DateTime.UtcNow, version = "1.0" };

            // Act
            var notification = NotificationFactory.CreateSessionEventNotification(sessionId, eventType, eventData);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/session_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(eventType, paramsObj!["eventType"]!.ToString());
            Assert.NotNull(paramsObj["eventData"]);
        }

        [Fact]
        public void CreateSessionEventNotification_WithNullEventData_ExcludesEventDataFromParams()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "session_started";

            // Act
            var notification = NotificationFactory.CreateSessionEventNotification(sessionId, eventType, eventData: null);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/session_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(eventType, paramsObj!["eventType"]!.ToString());
            Assert.False(paramsObj.ContainsKey("eventData"));
        }

        [Fact]
        public void CreateQueueEventNotification_WithValidParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "queue_updated";
            var queueSize = 5;

            // Act
            var notification = NotificationFactory.CreateQueueEventNotification(sessionId, eventType, queueSize);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/queue_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(eventType, paramsObj!["eventType"]!.ToString());
            Assert.Equal(queueSize, JsonSerializer.Deserialize<int>(paramsObj!["eventData"]!.ToString()!));
        }

        [Fact]
        public void CreateRecoveryNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var recoveryType = "session_timeout";
            var status = "attempting";

            // Act
            var notification = NotificationFactory.CreateRecoveryNotification(sessionId, recoveryType, status);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/recovery", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(recoveryType, paramsObj!["recoveryType"]!.ToString());
            Assert.Equal(status, paramsObj!["status"]!.ToString());
        }

        [Fact]
        public void CreateRecoveryNotification_WithDetails_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var recoveryType = "session_timeout";
            var status = "attempting";
            var details = new { retryCount = 3, lastError = "Connection lost" };

            // Act
            var notification = NotificationFactory.CreateRecoveryNotification(sessionId, recoveryType, status, details);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/recovery", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(recoveryType, paramsObj!["recoveryType"]!.ToString());
            Assert.Equal(status, paramsObj!["status"]!.ToString());
            Assert.NotNull(paramsObj!["details"]);
        }

        [Fact]
        public void CreateCustomNotification_WithValidParameters_CreatesValidNotification()
        {
            // Arrange
            var method = "custom/notification";
            var data = new { key = "data", value = 123 };

            // Act
            var notification = NotificationFactory.CreateCustomNotification(method, data);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal(method, notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal("data", paramsObj["key"]!.ToString());
            Assert.Equal(123, JsonSerializer.Deserialize<int>(paramsObj["value"]!.ToString()!));
        }

        [Theory]
        [InlineData("session-123", "", "running")]
        [InlineData("", "cmd-123", "running")]
        [InlineData("session-123", "cmd-123", "")]
        public void CreateCommandStatusNotification_WithEmptyStrings_CreatesNotificationWithEmptyValues(string sessionId, string commandId, string status)
        {
            // Act
            var notification = NotificationFactory.CreateCommandStatusNotification(sessionId, commandId, status);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_status", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsElement = (JsonElement)notification.Params!;
            var paramsObj = paramsElement.ValueKind == JsonValueKind.Object ?
                JsonSerializer.Deserialize<Dictionary<string, object>>(paramsElement.GetRawText()) : null;
            Assert.NotNull(paramsObj);
            Assert.Equal(sessionId, paramsObj!["sessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["commandId"]!.ToString());
            Assert.Equal(status, paramsObj!["status"]!.ToString());
        }
    }
}