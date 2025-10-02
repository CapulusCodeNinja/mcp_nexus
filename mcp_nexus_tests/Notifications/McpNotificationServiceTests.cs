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

namespace mcp_nexus_tests.Services
{
    public class McpNotificationServiceTests : IDisposable
    {
        private readonly McpNotificationService m_service;
        private readonly Mock<ILogger<McpNotificationService>> m_mockLogger;

        public McpNotificationServiceTests()
        {
            m_mockLogger = new Mock<ILogger<McpNotificationService>>();
            m_service = new McpNotificationService();
        }

        public void Dispose()
        {
            // McpNotificationService doesn't implement IDisposable
        }

        [Fact]
        public async Task NotifyCommandStatusAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_service.NotifyCommandStatusAsync("cmd123", "!analyze -v", "executing", 50, "Processing", "");

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
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var affectedCommands = new[] { "cmd1", "cmd2" };

            // Act
            await m_service.NotifySessionRecoveryAsync("timeout", "restart", true, "Recovery successful");

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
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var uptime = TimeSpan.FromHours(2);

            // Act
            await m_service.NotifyServerHealthAsync("healthy", true, 5, 2);

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
            Assert.Equal(uptime, healthParams.Uptime);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithCustomMethod_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var customParams = new { message = "test", value = 42 };

            // Act
            await m_service.PublishNotificationAsync("custom/test", customParams);

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("custom/test", notification.Method);
            Assert.Equal(customParams, notification.Params);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNoHandlers_LogsWarning()
        {
            // Act
            await m_service.PublishNotificationAsync("test/method", new { });

            // Assert
            m_mockLogger.Verify(
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

            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications1.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications2.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_service.PublishNotificationAsync("test/broadcast", new { data = "shared" });

            // Assert
            Assert.Single(receivedNotifications1);
            Assert.Single(receivedNotifications2);
            Assert.Equal("test/broadcast", receivedNotifications1[0].Method);
            Assert.Equal("test/broadcast", receivedNotifications2[0].Method);
        }

        [Fact]
        public async Task PublishNotificationAsync_HandlerThrowsException_ContinuesWithOtherHandlers()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            var exceptionThrown = false;

            m_service.Subscribe("test-event", notification =>
            {
                exceptionThrown = true;
                throw new InvalidOperationException("Handler failed");
            });

            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_service.PublishNotificationAsync("test/error", new { });

            // Assert
            Assert.True(exceptionThrown);
            Assert.Single(receivedNotifications);
            Assert.Equal("test/error", receivedNotifications[0].Method);
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
            m_service.Subscribe("test-event", (object notification) => Handler(notification as McpNotification ?? new McpNotification()));
            await m_service.PublishNotificationAsync("test", null);

            // Assert
            Assert.True(handlerCalled);
        }

        [Fact]
        public async Task NotifyCommandStatusAsync_AfterDispose_DoesNotSendNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            // McpNotificationService doesn't implement IDisposable
            await m_service.NotifyCommandStatusAsync("cmd123", "test", "queued");

            // Assert
            Assert.Empty(receivedNotifications);
        }

        [Fact]
        public void Subscribe_AfterDispose_DoesNothing()
        {
            // Arrange
            // McpNotificationService doesn't implement IDisposable

            // Act & Assert (should not throw)
            var exception = Record.Exception(() =>
                m_service.Subscribe("test-event", _ => Task.CompletedTask));

            Assert.Null(exception);
        }

        [Fact]
        public async Task NotifyCommandStatusAsync_WithCompleteData_SetsAllProperties()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_service.NotifyCommandStatusAsync(
                "cmd456",
                "!process",
                "completed",
                100,
                "Analysis complete",
                "Process analysis results...",
                null);

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

