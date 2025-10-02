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

namespace mcp_nexus_tests.Services
{
    public class McpNotificationHeartbeatTests : IDisposable
    {
        private readonly McpNotificationService m_service;
        private readonly Mock<ILogger<McpNotificationService>> m_mockLogger;

        public McpNotificationHeartbeatTests()
        {
            m_mockLogger = new Mock<ILogger<McpNotificationService>>();
            m_service = new McpNotificationService();
        }

        public void Dispose()
        {
            // McpNotificationService doesn't implement IDisposable
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var elapsed = TimeSpan.FromMinutes(2.5);

            // Act
            await m_service.NotifyCommandHeartbeatAsync("cmd123", "!analyze -v", elapsed, "Analyzing crash dump...");

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("notifications/commandHeartbeat", notification.Method);
            Assert.NotNull(notification.Params);

            var heartbeatParams = notification.Params as McpCommandHeartbeatNotification;
            Assert.NotNull(heartbeatParams);
            Assert.Equal("cmd123", heartbeatParams!.CommandId);
            Assert.Equal("!analyze -v", heartbeatParams!.Command);
            Assert.Equal(150.0, heartbeatParams!.ElapsedSeconds, 1); // 2.5 minutes = 150 seconds
            Assert.Equal("2.5m", heartbeatParams!.ElapsedDisplay);
            Assert.Equal("Analyzing crash dump...", heartbeatParams!.Details);
            Assert.True(heartbeatParams!.Timestamp <= DateTime.UtcNow);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_WithShortElapsed_DisplaysSeconds()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var elapsed = TimeSpan.FromSeconds(45);

            // Act
            await m_service.NotifyCommandHeartbeatAsync("cmd456", "lm", elapsed);

            // Assert
            var heartbeatParams = receivedNotifications[0].Params as McpCommandHeartbeatNotification;
            Assert.NotNull(heartbeatParams);
            Assert.Equal(45.0, heartbeatParams!.ElapsedSeconds);
            Assert.Equal("45s", heartbeatParams!.ElapsedDisplay);
            Assert.Null(heartbeatParams!.Details);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_WithLongElapsed_DisplaysMinutes()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var elapsed = TimeSpan.FromMinutes(10.3);

            // Act
            await m_service.NotifyCommandHeartbeatAsync("cmd789", "!process 0 0", elapsed, "Processing system data");

            // Assert
            var heartbeatParams = receivedNotifications[0].Params as McpCommandHeartbeatNotification;
            Assert.NotNull(heartbeatParams);
            Assert.Equal(618.0, heartbeatParams!.ElapsedSeconds, 1); // 10.3 minutes = 618 seconds
            Assert.Equal("10.3m", heartbeatParams!.ElapsedDisplay);
            Assert.Equal("Processing system data", heartbeatParams!.Details);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_AfterDispose_DoesNotSendNotification()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            m_service.Dispose();
            await m_service.NotifyCommandHeartbeatAsync("cmd123", "test", TimeSpan.FromMinutes(1));

            // Assert
            Assert.Empty(receivedNotifications);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_WithZeroElapsed_HandlesCorrectly()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var elapsed = TimeSpan.Zero;

            // Act
            await m_service.NotifyCommandHeartbeatAsync("cmd000", "version", elapsed);

            // Assert
            var heartbeatParams = receivedNotifications[0].Params as McpCommandHeartbeatNotification;
            Assert.NotNull(heartbeatParams);
            Assert.Equal(0.0, heartbeatParams!.ElapsedSeconds);
            Assert.Equal("0s", heartbeatParams!.ElapsedDisplay);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_WithExactOneMinute_DisplaysMinutes()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var elapsed = TimeSpan.FromMinutes(1);

            // Act
            await m_service.NotifyCommandHeartbeatAsync("cmd001", "!heap", elapsed);

            // Assert
            var heartbeatParams = receivedNotifications[0].Params as McpCommandHeartbeatNotification;
            Assert.NotNull(heartbeatParams);
            Assert.Equal(60.0, heartbeatParams!.ElapsedSeconds);
            Assert.Equal("1.0m", heartbeatParams!.ElapsedDisplay);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_WithNullDetails_AllowsNullDetails()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_service.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_service.NotifyCommandHeartbeatAsync("cmd999", "test", TimeSpan.FromMinutes(1), null!);

            // Assert
            var heartbeatParams = receivedNotifications[0].Params as McpCommandHeartbeatNotification;
            Assert.NotNull(heartbeatParams);
            Assert.Null(heartbeatParams!.Details);
        }
    }
}

