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
            var receivedNotifications = new List<McpNotification>();
            m_notificationService.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.NotifyToolsListChangedAsync();

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("notifications/tools/list_changed", notification.Method);
            Assert.Null(notification.Params); // Standard MCP tools notification has no parameters
            Assert.Equal("2.0", notification.JsonRpc);
        }

        [Fact]
        public async Task McpToolDefinitionService_WithNotificationService_CanNotifyToolsChanged()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_notificationService.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var toolDefinitionService = new McpToolDefinitionService(m_notificationService);

            // Act
            await toolDefinitionService.NotifyToolsChanged();

            // Assert
            Assert.Single(receivedNotifications);
            var notification = receivedNotifications[0];
            Assert.Equal("notifications/tools/list_changed", notification.Method);
        }

        [Fact]
        public async Task McpToolDefinitionService_WithoutNotificationService_DoesNotThrow()
        {
            // Arrange
            var toolDefinitionService = new McpToolDefinitionService();

            // Act & Assert - should not throw
            await toolDefinitionService.NotifyToolsChanged();
        }

        [Fact]
        public void McpCapabilities_IncludesToolsNotification()
        {
            // Arrange & Act
            var capabilities = new McpCapabilities();

            // Assert
            Assert.NotNull(capabilities.Notifications);
            var notificationsObj = capabilities.Notifications as dynamic;
            Assert.NotNull(notificationsObj);

            // Check that tools notification is declared
            var notificationsJson = System.Text.Json.JsonSerializer.Serialize(capabilities.Notifications);
            Assert.Contains("tools", notificationsJson);
            Assert.Contains("listChanged", notificationsJson);
        }
    }
}

