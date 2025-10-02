using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Notifications;
using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus_tests.Notifications
{
    /// <summary>
    /// Tests for McpNotificationService (previously NotificationHandlerManager)
    /// </summary>
    public class NotificationHandlerManagerTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly McpNotificationService _manager;

        public NotificationHandlerManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _manager = new McpNotificationService();
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
            var subscriptionId = _manager.Subscribe("TestEvent", handler);

            // Assert
            Assert.False(string.IsNullOrEmpty(subscriptionId));
        }

        [Fact]
        public void Subscribe_WithNullHandler_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _manager.Subscribe("TestEvent", null!));
        }

        [Fact]
        public void Subscribe_WithNullEventType_ThrowsArgumentException()
        {
            // Arrange
            var handler = new Func<object, Task>(_ => Task.CompletedTask);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _manager.Subscribe(null!, handler));
        }

        [Fact]
        public void Subscribe_WithEmptyEventType_ThrowsArgumentException()
        {
            // Arrange
            var handler = new Func<object, Task>(_ => Task.CompletedTask);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _manager.Subscribe("", handler));
        }

        [Fact]
        public void Unsubscribe_WithValidSubscriptionId_ReturnsTrue()
        {
            // Arrange
            var handler = new Func<object, Task>(_ => Task.CompletedTask);
            var subscriptionId = _manager.Subscribe("TestEvent", handler);

            // Act
            var result = _manager.Unsubscribe(subscriptionId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Unsubscribe_WithInvalidSubscriptionId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid().ToString();

            // Act
            var result = _manager.Unsubscribe(invalidId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Unsubscribe_WithNullSubscriptionId_ReturnsFalse()
        {
            // Act
            var result = _manager.Unsubscribe(null!);

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

            _manager.Subscribe("TestEvent", handler);

            // Act
            await _manager.PublishNotificationAsync("TestEvent", "TestData");

            // Assert
            Assert.Single(receivedData);
            Assert.Equal("TestData", receivedData[0]);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNoHandlers_DoesNotThrow()
        {
            // Act & Assert
            await _manager.PublishNotificationAsync("NonExistentEvent", "TestData");
            // Should not throw
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNullEventType_DoesNotThrow()
        {
            // Act & Assert
            await _manager.PublishNotificationAsync(null!, "TestData");
            // Should not throw
        }

        [Fact]
        public async Task PublishNotificationAsync_WithEmptyEventType_DoesNotThrow()
        {
            // Act & Assert
            await _manager.PublishNotificationAsync("", "TestData");
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

            _manager.Subscribe("TestEvent", handler1);
            _manager.Subscribe("TestEvent", handler2);

            // Act
            await _manager.PublishNotificationAsync("TestEvent", "TestData");

            // Assert
            Assert.Single(receivedData1);
            Assert.Single(receivedData2);
            Assert.Equal("TestData", receivedData1[0]);
            Assert.Equal("TestData", receivedData2[0]);
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

            _manager.Subscribe("TestEvent", handler1);
            _manager.Subscribe("TestEvent", handler2);

            // Act
            await _manager.PublishNotificationAsync("TestEvent", "TestData");

            // Assert
            Assert.Single(receivedData);
            Assert.Equal("TestData", receivedData[0]);
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

            _manager.Subscribe("CommandStatus", handler);

            // Act
            await _manager.NotifyCommandStatusAsync("session1", "cmd1", "executing");

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

            _manager.Subscribe("CommandHeartbeat", handler);

            // Act
            await _manager.NotifyCommandHeartbeatAsync("session1", "cmd1");

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

            _manager.Subscribe("SessionEvent", handler);

            // Act
            await _manager.NotifySessionEventAsync("session1", "event1", "data1");

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

            _manager.Subscribe("SessionRecovery", handler);

            // Act
            await _manager.NotifySessionRecoveryAsync("session1", "recovery1");

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

            _manager.Subscribe("ServerHealth", handler);

            // Act
            await _manager.NotifyServerHealthAsync("healthy");

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

            _manager.Subscribe("ToolsListChanged", handler);

            // Act
            await _manager.NotifyToolsListChangedAsync();

            // Assert
            Assert.Single(receivedNotifications);
        }
    }
}