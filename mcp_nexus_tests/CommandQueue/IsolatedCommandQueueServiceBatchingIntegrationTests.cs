using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.CommandQueue;
using mcp_nexus.Debugger;
using mcp_nexus.Notifications;
using Xunit;
using mcp_nexus_tests.Mocks;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Integration tests for IsolatedCommandQueueService with batching enabled
    /// </summary>
    public class IsolatedCommandQueueServiceBatchingIntegrationTests : IDisposable
    {
        private readonly Mock<ICdbSession> m_MockCdbSession;
        private readonly Mock<ILogger<IsolatedCommandQueueService>> m_MockLogger;
        private readonly Mock<IMcpNotificationService> m_MockNotificationService;
        private readonly Mock<ILoggerFactory> m_MockLoggerFactory;
        private readonly Mock<IOptions<BatchingConfiguration>> m_MockBatchingOptions;
        private readonly BatchingConfiguration m_BatchingConfig;
        private readonly string m_SessionId = "test-session-batching-123";
        private IsolatedCommandQueueService? m_Service;

        public IsolatedCommandQueueServiceBatchingIntegrationTests()
        {
            m_MockCdbSession = new Mock<ICdbSession>();
            m_MockLogger = new Mock<ILogger<IsolatedCommandQueueService>>();
            m_MockNotificationService = new Mock<IMcpNotificationService>();
            m_MockLoggerFactory = new Mock<ILoggerFactory>();
            m_MockBatchingOptions = new Mock<IOptions<BatchingConfiguration>>();

            // Setup batching configuration
            m_BatchingConfig = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 3,
                BatchWaitTimeoutMs = 1000, // 1 second for faster tests
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 5,
                ExcludedCommands = new[] { "!analyze", "!dump", "!heap" }
            };

            m_MockBatchingOptions.Setup(x => x.Value).Returns(m_BatchingConfig);

            // Setup logger factory to return mock loggers
            m_MockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>());
        }

        public void Dispose()
        {
            m_Service?.Dispose();
        }

        private IsolatedCommandQueueService CreateServiceWithBatching()
        {
            return new IsolatedCommandQueueService(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_SessionId,
                m_MockLoggerFactory.Object,
                null, // resultCache
                m_MockBatchingOptions.Object);
        }

        private IsolatedCommandQueueService CreateServiceWithoutBatching()
        {
            var configWithoutBatching = new BatchingConfiguration { Enabled = false };
            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithoutBatching);

            return new IsolatedCommandQueueService(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_MockNotificationService.Object,
                m_SessionId,
                m_MockLoggerFactory.Object,
                null, // resultCache
                mockOptions.Object);
        }

        [Fact]
        public async Task QueueMultipleBatchableCommands_WithBatchingEnabled_ShouldExecuteAsBatch()
        {
            // Arrange
            m_Service = CreateServiceWithBatching();
            var commandResults = new List<string>();
            var commandIds = new List<string>();

            // Setup CDB session to return batch output with actual command IDs
            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    // Verify it's a batch command
                    Assert.Contains("MCP_NEXUS_BATCH_START", cmd);
                    Assert.Contains("MCP_NEXUS_BATCH_END", cmd);
                    Assert.Contains("lm", cmd);
                    Assert.Contains("!threads", cmd);
                    Assert.Contains("!peb", cmd);

                    // Extract command IDs from the batch command
                    var cmdIdMatches = System.Text.RegularExpressions.Regex.Matches(cmd, @"MCP_NEXUS_CMD_SEP_([^_\r\n]+)");
                    var cmdIds = cmdIdMatches.Select(m => m.Groups[1].Value).Distinct().ToList();

                    // Build batch output with actual command IDs
                    var output = "MCP_NEXUS_BATCH_START\n";
                    if (cmdIds.Count >= 1)
                    {
                        output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[0]}\n";
                        output += "Module list output\n";
                        output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[0]}_END\n";
                    }
                    if (cmdIds.Count >= 2)
                    {
                        output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[1]}\n";
                        output += "Thread information\n";
                        output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[1]}_END\n";
                    }
                    if (cmdIds.Count >= 3)
                    {
                        output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[2]}\n";
                        output += "Process Environment Block\n";
                        output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[2]}_END\n";
                    }
                    output += "MCP_NEXUS_BATCH_END";

                    return Task.FromResult(output);
                });

            // Act - Queue multiple batchable commands quickly
            var task1 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("lm");
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            var task2 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("!threads");
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            var task3 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("!peb");
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            await Task.WhenAll(task1, task2, task3);

            // Assert
            Assert.Equal(3, commandIds.Count);
            Assert.Equal(3, commandResults.Count);
            
            // Verify all commands completed successfully
            foreach (var result in commandResults)
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }

            // Verify CDB session was called only once (for the batch)
            m_MockCdbSession.Verify(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task QueueMixedCommands_WithBatchingEnabled_ShouldBatchBatchableAndExecuteExcludedIndividually()
        {
            // Arrange
            m_Service = CreateServiceWithBatching();
            var commandResults = new List<string>();
            var commandIds = new List<string>();
            var executeCallCount = 0;

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    executeCallCount++;
                    
                    if (cmd.Contains("!analyze")) // Excluded command
                    {
                        return Task.FromResult("Analysis output");
                    }
                    else if (cmd.Contains("MCP_NEXUS_BATCH_START")) // Batch command
                    {
                        // Extract command IDs from the batch command
                        var cmdIdMatches = System.Text.RegularExpressions.Regex.Matches(cmd, @"MCP_NEXUS_CMD_SEP_([^_\r\n]+)");
                        var cmdIds = cmdIdMatches.Select(m => m.Groups[1].Value).Distinct().ToList();

                        // Build batch output with actual command IDs
                        var output = "MCP_NEXUS_BATCH_START\n";
                        if (cmdIds.Count >= 1)
                        {
                            output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[0]}\n";
                            output += "Module list output\n";
                            output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[0]}_END\n";
                        }
                        if (cmdIds.Count >= 2)
                        {
                            output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[1]}\n";
                            output += "Thread information\n";
                            output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[1]}_END\n";
                        }
                        output += "MCP_NEXUS_BATCH_END";

                        return Task.FromResult(output);
                    }
                    
                    return Task.FromResult("Unknown command output");
                });

            // Act - Queue mixed commands
            var task1 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("lm");
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            var task2 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("!analyze"); // Excluded
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            var task3 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("!threads");
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            await Task.WhenAll(task1, task2, task3);

            // Assert
            Assert.Equal(3, commandIds.Count);
            Assert.Equal(3, commandResults.Count);
            
            // Verify all commands completed successfully
            foreach (var result in commandResults)
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }

            // Verify CDB session was called twice: once for !analyze (individual) and once for the batch
            Assert.Equal(2, executeCallCount);
        }

        [Fact]
        public async Task QueueCommands_WithBatchingDisabled_ShouldExecuteIndividually()
        {
            // Arrange
            m_Service = CreateServiceWithoutBatching();
            var commandResults = new List<string>();
            var commandIds = new List<string>();
            var executeCallCount = 0;

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    executeCallCount++;
                    return Task.FromResult($"Output for command: {cmd}");
                });

            // Act - Queue multiple commands
            var task1 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("lm");
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            var task2 = Task.Run(async () =>
            {
                var id = m_Service.QueueCommand("!threads");
                commandIds.Add(id);
                var result = await m_Service.GetCommandResult(id);
                commandResults.Add(result);
            });

            await Task.WhenAll(task1, task2);

            // Assert
            Assert.Equal(2, commandIds.Count);
            Assert.Equal(2, commandResults.Count);
            
            // Verify all commands completed successfully
            foreach (var result in commandResults)
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }

            // Verify CDB session was called twice (individual execution)
            Assert.Equal(2, executeCallCount);
        }

        [Fact]
        public async Task QueueCommands_ExceedingMaxBatchSize_ShouldExecuteMultipleBatches()
        {
            // Arrange
            m_Service = CreateServiceWithBatching();
            var commandResults = new List<string>();
            var commandIds = new List<string>();
            var executeCallCount = 0;

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    executeCallCount++;
                    
                    if (cmd.Contains("MCP_NEXUS_BATCH_START"))
                    {
                        // Extract command IDs from the batch command
                        var cmdIdMatches = System.Text.RegularExpressions.Regex.Matches(cmd, @"MCP_NEXUS_CMD_SEP_([^_\r\n]+)");
                        var cmdIds = cmdIdMatches.Select(m => m.Groups[1].Value).Distinct().ToList();
                        
                        Assert.True(cmdIds.Count <= 3, $"Batch should not exceed MaxBatchSize (3), but had {cmdIds.Count} commands");
                        
                        // Build batch output with actual command IDs
                        var output = "MCP_NEXUS_BATCH_START\n";
                        for (int i = 0; i < cmdIds.Count; i++)
                        {
                            output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[i]}\n";
                            output += $"Output {i + 1}\n";
                            output += $"echo MCP_NEXUS_CMD_SEP_{cmdIds[i]}_END\n";
                        }
                        output += "MCP_NEXUS_BATCH_END";
                        
                        return Task.FromResult(output);
                    }
                    
                    return Task.FromResult("Individual command output");
                });

            // Act - Queue 5 commands (should create 2 batches: 3 + 2)
            // First, queue 3 commands (should trigger immediate batch)
            for (int cmdIndex = 0; cmdIndex < 3; cmdIndex++)
            {
                var id = m_Service.QueueCommand($"command-{cmdIndex}");
                commandIds.Add(id);
            }
            
            // Wait for first batch to complete
            await Task.Delay(100);
            
            // Then queue 2 more commands (should form second batch after timeout)
            for (int cmdIndex = 3; cmdIndex < 5; cmdIndex++)
            {
                var id = m_Service.QueueCommand($"command-{cmdIndex}");
                commandIds.Add(id);
            }
            
            // Wait for second batch timeout + processing time
            await Task.Delay(1500); // Wait longer than BatchWaitTimeoutMs (1000ms)

            // Get results for all commands
            for (int i = 0; i < commandIds.Count; i++)
            {
                var result = await m_Service.GetCommandResult(commandIds[i]);
                commandResults.Add(result);
            }

            // Assert
            Assert.Equal(5, commandIds.Count);
            Assert.Equal(5, commandResults.Count);
            
            // Verify all commands completed successfully
            foreach (var result in commandResults)
            {
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }

            // Verify CDB session was called twice (2 batches)
            Assert.Equal(2, executeCallCount);
        }

        [Fact]
        public async Task QueueCommands_WithBatchTimeout_ShouldExecuteBatchAfterTimeout()
        {
            // Arrange
            m_Service = CreateServiceWithBatching();
            var commandResults = new List<string>();
            var commandIds = new List<string>();
            var executeCallCount = 0;

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    executeCallCount++;
                    
                    if (cmd.Contains("MCP_NEXUS_BATCH_START"))
                    {
                        return Task.FromResult(
                            "MCP_NEXUS_BATCH_START\n" +
                            "echo MCP_NEXUS_CMD_SEP_cmd-1\n" +
                            "Timeout batch output\n" +
                            "echo MCP_NEXUS_CMD_SEP_cmd-1_END\n" +
                            "MCP_NEXUS_BATCH_END"
                        );
                    }
                    
                    return Task.FromResult("Individual command output");
                });

            // Act - Queue one command and wait for timeout
            var id = m_Service.QueueCommand("lm");
            commandIds.Add(id);

            // Wait for batch timeout (1 second) plus a small buffer
            await Task.Delay(1200);

            var result = await m_Service.GetCommandResult(id);
            commandResults.Add(result);

            // Assert
            Assert.Single(commandIds);
            Assert.Single(commandResults);
            Assert.NotNull(commandResults[0]);
            Assert.NotEmpty(commandResults[0]);

            // Verify CDB session was called once (after timeout)
            Assert.Equal(1, executeCallCount);
        }

        [Fact]
        public void Dispose_WithQueuedCommands_ShouldCompleteGracefully()
        {
            // Arrange
            m_Service = CreateServiceWithBatching();
            var commandIds = new List<string>();

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    // Simulate some processing time
                    Thread.Sleep(100);
                    return Task.FromResult("Command output");
                });

            // Act - Queue commands and immediately dispose
            var id1 = m_Service.QueueCommand("lm");
            var id2 = m_Service.QueueCommand("!threads");
            commandIds.AddRange(new[] { id1, id2 });

            // Dispose immediately (commands may still be processing)
            m_Service.Dispose();

            // Assert - Should not throw
            Assert.NotNull(m_Service);
        }

        [Fact]
        public void Constructor_WithBatchingEnabled_ShouldLogBatchingEnabled()
        {
            // Arrange & Act
            m_Service = CreateServiceWithBatching();

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Command batching enabled")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithBatchingDisabled_ShouldLogBatchingDisabled()
        {
            // Arrange & Act
            m_Service = CreateServiceWithoutBatching();

            // Assert
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Command batching disabled")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
