using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Notifications;
using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus_tests.Notifications
{
    /// <summary>
    /// Tests for McpNotificationService
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
        public void Constructor_InitializesCorrectly()
        {
            // Act
            var manager = new McpNotificationService();

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public async Task Subscribe_WithValidHandler_ReturnsSubscriptionId()
        {
            // Arrange
            var handler = new Func<object, Task>(data => Task.CompletedTask);

            // Act
            var subscriptionId = _manager.Subscribe("test-event", handler);

            // Assert
            Assert.NotNull(subscriptionId);
            Assert.NotEmpty(subscriptionId);
        }

        [Fact]
        public async Task Subscribe_WithNullHandler_ReturnsSubscriptionId()
        {
            // Act
            var subscriptionId = _manager.Subscribe("test-event", null!);

            // Assert
            Assert.NotNull(subscriptionId);
            Assert.NotEmpty(subscriptionId);
        }

        [Fact]
        public async Task Subscribe_MultipleHandlers_ReturnsUniqueIds()
        {
            // Arrange
            var handler1 = new Func<object, Task>(data => Task.CompletedTask);
            var handler2 = new Func<object, Task>(data => Task.CompletedTask);

            // Act
            var id1 = _manager.Subscribe("test-event", handler1);
            var id2 = _manager.Subscribe("test-event", handler2);

            // Assert
            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public async Task Unsubscribe_WithValidId_ReturnsTrue()
        {
            // Arrange
            var handler = new Func<object, Task>(data => Task.CompletedTask);
            var subscriptionId = _manager.Subscribe("test-event", handler);

            // Act
            var result = _manager.Unsubscribe(subscriptionId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Unsubscribe_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var handler = new Func<object, Task>(data => Task.CompletedTask);
            _manager.Subscribe("test-event", handler);

            // Act
            var result = _manager.Unsubscribe("invalid-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task Unsubscribe_WithNullId_ReturnsFalse()
        {
            // Arrange
            var handler = new Func<object, Task>(data => Task.CompletedTask);
            _manager.Subscribe("test-event", handler);

            // Act
            var result = _manager.Unsubscribe(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithValidData_CallsHandlers()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler1 = new Func<object, Task>(data => { handler1Called = true; return Task.CompletedTask; });
            var handler2 = new Func<object, Task>(data => { handler2Called = true; return Task.CompletedTask; });

            _manager.Subscribe("test-event", handler1);
            _manager.Subscribe("test-event", handler2);

            // Act
            await _manager.PublishNotificationAsync("test-event", new { Test = "data" });

            // Assert
            Assert.True(handler1Called);
            Assert.True(handler2Called);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNoHandlers_DoesNotThrow()
        {
            // Act & Assert
            await _manager.PublishNotificationAsync("non-existent-event", new { Test = "data" });
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNullEventType_DoesNotThrow()
        {
            // Act & Assert
            await _manager.PublishNotificationAsync(null!, new { Test = "data" });
        }

        [Fact]
        public async Task PublishNotificationAsync_WithEmptyEventType_DoesNotThrow()
        {
            // Act & Assert
            await _manager.PublishNotificationAsync(string.Empty, new { Test = "data" });
        }

        [Fact]
        public async Task PublishNotificationAsync_WithNullData_DoesNotThrow()
        {
            // Arrange
            var handlerCalled = false;
            var handler = new Func<object, Task>(data => { handlerCalled = true; return Task.CompletedTask; });
            _manager.Subscribe("test-event", handler);

            // Act
            await _manager.PublishNotificationAsync("test-event", null!);

            // Assert
            Assert.True(handlerCalled);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithHandlerException_ContinuesWithOtherHandlers()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler1 = new Func<object, Task>(data => { throw new Exception("Handler 1 error"); });
            var handler2 = new Func<object, Task>(data => { handler2Called = true; return Task.CompletedTask; });

            _manager.Subscribe("test-event", handler1);
            _manager.Subscribe("test-event", handler2);

            // Act
            await _manager.PublishNotificationAsync("test-event", new { Test = "data" });

            // Assert
            Assert.True(handler2Called);
        }

        [Fact]
        public async Task PublishNotificationAsync_WithDifferentEventTypes_OnlyCallsMatchingHandlers()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler1 = new Func<object, Task>(data => { handler1Called = true; return Task.CompletedTask; });
            var handler2 = new Func<object, Task>(data => { handler2Called = true; return Task.CompletedTask; });

            _manager.Subscribe("event1", handler1);
            _manager.Subscribe("event2", handler2);

            // Act
            await _manager.PublishNotificationAsync("event1", new { Test = "data" });

            // Assert
            Assert.True(handler1Called);
            Assert.False(handler2Called);
        }
    }
}