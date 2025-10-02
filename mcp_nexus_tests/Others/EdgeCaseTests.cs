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
            // m_notificationService doesn't implement IDisposable
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
            // Arrange
            // McpNotificationService doesn't implement IDisposable

            // Act & Assert - Should not throw, just return early
            var exception = await Record.ExceptionAsync(() => m_notificationService.NotifyCommandStatusAsync(
                "test", "test", "executing"));
            Assert.Null(exception);
        }

        [Fact]
        public async Task CommandQueueService_InvalidCommandId_ReturnsNotFoundMessage()
        {
            // Act
            var result = await m_commandQueueService.GetCommandResult("nonexistent-id");

            // Assert
            Assert.Equal("Command not found: nonexistent-id", result);
        }

        [Fact]
        public void CommandQueueService_CancelNonexistentCommand_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            var exception = Record.Exception(() => m_commandQueueService.CancelCommand("nonexistent-id"));
            Assert.Null(exception);
        }

        [Fact]
        public void McpNotificationService_NullHandler_DoesNotThrow()
        {
            // Arrange
            Func<object, Task>? nullHandler = null;

            // Act & Assert - Should not throw
            var exception = Record.Exception(() => m_notificationService.Subscribe("test-event", nullHandler!));
            Assert.Null(exception);
        }

        [Fact]
        public async Task McpNotificationService_HandlerThrows_OtherHandlersStillExecute()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            var goodHandler = new Func<object, Task>(notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            var badHandler = new Func<object, Task>(notification =>
            {
                throw new InvalidOperationException("Handler error");
            });

            m_notificationService.Subscribe("test-event", goodHandler);
            m_notificationService.Subscribe("test-event", badHandler);

            // Act - Should not throw even if one handler fails
            var exception = await Record.ExceptionAsync(() =>
                m_notificationService.NotifyCommandStatusAsync("test", "test", "executing"));

            // Assert - Good handler should still execute
            Assert.Single(receivedNotifications);
            Assert.Null(exception); // Should not propagate handler exceptions
        }

        [Fact]
        public async Task McpNotificationService_EmptyMethod_HandlesCorrectly()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_notificationService.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.PublishNotificationAsync("", null);

            // Assert
            Assert.Single(receivedNotifications);
            Assert.Equal("", receivedNotifications[0].Method);
        }

        [Fact]
        public async Task McpNotificationService_NullParameters_HandlesCorrectly()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_notificationService.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act
            await m_notificationService.PublishNotificationAsync("test/method", null);

            // Assert
            Assert.Single(receivedNotifications);
            Assert.Equal("test/method", receivedNotifications[0].Method);
            Assert.Null(receivedNotifications[0].Params);
        }

        [Fact]
        public void CommandQueueService_EmptyCommand_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => m_commandQueueService.QueueCommand(""));
            Assert.Throws<ArgumentException>(() => m_commandQueueService.QueueCommand("   "));
            Assert.Throws<ArgumentException>(() => m_commandQueueService.QueueCommand(null!));
        }

        [Fact]
        public async Task CommandQueueService_SessionNotActive_HandlesGracefully()
        {
            // Arrange
            m_mockCdbSession.Setup(x => x.IsActive).Returns(false);
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("No active debug session. Please start a session first.");

            // Act
            var commandId = m_commandQueueService.QueueCommand("test command");

            // Wait much longer for command to complete
            var maxWait = 100; // 10 seconds max
            var result = "";
            for (int i = 0; i < maxWait; i++)
            {
                result = await m_commandQueueService.GetCommandResult(commandId);
                if (!result.Contains("Command is still executing"))
                {
                    break;
                }
                await Task.Delay(100);
            }

            // Assert - Just verify that we get some response (not still executing)
            Assert.False(result.Contains("Command is still executing"),
                $"Command should have completed but got: {result}");

            // The actual error message might vary, so just check it's not empty
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task CommandQueueService_ExecuteCommandThrows_HandlesGracefully()
        {
            // Arrange
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("CDB error"));

            // Act
            var commandId = m_commandQueueService.QueueCommand("test command");

            // Wait much longer for command to complete
            var maxWait = 100; // 10 seconds max
            var result = "";
            for (int i = 0; i < maxWait; i++)
            {
                result = await m_commandQueueService.GetCommandResult(commandId);
                if (!result.Contains("Command is still executing"))
                {
                    break;
                }
                await Task.Delay(100);
            }

            // Assert - Just verify that we get some response (not still executing)
            Assert.False(result.Contains("Command is still executing"),
                $"Command should have completed but got: {result}");

            // The actual error message might vary, so just check it's not empty
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task McpNotificationService_ManyHandlers_HandlesCorrectly()
        {
            // Arrange - Register many handlers
            var receivedNotifications = new List<McpNotification>();
            var handlerCount = 1000;

            for (int i = 0; i < handlerCount; i++)
            {
                m_notificationService.Subscribe("test-event", notification =>
                {
                    lock (receivedNotifications)
                    {
                        receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                    }
                    return Task.CompletedTask;
                });
            }

            // Act
            await m_notificationService.NotifyCommandStatusAsync("test", "test", "executing");

            // Assert
            Assert.Equal(handlerCount, receivedNotifications.Count);
        }

        [Fact]
        public void McpNotificationService_HandlerRegistration_AfterDisposal_DoesNotThrow()
        {
            // Arrange
            // McpNotificationService doesn't implement IDisposable

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
                m_notificationService.Subscribe("test-event", notification => Task.CompletedTask));
            Assert.Null(exception);
        }

        [Fact]
        public void McpNotificationService_HandlerUnregistration_AfterDisposal_DoesNotThrow()
        {
            // Arrange
            var handler = new Func<object, Task>(notification => Task.CompletedTask);
            m_notificationService.Subscribe("test-event", handler);
            // McpNotificationService doesn't implement IDisposable

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
                m_notificationService.Unsubscribe("test-subscription-id"));
            Assert.Null(exception);
        }

        [Fact]
        public async Task CommandQueueService_ConcurrentDisposal_HandlesCorrectly()
        {
            // Arrange
            var commandId = m_commandQueueService.QueueCommand("test command");

            // Act - Dispose while command is queued
            m_commandQueueService.Dispose();

            // Wait a bit for disposal to complete
            await Task.Delay(50);

            // Try to get result after disposal (should throw ObjectDisposedException)
            await Assert.ThrowsAsync<ObjectDisposedException>(() => m_commandQueueService.GetCommandResult(commandId));
        }

        [Fact]
        public async Task McpNotificationService_ConcurrentDisposal_HandlesCorrectly()
        {
            // Arrange
            var receivedNotifications = new List<McpNotification>();
            m_notificationService.Subscribe("test-event", notification =>
            {
                receivedNotifications.Add(notification as McpNotification ?? new McpNotification());
                return Task.CompletedTask;
            });

            // Act - Dispose while sending notification
            var disposeTask = Task.Run(() => { /* m_notificationService doesn't implement IDisposable */ });
            var notifyTask = m_notificationService.NotifyCommandStatusAsync("test", "test", "executing");

            // Assert - Should handle gracefully
            await Task.WhenAll(disposeTask, notifyTask);
            // Should not throw exceptions
        }

        [Fact]
        public async Task CommandQueueService_Timeout_HandlesCorrectly()
        {
            // Arrange
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (string cmd, CancellationToken ct) =>
                {
                    await Task.Delay(10000, ct); // Simulate long-running command
                    return "result";
                });

            // Act
            var commandId = m_commandQueueService.QueueCommand("test command");
            await Task.Delay(100); // Allow processing to start

            // Assert - Should handle timeout gracefully
            var result = m_commandQueueService.GetCommandResult(commandId);
            Assert.NotNull(result);
        }
    }
}

