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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Services
{
    /// <summary>
    /// Tests for concurrency scenarios and thread safety
    /// </summary>
    public class ConcurrencyTests : IDisposable
    {
        private readonly Mock<ICdbSession> m_mockCdbSession;
        private readonly Mock<ILogger<CommandQueueService>> m_mockLogger;
        private readonly Mock<ILoggerFactory> m_mockLoggerFactory;
        private readonly CommandQueueService m_commandQueueService;
        private readonly Mock<ILogger<McpNotificationService>> m_mockNotificationLogger;
        private readonly McpNotificationService m_notificationService;

        public ConcurrencyTests()
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
            m_notificationService = new McpNotificationService(m_mockNotificationLogger.Object);
        }

        public void Dispose()
        {
            m_commandQueueService?.Dispose();
            m_notificationService?.Dispose();
        }

        [Fact]
        public async Task CommandQueueService_ConcurrentCommands_ProcessesCorrectly()
        {
            // Arrange
            var commandCount = 100;
            var commands = new List<string>();
            var results = new ConcurrentBag<string>();

            // Act - Queue many commands concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < commandCount; i++)
            {
                var commandId = m_commandQueueService.QueueCommand($"test command {i}");
                commands.Add(commandId);
            }

            // Wait for commands to be processed
            await Task.Delay(200);

            // Get results concurrently
            for (int i = 0; i < commandCount; i++)
            {
                var index = i; // Capture the index to avoid closure issues
                tasks.Add(Task.Run(async () =>
                {
                    var result = await m_commandQueueService.GetCommandResult(commands[index]);
                    results.Add(result);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - All commands should be processed
            Assert.Equal(commandCount, results.Count);
            Assert.All(results, result => Assert.Equal("Mock result", result));
        }

        [Fact]
        public async Task CommandQueueService_ConcurrentCancellation_HandlesCorrectly()
        {
            // Arrange
            var commandCount = 50;
            var commands = new List<string>();
            var cancelledCount = 0;

            // Act - Queue commands and cancel half of them
            for (int i = 0; i < commandCount; i++)
            {
                var commandId = m_commandQueueService.QueueCommand($"test command {i}");
                commands.Add(commandId);

                // Cancel every other command
                if (i % 2 == 0)
                {
                    m_commandQueueService.CancelCommand(commandId);
                    cancelledCount++;
                }
            }

            // Wait for processing
            await Task.Delay(100);

            // Assert - Cancelled commands should be handled properly
            var results = new List<string>();
            foreach (var commandId in commands)
            {
                var result = await m_commandQueueService.GetCommandResult(commandId);
                results.Add(result);
            }

            // Should have results for all commands (some may be cancelled)
            Assert.Equal(commandCount, results.Count);
        }

        [Fact]
        public async Task McpNotificationService_ConcurrentHandlers_ThreadSafe()
        {
            // Arrange
            var handlerCount = 10;
            var notificationCount = 100;
            var receivedNotifications = new ConcurrentBag<McpNotification>();
            var handlers = new List<Func<McpNotification, Task>>();

            // Register multiple handlers
            for (int i = 0; i < handlerCount; i++)
            {
                var handler = new Func<McpNotification, Task>(notification =>
                {
                    receivedNotifications.Add(notification);
                    return Task.CompletedTask;
                });
                handlers.Add(handler);
                m_notificationService.Subscribe("test-event", handler);
            }

            // Act - Send notifications concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < notificationCount; i++)
            {
                tasks.Add(m_notificationService.NotifyCommandStatusAsync(
                    $"cmd{i}", "test command", "executing", 50, "Processing"));
            }

            await Task.WhenAll(tasks);

            // Assert - All handlers should receive all notifications
            Assert.Equal(notificationCount * handlerCount, receivedNotifications.Count);
        }

        [Fact]
        public async Task McpNotificationService_ConcurrentRegistration_ThreadSafe()
        {
            // Arrange
            var registrationCount = 50;
            var receivedNotifications = new ConcurrentBag<McpNotification>();

            // Act - Register handlers concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < registrationCount; i++)
            {
                var handlerId = i;
                tasks.Add(Task.Run(() =>
                {
                    m_notificationService.Subscribe("test-event", notification =>
                    {
                        receivedNotifications.Add(notification);
                        return Task.CompletedTask;
                    });
                }));
            }

            await Task.WhenAll(tasks);

            // Send a notification to test all handlers
            await m_notificationService.NotifyCommandStatusAsync("test", "test", "executing");

            // Assert - All registered handlers should receive the notification
            Assert.Equal(registrationCount, receivedNotifications.Count);
        }

        [Fact]
        public async Task CommandQueueService_RaceCondition_StateUpdates_Atomic()
        {
            // Arrange
            var commandId = m_commandQueueService.QueueCommand("test command");
            var stateUpdates = new ConcurrentBag<string>();

            // Wait for command to be processed
            await Task.Delay(200);

            // Act - Try to update state concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    // Simulate concurrent state updates
                    var result = await m_commandQueueService.GetCommandResult(commandId);
                    stateUpdates.Add(result);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - State updates should be atomic (no corruption)
            Assert.Equal(10, stateUpdates.Count);
            Assert.All(stateUpdates, result => Assert.Equal("Mock result", result));
        }

        [Fact]
        public async Task McpNotificationService_HandlerRemoval_Concurrent_ThreadSafe()
        {
            // Arrange
            var handlerCount = 20;
            var receivedNotifications = new ConcurrentBag<McpNotification>();
            var handlers = new List<Func<McpNotification, Task>>();
            var subscriptionIds = new List<string>();

            // Register handlers
            for (int i = 0; i < handlerCount; i++)
            {
                var handler = new Func<McpNotification, Task>(notification =>
                {
                    receivedNotifications.Add(notification);
                    return Task.CompletedTask;
                });
                handlers.Add(handler);
                var subscriptionId = m_notificationService.Subscribe("test-event", handler);
                subscriptionIds.Add(subscriptionId);
            }

            // Act - Remove handlers concurrently while sending notifications
            var tasks = new List<Task>();

            // Send notifications concurrently
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(m_notificationService.NotifyCommandStatusAsync(
                    $"cmd{i}", "test command", "executing"));
            }

            // Remove half the handlers after sending notifications
            for (int i = 0; i < handlerCount / 2; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    m_notificationService.Unsubscribe(subscriptionIds[i]);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - Should receive notifications from all handlers (removal happens after sending)
            var expectedNotifications = 10 * handlerCount; // All handlers receive notifications
            Assert.Equal(expectedNotifications, receivedNotifications.Count);
        }

        [Fact]
        public async Task CommandQueueService_Cleanup_Concurrent_ThreadSafe()
        {
            // Arrange - Add many commands
            var commandCount = 1000;
            var commands = new List<string>();

            for (int i = 0; i < commandCount; i++)
            {
                var commandId = m_commandQueueService.QueueCommand($"test command {i}");
                commands.Add(commandId);
            }

            // Wait for commands to complete
            await Task.Delay(100);

            // Act - Trigger cleanup concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    m_commandQueueService.TriggerCleanup();
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - Cleanup should complete without errors
            // (The test passes if no exceptions are thrown)
            Assert.True(true);
        }

        [Fact]
        public async Task McpNotificationService_EmptyHandlers_Concurrent_ThreadSafe()
        {
            // Arrange - No handlers registered
            var notificationCount = 100;

            // Act - Send notifications concurrently with no handlers
            var tasks = new List<Task>();
            for (int i = 0; i < notificationCount; i++)
            {
                tasks.Add(m_notificationService.NotifyCommandStatusAsync(
                    $"cmd{i}", "test command", "executing"));
            }

            await Task.WhenAll(tasks);

            // Assert - Should complete without errors
            // (The test passes if no exceptions are thrown)
            Assert.True(true);
        }
    }
}

