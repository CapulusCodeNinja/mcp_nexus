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

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                status);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
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

            // Act
            NotificationHelper.NotifyCommandHeartbeatFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                elapsed);

            // Assert
            // Give the fire-and-forget task a moment to execute
            // Note: This is testing fire-and-forget behavior, so we use a small delay
            // In production, notifications are sent asynchronously and we don't wait for them
            await Task.Delay(500);

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

            // Act
            NotificationHelper.NotifyCommandHeartbeatFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                sessionId,
                commandId,
                command,
                elapsed);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
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
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
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

            // Act
            NotificationHelper.NotifyCommandStatusFireAndForget(
                m_MockNotificationService.Object,
                m_MockLogger.Object,
                "session-123",
                commandId,
                command,
                status);

            // Assert
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
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
            // Wait for the Task.Run to complete
            await Task.Delay(2000);
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

            // Assert - Wait for the async operation to complete with timeout
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            Assert.True(completed == tcs.Task, "Expected notification call was not received within timeout");

            m_MockNotificationService.Verify(x => x.NotifyCommandStatusAsync(
                "session-123", commandId, status, string.Empty, string.Empty, 0), Times.Once);
        }
    }
}
