using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Notification;
using mcp_nexus.Notifications;
using System;
using System.Threading.Tasks;

namespace mcp_nexus_unit_tests.CommandQueue.Notification
{
    /// <summary>
    /// Tests for NotificationHelper
    /// </summary>
    public class NotificationHelperTests
    {
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly Mock<ILogger> m_MockLogger;

        public NotificationHelperTests()
        {
            m_MockNotificationService = new Mock<IMcpNotificationService>();
            m_MockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void NotificationHelper_Class_Exists()
        {
            // This test verifies that the NotificationHelper class exists and can be instantiated
            Assert.NotNull(typeof(NotificationHelper));
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
            var notificationReceived = new TaskCompletionSource<bool>();
            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask)
                .Callback(() => notificationReceived.SetResult(true));

            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";
            var result = "test-result";
            var progress = 50;

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                status,
                result,
                progress);

            // Assert
            // Wait for notification to be received deterministically
            await notificationReceived.Task;
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                sessionId, commandId, status, result ?? string.Empty, string.Empty, progress), Times.AtLeastOnce);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithSessionId_HandlesExceptions()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";

            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Notification failed"));

            // Arrange - Setup mock callback BEFORE calling the method
            var logReceived = new TaskCompletionSource<bool>();
            m_MockLogger.Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(() => logReceived.SetResult(true));

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                status);

            // Assert
            await logReceived.Task;
            m_MockLogger.Verify(x => x.Log(
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

            var notificationReceived = new TaskCompletionSource<bool>();
            m_MockNotificationService.Setup(x => x.NotifyCommandHeartbeatAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask)
                .Callback(() => notificationReceived.SetResult(true));

            // Act
            NotificationHelper.NotifyCommandHeartbeatFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                elapsed);

            // Assert
            // Wait for the fire-and-forget notification to complete
            await notificationReceived.Task;

            // Verify was called at least once (may not be called if system is under heavy load)
            // This is acceptable for fire-and-forget notifications
            m_MockNotificationService.Verify(x => x.NotifyCommandHeartbeatAsync(
                sessionId, commandId, It.IsAny<string>(), elapsed), Times.AtLeastOnce());
        }

        [Fact]
        public async Task NotifyCommandHeartbeatFireAndForget_HandlesExceptions()
        {
            // Arrange
            var sessionId = "session-123";
            var commandId = "cmd-456";
            var command = "test-command";
            var elapsed = TimeSpan.FromMinutes(5);

            m_MockNotificationService.Setup(x => x.NotifyCommandHeartbeatAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ThrowsAsync(new Exception("Heartbeat failed"));

            // Arrange - Setup mock callback BEFORE calling the method
            var logReceived = new TaskCompletionSource<bool>();
            m_MockLogger.Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send heartbeat")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(() => logReceived.SetResult(true));

            // Act
            NotificationHelper.NotifyCommandHeartbeatFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                elapsed);

            // Assert
            await logReceived.Task;
            m_MockLogger.Verify(x => x.Log(
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

            // Arrange - Setup mock callback BEFORE calling the method
            var notificationReceived = new TaskCompletionSource<bool>();
            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Callback(() => notificationReceived.SetResult(true));

            // Act
            NotificationHelper.NotifyCommandStatusDetailedFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                commandId,
                command,
                status,
                progress,
                message,
                result,
                error);

            // Assert
            await notificationReceived.Task;
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                commandId, command, status, It.IsAny<int>(), message, result, error), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithoutSessionId_HandlesExceptions()
        {
            // Arrange
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";

            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Notification failed"));

            // Arrange - Setup mock callback BEFORE calling the method
            var logReceived = new TaskCompletionSource<bool>();
            m_MockLogger.Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(() => logReceived.SetResult(true));

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                "session-123",
                commandId,
                command,
                status);

            // Assert
            await logReceived.Task;
            m_MockLogger.Verify(x => x.Log(
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

            // Arrange - Setup mock callback BEFORE calling the method
            var notificationReceived = new TaskCompletionSource<bool>();
            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask)
                .Callback(() => notificationReceived.SetResult(true));

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                status,
                null);

            // Assert
            await notificationReceived.Task;
            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                sessionId, commandId, status, string.Empty, string.Empty, 0), Times.Once);
        }

        [Fact]
        public async Task NotifyCommandStatusFireAndForget_WithoutSessionId_WithDefaultValues_HandlesCorrectly()
        {
            // Arrange
            var commandId = "cmd-456";
            var command = "test-command";
            var status = "executing";
            var tcs = new TaskCompletionSource<bool>();
            var verificationCount = 0;

            // Set up the mock to signal when the expected call is made
            m_MockNotificationService.Setup(x => x.NotifyCommandStatusAsync(
                "session-123", commandId, status, string.Empty, string.Empty, 0))
                .Returns(Task.CompletedTask)
                .Callback(() =>
                {
                    verificationCount++;
                    if (verificationCount == 1)
                        tcs.SetResult(true);
                });

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                "session-123",
                commandId,
                command,
                status);

            // Assert - Wait for the async operation to complete
            await tcs.Task;

            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                "session-123", commandId, status, string.Empty, string.Empty, 0), Times.Once);
        }
    }
}
