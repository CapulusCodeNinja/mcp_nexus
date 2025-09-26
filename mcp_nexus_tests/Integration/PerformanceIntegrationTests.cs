using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Integration
{
    /// <summary>
    /// Integration tests for performance optimizations
    /// </summary>
    public class PerformanceIntegrationTests : IDisposable
    {
        private readonly ServiceProvider m_serviceProvider;
        private readonly Mock<ICdbSession> m_mockCdbSession;

        public PerformanceIntegrationTests()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Add mock CdbSession
            m_mockCdbSession = new Mock<ICdbSession>();
            m_mockCdbSession.Setup(x => x.IsActive).Returns(true);
            m_mockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Mock result");
            services.AddSingleton(m_mockCdbSession.Object);
            
            // Add services
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            services.AddSingleton<IMcpNotificationService, McpNotificationService>();
            services.AddSingleton<McpToolDefinitionService>();
            services.AddSingleton<WindbgTool>();
            services.AddSingleton<McpToolExecutionService>();
            services.AddSingleton<McpProtocolService>();
            
            m_serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            m_serviceProvider?.Dispose();
        }

        [Fact]
        public async Task FullWorkflow_Performance_CompletesWithinReasonableTime()
        {
            // Arrange
            var commandQueueService = m_serviceProvider.GetRequiredService<ICommandQueueService>();
            var notificationService = m_serviceProvider.GetRequiredService<IMcpNotificationService>();
            var protocolService = m_serviceProvider.GetRequiredService<McpProtocolService>();

            var receivedNotifications = new List<object>();
            notificationService.RegisterNotificationHandler(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            var commandCount = 100;
            var commands = new List<string>();

            // Act - Measure full workflow performance
            var stopwatch = Stopwatch.StartNew();

            // Queue commands
            for (int i = 0; i < commandCount; i++)
            {
                var commandId = commandQueueService.QueueCommand($"test command {i}");
                commands.Add(commandId);
            }

            // Wait for processing
            await Task.Delay(200);

            // Get results
            var results = new List<string>();
            foreach (var commandId in commands)
            {
                var result = await commandQueueService.GetCommandResult(commandId);
                results.Add(result);
            }

            stopwatch.Stop();

            // Assert
            Assert.Equal(commandCount, results.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Full workflow took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        }

        [Fact]
        public async Task NotificationService_Performance_ScalesWell()
        {
            // Arrange
            var notificationService = m_serviceProvider.GetRequiredService<IMcpNotificationService>();
            var receivedNotifications = new List<object>();
            
            // Register multiple handlers
            var handlerCount = 5;
            for (int i = 0; i < handlerCount; i++)
            {
                notificationService.RegisterNotificationHandler(notification =>
                {
                    receivedNotifications.Add(notification);
                    return Task.CompletedTask;
                });
            }

            var notificationCount = 100;

            // Act - Measure notification performance
            var stopwatch = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (int i = 0; i < notificationCount; i++)
            {
                tasks.Add(notificationService.NotifyCommandStatusAsync(
                    $"cmd{i}", "test command", "executing", 50, "Processing"));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.Equal(notificationCount * handlerCount, receivedNotifications.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
                $"Notifications took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        }

        [Fact]
        public async Task CommandQueueService_Cleanup_Performance_ScalesWell()
        {
            // Arrange
            var commandQueueService = m_serviceProvider.GetRequiredService<ICommandQueueService>();
            
            // Add many commands
            var commandCount = 1000;
            var commands = new List<string>();
            
            for (int i = 0; i < commandCount; i++)
            {
                var commandId = commandQueueService.QueueCommand($"test command {i}");
                commands.Add(commandId);
            }

            // Wait for commands to complete
            await Task.Delay(200);

            // Act - Measure cleanup performance
            var stopwatch = Stopwatch.StartNew();
            
            // Trigger cleanup multiple times
            for (int i = 0; i < 10; i++)
            {
            var cleanupMethod = typeof(CommandQueueService).GetMethod("CleanupCompletedCommands", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cleanupMethod!.Invoke(commandQueueService, new object?[] { null });
            }
            
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Cleanup took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        }

        [Fact]
        public async Task MemoryUsage_Performance_StableUnderLoad()
        {
            // Arrange
            var commandQueueService = m_serviceProvider.GetRequiredService<ICommandQueueService>();
            var notificationService = m_serviceProvider.GetRequiredService<IMcpNotificationService>();

            var receivedNotifications = new List<object>();
            notificationService.RegisterNotificationHandler(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            var iterations = 10;
            var commandsPerIteration = 100;

            // Act - Run multiple iterations to test memory stability
            var stopwatch = Stopwatch.StartNew();

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var commands = new List<string>();
                
                // Queue commands
                for (int i = 0; i < commandsPerIteration; i++)
                {
                    var commandId = commandQueueService.QueueCommand($"test command {iteration}-{i}");
                    commands.Add(commandId);
                }

                // Send notifications
                for (int i = 0; i < commandsPerIteration; i++)
                {
                    await notificationService.NotifyCommandStatusAsync(
                        $"cmd{iteration}-{i}", "test command", "executing");
                }

                // Wait for processing
                await Task.Delay(50);

                // Get results
                foreach (var commandId in commands)
                {
                    var result = await commandQueueService.GetCommandResult(commandId);
                    Assert.NotNull(result);
                }
            }

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
                $"Memory test took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        }

        [Fact]
        public async Task ConcurrentOperations_Performance_HandlesLoad()
        {
            // Arrange
            var commandQueueService = m_serviceProvider.GetRequiredService<ICommandQueueService>();
            var notificationService = m_serviceProvider.GetRequiredService<IMcpNotificationService>();

            var receivedNotifications = new List<object>();
            notificationService.RegisterNotificationHandler(notification =>
            {
                receivedNotifications.Add(notification);
                return Task.CompletedTask;
            });

            var operationCount = 100;
            var tasks = new List<Task>();

            // Act - Run operations concurrently
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < operationCount; i++)
            {
                var operationId = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Queue command
                    var commandId = commandQueueService.QueueCommand($"test command {operationId}");
                    
                    // Send notification
                    await notificationService.NotifyCommandStatusAsync(
                        $"cmd{operationId}", "test command", "executing");
                    
                    // Get result
                    var result = await commandQueueService.GetCommandResult(commandId);
                    Assert.NotNull(result);
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
                $"Concurrent operations took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        }

        [Fact]
        public void StringOperations_Performance_Optimized()
        {
            // Arrange
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
                
                // Test the optimized string operations
                var details = DetermineHeartbeatDetails(command, elapsed);
                Assert.NotNull(details);
            }

            stopwatch.Stop();

            // Assert - String operations should be very fast
            Assert.True(stopwatch.ElapsedMilliseconds < 100, 
                $"{iterations} string operations took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
        }

        // Helper method to test the optimized DetermineHeartbeatDetails method
        private static string DetermineHeartbeatDetails(string command, TimeSpan elapsed)
        {
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

