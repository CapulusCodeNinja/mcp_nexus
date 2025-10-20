using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Batching;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Batching
{
    /// <summary>
    /// Unit tests for BatchCommandProcessor
    /// </summary>
    public class BatchCommandProcessorTests : IDisposable
    {
        #region Private Fields

        private readonly Mock<ICdbSession> m_MockCdbSession;
        private readonly Mock<ILogger<BatchCommandProcessor>> m_MockLogger;
        private readonly Mock<IOptions<BatchingConfiguration>> m_MockOptions;
        private readonly SessionCommandResultCache m_ResultCache;
        private readonly BatchingConfiguration m_Config;

        #endregion

        #region Constructor

        public BatchCommandProcessorTests()
        {
            m_MockCdbSession = new Mock<ICdbSession>();
            m_MockLogger = new Mock<ILogger<BatchCommandProcessor>>();
            m_MockOptions = new Mock<IOptions<BatchingConfiguration>>();

            // Create a real SessionCommandResultCache instance instead of mocking
            m_ResultCache = new SessionCommandResultCache();

            m_Config = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 3,
                BatchWaitTimeoutMs = 1000,
                BatchTimeoutMultiplier = 1.5,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = new[] { "!analyze", "!dump" }
            };

            m_MockOptions.Setup(x => x.Value).Returns(m_Config);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            // Assert
            Assert.NotNull(processor);
        }

        [Fact]
        public void Constructor_WithNullCdbSession_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchCommandProcessor(
                null!,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id"));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                null!,
                m_MockOptions.Object,
                "test-session-id"));
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                null!,
                "test-session-id"));
        }

        [Fact]
        public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                null!));
        }

        #endregion

        #region ProcessCommandAsync Tests

        [Fact]
        public async Task ProcessCommandAsync_WithNullCommand_ShouldThrowArgumentNullException()
        {
            // Arrange
            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => processor.ProcessCommandAsync(null!));
        }

        [Fact]
        public async Task ProcessCommandAsync_WithExcludedCommand_ShouldExecuteImmediately()
        {
            // Arrange
            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            var command = new QueuedCommand(
                "test-id",
                "!analyze -v",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource(),
                CommandState.Queued);

            m_MockCdbSession.Setup(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Analysis complete");

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithBatchableCommand_ShouldAddToBatch()
        {
            // Arrange
            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            var command = new QueuedCommand(
                "test-id",
                "lm",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource(),
                CommandState.Queued);

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert
            // Command should be added to batch queue, not executed immediately
            m_MockCdbSession.Verify(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithMaxBatchSize_ShouldExecuteBatch()
        {
            // Arrange
            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            var commands = new List<QueuedCommand>();
            for (int i = 0; i < m_Config.MaxBatchSize; i++)
            {
                commands.Add(new QueuedCommand(
                    $"test-id-{i}",
                    "lm",
                    DateTime.Now,
                    new TaskCompletionSource<string>(),
                    new CancellationTokenSource(),
                    CommandState.Queued));
            }

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Batch output");

            // Act
            foreach (var command in commands)
            {
                await processor.ProcessCommandAsync(command);
            }

            // Wait for all commands to complete
            await Task.WhenAll(commands.Select(c => c.CompletionSource!.Task)).WaitAsync(TimeSpan.FromSeconds(2));

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteBatchCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            // Act
            processor.Dispose();

            // Assert
            // Should not throw any exceptions
            Assert.True(true);
        }

        [Fact]
        public async Task ProcessCommandAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            processor.Dispose();

            var command = new QueuedCommand(
                "test-id",
                "lm",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource(),
                CommandState.Queued);

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => processor.ProcessCommandAsync(command));
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task ProcessCommandAsync_WithBatchingDisabled_ShouldExecuteImmediately()
        {
            // Arrange
            var configWithBatchingDisabled = new BatchingConfiguration
            {
                Enabled = false,
                MaxBatchSize = 3,
                BatchWaitTimeoutMs = 1000,
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = new[] { "!analyze" }
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithBatchingDisabled);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command = new QueuedCommand("cmd-1", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Module list output");

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteCommand("lm", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithZeroMaxBatchSize_ShouldExecuteImmediately()
        {
            // Arrange
            var configWithZeroBatchSize = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 0, // Zero batch size
                BatchWaitTimeoutMs = 1000,
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = new[] { "!analyze" }
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithZeroBatchSize);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command = new QueuedCommand("cmd-1", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Module list output");

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteCommand("lm", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithNegativeMaxBatchSize_ShouldExecuteImmediately()
        {
            // Arrange
            var configWithNegativeBatchSize = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = -1, // Negative batch size
                BatchWaitTimeoutMs = 1000,
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = new[] { "!analyze" }
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithNegativeBatchSize);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command = new QueuedCommand("cmd-1", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Module list output");

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteCommand("lm", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithZeroTimeout_ShouldExecuteImmediately()
        {
            // Arrange
            var configWithZeroTimeout = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 3,
                BatchWaitTimeoutMs = 0, // Zero timeout
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = new[] { "!analyze" }
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithZeroTimeout);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command = new QueuedCommand("cmd-1", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Module list output");

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteCommand("lm", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithNegativeTimeout_ShouldExecuteImmediately()
        {
            // Arrange
            var configWithNegativeTimeout = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 3,
                BatchWaitTimeoutMs = -1000, // Negative timeout
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = new[] { "!analyze" }
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithNegativeTimeout);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command = new QueuedCommand("cmd-1", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Module list output");

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteCommand("lm", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithEmptyExcludedCommands_ShouldBatchAllCommands()
        {
            // Arrange
            var configWithEmptyExclusions = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 2,
                BatchWaitTimeoutMs = 10, // Very small timeout for immediate execution
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = new string[0] // Empty exclusions
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithEmptyExclusions);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command1 = new QueuedCommand("cmd-1", "!analyze", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());
            var command2 = new QueuedCommand("cmd-2", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            var batchExecutionTcs = new TaskCompletionSource<string>();
            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    // Should be a batch command
                    Assert.Contains("MCP_NEXUS_BATCH_START", cmd);
                    Assert.Contains("!analyze", cmd);
                    Assert.Contains("lm", cmd);
                    return batchExecutionTcs.Task;
                });

            // Act
            await processor.ProcessCommandAsync(command1);
            await processor.ProcessCommandAsync(command2);

            // Wait for the batch timer to fire (BatchWaitTimeoutMs = 10ms)
            await Task.Delay(50);

            // Complete the batch execution immediately
            batchExecutionTcs.SetResult("Batch output");

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteBatchCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithNullExcludedCommands_ShouldBatchAllCommands()
        {
            // Arrange
            var configWithNullExclusions = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 2,
                BatchWaitTimeoutMs = 10, // Very small timeout for immediate execution
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 10,
                ExcludedCommands = null! // Null exclusions
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithNullExclusions);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command1 = new QueuedCommand("cmd-1", "!analyze", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());
            var command2 = new QueuedCommand("cmd-2", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            var batchExecutionTcs = new TaskCompletionSource<string>();
            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((cmd, token) =>
                {
                    // Should be a batch command
                    Assert.Contains("MCP_NEXUS_BATCH_START", cmd);
                    Assert.Contains("!analyze", cmd);
                    Assert.Contains("lm", cmd);
                    return batchExecutionTcs.Task;
                });

            // Act
            await processor.ProcessCommandAsync(command1);
            await processor.ProcessCommandAsync(command2);

            // Wait for the batch timer to fire (BatchWaitTimeoutMs = 10ms)
            await Task.Delay(50);

            // Complete the batch execution immediately
            batchExecutionTcs.SetResult("Batch output");

            // Wait for commands to complete
            await Task.WhenAll(command1.CompletionSource!.Task, command2.CompletionSource!.Task).WaitAsync(TimeSpan.FromSeconds(2));

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteBatchCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithExtremeTimeoutValues_ShouldHandleGracefully()
        {
            // Arrange
            var configWithExtremeValues = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 3,
                BatchWaitTimeoutMs = int.MaxValue, // Extreme timeout
                BatchTimeoutMultiplier = double.MaxValue, // Extreme multiplier
                MaxBatchTimeoutMinutes = int.MaxValue, // Extreme max timeout
                ExcludedCommands = new[] { "!analyze" }
            };

            var mockOptions = new Mock<IOptions<BatchingConfiguration>>();
            mockOptions.Setup(x => x.Value).Returns(configWithExtremeValues);

            using var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                mockOptions.Object);

            var command = new QueuedCommand("cmd-1", "lm", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource());

            m_MockCdbSession.Setup(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Module list output");

            // Act
            await processor.ProcessCommandAsync(command);

            // Assert - Should not throw and should add to batch (not execute immediately with extreme but valid values)
            // The command should be queued for batching since the values are extreme but valid
            m_MockCdbSession.Verify(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            // Clean up
            processor.Dispose();
        }

        [Fact]
        public async Task Dispose_WithActiveTimer_DisposesTimerCorrectly()
        {
            // Arrange - Create processor with batching enabled (creates timer)
            var config = new BatchingConfiguration
            {
                Enabled = true,
                MaxBatchSize = 5,
                BatchWaitTimeoutMs = 5000, // Long timeout to ensure timer exists
                BatchTimeoutMultiplier = 1.0,
                MaxBatchTimeoutMinutes = 30,
                ExcludedCommands = []
            };
            m_MockOptions.Setup(o => o.Value).Returns(config);

            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object,
                "test-session-id");

            // Queue a command to start the batch processing loop (which creates the timer)
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<string>();
            var command = new QueuedCommand("test-cmd", "lm", DateTime.Now, tcs, cts);
            await processor.ProcessCommandAsync(command);

            // Wait a tiny bit to ensure timer is created
            await Task.Delay(50);

            // Act - Dispose should dispose the timer (line 289 TRUE branch)
            processor.Dispose();

            // Assert - Disposal should complete without error
            Assert.True(true); // Timer disposal is verified by no exception
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            m_ResultCache?.Dispose();
        }

        #endregion
    }
}
