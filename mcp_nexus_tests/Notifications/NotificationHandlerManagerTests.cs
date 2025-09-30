using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Notifications;
using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus_tests.Notifications
{
    /// <summary>
    /// Tests for NotificationHandlerManager
    /// </summary>
    public class NotificationHandlerManagerTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly NotificationHandlerManager _manager;

        public NotificationHandlerManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _manager = new NotificationHandlerManager(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NotificationHandlerManager(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Act
            var manager = new NotificationHandlerManager(_mockLogger.Object);

            // Assert
            Assert.NotNull(manager);
            Assert.Equal(0, manager.GetHandlerCount());
        }

        [Fact]
        public void RegisterHandler_WithValidHandler_ReturnsGuid()
        {
            // Arrange
            var handler = new Func<McpNotification, Task>(_ => Task.CompletedTask);

            // Act
            var handlerId = _manager.RegisterHandler(handler);

            // Assert
            Assert.NotEqual(Guid.Empty, handlerId);
            Assert.Equal(1, _manager.GetHandlerCount());
        }

        [Fact]
        public void RegisterHandler_WithNullHandler_ReturnsEmptyGuid()
        {
            // Act
            var handlerId = _manager.RegisterHandler(null!);

            // Assert
            Assert.Equal(Guid.Empty, handlerId);
            Assert.Equal(0, _manager.GetHandlerCount());
        }

        [Fact]
        public void RegisterHandler_MultipleHandlers_ReturnsUniqueIds()
        {
            // Arrange
            var handler1 = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            var handler2 = new Func<McpNotification, Task>(_ => Task.CompletedTask);

            // Act
            var id1 = _manager.RegisterHandler(handler1);
            var id2 = _manager.RegisterHandler(handler2);

            // Assert
            Assert.NotEqual(id1, id2);
            Assert.Equal(2, _manager.GetHandlerCount());
        }

        [Fact]
        public void UnregisterHandler_WithValidId_RemovesHandler()
        {
            // Arrange
            var handler = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            var handlerId = _manager.RegisterHandler(handler);

            // Act
            _manager.UnregisterHandler(handlerId);

            // Assert
            Assert.Equal(0, _manager.GetHandlerCount());
        }

        [Fact]
        public void UnregisterHandler_WithInvalidId_DoesNothing()
        {
            // Arrange
            var handler = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            _manager.RegisterHandler(handler);

            // Act
            _manager.UnregisterHandler(Guid.NewGuid());

            // Assert
            Assert.Equal(1, _manager.GetHandlerCount());
        }

        [Fact]
        public void UnregisterHandler_WithHandlerReference_RemovesHandler()
        {
            // Arrange
            var handler = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            _manager.RegisterHandler(handler);

            // Act
            _manager.UnregisterHandler(handler);

            // Assert
            Assert.Equal(0, _manager.GetHandlerCount());
        }

        [Fact]
        public void UnregisterHandler_WithNullHandler_DoesNothing()
        {
            // Arrange
            var handler = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            _manager.RegisterHandler(handler);

            // Act
            _manager.UnregisterHandler(null!);

            // Assert
            Assert.Equal(1, _manager.GetHandlerCount());
        }

        [Fact]
        public async Task SendNotificationAsync_WithValidNotification_CallsAllHandlers()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler1 = new Func<McpNotification, Task>(_ => { handler1Called = true; return Task.CompletedTask; });
            var handler2 = new Func<McpNotification, Task>(_ => { handler2Called = true; return Task.CompletedTask; });

            _manager.RegisterHandler(handler1);
            _manager.RegisterHandler(handler2);

            var notification = new McpNotification
            {
                Method = "test/method",
                Params = JsonSerializer.SerializeToElement(new { test = "data" })
            };

            // Act
            await _manager.SendNotificationAsync(notification);

            // Assert
            Assert.True(handler1Called);
            Assert.True(handler2Called);
        }

        [Fact]
        public async Task SendNotificationAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _manager.SendNotificationAsync(null!));
        }

        [Fact]
        public async Task SendNotificationAsync_WithNoHandlers_DoesNotThrow()
        {
            // Arrange
            var notification = new McpNotification
            {
                Method = "test/method",
                Params = JsonSerializer.SerializeToElement(new { test = "data" })
            };

            // Act & Assert
            await _manager.SendNotificationAsync(notification);
            // Should not throw
        }

        [Fact]
        public async Task SendNotificationAsync_WithHandlerThrowingException_ContinuesWithOtherHandlers()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler1 = new Func<McpNotification, Task>(_ => { handler1Called = true; throw new Exception("Handler 1 error"); });
            var handler2 = new Func<McpNotification, Task>(_ => { handler2Called = true; return Task.CompletedTask; });

            _manager.RegisterHandler(handler1);
            _manager.RegisterHandler(handler2);

            var notification = new McpNotification
            {
                Method = "test/method",
                Params = JsonSerializer.SerializeToElement(new { test = "data" })
            };

            // Act
            await _manager.SendNotificationAsync(notification);

            // Assert
            Assert.True(handler1Called);
            Assert.True(handler2Called);
        }

        [Fact]
        public void GetRegisteredHandlerIds_ReturnsAllHandlerIds()
        {
            // Arrange
            var handler1 = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            var handler2 = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            var id1 = _manager.RegisterHandler(handler1);
            var id2 = _manager.RegisterHandler(handler2);

            // Act
            var handlerIds = _manager.GetRegisteredHandlerIds();

            // Assert
            Assert.Equal(2, handlerIds.Count);
            Assert.Contains(id1, handlerIds);
            Assert.Contains(id2, handlerIds);
        }

        [Fact]
        public void GetHandlerCount_Initially_ReturnsZero()
        {
            // Act
            var count = _manager.GetHandlerCount();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void ClearAllHandlers_RemovesAllHandlers()
        {
            // Arrange
            var handler1 = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            var handler2 = new Func<McpNotification, Task>(_ => Task.CompletedTask);
            _manager.RegisterHandler(handler1);
            _manager.RegisterHandler(handler2);

            // Act
            _manager.ClearAllHandlers();

            // Assert
            Assert.Equal(0, _manager.GetHandlerCount());
        }
    }
}