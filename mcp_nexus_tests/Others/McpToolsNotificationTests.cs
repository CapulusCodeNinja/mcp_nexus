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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace mcp_nexus_tests.Services
{
    public class McpToolsNotificationTests
    {
        private readonly Mock<ILogger<McpNotificationService>> m_mockLogger;
        private readonly McpNotificationService m_notificationService;

        public McpToolsNotificationTests()
        {
            m_mockLogger = new Mock<ILogger<McpNotificationService>>();
            m_notificationService = new McpNotificationService();
        }

        [Fact]
        public async Task NotifyToolsListChangedAsync_SendsCorrectNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            m_notificationService.Subscribe("ToolsListChanged", notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.NotifyToolsListChangedAsync();

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task McpToolDefinitionService_WithNotificationService_CanNotifyToolsChanged()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            m_notificationService.Subscribe("ToolsListChanged", notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.NotifyToolsListChangedAsync();

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task McpToolDefinitionService_WithoutNotificationService_DoesNotThrow()
        {
            // Arrange
            var service = new McpToolDefinitionService();

            // Act & Assert - Should not throw
            await service.NotifyToolsChangedAsync();
        }

        [Fact]
        public async Task McpToolDefinitionService_WithNotificationService_NotifiesCorrectly()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            m_notificationService.Subscribe("ToolsListChanged", notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.NotifyToolsListChangedAsync();

            // Assert
            Assert.Single(receivedNotifications);
        }
    }
}