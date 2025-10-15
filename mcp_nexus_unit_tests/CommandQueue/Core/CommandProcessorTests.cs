using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Debugger;
using System.Collections.Concurrent;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for CommandProcessor
    /// </summary>
    public class CommandProcessorTests : IDisposable
    {
        private readonly Mock<ICdbSession> m_MockCdbSession;
        private readonly Mock<ILogger> m_MockLogger;
        private readonly CommandQueueConfiguration m_Config;
        private readonly CommandTracker m_Tracker;
        private readonly BlockingCollection<QueuedCommand> m_CommandQueue;
        private readonly CancellationTokenSource m_ProcessingCts;
        private readonly SessionCommandResultCache m_ResultCache;

        public CommandProcessorTests()
        {
            m_MockCdbSession = new Mock<ICdbSession>();
            m_MockLogger = new Mock<ILogger>();
            m_CommandQueue = new BlockingCollection<QueuedCommand>();
            m_ProcessingCts = new CancellationTokenSource();

            m_Config = new CommandQueueConfiguration(
                sessionId: "test-session",
                defaultCommandTimeout: TimeSpan.FromMinutes(5),
                heartbeatInterval: TimeSpan.FromSeconds(30));

            m_Tracker = new CommandTracker(m_MockLogger.Object, m_Config, m_CommandQueue);
            m_ResultCache = new SessionCommandResultCache(
                maxMemoryBytes: 10_000_000,
                maxResults: 1000,
                memoryPressureThreshold: 0.9,
                logger: null);
        }

        [Fact]
        public void Constructor_WithNullCdbSession_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommandProcessor(
                null!,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommandProcessor(
                m_MockCdbSession.Object,
                null!,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                null!,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache));
        }

        [Fact]
        public void Constructor_WithNullTracker_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                null!,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache));
        }

        [Fact]
        public void Constructor_WithNullCommandQueue_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                null!,
                m_ProcessingCts,
                m_ResultCache));
        }

        [Fact]
        public void Constructor_WithNullProcessingCts_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                null!,
                m_ResultCache));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Assert
            Assert.NotNull(processor);
        }

        [Fact]
        public void Constructor_WithNullResultCache_CreatesInstance()
        {
            // Act
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                null);

            // Assert
            Assert.NotNull(processor);
        }

        [Fact]
        public void CancelCommand_WithNullCommandId_ThrowsArgumentNullException()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => processor.CancelCommand(null!));
        }

        [Fact]
        public void CancelCommand_WithEmptyCommandId_ReturnsFalse()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Act
            var result = processor.CancelCommand("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelCommand_WithWhitespaceCommandId_ReturnsFalse()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Act
            var result = processor.CancelCommand("   ");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelCommand_WithNonExistentCommandId_ReturnsFalse()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Act
            var result = processor.CancelCommand("non-existent-cmd");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancelCommand_WithValidCommandId_ReturnsTrue()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command = new QueuedCommand(
                "cmd-1",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());

            m_Tracker.TryAddCommand("cmd-1", command);

            // Act
            var result = processor.CancelCommand("cmd-1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CancelCommand_WithAlreadyCancelledCommand_ReturnsTrue()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var cts = new CancellationTokenSource();
            var command = new QueuedCommand(
                "cmd-1",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                cts);

            m_Tracker.TryAddCommand("cmd-1", command);
            cts.Cancel(); // Cancel it first

            // Act
            var result = processor.CancelCommand("cmd-1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetCommandResult_WithNullCache_ReturnsNull()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                null); // No cache

            // Act
            var result = processor.GetCommandResult("cmd-1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCommandResult_WithNonExistentCommand_ReturnsNull()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Act
            var result = processor.GetCommandResult("non-existent-cmd");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCommandResult_WithExistingCommand_ReturnsResult()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var commandId = "cmd-1";
            var commandResult = CommandResult.Success("test output", TimeSpan.FromSeconds(1));
            m_ResultCache.StoreResult(commandId, commandResult, "test command",
                DateTime.Now, DateTime.Now, DateTime.Now);

            // Act
            var result = processor.GetCommandResult(commandId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test output", result.Output);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void GetCachedResultWithMetadata_WithNullCache_ReturnsNull()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                null); // No cache

            // Act
            var result = processor.GetCachedResultWithMetadata("cmd-1");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCachedResultWithMetadata_WithNonExistentCommand_ReturnsNull()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Act
            var result = processor.GetCachedResultWithMetadata("non-existent-cmd");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCachedResultWithMetadata_WithExistingCommand_ReturnsResultWithMetadata()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var commandId = "cmd-1";
            var commandResult = CommandResult.Success("test output", TimeSpan.FromSeconds(1));
            var queueTime = DateTime.Now;
            var startTime = DateTime.Now.AddSeconds(1);
            var endTime = DateTime.Now.AddSeconds(2);

            m_ResultCache.StoreResult(commandId, commandResult, "test command",
                queueTime, startTime, endTime);

            // Act
            var result = processor.GetCachedResultWithMetadata(commandId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test command", result.OriginalCommand);
            Assert.NotNull(result.Result);
            Assert.Equal("test output", result.Result.Output);
        }

        [Fact]
        public void GetCacheStatistics_WithNullCache_ReturnsNull()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                null); // No cache

            // Act
            var result = processor.GetCacheStatistics();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetCacheStatistics_WithEmptyCache_ReturnsStatistics()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Act
            var result = processor.GetCacheStatistics();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalResults);
            Assert.Equal(0, result.CurrentMemoryUsage);
        }

        [Fact]
        public void GetCacheStatistics_WithCachedResults_ReturnsStatistics()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            // Store a result
            var commandId = "cmd-1";
            var commandResult = CommandResult.Success("test output", TimeSpan.FromSeconds(1));
            m_ResultCache.StoreResult(commandId, commandResult, "test command",
                DateTime.Now, DateTime.Now, DateTime.Now);

            // Act
            var result = processor.GetCacheStatistics();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalResults);
            Assert.True(result.CurrentMemoryUsage > 0);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithSingleCommand_ProcessesSuccessfully()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Command output");

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command = new QueuedCommand(
                "cmd-1",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());

            m_Tracker.TryAddCommand("cmd-1", command);
            m_CommandQueue.Add(command);
            m_CommandQueue.CompleteAdding(); // Signal no more commands

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();
            var result = await command.CompletionSource!.Task;

            await processingTask;

            // Assert
            Assert.Equal("Command output", result);
            m_MockCdbSession.Verify(s => s.ExecuteCommand("test command", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithMultipleCommands_ProcessesInOrder()
        {
            // Arrange
            var outputs = new[] { "Output 1", "Output 2", "Output 3" };
            var callCount = 0;
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => outputs[callCount++]);

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var commands = new[]
            {
                new QueuedCommand("cmd-1", "command 1", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource()),
                new QueuedCommand("cmd-2", "command 2", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource()),
                new QueuedCommand("cmd-3", "command 3", DateTime.Now, new TaskCompletionSource<string>(), new CancellationTokenSource())
            };

            foreach (var cmd in commands)
            {
                m_Tracker.TryAddCommand(cmd.Id!, cmd);
                m_CommandQueue.Add(cmd);
            }
            m_CommandQueue.CompleteAdding();

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();
            var results = await Task.WhenAll(commands.Select(c => c.CompletionSource!.Task));
            await processingTask;

            // Assert
            Assert.Equal("Output 1", results[0]);
            Assert.Equal("Output 2", results[1]);
            Assert.Equal("Output 3", results[2]);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithCancellation_StopsProcessing()
        {
            // Arrange
            var taskSource = new TaskCompletionSource<string>();
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(taskSource.Task);

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command = new QueuedCommand(
                "cmd-1",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());

            m_Tracker.TryAddCommand("cmd-1", command);
            m_CommandQueue.Add(command);

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();

            // Cancel processing
            m_ProcessingCts.Cancel();

            // Complete the command so it can finish
            taskSource.SetResult("output");

            // Wait for processing to stop
            await processingTask;

            // Assert - Processing stopped (no exception thrown)
            Assert.True(m_ProcessingCts.IsCancellationRequested);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithCommandExecutionFailure_CompletesWithError()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test error"));

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command = new QueuedCommand(
                "cmd-1",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());

            m_Tracker.TryAddCommand("cmd-1", command);
            m_CommandQueue.Add(command);
            m_CommandQueue.CompleteAdding();

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();
            var result = await command.CompletionSource!.Task;
            await processingTask;

            // Assert
            Assert.Contains("Test error", result);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithCommandCancellation_MarksCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command = new QueuedCommand(
                "cmd-1",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                cts);

            m_Tracker.TryAddCommand("cmd-1", command);
            m_CommandQueue.Add(command);
            m_CommandQueue.CompleteAdding();

            // Cancel the command before it executes
            cts.Cancel();

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();
            var result = await command.CompletionSource!.Task;
            await processingTask;

            // Assert
            Assert.Contains("cancelled", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithTimeout_CompletesWithTimeoutMessage()
        {
            // Arrange
            var shortTimeoutConfig = new CommandQueueConfiguration(
                sessionId: "test-session",
                defaultCommandTimeout: TimeSpan.FromMilliseconds(100), // Very short timeout
                heartbeatInterval: TimeSpan.FromSeconds(30));

            var shortTimeoutTracker = new CommandTracker(m_MockLogger.Object, shortTimeoutConfig, m_CommandQueue);

            // Setup command that takes longer than timeout
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (string cmd, CancellationToken ct) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct); // Will be cancelled by timeout
                    return "Should not reach here";
                });

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                shortTimeoutConfig,
                shortTimeoutTracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command = new QueuedCommand(
                "cmd-1",
                "slow command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());

            shortTimeoutTracker.TryAddCommand("cmd-1", command);
            m_CommandQueue.Add(command);
            m_CommandQueue.CompleteAdding();

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();
            var result = await command.CompletionSource!.Task;
            await processingTask;

            // Assert
            Assert.Contains("timed out", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_StoresResultInCache()
        {
            // Arrange
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Cached output");

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command = new QueuedCommand(
                "cmd-cache-1",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());

            m_Tracker.TryAddCommand("cmd-cache-1", command);
            m_CommandQueue.Add(command);
            m_CommandQueue.CompleteAdding();

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();
            await command.CompletionSource!.Task;
            await processingTask;

            // Assert
            var cachedResult = processor.GetCommandResult("cmd-cache-1");
            Assert.NotNull(cachedResult);
            Assert.Equal("Cached output", cachedResult.Output);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithEmptyQueue_CompletesImmediately()
        {
            // Arrange
            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            m_CommandQueue.CompleteAdding(); // No commands

            // Act
            await processor.ProcessCommandQueueAsync();

            // Assert - Should complete without errors
            Assert.True(true);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_ContinuesAfterSingleCommandFailure()
        {
            // Arrange
            var callCount = 0;
            m_MockCdbSession.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        throw new InvalidOperationException("First command fails");
                    return Task.FromResult("Second command succeeds");
                });

            var processor = new CommandProcessor(
                m_MockCdbSession.Object,
                m_MockLogger.Object,
                m_Config,
                m_Tracker,
                m_CommandQueue,
                m_ProcessingCts,
                m_ResultCache);

            var command1 = new QueuedCommand("cmd-1", "command 1", DateTime.Now,
                new TaskCompletionSource<string>(), new CancellationTokenSource());
            var command2 = new QueuedCommand("cmd-2", "command 2", DateTime.Now,
                new TaskCompletionSource<string>(), new CancellationTokenSource());

            m_Tracker.TryAddCommand("cmd-1", command1);
            m_Tracker.TryAddCommand("cmd-2", command2);
            m_CommandQueue.Add(command1);
            m_CommandQueue.Add(command2);
            m_CommandQueue.CompleteAdding();

            // Act
            var processingTask = processor.ProcessCommandQueueAsync();
            var result1 = await command1.CompletionSource!.Task;
            var result2 = await command2.CompletionSource!.Task;
            await processingTask;

            // Assert
            Assert.Contains("First command fails", result1);
            Assert.Equal("Second command succeeds", result2);
        }

        public void Dispose()
        {
            m_CommandQueue?.Dispose();
            m_ProcessingCts?.Dispose();
            m_ResultCache?.Dispose();
        }
    }
}
