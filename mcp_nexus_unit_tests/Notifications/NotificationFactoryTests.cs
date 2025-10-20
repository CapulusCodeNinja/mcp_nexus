using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Notifications;
using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus_unit_tests.Notifications
{
    /// <summary>
    /// Tests for NotificationFactory
    /// </summary>
    public class NotificationFactoryTests
    {
        private static Dictionary<string, object> GetParamsAsDictionary(object? @params)
        {
            var json = JsonSerializer.Serialize(@params!);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json)!;
        }
        [Fact]
        public void CreateCommandStatusNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var status = "running";

            // Act
            var notification = NotificationFactory.CreateCommandStatusNotification(sessionId, commandId, status, 0, "");

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_status", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(sessionId, paramsObj!["SessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(status, paramsObj!["Status"]!.ToString());
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

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(sessionId, paramsObj!["SessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(status, paramsObj!["Status"]!.ToString());
            Assert.Equal(progress, JsonSerializer.Deserialize<int>(paramsObj!["Progress"]!.ToString()!));
            Assert.Equal(message, paramsObj["Message"]!.ToString());
        }

        [Fact]
        public void CreateCommandStatusNotification_WithNullMessage_ExcludesMessageFromParams()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var status = "running";

            // Act
            var notification = NotificationFactory.CreateCommandStatusNotification(sessionId, commandId, status, 0, null!);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_status", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(sessionId, paramsObj["SessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj["CommandId"]!.ToString());
            Assert.Equal(status, paramsObj["Status"]!.ToString());
            Assert.Null(paramsObj["Message"]);
        }

        [Fact]
        public void CreateCommandCompletionNotification_WithValidParameters_CreatesValidNotification()
        {
            // Arrange
            var commandId = "cmd-456";
            var result = new { output = "Command completed successfully" };

            // Act
            var notification = NotificationFactory.CreateCommandCompletionNotification(commandId, result.output, "");

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_completion", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(result.output, paramsObj!["Result"]!.ToString());
            Assert.Equal("", paramsObj!["Error"]!.ToString());
        }

        [Fact]
        public void CreateCommandFailureNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var commandId = "cmd-456";
            var error = "Command failed";

            // Act
            var notification = NotificationFactory.CreateCommandFailureNotification(commandId, error);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_failure", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(error, paramsObj!["Error"]!.ToString());
        }

        [Fact]
        public void CreateCommandFailureNotification_WithDetails_CreatesValidNotification()
        {
            // Arrange
            var commandId = "cmd-456";
            var error = "Command failed";

            // Act
            var notification = NotificationFactory.CreateCommandFailureNotification(commandId, error);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_failure", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(error, paramsObj!["Error"]!.ToString());
        }

        [Fact]
        public void CreateCommandFailureNotification_WithNullDetails_ExcludesDetailsFromParams()
        {
            // Arrange
            var commandId = "cmd-456";
            var error = "Command failed";

            // Act
            var notification = NotificationFactory.CreateCommandFailureNotification(commandId, error);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_failure", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(error, paramsObj!["Error"]!.ToString());
            Assert.False(paramsObj!.ContainsKey("Details"));
        }

        [Fact]
        public void CreateCommandHeartbeatNotification_WithValidParameters_CreatesValidNotification()
        {
            // Arrange
            var commandId = "cmd-456";
            var elapsed = TimeSpan.FromSeconds(75);

            // Act
            var notification = NotificationFactory.CreateCommandHeartbeatNotification(commandId, elapsed);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_heartbeat", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(elapsed.TotalMilliseconds, JsonSerializer.Deserialize<double>(paramsObj!["Elapsed"]!.ToString()!));
        }

        [Fact]
        public void CreateSessionEventNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "session_started";

            // Act
            var notification = NotificationFactory.CreateSessionEventNotification(sessionId, eventType, new { });

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/session_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(sessionId, paramsObj!["SessionId"]!.ToString());
            Assert.Equal(eventType, paramsObj!["EventType"]!.ToString());
        }

        [Fact]
        public void CreateSessionEventNotification_WithEventData_CreatesValidNotification()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "session_started";
            var eventData = new { timestamp = DateTime.Now, version = "1.0" };

            // Act
            var notification = NotificationFactory.CreateSessionEventNotification(sessionId, eventType, eventData);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/session_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(sessionId, paramsObj!["SessionId"]!.ToString());
            Assert.Equal(eventType, paramsObj!["EventType"]!.ToString());
            Assert.NotNull(paramsObj["Data"]);
        }

        [Fact]
        public void CreateSessionEventNotification_WithNullEventData_ExcludesEventDataFromParams()
        {
            // Arrange
            var sessionId = "session-123";
            var eventType = "session_started";

            // Act
            var notification = NotificationFactory.CreateSessionEventNotification(sessionId, eventType, null!);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/session_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(sessionId, paramsObj!["SessionId"]!.ToString());
            Assert.Equal(eventType, paramsObj!["EventType"]!.ToString());
            Assert.Null(paramsObj["Data"]);
        }

        [Fact]
        public void CreateQueueEventNotification_WithValidParameters_CreatesValidNotification()
        {
            // Arrange
            var eventType = "queue_updated";
            var queueSize = 5;

            // Act
            var notification = NotificationFactory.CreateQueueEventNotification(eventType, new { QueueSize = queueSize });

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/queue_event", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(eventType, paramsObj!["EventType"]!.ToString());
            Assert.NotNull(paramsObj["Data"]);
        }

        [Fact]
        public void CreateRecoveryNotification_WithRequiredParameters_CreatesValidNotification()
        {
            // Arrange
            var recoveryType = "session_timeout";
            var success = false;

            // Act
            var notification = NotificationFactory.CreateRecoveryNotification(recoveryType, success);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/recovery", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(recoveryType, paramsObj!["Reason"]!.ToString());
            Assert.Equal(success.ToString().ToLower(), paramsObj!["Success"]?.ToString()?.ToLower());
        }

        [Fact]
        public void CreateRecoveryNotification_WithDetails_CreatesValidNotification()
        {
            // Arrange
            var recoveryType = "session_timeout";
            var success = false;

            // Act
            var notification = NotificationFactory.CreateRecoveryNotification(recoveryType, success);

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/recovery", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(recoveryType, paramsObj!["Reason"]!.ToString());
            Assert.Equal(success.ToString().ToLower(), paramsObj!["Success"]?.ToString()?.ToLower());
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

            var paramsObj = GetParamsAsDictionary(notification.Params);
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
            var notification = NotificationFactory.CreateCommandStatusNotification(sessionId, commandId, status, 0, "");

            // Assert
            Assert.NotNull(notification);
            Assert.Equal("notifications/command_status", notification.Method);
            Assert.NotNull(notification.Params);

            var paramsObj = GetParamsAsDictionary(notification.Params);
            Assert.Equal(sessionId, paramsObj!["SessionId"]!.ToString());
            Assert.Equal(commandId, paramsObj!["CommandId"]!.ToString());
            Assert.Equal(status, paramsObj!["Status"]!.ToString());
        }
    }
}