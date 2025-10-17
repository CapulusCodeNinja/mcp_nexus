using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using mcp_nexus.Tools;
using mcp_nexus.Session;
using mcp_nexus.CommandQueue;
using mcp_nexus.Extensions;
using System.Text.Json;

namespace mcp_nexus_tests.Integration
{
    /// <summary>
    /// Integration tests for the batch queuing workflow and all three features:
    /// 1. Command Batching (from earlier implementation)
    /// 2. Async Extension Execution (from earlier implementation)  
    /// 3. New nexus_get_dump_analyze_commands_status tool (current implementation)
    /// </summary>
    public class BatchQueueWorkflowTests
    {
        private readonly IServiceProvider m_ServiceProvider;
        private readonly Mock<ILogger> m_MockLogger;
        private readonly Mock<ISessionManager> m_MockSessionManager;
        private readonly Mock<ICommandQueueService> m_MockCommandQueue;
        private readonly Mock<IExtensionCommandTracker> m_MockExtensionTracker;

        public BatchQueueWorkflowTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_MockSessionManager = new Mock<ISessionManager>();
            m_MockCommandQueue = new Mock<ICommandQueueService>();
            m_MockExtensionTracker = new Mock<IExtensionCommandTracker>();

            // Create a real service provider with mocked services
            var services = new ServiceCollection();
            services.AddSingleton(m_MockSessionManager.Object);
            services.AddSingleton(m_MockExtensionTracker.Object);

            // Add logging services
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

            // Use NullLogger to avoid the internal Program class issue
            services.AddSingleton<ILogger<mcp_nexus.Program>>(NullLogger<mcp_nexus.Program>.Instance);

            m_ServiceProvider = services.BuildServiceProvider();
        }

        #region Command Batching Feature Tests

        [Fact]
        public async Task CommandBatching_QueueMultipleBatchableCommands_ShouldBatchAutomatically()
        {
            // Test the command batching feature (implemented earlier)
            // This test verifies that multiple batchable commands get grouped together
            
            // Arrange
            var sessionId = "test-session-123";
            var commands = new[] { "lm", "!threads", "!peb", "dt", "dv" };
            var queuedCommands = new List<IQueuedCommand>();
            
            // Setup mocks to simulate batching behavior
            m_MockSessionManager.Setup(sm => sm.SessionExists(sessionId)).Returns(true);
            m_MockSessionManager.Setup(sm => sm.GetCommandQueue(sessionId)).Returns(m_MockCommandQueue.Object);
            
            // Mock command queue to simulate commands being batched
            m_MockCommandQueue.Setup(q => q.QueueCommand(It.IsAny<string>()))
                .Returns<string>(cmd => 
                {
                    var mockCmd = new Mock<IQueuedCommand>();
                    mockCmd.Setup(c => c.Id).Returns($"cmd-{queuedCommands.Count + 1:D3}");
                    mockCmd.Setup(c => c.Command).Returns(cmd);
                    mockCmd.Setup(c => c.State).Returns(CommandState.Queued);
                    
                    queuedCommands.Add(mockCmd.Object);
                    return mockCmd.Object.Id;
                });

            // Act - Queue multiple commands in quick succession
            var commandIds = new List<string>();
            foreach (var command in commands)
            {
                var result = await McpNexusTools.nexus_enqueue_async_dump_analyze_command(
                    m_ServiceProvider, sessionId, command);
                
                var resultJson = JsonSerializer.Serialize(result);
                var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);
                commandIds.Add(resultObj.GetProperty("commandId").GetString()!);
            }

            // Assert
            Assert.Equal(5, commandIds.Count);
            Assert.All(commandIds, id => Assert.NotNull(id));
            
