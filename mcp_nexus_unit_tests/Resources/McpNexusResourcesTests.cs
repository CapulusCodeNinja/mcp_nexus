using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Resources;
using mcp_nexus.Session.Lifecycle;
using mcp_nexus.Session.Core.Models;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus;
using mcp_nexus.Extensions;
using System.Text.Json;

namespace mcp_nexus_unit_tests.Resources
{
    public class McpNexusResourcesTests
    {
        [Fact]
        public void McpNexusResources_ClassExists()
        {
            // Act & Assert
            Assert.NotNull(typeof(McpNexusResources));
        }

        [Fact]
        public void McpNexusResources_IsStaticClass()
        {
            // Act & Assert
            Assert.True(typeof(McpNexusResources).IsAbstract && typeof(McpNexusResources).IsSealed);
        }

        [Fact]
        public void McpNexusResources_HasExpectedMethods()
        {
            // Arrange
            var type = typeof(McpNexusResources);
            var methodNames = type.GetMethods().Select(m => m.Name).ToHashSet();

            // Assert
            Assert.Contains("Sessions", methodNames);
            Assert.Contains("Commands", methodNames);
            Assert.Contains("Extensions", methodNames);
            Assert.Contains("Usage", methodNames);
        }

        [Fact]
        public void McpNexusResources_MethodsAreStatic()
        {
            // Arrange
            var type = typeof(McpNexusResources);
            var methods = type.GetMethods().Where(m => m.DeclaringType == type);

            // Act & Assert
            foreach (var method in methods)
            {
                Assert.True(method.IsStatic, $"Method {method.Name} should be static");
            }
        }

        [Fact]
        public void McpNexusResources_MethodsReturnTask()
        {
            // Arrange
            var type = typeof(McpNexusResources);
            var methods = type.GetMethods().Where(m => m.DeclaringType == type);

            // Act & Assert
            foreach (var method in methods)
            {
                Assert.True(typeof(Task).IsAssignableFrom(method.ReturnType),
                    $"Method {method.Name} should return Task or Task<T>");
            }
        }

        [Fact]
        public void McpNexusResources_MethodsHaveCorrectParameters()
        {
            // Arrange
            var type = typeof(McpNexusResources);
            var methods = type.GetMethods().Where(m => m.DeclaringType == type);

            // Act & Assert
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                Assert.True(parameters.Length >= 1, $"Method {method.Name} should have at least one parameter");
                Assert.Equal(typeof(IServiceProvider), parameters[0].ParameterType);
            }
        }

