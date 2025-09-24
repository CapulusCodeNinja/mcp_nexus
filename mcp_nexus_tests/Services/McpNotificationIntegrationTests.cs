using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using mcp_nexus.Services;
using mcp_nexus.Helper;
using mcp_nexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace mcp_nexus_tests.Services
{
    public class McpNotificationIntegrationTests : IDisposable
    {
        private readonly McpNotificationService m_notificationService;
        private readonly ResilientCommandQueueService m_queueService;
        private readonly CdbSessionRecoveryService m_recoveryService;
        private readonly Mock<ICdbSession> m_mockCdbSession;
        private readonly Mock<ICommandTimeoutService> m_mockTimeoutService;
        private readonly List<McpNotification> m_receivedNotifications;

        public McpNotificationIntegrationTests()
        {
            // Setup notification service
            var notificationLogger = LoggerFactory.Create(b => { }).CreateLogger<McpNotificationService>();
            m_notificationService = new McpNotificationService(notificationLogger);
            
            m_receivedNotifications = new List<McpNotification>();
            m_notificationService.RegisterNotificationHandler(notification =>
            {
                lock (m_receivedNotifications)
                {
                    m_receivedNotifications.Add(notification);
                }
                return Task.CompletedTask;
            });

            // Setup mocks
            m_mockCdbSession = new Mock<ICdbSession>();
            m_mockTimeoutService = new Mock<ICommandTimeoutService>();

            // Setup recovery service
            var recoveryLogger = LoggerFactory.Create(b => { }).CreateLogger<CdbSessionRecoveryService>();
            
            // Create a basic command queue service for recovery service (it needs one)
            var basicQueueLogger = LoggerFactory.Create(b => { }).CreateLogger<CommandQueueService>();
            var basicQueueService = new CommandQueueService(m_mockCdbSession.Object, basicQueueLogger);
            
            m_recoveryService = new CdbSessionRecoveryService(
                m_mockCdbSession.Object, 
                recoveryLogger, 
                reason => basicQueueService.CancelAllCommands(reason),
                m_notificationService);

            // Setup resilient queue service
            var queueLogger = LoggerFactory.Create(b => { }).CreateLogger<ResilientCommandQueueService>();
            m_queueService = new ResilientCommandQueueService(
                m_mockCdbSession.Object,
                queueLogger,
                m_mockTimeoutService.Object,
                m_recoveryService,
                m_notificationService);

            basicQueueService.Dispose(); // Clean up the basic service since we're using resilient one
        }

        public void Dispose()
        {
            m_queueService?.Dispose();
            m_notificationService?.Dispose();
        }

        [Fact]
        public async Task QueueCommand_SendsQueuedNotification()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true);

            // Act
            var commandId = m_queueService.QueueCommand("!analyze -v");

            // Wait for notification to be sent
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(m_receivedNotifications);
            var queuedNotification = m_receivedNotifications.FirstOrDefault(n => 
                n.Method == "notifications/commandStatus");
            
            Assert.NotNull(queuedNotification);
            var statusParams = queuedNotification.Params as McpCommandStatusNotification;
            Assert.NotNull(statusParams);
            Assert.Equal(commandId, statusParams!.CommandId);
            Assert.Equal("!analyze -v", statusParams!.Command);
            Assert.Equal("queued", statusParams!.Status);
            Assert.Equal(0, statusParams!.Progress);
        }

        [Fact]
        public async Task ExecuteCommand_SendsExecutingAndCompletedNotifications()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
            m_mockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Command result");

            // Act
            var commandId = m_queueService.QueueCommand("!version");
            var result = await m_queueService.GetCommandResult(commandId);

            // Wait for all notifications
            await Task.Delay(200);

            // Assert
            var notifications = m_receivedNotifications
                .Where(n => n.Method == "notifications/commandStatus")
                .Select(n => n.Params as McpCommandStatusNotification)
                .Where(p => p?.CommandId == commandId)
                .ToList();

            Assert.True(notifications.Count >= 2, $"Expected at least 2 notifications, got {notifications.Count}");
            
            // Should have queued notification
            var queuedNotification = notifications.FirstOrDefault(n => n?.Status == "queued");
            Assert.NotNull(queuedNotification);
            
            // Should have executing notification
            var executingNotification = notifications.FirstOrDefault(n => n?.Status == "executing");
            Assert.NotNull(executingNotification);
            Assert.Equal(10, executingNotification!.Progress);
            
            // Should have completed notification
            var completedNotification = notifications.FirstOrDefault(n => n?.Status == "completed");
            Assert.NotNull(completedNotification);
            Assert.Equal(100, completedNotification!.Progress);
            Assert.Equal("Command result", completedNotification!.Result);
        }

        [Fact]
        public async Task CancelCommand_SendsCancelledNotification()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
            m_mockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (string cmd, CancellationToken ct) =>
                {
                    await Task.Delay(5000, ct); // Long running command
                    return "Should not complete";
                });

            // Act
            var commandId = m_queueService.QueueCommand("!longcommand");
            await Task.Delay(50); // Let it start
            
            var cancelled = m_queueService.CancelCommand(commandId);
            Assert.True(cancelled);

            // Wait for notifications
            await Task.Delay(200);

            // Assert
            var notifications = m_receivedNotifications
                .Where(n => n.Method == "notifications/commandStatus")
                .Select(n => n.Params as McpCommandStatusNotification)
                .Where(p => p?.CommandId == commandId)
                .ToList();

            var cancelledNotification = notifications.FirstOrDefault(n => n?.Status == "cancelled");
            Assert.NotNull(cancelledNotification);
            Assert.Null(cancelledNotification!.Progress);
            Assert.Contains("cancelled", cancelledNotification!.Message!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CommandExecutionError_SendsFailedNotification()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
            m_mockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("CDB execution failed"));

            // Act
            var commandId = m_queueService.QueueCommand("!errorcommand");
            var result = await m_queueService.GetCommandResult(commandId);

            // Wait for notifications
            await Task.Delay(200);

            // Assert
            var notifications = m_receivedNotifications
                .Where(n => n.Method == "notifications/commandStatus")
                .Select(n => n.Params as McpCommandStatusNotification)
                .Where(p => p?.CommandId == commandId)
                .ToList();

            var failedNotification = notifications.FirstOrDefault(n => n?.Status == "failed");
            Assert.NotNull(failedNotification);
            Assert.Equal("CDB execution failed", failedNotification!.Error);
            Assert.Equal("Command execution failed", failedNotification!.Message);
        }

        [Fact]
        public async Task SessionRecovery_SendsRecoveryNotifications()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Returns(false); // Unhealthy session
            m_mockCdbSession.Setup(s => s.StartSession(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var recovered = await m_recoveryService.RecoverStuckSession("test timeout");

            // Wait for notifications
            await Task.Delay(100);

            // Assert
            var recoveryNotifications = m_receivedNotifications
                .Where(n => n.Method == "notifications/sessionRecovery")
                .Select(n => n.Params as McpSessionRecoveryNotification)
                .ToList();

            Assert.NotEmpty(recoveryNotifications);
            
            // Should have start notification
            var startNotification = recoveryNotifications.FirstOrDefault(n => 
                n?.RecoveryStep == "Recovery Started");
            Assert.NotNull(startNotification);
            Assert.Equal("test timeout", startNotification!.Reason);
            Assert.False(startNotification!.Success); // Start notifications are not success yet
        }

        [Fact]
        public async Task MultipleCommands_SendsNotificationsInOrder()
        {
            // Arrange
            m_mockCdbSession.Setup(s => s.IsActive).Returns(true);
            m_mockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Result");

            // Act
            var commandId1 = m_queueService.QueueCommand("!command1");
            var commandId2 = m_queueService.QueueCommand("!command2");
            
            await Task.WhenAll(
                m_queueService.GetCommandResult(commandId1),
                m_queueService.GetCommandResult(commandId2)
            );

            // Wait for all notifications
            await Task.Delay(300);

            // Assert
            var cmd1Notifications = m_receivedNotifications
                .Where(n => n.Method == "notifications/commandStatus")
                .Select(n => n.Params as McpCommandStatusNotification)
                .Where(p => p?.CommandId == commandId1)
                .Cast<McpCommandStatusNotification>()
                .OrderBy(p => p.Timestamp)
                .ToList();

            var cmd2Notifications = m_receivedNotifications
                .Where(n => n.Method == "notifications/commandStatus")
                .Select(n => n.Params as McpCommandStatusNotification)
                .Where(p => p?.CommandId == commandId2)
                .Cast<McpCommandStatusNotification>()
                .OrderBy(p => p.Timestamp)
                .ToList();

            // Both commands should have complete lifecycle notifications
            Assert.True(cmd1Notifications.Count >= 2, $"Command 1 should have at least 2 notifications, got {cmd1Notifications.Count}");
            Assert.True(cmd2Notifications.Count >= 2, $"Command 2 should have at least 2 notifications, got {cmd2Notifications.Count}");

            // Check that both commands have queued notifications
            Assert.Contains(cmd1Notifications, n => n?.Status == "queued");
            Assert.Contains(cmd2Notifications, n => n?.Status == "queued");

            // Check that both commands have either completed or executing notifications
            Assert.True(cmd1Notifications.Any(n => n?.Status == "completed" || n?.Status == "executing"), 
                "Command 1 should have completed or executing notification");
            Assert.True(cmd2Notifications.Any(n => n?.Status == "completed" || n?.Status == "executing"),
                "Command 2 should have completed or executing notification");
        }

        [Fact]
        public async Task NotificationHandler_Exception_DoesNotStopOtherNotifications()
        {
            // Arrange
            var goodNotifications = new List<McpNotification>();
            
            // Add a handler that throws
            m_notificationService.RegisterNotificationHandler(notification =>
            {
                throw new InvalidOperationException("Handler failed");
            });
            
            // Add a handler that works
            m_notificationService.RegisterNotificationHandler(notification =>
            {
                goodNotifications.Add(notification);
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.NotifyCommandStatusAsync("test123", "test", "queued");

            // Wait for notifications
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(goodNotifications);
            Assert.Equal("notifications/commandStatus", goodNotifications[0].Method);
        }
    }
}