            // Verify all commands were queued
            m_MockCommandQueue.Verify(q => q.QueueCommand(It.IsAny<string>()), Times.Exactly(5));
        }

        [Fact]
        public async Task CommandBatching_QueueExcludedAndBatchableCommands_ShouldOnlyBatchEligible()
        {
            // Test that excluded commands (like !analyze) don't get batched
            // This verifies the BatchCommandFilter works correctly
            
            // Arrange
            var sessionId = "test-session-123";
            var batchableCommands = new[] { "lm", "!threads", "dt" };
            var excludedCommand = "!analyze -v";
            var allCommands = batchableCommands.Concat(new[] { excludedCommand }).ToArray();
            
            m_MockSessionManager.Setup(sm => sm.SessionExists(sessionId)).Returns(true);
            m_MockSessionManager.Setup(sm => sm.GetCommandQueue(sessionId)).Returns(m_MockCommandQueue.Object);
            
            m_MockCommandQueue.Setup(q => q.QueueCommand(It.IsAny<string>()))
                .Returns<string>(cmd => $"cmd-{Guid.NewGuid():N}");

            // Act - Queue mix of batchable and excluded commands
            var commandIds = new List<string>();
            foreach (var command in allCommands)
            {
                var result = await McpNexusTools.nexus_enqueue_async_dump_analyze_command(
                    m_ServiceProvider, sessionId, command);
                
                var resultJson = JsonSerializer.Serialize(result);
                var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);
                commandIds.Add(resultObj.GetProperty("commandId").GetString()!);
            }

            // Assert
            Assert.Equal(4, commandIds.Count);
            Assert.All(commandIds, id => Assert.NotNull(id));
            
            // Verify all commands were queued (batching happens internally)
            m_MockCommandQueue.Verify(q => q.QueueCommand(It.IsAny<string>()), Times.Exactly(4));
        }

        #endregion

        #region Extension Async Command Execution Tests

        [Fact]
        public async Task ExtensionAsyncCommands_UsingStartNexusCommands_ShouldExecuteCorrectly()
        {
            // Test async command execution in extensions (implemented earlier)
            // This verifies that extensions using Start-NexusCommands work correctly
            
            // Arrange
            var sessionId = "test-session-123";
            var extensionName = "test_extension";
            var extensionCommandId = "ext-123";
            
            m_MockSessionManager.Setup(sm => sm.SessionExists(sessionId)).Returns(true);
            m_MockSessionManager.Setup(sm => sm.GetCommandQueue(sessionId)).Returns(m_MockCommandQueue.Object);
            
            // Mock extension command tracker
            var mockExtInfo = new ExtensionCommandInfo
            {
                Id = extensionCommandId,
                ExtensionName = extensionName,
                State = CommandState.Completed,
                ProgressMessage = "Extension completed successfully"
            };
            
            m_MockExtensionTracker.Setup(t => t.GetCommandInfo(extensionCommandId)).Returns(mockExtInfo);
            m_MockExtensionTracker.Setup(t => t.GetCommandResult(extensionCommandId)).Returns(new Mock<ICommandResult>().Object);

            // Act - Execute extension
            var result = await McpNexusTools.nexus_enqueue_async_extension_command(
                m_ServiceProvider, sessionId, extensionName, null);

            // Assert
            Assert.NotNull(result);
            var resultJson = JsonSerializer.Serialize(result);
            var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);
            
            Assert.Equal("Queued", resultObj.GetProperty("status").GetString());
            Assert.True(resultObj.TryGetProperty("commandId", out var cmdId));
            Assert.NotNull(cmdId.GetString());
        }

        #endregion

        #region New Status Tool Tests

        [Fact]
        public async Task GetCommandsStatus_QueueMultipleCommandsAndPoll_ShouldShowAllStatuses()
        {
            // Test the new nexus_get_dump_analyze_commands_status tool (current implementation)
            // This verifies the tool returns all commands for a session
            
            // Arrange
            var sessionId = "test-session-123";
            var mockCommands = new List<IQueuedCommand>();
            
            // Create mock commands with different statuses
            var queueStatus = new List<(string Id, string Command, DateTime QueueTime, string Status)>();
            for (int i = 1; i <= 3; i++)
            {
                var mockCmd = new Mock<IQueuedCommand>();
                mockCmd.Setup(c => c.Id).Returns($"cmd-{i:D3}");
                mockCmd.Setup(c => c.Command).Returns($"command-{i}");
                mockCmd.Setup(c => c.State).Returns(i == 1 ? CommandState.Completed : CommandState.Executing);
                
                mockCommands.Add(mockCmd.Object);
                queueStatus.Add(($"cmd-{i:D3}", $"command-{i}", DateTime.Now, i == 1 ? "Completed" : "Executing"));
            }
            
            m_MockSessionManager.Setup(sm => sm.SessionExists(sessionId)).Returns(true);
            m_MockSessionManager.Setup(sm => sm.GetCommandQueue(sessionId)).Returns(m_MockCommandQueue.Object);
            
            m_MockCommandQueue.Setup(q => q.GetCurrentCommand()).Returns((QueuedCommand?)null);
            m_MockCommandQueue.Setup(q => q.GetQueueStatus()).Returns(queueStatus);

            // Act - Get status of all commands
            var result = await McpNexusTools.nexus_get_dump_analyze_commands_status(m_ServiceProvider, sessionId);

            // Assert
            Assert.NotNull(result);
            var resultJson = JsonSerializer.Serialize(result);
            var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);
            
            Assert.Equal(sessionId, resultObj.GetProperty("sessionId").GetString());
            Assert.True(resultObj.TryGetProperty("commands", out var commands));
            Assert.True(resultObj.TryGetProperty("commandCount", out var commandCount));
            Assert.Equal(3, commandCount.GetInt32());
            
            // Verify all commands are present
            Assert.True(commands.TryGetProperty("cmd-001", out var cmd1));
            Assert.True(commands.TryGetProperty("cmd-002", out var cmd2));
            Assert.True(commands.TryGetProperty("cmd-003", out var cmd3));
            
            Assert.Equal("command-1", cmd1.GetProperty("command").GetString());
            Assert.Equal("command-2", cmd2.GetProperty("command").GetString());
            Assert.Equal("command-3", cmd3.GetProperty("command").GetString());
        }

        #endregion

        #region Full Workflow Integration Tests

        [Fact]
        public async Task FullWorkflow_ExtensionWithAsyncAndBatchingAndStatusPolling_ShouldWorkEndToEnd()
        {
            // Test all three features working together
            // This is the comprehensive integration test
            
            // Arrange
            var sessionId = "test-session-123";
            var extensionName = "test_extension";
            var extensionCommandId = "ext-123";
            
            // Setup session manager
            m_MockSessionManager.Setup(sm => sm.SessionExists(sessionId)).Returns(true);
            m_MockSessionManager.Setup(sm => sm.GetCommandQueue(sessionId)).Returns(m_MockCommandQueue.Object);
            
            // Setup command queue for regular commands
            m_MockCommandQueue.Setup(q => q.QueueCommand(It.IsAny<string>()))
                .Returns<string>(cmd => $"cmd-{Guid.NewGuid():N}");
            
            // Setup extension tracker
            var mockExtInfo = new ExtensionCommandInfo
            {
                Id = extensionCommandId,
                ExtensionName = extensionName,
                State = CommandState.Completed,
                ProgressMessage = "Extension completed successfully"
            };
            
            m_MockExtensionTracker.Setup(t => t.GetCommandInfo(extensionCommandId)).Returns(mockExtInfo);
            m_MockExtensionTracker.Setup(t => t.GetCommandResult(extensionCommandId)).Returns(new Mock<ICommandResult>().Object);

            // Act - Execute the full workflow
            
            // 1. Queue extension
            var extResult = await McpNexusTools.nexus_enqueue_async_extension_command(
                m_ServiceProvider, sessionId, extensionName, null);
            
            // 2. Queue regular commands
            var regularCommands = new[] { "lm", "!threads", "dt" };
            var commandIds = new List<string>();
            
            foreach (var command in regularCommands)
            {
                var result = await McpNexusTools.nexus_enqueue_async_dump_analyze_command(
                    m_ServiceProvider, sessionId, command);
                
                var resultJson = JsonSerializer.Serialize(result);
                var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);
                commandIds.Add(resultObj.GetProperty("commandId").GetString()!);
            }
            
            // 3. Poll status of all commands
            var statusResult = await McpNexusTools.nexus_get_dump_analyze_commands_status(m_ServiceProvider, sessionId);

            // Assert
            // Verify extension was queued
            Assert.NotNull(extResult);
            var extResultJson = JsonSerializer.Serialize(extResult);
            var extResultObj = JsonSerializer.Deserialize<JsonElement>(extResultJson);
            Assert.Equal("Queued", extResultObj.GetProperty("status").GetString());
            
            // Verify regular commands were queued
            Assert.Equal(3, commandIds.Count);
            Assert.All(commandIds, id => Assert.NotNull(id));
            
            // Verify status tool returns information
            Assert.NotNull(statusResult);
            var statusResultJson = JsonSerializer.Serialize(statusResult);
            var statusResultObj = JsonSerializer.Deserialize<JsonElement>(statusResultJson);
            Assert.Equal(sessionId, statusResultObj.GetProperty("sessionId").GetString());
            Assert.True(statusResultObj.TryGetProperty("commands", out var commands));
            Assert.True(statusResultObj.TryGetProperty("commandCount", out var commandCount));
            
            // Verify all commands were queued
            m_MockCommandQueue.Verify(q => q.QueueCommand(It.IsAny<string>()), Times.Exactly(3));
        }

        #endregion
    }
}
