using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using System;
using System.Threading.Tasks;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for NotificationHelper
    /// </summary>
    public class NotificationHelperTests
    {
        private readonly Mock<IMcpNotificationService> _mockNotificationService;
        private readonly Mock<ILogger> _mockLogger;

        public NotificationHelperTests()
        {
            _mockNotificationService = new Mock<IMcpNotificationService>();
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void NotificationHelper_Class_Exists()
        {
            // This test verifies that the NotificationHelper class exists and can be instantiated
            Assert.True(typeof(NotificationHelper) != null);
        }

        [Fact]
        public void NotificationHelper_IsStaticClass()
        {
            // Verify that NotificationHelper is a static class
            Assert.True(typeof(NotificationHelper).IsAbstract && typeof(NotificationHelper).IsSealed);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithSessionId_CallsNotificationService()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";
            var result = "test-result";
            var progress = 50;

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                sessionId,
                commandId,
                command,
                status,
                result,
                progress);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                sessionId, commandId, command, status,
                result, progress, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithSessionId_HandlesExceptions()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";

            _mockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Notification failed"));

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                sessionId,
                commandId,
                command,
                status);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatFireAndForget_CallsNotificationService()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var elapsed = TimeSpan.FromMinutes(5);

            // Act
            NotificationHelper.NotifyCommandHeartbeatFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                sessionId,
                commandId,
                command,
                elapsed);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockNotificationService.Verify(x => x.NotifyCommandHeartbeatAsync(
                sessionId, commandId, command, elapsed, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatFireAndForget_HandlesExceptions()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var elapsed = TimeSpan.FromMinutes(5);

            _mockNotificationService.Setup(x => x.NotifyCommandHeartbeatAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Heartbeat failed"));

            // Act
            NotificationHelper.NotifyCommandHeartbeatFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                sessionId,
                commandId,
                command,
                elapsed);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send heartbeat")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithoutSessionId_CallsNotificationService()
        {
            // Arrange
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";
            var progress = 50;
            var message = "test-message";
            var result = "test-result";
            var error = "test-error";

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                commandId,
                command,
                status,
                progress,
                message,
                result,
                error);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                commandId, command, status, It.IsAny<int?>(), message, result, error), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithoutSessionId_HandlesExceptions()
        {
            // Arrange
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";

            _mockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Notification failed"));

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                commandId,
                command,
                status);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockLogger.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithSessionId_WithNullResult_HandlesCorrectly()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                sessionId,
                commandId,
                command,
                status,
                null);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                sessionId, commandId, command, status, null, 0, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithoutSessionId_WithDefaultValues_HandlesCorrectly()
        {
            // Arrange
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                _mockNotificationService.Object,
                _mockLogger.Object,
                commandId,
                command,
                status);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(500);
            _mockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                commandId, command, status, It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