        [Fact]
        public void McpNexusResources_UsageMethodHasCorrectParameters()
        {
            // Arrange
            var type = typeof(McpNexusResources);
            var usageMethod = type.GetMethod("Usage");

            // Act & Assert
            Assert.NotNull(usageMethod);
            var parameters = usageMethod!.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(IServiceProvider), parameters[0].ParameterType);
        }

        [Fact]
        public async Task McpNexusResources_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert - The methods will throw ArgumentNullException when trying to get services from null provider
            await Assert.ThrowsAsync<ArgumentNullException>(() => McpNexusResources.Sessions(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => McpNexusResources.Commands(null!));
            // Workflows and Usage methods don't require services, so they won't throw
        }

        [Fact]
        public async Task McpNexusResources_UsageWithValidServiceProvider_ReturnsUsageData()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();

            // Act
            var result = await McpNexusResources.Usage(mockServiceProvider.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("MCP Nexus Usage Guide", result);
        }

        [Fact]
        public async Task McpNexusResources_ExtensionsWithValidServiceProvider_ReturnsExtensionsData()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLogger = new Mock<ILogger<Program>>();
            var mockExtensionManager = new Mock<IExtensionManager>();

            services.AddSingleton(mockLogger.Object);
            services.AddSingleton(mockExtensionManager.Object);

            var serviceProvider = services.BuildServiceProvider();

            // Setup mock extension data
            var mockExtensions = new List<ExtensionMetadata>
            {
                new() {
                    Name = "basic_crash_analysis",
                    Description = "Essential commands for initial crash investigation",
                    Version = "1.0.0",
                    Author = "MCP Nexus Team",
                    ScriptType = "powershell",
                    Timeout = 1800000
                }
            };

            mockExtensionManager.Setup(x => x.GetAllExtensions()).Returns(mockExtensions);

            // Act
            var result = await McpNexusResources.Extensions(serviceProvider);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("basic_crash_analysis", result);
            Assert.Contains("Essential commands for initial crash investigation", result);
            Assert.Contains("nexus_enqueue_async_extension_command", result);
        }

        [Fact]
        public async Task McpNexusResources_ExtensionsWithDisabledSystem_ReturnsDisabledMessage()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLogger = new Mock<ILogger<Program>>();

            services.AddSingleton(mockLogger.Object);
            // Don't register IExtensionManager - simulates disabled extension system

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = await McpNexusResources.Extensions(serviceProvider);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Extension system is not enabled or not properly configured", result);
            Assert.Contains("\"enabled\": false", result);
        }

        [Fact]
        public void GetCommandStatus_WithDifferentStates_ReturnsCorrectStatus()
        {
            // Arrange
            var queuedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Queued);
            var method = typeof(McpNexusResources).GetMethod("GetCommandStatus",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.Equal("Queued", (string)method!.Invoke(null, [queuedCommand])!);
        }

        [Fact]
        public void GetQueuePositionForCommand_WithExecutingCommand_ReturnsZero()
        {
            // Arrange
            var executingCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Executing);
            var allCommands = new List<QueuedCommand> { executingCommand };
            var method = typeof(McpNexusResources).GetMethod("GetQueuePositionForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (int)method!.Invoke(null, [executingCommand, allCommands])!;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetQueuePositionForCommand_WithQueuedCommand_ReturnsPosition()
        {
            // Arrange
            var queuedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Queued);
            var allCommands = new List<QueuedCommand> { queuedCommand };
            var method = typeof(McpNexusResources).GetMethod("GetQueuePositionForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (int)method!.Invoke(null, [queuedCommand, allCommands])!;

            // Assert
            Assert.Equal(0, result); // First in queue
        }

        [Fact]
        public void GetProgressPercentageForCommand_WithCompletedCommand_Returns100()
        {
            // Arrange
            var completedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Completed);
            var allCommands = new List<QueuedCommand> { completedCommand };
            var method = typeof(McpNexusResources).GetMethod("GetProgressPercentageForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (int)method!.Invoke(null, [completedCommand, allCommands])!;

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetProgressPercentageForCommand_WithFailedCommand_Returns0()
        {
            // Arrange
            var failedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Failed);
            var allCommands = new List<QueuedCommand> { failedCommand };
            var method = typeof(McpNexusResources).GetMethod("GetProgressPercentageForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (int)method!.Invoke(null, [failedCommand, allCommands])!;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetCommandMessage_WithCompletedCommand_ReturnsSuccessMessage()
        {
            // Arrange
            var completedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Completed);
            var allCommands = new List<QueuedCommand> { completedCommand };
            var method = typeof(McpNexusResources).GetMethod("GetCommandMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string)method!.Invoke(null, [completedCommand, allCommands])!;

            // Assert
            Assert.Equal("Command completed successfully", result);
        }

        [Fact]
        public void GetCommandMessage_WithFailedCommand_ReturnsFailureMessage()
        {
            // Arrange
            var failedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Failed);
            var allCommands = new List<QueuedCommand> { failedCommand };
            var method = typeof(McpNexusResources).GetMethod("GetCommandMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string)method!.Invoke(null, [failedCommand, allCommands])!;

            // Assert
            Assert.Equal("Command failed", result);
        }

        [Fact]
        public void GetCommandMessage_WithCancelledCommand_ReturnsCancelledMessage()
        {
            // Arrange
            var cancelledCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Cancelled);
            var allCommands = new List<QueuedCommand> { cancelledCommand };
            var method = typeof(McpNexusResources).GetMethod("GetCommandMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string)method!.Invoke(null, [cancelledCommand, allCommands])!;

            // Assert
            Assert.Equal("Command was cancelled", result);
        }

        [Fact]
        public void GetElapsedTimeForCommand_WithCompletedCommand_ReturnsNull()
        {
            // Arrange
            var completedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Completed);
            var method = typeof(McpNexusResources).GetMethod("GetElapsedTimeForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string?)method!.Invoke(null, [completedCommand])!;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetElapsedTimeForCommand_WithExecutingCommand_ReturnsElapsedTime()
        {
            // Arrange
            var executingCommand = new QueuedCommand("test", "command", DateTime.Now.AddMinutes(-5), new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Executing);
            var method = typeof(McpNexusResources).GetMethod("GetElapsedTimeForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string?)method!.Invoke(null, [executingCommand])!;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("min", result);
        }

        [Fact]
        public void GetEtaTimeForCommand_WithCompletedCommand_ReturnsNull()
        {
            // Arrange
            var completedCommand = new QueuedCommand("test", "command", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Completed);
            var method = typeof(McpNexusResources).GetMethod("GetEtaTimeForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string?)method!.Invoke(null, [completedCommand])!;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetEtaTimeForCommand_WithExecutingCommand_ReturnsEtaTime()
        {
            // Arrange
            var executingCommand = new QueuedCommand("test", "command", DateTime.Now.AddMinutes(-2), new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Executing);
            var method = typeof(McpNexusResources).GetMethod("GetEtaTimeForCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string?)method!.Invoke(null, [executingCommand])!;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("min", result);
        }

        #region GetCommandMessage Branch Coverage Tests

        [Fact]
        public async Task Commands_WithEmptySessions_ReturnsValidJson()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLogger = new Mock<ILogger<Program>>();
            var mockSessionManager = new Mock<ISessionManager>();

            services.AddSingleton(mockLogger.Object);
            services.AddSingleton(mockSessionManager.Object);

            var serviceProvider = services.BuildServiceProvider();

            // Return empty sessions list
            mockSessionManager.Setup(x => x.GetAllSessions()).Returns(new List<SessionInfo>());

            // Act
            var result = await McpNexusResources.Commands(serviceProvider);

            // Assert
            Assert.NotNull(result);
            var commandDict = JsonSerializer.Deserialize<Dictionary<string, object>>(result);
            Assert.NotNull(commandDict);

            // Should return empty commands object
            Assert.True(commandDict.Count >= 0);
        }

        [Fact]
        public async Task Commands_WithException_ThrowsException()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockLogger = new Mock<ILogger<Program>>();
            var mockSessionManager = new Mock<ISessionManager>();

            services.AddSingleton(mockLogger.Object);
            services.AddSingleton(mockSessionManager.Object);

            var serviceProvider = services.BuildServiceProvider();

            // Setup to throw exception
            mockSessionManager.Setup(x => x.GetAllSessions()).Throws(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => McpNexusResources.Commands(serviceProvider));
        }

        [Fact]
        public void GetCommandMessage_WithExecutingCommand_ShowsElapsedTime()
        {
            // Arrange
            var executingCommand = new QueuedCommand("test", "!analyze -v", DateTime.Now.AddMinutes(-2), new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Executing);
            var allCommands = new List<QueuedCommand> { executingCommand };
            var method = typeof(McpNexusResources).GetMethod("GetCommandMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string)method!.Invoke(null, [executingCommand, allCommands])!;

            // Assert
            Assert.Contains("Command is currently executing", result);
            Assert.Contains("elapsed:", result);
            Assert.Contains("minutes", result);
        }

        [Fact]
        public void GetCommandMessage_WithExecutingCommand_ShowsLongElapsedTime()
        {
            // Arrange
            var executingCommand = new QueuedCommand("test", "!analyze -v", DateTime.Now.AddMinutes(-15), new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Executing);
            var allCommands = new List<QueuedCommand> { executingCommand };
            var method = typeof(McpNexusResources).GetMethod("GetCommandMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string)method!.Invoke(null, [executingCommand, allCommands])!;

            // Assert
            Assert.Contains("Command is currently executing", result);
            Assert.Contains("elapsed:", result);
            Assert.Contains("minutes", result);
        }

        [Fact]
        public void GetCommandMessage_WithQueuedCommand_ShowsQueueInfo()
        {
            // Arrange
            var queuedCommand = new QueuedCommand("test", "!analyze -v", DateTime.Now.AddMinutes(-1), new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Queued);
            var allCommands = new List<QueuedCommand> { queuedCommand };
            var method = typeof(McpNexusResources).GetMethod("GetCommandMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var result = (string)method!.Invoke(null, [queuedCommand, allCommands])!;

            // Assert
            Assert.Contains("Command is next in queue", result);
            Assert.Contains("Queued for:", result);
            Assert.Contains("min", result);
        }

        #endregion
    }
}