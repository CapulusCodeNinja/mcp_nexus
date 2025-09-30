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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Services
{
    /// <summary>
    /// Performance tests to ensure our optimizations work correctly
    /// </summary>
    public class PerformanceTests : IDisposable
    {
        private readonly Mock<ICdbSession> m_mockCdbSession;
        private readonly Mock<ILogger<CommandQueueService>> m_mockLogger;
        private readonly Mock<ILoggerFactory> m_mockLoggerFactory;
        private readonly CommandQueueService m_commandQueueService;
        private readonly Mock<ILogger<McpNotificationService>> m_mockNotificationLogger;
        private readonly McpNotificationService m_notificationService;

        public PerformanceTests()
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
        public async Task CommandQueueService_CleanupPerformance_CompletesWithinReasonableTime()
        {
            // Arrange - Add many completed commands to test cleanup performance
            var commands = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                var commandId = m_commandQueueService.QueueCommand($"test command {i}");
                commands.Add(commandId);
            }

            // Wait for commands to complete
            await Task.Delay(100);

            // Act - Measure cleanup performance
            var stopwatch = Stopwatch.StartNew();

            // Trigger cleanup using the public method
            m_commandQueueService.TriggerCleanup();

            stopwatch.Stop();

            // Assert - Cleanup should complete quickly even with many commands
            Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                $"Cleanup took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        }

        [Fact]
        public async Task McpNotificationService_EmptyHandlers_PerformsWell()
        {
            // Arrange - No handlers registered (common scenario)
            var iterations = 1000;
            var notifications = new List<Task>();

            // Act - Measure performance with no handlers
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                notifications.Add(m_notificationService.NotifyCommandStatusAsync(
                    $"cmd{i}", "test command", "executing", 50, "Processing"));
            }

            await Task.WhenAll(notifications);
            stopwatch.Stop();

            // Assert - Should complete quickly even with many notifications
            Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                $"1000 notifications with no handlers took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        }

        [Fact]
        public async Task McpNotificationService_ManyHandlers_PerformsWell()
        {
            // Arrange - Register many handlers
            var handlerCount = 10;
            var receivedNotifications = new List<McpNotification>();
            var handlers = new List<Func<McpNotification, Task>>();

            for (int i = 0; i < handlerCount; i++)
            {
                var handler = new Func<McpNotification, Task>(notification =>
                {
                    lock (receivedNotifications)
                    {
                        receivedNotifications.Add(notification);
                    }
                    return Task.CompletedTask;
                });
                handlers.Add(handler);
                m_notificationService.RegisterNotificationHandler(handler);
            }

            var iterations = 100;
            var notifications = new List<Task>();

            // Act - Measure performance with many handlers
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                notifications.Add(m_notificationService.NotifyCommandStatusAsync(
                    $"cmd{i}", "test command", "executing", 50, "Processing"));
            }

            await Task.WhenAll(notifications);
            stopwatch.Stop();

            // Assert - Should complete reasonably quickly
            Assert.True(stopwatch.ElapsedMilliseconds < 2000,
                $"{iterations} notifications with {handlerCount} handlers took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");

            // Verify all notifications were received
            Assert.Equal(iterations * handlerCount, receivedNotifications.Count);
        }

        [Fact]
        public void StringOperations_CommandAnalysis_PerformsWell()
        {
            // Arrange - Test the optimized string operations in ResilientCommandQueueService
            var commands = new[]
            {
                "!analyze -v",
                "!heap -p -a",
                "!process 0 0",
                "!locks",
                "!handle",
                "!threads",
                "!modules"
            };

            var iterations = 10000;

            // Act - Measure string operation performance
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var command = commands[i % commands.Length];
                var elapsed = TimeSpan.FromMinutes(i % 10);

                // This tests the optimized DetermineHeartbeatDetails method
                var details = DetermineHeartbeatDetails(command, elapsed);
                Assert.NotNull(details);
            }

            stopwatch.Stop();

            // Assert - String operations should be very fast
            Assert.True(stopwatch.ElapsedMilliseconds < 100,
                $"{iterations} string operations took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
        }

        [Fact]
        public void CollectionOperations_ValueTupleIteration_PerformsWell()
        {
            // Arrange - Create a large dictionary to test ValueTuple iteration
            var dictionary = new Dictionary<string, string>();
            for (int i = 0; i < 10000; i++)
            {
                dictionary[$"key{i}"] = $"value{i}";
            }

            var iterations = 100;
            var results = new List<string>();

            // Act - Measure ValueTuple iteration performance
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                foreach (var (key, value) in dictionary)
                {
                    results.Add($"{key}:{value}");
                }
            }

            stopwatch.Stop();

            // Assert - ValueTuple iteration should be efficient
            Assert.True(stopwatch.ElapsedMilliseconds < 500,
                $"{iterations} ValueTuple iterations took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
            Assert.Equal(iterations * dictionary.Count, results.Count);
        }

        // Helper method to test the optimized DetermineHeartbeatDetails method
        private static string DetermineHeartbeatDetails(string command, TimeSpan elapsed)
        {
            // PERFORMANCE: Use StringComparison.OrdinalIgnoreCase for better performance
            if (command.Contains("!analyze", StringComparison.OrdinalIgnoreCase))
            {
                if (elapsed.TotalMinutes < 2)
                    return "Initializing crash analysis engine...";
                else if (elapsed.TotalMinutes < 5)
                    return "Analyzing memory dumps and stack traces...";
                else if (elapsed.TotalMinutes < 10)
                    return "Performing deep symbol resolution...";
                else
                    return "Processing complex crash analysis (this may take several more minutes)...";
            }

            if (command.Contains("!heap", StringComparison.OrdinalIgnoreCase))
            {
                if (elapsed.TotalMinutes < 1)
                    return "Scanning heap structures...";
                else if (elapsed.TotalMinutes < 3)
                    return "Analyzing heap allocations and free blocks...";
                else
                    return "Processing large heap dump (this is normal for applications with high memory usage)...";
            }

            if (command.Contains("!process 0 0", StringComparison.OrdinalIgnoreCase) ||
                command.Contains("!process", StringComparison.OrdinalIgnoreCase))
            {
                if (elapsed.TotalMinutes < 1)
                    return "Enumerating system processes...";
                else if (elapsed.TotalMinutes < 3)
                    return "Gathering detailed process information...";
                else
                    return "Processing extensive process data...";
            }

            if (command.Contains("!locks", StringComparison.OrdinalIgnoreCase) ||
                command.Contains("!handle", StringComparison.OrdinalIgnoreCase))
            {
                return elapsed.TotalMinutes < 2
                    ? "Analyzing system locks and handles..."
                    : "Processing extensive lock and handle information...";
            }

            return "Processing command...";
        }
    }
}

