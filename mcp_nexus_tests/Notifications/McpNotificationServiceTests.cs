using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace mcp_nexus_tests.Notifications
{
    public class McpNotificationServiceTests : IDisposable
    {
        private readonly McpNotificationService m_Service;
        private readonly Mock<ILogger<McpNotificationService>> m_MockLogger;

        public McpNotificationServiceTests()
        {
            m_MockLogger = new Mock<ILogger<McpNotificationService>>();
            m_Service = new McpNotificationService(m_MockLogger.Object);
        }

        public void Dispose()
        {
            m_Service?.Dispose();
        }

        [Fact]
        public async Task NotifyCommandStatusAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_Service.Subscribe("CommandStatus", notification =>
            {
                if (notification is McpNotification mcpNotification)
                {
                    receivedNotifications.Add(mcpNotification);
                }
                return Task.CompletedTask;
            });

            // Act
            await m_Service.NotifyCommandStatusAsync("cmd123", "!analyze -v", "executing", 50, "Processing");

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("notifications/commandStatus", notification.Method);
            Assert.NotNull(notification.Params);

            var statusParams = notification.Params as McpCommandStatusNotification;
            Assert.NotNull(statusParams);
            Assert.Equal("cmd123", statusParams.CommandId);
            Assert.Equal("!analyze -v", statusParams.Command);
            Assert.Equal("executing", statusParams.Status);
            Assert.Equal(50, statusParams.Progress);
            Assert.Equal("Processing", statusParams.Message);
        }

        [Fact]
        public async Task NotifySessionRecoveryAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_Service.Subscribe("SessionRecovery", notification =>
            {
                if (notification is McpNotification mcpNotification)
                {
                    receivedNotifications.Add(mcpNotification);
                }
                return Task.CompletedTask;
            });

            var affectedCommands = new[] { "cmd1", "cmd2" };

            // Act
            await m_Service.NotifySessionRecoveryAsync("timeout", "restart", true, "Recovery successful", affectedCommands);

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("notifications/sessionRecovery", notification.Method);

            var recoveryParams = notification.Params as McpSessionRecoveryNotification;
            Assert.NotNull(recoveryParams);
            Assert.Equal("timeout", recoveryParams.Reason);
            Assert.Equal("restart", recoveryParams.RecoveryStep);
            Assert.True(recoveryParams.Success);
            Assert.Equal("Recovery successful", recoveryParams.Message);
            Assert.Equal(affectedCommands, recoveryParams.AffectedCommands);
        }

        [Fact]
        public async Task NotifyServerHealthAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_Service.Subscribe("ServerHealth", notification =>
            {
                if (notification is McpNotification mcpNotification)
                {
                    receivedNotifications.Add(mcpNotification);
                }
                return Task.CompletedTask;
            });

            // Act
            await m_Service.NotifyServerHealthAsync("healthy", "operational", true, 5, 2);

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("notifications/serverHealth", notification.Method);

            var healthParams = notification.Params as McpServerHealthNotification;
            Assert.NotNull(healthParams);
            Assert.Equal("healthy", healthParams.Status);
            Assert.True(healthParams.CdbSessionActive);
            Assert.Equal(5, healthParams.QueueSize);
            Assert.Equal(2, healthParams.ActiveCommands);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithCustomMethod_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_Service.Subscribe("custom/test", notification =>
            {
                if (notification is McpNotification mcpNotification)
                {
                    receivedNotifications.Add(mcpNotification);
                }
                return Task.CompletedTask;
            });

            var customParams = new { message = "test", value = 42 };

            // Act
            await m_Service.PublishNotificationAsync("custom/test", customParams);

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("notifications/custom/test", notification.Method);
            Assert.Equal(customParams, notification.Params);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNoHandlers_LogsWarning()
        {
            // Act
            await m_Service.PublishNotificationAsync("test/method", new { });

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No notification handlers registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithMultipleHandlers_SendsToAllHandlers()
        {
            // Arrange
            var receivedNotifications1 = new List<McpNotification>();
            var receivedNotifications2 = new List<McpNotification>();

            m_Service.Subscribe("test/broadcast", notification =>
            {
                receivedNotifications1.Add(notification);
                return Task.CompletedTask;
            });

            m_Service.Subscribe("test/broadcast", notification =>
            {
                receivedNotifications2.Add(notification);
                return Task.CompletedTask;
            });

            // Act
            await m_Service.PublishNotificationAsync("test/broadcast", new { data = "shared" });

            // Assert
            Assert.Single(receivedNotifications1);
            Assert.Single(receivedNotifications2);
            Assert.Equal("notifications/test/broadcast", receivedNotifications1[0].Method);
            Assert.Equal("notifications/test/broadcast", receivedNotifications2[0].Method);
        }

        [Fact]
        public async Task PublishNotificationAsync_HandlerThrowsException_ContinuesWithOtherHandlers()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            var exceptionThrown = false;

            m_Service.Subscribe("test/error", notification =>
            {
                exceptionThrown = true;
                throw new InvalidOperationException("Handler failed");
            });

            m_Service.Subscribe("test/error", notification =>
            {
                if (notification is McpNotification mcpNotification)
                {
                    receivedNotifications.Add(mcpNotification);
                }
                return Task.CompletedTask;
            });

            // Act
            await m_Service.PublishNotificationAsync("test/error", new { });

            // Assert
            Assert.True(exceptionThrown);
            Assert.Single(receivedNotifications);
            Assert.Equal("notifications/test/error", receivedNotifications[0].Method);
        }

        [Fact]
        public async Task Subscribe_AddsHandler()
        {
            // Arrange
            var handlerCalled = false;
            Task Handler(McpNotification notification)
            {
                handlerCalled = true;
                return Task.CompletedTask;
            }

            // Act
            m_Service.Subscribe("test-event", Handler);
            await m_Service.PublishNotificationAsync("test-event", null!);

            // Assert
            Assert.True(handlerCalled);
        }

        [Fact]
        public async Task NotifyCommandStatusAsync_AfterDispose_DoesNotSendNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_Service.Subscribe("CommandStatus", notification =>
            {
                if (notification is McpNotification mcpNotification)
                {
                    receivedNotifications.Add(mcpNotification);
                }
                return Task.CompletedTask;
            });

            // Act
            m_Service.Dispose();
            await m_Service.NotifyCommandStatusAsync("cmd123", "test", "queued");

            // Assert
            Assert.Empty(receivedNotifications);
        }

        [Fact]
        public void Subscribe_AfterDispose_DoesNothing()
        {
            // Arrange
            m_Service.Dispose();

            // Act & Assert (should not throw)
            var exception = Record.Exception(() =>
                m_Service.Subscribe("test-event", _ => Task.CompletedTask));

            Assert.Null(exception);
        }

        [Fact]
        public async Task NotifyCommandStatusAsync_WithCompleteData_SetsAllProperties()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_Service.Subscribe("CommandStatus", notification =>
            {
                if (notification is McpNotification mcpNotification)
                {
                    receivedNotifications.Add(mcpNotification);
                }
                return Task.CompletedTask;
            });

            // Act
            await m_Service.NotifyCommandStatusAsync(
                "cmd456",
                "!process",
                "completed",
                100,
                "Analysis complete",
                "Process analysis results...",
                null!);

            // Assert
            var statusParams = receivedNotifications[0].Params as McpCommandStatusNotification;
            Assert.NotNull(statusParams);
            Assert.Equal("cmd456", statusParams.CommandId);
            Assert.Equal("!process", statusParams.Command);
            Assert.Equal("completed", statusParams.Status);
            Assert.Equal(100, statusParams.Progress);
            Assert.Equal("Analysis complete", statusParams.Message);
            Assert.Equal("Process analysis results...", statusParams.Result);
            Assert.Null(statusParams.Error);
            Assert.True(statusParams.Timestamp <= DateTime.UtcNow);
        }
    }
}

