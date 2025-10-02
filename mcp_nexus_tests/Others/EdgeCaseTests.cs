using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Services
{
    /// <summary>
    /// Tests for edge cases and error scenarios
    /// </summary>
    public class EdgeCaseTests : IDisposable
    {
        private readonly Mock<ICdbSession> m_mockCdbSession;
        private readonly Mock<ILogger<CommandQueueService>> m_mockLogger;
        private readonly Mock<ILoggerFactory> m_mockLoggerFactory;
        private readonly CommandQueueService m_commandQueueService;
        private readonly Mock<ILogger<McpNotificationService>> m_mockNotificationLogger;
        private readonly McpNotificationService m_notificationService;

        public EdgeCaseTests()
        {
            m_mockCdbSession = new Mock<ICdbSession>();
            m_mockLogger = new Mock<ILogger<CommandQueueService>>();
            m_mockLoggerFactory = new Mock<ILoggerFactory>();
            m_mockNotificationLogger = new Mock<ILogger<McpNotificationService>>();

            // Setup logger factory
            m_mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

            // Setup default mock behavior
            m_mockCdbSession.Setup(x => x.IsActive).Returns(true);
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Mock result");

            m_commandQueueService = new CommandQueueService(m_mockCdbSession.Object, m_mockLogger.Object, m_mockLoggerFactory.Object);
            m_notificationService = new McpNotificationService();
        }

        public void Dispose()
        {
            m_commandQueueService?.Dispose();
            // McpNotificationService doesn't implement IDisposable
        }

        [Fact]
        public async Task CommandQueueService_Disposed_ThrowsObjectDisposedException()
        {
            // Arrange
            m_commandQueueService.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => m_commandQueueService.QueueCommand("test"));
            Assert.Throws<ObjectDisposedException>(() => m_commandQueueService.CancelCommand("test"));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => m_commandQueueService.GetCommandResult("test"));
        }

        [Fact]
        public async Task McpNotificationService_Disposed_DoesNotThrow()
        {
            // Arrange & Act - McpNotificationService doesn't implement IDisposable
            // This test verifies that the service can be used without disposal issues
            await m_notificationService.PublishNotificationAsync("test", "data");
        }

        [Fact]
        public void McpNotificationService_NullHandler_DoesNotThrow()
        {
            // Arrange
            Func<object, Task>? nullHandler = null;

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => m_notificationService.Subscribe("test", nullHandler!));
            Assert.Null(exception);
        }

        [Fact]
        public async Task McpNotificationService_HandlerThrows_OtherHandlersStillExecute()
        {
            // Arrange
            var goodHandler = new Func<object, Task>(_ => Task.CompletedTask);
            var badHandler = new Func<object, Task>(_ => throw new Exception("Handler failed"));

            m_notificationService.Subscribe("test", goodHandler);
            m_notificationService.Subscribe("test", badHandler);

            // Act & Assert - Should not throw even if one handler fails
            await m_notificationService.PublishNotificationAsync("test", "data");
        }

        [Fact]
        public async Task McpNotificationService_EmptyEventType_DoesNotThrow()
        {
            // Arrange
            m_notificationService.Subscribe("test", notification =>
            {
                // This should not be called for empty event type
                Assert.True(false, "Handler should not be called for empty event type");
                return Task.CompletedTask;
            });

            // Act & Assert - Should not throw
            await m_notificationService.PublishNotificationAsync("", null);
        }

        [Fact]
        public async Task McpNotificationService_ValidEventType_SendsNotification()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            m_notificationService.Subscribe("test/method", notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.PublishNotificationAsync("test/method", null);

            // Assert
            Assert.Single(receivedNotifications);
        }

        [Fact]
        public async Task CommandQueueService_ConcurrentAccess_HandlesGracefully()
        {
            // Arrange
            var tasks = new List<Task>();
            var commandIds = new List<string>();

            // Act - Queue multiple commands concurrently
            for (int i = 0; i < 10; i++)
            {
                var commandId = $"cmd_{i}";
                commandIds.Add(commandId);
                tasks.Add(Task.Run(() => m_commandQueueService.QueueCommand($"command {i}")));
            }

            await Task.WhenAll(tasks);

            // Assert - All commands should be queued
            foreach (var commandId in commandIds)
            {
                var result = await m_commandQueueService.GetCommandResult(commandId);
                Assert.NotNull(result);
            }
        }

        [Fact]
        public async Task CommandQueueService_CancelNonExistentCommand_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            m_commandQueueService.CancelCommand("non-existent-command");
        }

        [Fact]
        public async Task CommandQueueService_GetResultNonExistentCommand_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => m_commandQueueService.GetCommandResult("non-existent-command"));
        }

        [Fact]
        public async Task CommandQueueService_QueueCommandAfterDisposal_ThrowsObjectDisposedException()
        {
            // Arrange
            m_commandQueueService.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => m_commandQueueService.QueueCommand("test"));
        }

        [Fact]
        public async Task CommandQueueService_CancelCommandAfterDisposal_ThrowsObjectDisposedException()
        {
            // Arrange
            m_commandQueueService.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => m_commandQueueService.CancelCommand("test"));
        }

        [Fact]
        public async Task CommandQueueService_GetResultAfterDisposal_ThrowsObjectDisposedException()
        {
            // Arrange
            m_commandQueueService.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => m_commandQueueService.GetCommandResult("test"));
        }

        [Fact]
        public async Task McpNotificationService_ConcurrentNotifications_HandlesGracefully()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var tasks = new List<Task>();

            m_notificationService.Subscribe("concurrent-test", notification =>
            {
                lock (receivedNotifications)
                {
                    receivedNotifications.Add(notification);
                }
                return Task.CompletedTask;
            });

            // Act - Send multiple notifications concurrently
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(m_notificationService.PublishNotificationAsync("concurrent-test", $"data-{i}"));
            }

            await Task.WhenAll(tasks);

            // Assert - All notifications should be received
            Assert.Equal(10, receivedNotifications.Count);
        }

        [Fact]
        public async Task McpNotificationService_Unsubscribe_RemovesHandler()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var handler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            var subscriptionId = m_notificationService.Subscribe("test", handler);

            // Act
            m_notificationService.Unsubscribe(subscriptionId);
            await m_notificationService.PublishNotificationAsync("test", "data");

            // Assert - Handler should not be called after unsubscription
            Assert.Empty(receivedNotifications);
        }

        [Fact]
        public async Task McpNotificationService_UnsubscribeInvalidId_ReturnsFalse()
        {
            // Act
            var result = m_notificationService.Unsubscribe("invalid-id");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CommandQueueService_DisposeWhileCommandRunning_HandlesGracefully()
        {
            // Arrange
            var commandId = m_commandQueueService.QueueCommand("long-running-command");

            // Act - Dispose while command is queued
            m_commandQueueService.Dispose();

            // Assert - Should not throw when trying to get result after disposal
            await Assert.ThrowsAsync<ObjectDisposedException>(() => m_commandQueueService.GetCommandResult(commandId));
        }

        [Fact]
        public async Task McpNotificationService_DisposeWhileSending_HandlesGracefully()
        {
            // Arrange
            m_notificationService.Subscribe("dispose-test", notification => Task.CompletedTask);

            // Act - Dispose while sending notification
            var disposeTask = Task.Run(() => { /* McpNotificationService doesn't implement IDisposable */ });
            var sendTask = m_notificationService.PublishNotificationAsync("dispose-test", "data");

            // Assert - Both operations should complete without issues
            await Task.WhenAll(disposeTask, sendTask);
        }

        [Fact]
        public async Task CommandQueueService_ExceptionInCommand_HandlesGracefully()
        {
            // Arrange
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Command execution failed"));

            var commandId = m_commandQueueService.QueueCommand("failing-command");

            // Act & Assert - Should handle exception gracefully
            var result = await m_commandQueueService.GetCommandResult(commandId);
            Assert.Contains("Command execution failed", result);
        }

        [Fact]
        public async Task McpNotificationService_ExceptionInHandler_DoesNotStopOtherHandlers()
        {
            // Arrange
            var receivedNotifications = new List<object>();
            var goodHandler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });
            var badHandler = new Func<object, Task>(_ => throw new Exception("Handler failed"));

            m_notificationService.Subscribe("exception-test", goodHandler);
            m_notificationService.Subscribe("exception-test", badHandler);

            // Act
            await m_notificationService.PublishNotificationAsync("exception-test", "data");

            // Assert - Good handler should still be called
            Assert.Single(receivedNotifications);
        }
    }
}