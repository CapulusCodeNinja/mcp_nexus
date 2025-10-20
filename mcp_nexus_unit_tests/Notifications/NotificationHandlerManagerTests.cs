using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Notifications;
using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus_unit_tests.Notifications
{
    /// <summary>
    /// Tests for McpNotificationService (previously NotificationHandlerManager)
    /// </summary>
    public class NotificationHandlerManagerTests
    {
        private readonly Mock<ILogger> m_MockLogger;
        private readonly McpNotificationService m_Manager;

        public NotificationHandlerManagerTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_Manager = new McpNotificationService();
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Act
            var manager = new McpNotificationService();

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void Subscribe_WithValidHandler_ReturnsSubscriptionId()
        {
            // Arrange
            var handler = new Func<object, Task>(_ => Task.CompletedTask);

            // Act
            var subscriptionId = m_Manager.Subscribe("TestEvent", handler);

            // Assert
            Assert.False(string.IsNullOrEmpty(subscriptionId));
        }

        [Fact]
        public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => m_Manager.Subscribe("TestEvent", null!));
        }

        [Fact]
        public void Subscribe_WithNullEventType_ThrowsArgumentException()
        {
            // Arrange
            var handler = new Func<object, Task>(_ => Task.CompletedTask);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => m_Manager.Subscribe(null!, handler));
        }

        [Fact]
        public void Subscribe_WithEmptyEventType_ThrowsArgumentException()
        {
            // Arrange
            var handler = new Func<object, Task>(_ => Task.CompletedTask);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => m_Manager.Subscribe("", handler));
        }

        [Fact]
        public void Unsubscribe_WithValidSubscriptionId_ReturnsTrue()
        {
            // Arrange
            var handler = new Func<object, Task>(_ => Task.CompletedTask);
            var subscriptionId = m_Manager.Subscribe("TestEvent", handler);

            // Act
            var result = m_Manager.Unsubscribe(subscriptionId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Unsubscribe_WithInvalidSubscriptionId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = m_Manager.Unsubscribe(invalidId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Unsubscribe_WithNullSubscriptionId_ReturnsFalse()
        {
            // Act
            var result = m_Manager.Unsubscribe(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithValidData_InvokesHandlers()
        {
            // Arrange
            var receivedData = new List<object>();
            var handler = new Func<object, Task>(data =>
            {
                receivedData.Add(data);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("TestEvent", handler);

            // Act
            await m_Manager.PublishNotificationAsync("TestEvent", "TestData");

            // Assert
            Assert.Single(receivedData);
            var notification = Assert.IsType<McpNotification>(receivedData[0]);
            Assert.Equal("notifications/testEvent", notification.Method);
            Assert.Equal("TestData", notification.Params);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNoHandlers_DoesNotThrow()
        {
            // Act & Assert
            await m_Manager.PublishNotificationAsync("NonExistentEvent", "TestData");
            // Should not throw
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNullEventType_DoesNotThrow()
        {
            // Act & Assert
            await m_Manager.PublishNotificationAsync(null!, "TestData");
            // Should not throw
        }

        [Fact]
        public async Task PublishNotificationAsync_WithEmptyEventType_DoesNotThrow()
        {
            // Act & Assert
            await m_Manager.PublishNotificationAsync("", "TestData");
            // Should not throw
        }

        [Fact]
        public async Task PublishNotificationAsync_WithMultipleHandlers_InvokesAllHandlers()
        {
            // Arrange
            var receivedData1 = new List<object>();
            var receivedData2 = new List<object>();

            var handler1 = new Func<object, Task>(data =>
            {
                receivedData1.Add(data);
                return Task.CompletedTask;
            });

            var handler2 = new Func<object, Task>(data =>
            {
                receivedData2.Add(data);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("TestEvent", handler1);
            m_Manager.Subscribe("TestEvent", handler2);

            // Act
            await m_Manager.PublishNotificationAsync("TestEvent", "TestData");

            // Assert
            Assert.Single(receivedData1);
            Assert.Single(receivedData2);
            var notification1 = Assert.IsType<McpNotification>(receivedData1[0]);
            var notification2 = Assert.IsType<McpNotification>(receivedData2[0]);
            Assert.Equal("notifications/testEvent", notification1.Method);
            Assert.Equal("TestData", notification1.Params);
            Assert.Equal("notifications/testEvent", notification2.Method);
            Assert.Equal("TestData", notification2.Params);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithHandlerThrowingException_ContinuesWithOtherHandlers()
        {
            // Arrange
            var receivedData = new List<object>();
            var handler1 = new Func<object, Task>(_ => throw new Exception("Handler failed"));
            var handler2 = new Func<object, Task>(data =>
            {
                receivedData.Add(data);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("TestEvent", handler1);
            m_Manager.Subscribe("TestEvent", handler2);

            // Act
            await m_Manager.PublishNotificationAsync("TestEvent", "TestData");

            // Assert
            Assert.Single(receivedData);
            var notification = Assert.IsType<McpNotification>(receivedData[0]);
            Assert.Equal("notifications/testEvent", notification.Method);
            Assert.Equal("TestData", notification.Params);
        }

        [Fact]
        public async Task NotifyCommandStatusAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var handler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("CommandStatus", handler);

            // Act
            await m_Manager.NotifyCommandStatusAsync("session1", "cmd1", "executing");

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task NotifyCommandHeartbeatAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var handler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("CommandHeartbeat", handler);

            // Act
            await m_Manager.NotifyCommandHeartbeatAsync("session1", "cmd1");

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task NotifySessionEventAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var handler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("SessionEvent", handler);

            // Act
            await m_Manager.NotifySessionEventAsync("session1", "event1", "data1");

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task NotifySessionRecoveryAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var handler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("SessionRecovery", handler);

            // Act
            await m_Manager.NotifySessionRecoveryAsync("session1", "recovery1");

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task NotifyServerHealthAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var handler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("ServerHealth", handler);

            // Act
            await m_Manager.NotifyServerHealthAsync("healthy");

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task NotifyToolsListChangedAsync_WithValidParameters_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var handler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            m_Manager.Subscribe("ToolsListChanged", handler);

            // Act
            await m_Manager.NotifyToolsListChangedAsync();

            // Assert
            Assert.Single(receivedNotifications);
        }
    }
}