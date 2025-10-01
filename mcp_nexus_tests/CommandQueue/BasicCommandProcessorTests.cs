using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;
using mcp_nexus.Debugger;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for BasicCommandProcessor
    /// </summary>
    public class BasicCommandProcessorTests : IDisposable
    {
        private readonly Mock<ICdbSession> _mockCdbSession;
        private readonly Mock<ILogger<BasicCommandProcessor>> _mockLogger;
        private readonly BasicQueueConfiguration _config;
        private readonly ConcurrentDictionary<string, QueuedCommand> _activeCommands;
        private readonly BasicCommandProcessor _processor;

        public BasicCommandProcessorTests()
        {
            _mockCdbSession = new Mock<ICdbSession>();
            _mockLogger = new Mock<ILogger<BasicCommandProcessor>>();
            _config = new BasicQueueConfiguration();
            _activeCommands = new ConcurrentDictionary<string, QueuedCommand>();
            _processor = new BasicCommandProcessor(_mockCdbSession.Object, _mockLogger.Object, _config, _activeCommands);
        }

        [Fact]
        public void Constructor_WithNullCdbSession_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new BasicCommandProcessor(null!, _mockLogger.Object, _config, _activeCommands));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new BasicCommandProcessor(_mockCdbSession.Object, null!, _config, _activeCommands));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new BasicCommandProcessor(_mockCdbSession.Object, _mockLogger.Object, null!, _activeCommands));
        }

        [Fact]
        public void Constructor_WithNullActiveCommands_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new BasicCommandProcessor(_mockCdbSession.Object, _mockLogger.Object, _config, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var processor = new BasicCommandProcessor(_mockCdbSession.Object, _mockLogger.Object, _config, _activeCommands);

            // Assert
            Assert.NotNull(processor);
            Assert.Equal((0, 0, 0), processor.GetPerformanceStats());
            Assert.Null(processor.GetCurrentCommand());
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithEmptyQueue_CompletesSuccessfully()
        {
            // Arrange
            var commandQueue = new BlockingCollection<QueuedCommand>();
            commandQueue.CompleteAdding();
            var cancellationToken = new CancellationTokenSource().Token;

            // Act
            await _processor.ProcessCommandQueueAsync(commandQueue, cancellationToken);

            // Assert
            // Should complete without throwing
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithSingleCommand_ProcessesSuccessfully()
        {
            // Arrange
            var commandQueue = new BlockingCollection<QueuedCommand>();
            var cancellationTokenSource = new CancellationTokenSource();
            var completionSource = new TaskCompletionSource<string>();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource);
            
            commandQueue.Add(queuedCommand);
            commandQueue.CompleteAdding();

            _mockCdbSession.Setup(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Analysis completed");

            // Act
            await _processor.ProcessCommandQueueAsync(commandQueue, cancellationTokenSource.Token);

            // Assert
            _mockCdbSession.Verify(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(completionSource.Task.IsCompleted);
            var stats = _processor.GetPerformanceStats();
            Assert.Equal(1, stats.Processed);
            Assert.Equal(0, stats.Failed);
            Assert.Equal(0, stats.Cancelled);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithFailingCommand_HandlesFailure()
        {
            // Arrange
            var commandQueue = new BlockingCollection<QueuedCommand>();
            var cancellationTokenSource = new CancellationTokenSource();
            var completionSource = new TaskCompletionSource<string>();
            var queuedCommand = new QueuedCommand("cmd-1", "!invalid", DateTime.UtcNow, completionSource, cancellationTokenSource);
            
            commandQueue.Add(queuedCommand);
            commandQueue.CompleteAdding();

            _mockCdbSession.Setup(x => x.ExecuteCommand("!invalid", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Command failed"));

            // Act
            await _processor.ProcessCommandQueueAsync(commandQueue, cancellationTokenSource.Token);

            // Assert
            _mockCdbSession.Verify(x => x.ExecuteCommand("!invalid", It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(completionSource.Task.IsCompleted);
            var stats = _processor.GetPerformanceStats();
            Assert.Equal(0, stats.Processed);
            Assert.Equal(1, stats.Failed);
            Assert.Equal(0, stats.Cancelled);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithCancelledCommand_HandlesCancellation()
        {
            // Arrange
            var commandQueue = new BlockingCollection<QueuedCommand>();
            var cancellationTokenSource = new CancellationTokenSource();
            var commandCancellationTokenSource = new CancellationTokenSource();
            var completionSource = new TaskCompletionSource<string>();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, commandCancellationTokenSource);
            
            commandQueue.Add(queuedCommand);
            commandQueue.CompleteAdding();

            // Cancel the command before it starts
            commandCancellationTokenSource.Cancel();

            _mockCdbSession.Setup(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            await _processor.ProcessCommandQueueAsync(commandQueue, cancellationTokenSource.Token);

            // Assert
            var stats = _processor.GetPerformanceStats();
            Assert.Equal(0, stats.Processed);
            Assert.Equal(0, stats.Failed);
            Assert.Equal(1, stats.Cancelled);
        }

        [Fact]
        public async Task ProcessCommandQueueAsync_WithServiceShutdown_HandlesCancellation()
        {
            // Arrange
            var commandQueue = new BlockingCollection<QueuedCommand>();
            var serviceCancellationTokenSource = new CancellationTokenSource();
            var commandCancellationTokenSource = new CancellationTokenSource();
            var completionSource = new TaskCompletionSource<string>();
            var queuedCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, commandCancellationTokenSource);
            
            commandQueue.Add(queuedCommand);
            commandQueue.CompleteAdding();

            // Setup the mock to simulate a long-running command that will be cancelled
            _mockCdbSession.Setup(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>(async (cmd, token) =>
                {
                    // Simulate a long-running command that checks for cancellation
                    await Task.Delay(200, token); // This will throw OperationCanceledException when token is cancelled
                    return "Command completed";
                });

            // Act - Start processing and then cancel the service
            var processingTask = _processor.ProcessCommandQueueAsync(commandQueue, serviceCancellationTokenSource.Token);
            
            // Cancel the service after a short delay to allow processing to start
            await Task.Delay(100);
            serviceCancellationTokenSource.Cancel();

            await processingTask;

            // Assert
            var stats = _processor.GetPerformanceStats();
            // The command should be cancelled due to service shutdown
            // Note: Service shutdown cancellation doesn't increment the cancelled counter in the production code
            Assert.Equal(0, stats.Processed);
            Assert.Equal(0, stats.Failed);
            Assert.Equal(0, stats.Cancelled); // Service shutdown doesn't increment this counter
        }

        [Fact]
        public void GetCurrentCommand_WhenNoCommandIsExecuting_ReturnsNull()
        {
            // Act
            var currentCommand = _processor.GetCurrentCommand();

            // Assert
            Assert.Null(currentCommand);
        }

        [Fact]
        public void GetPerformanceStats_Initially_ReturnsZeroStats()
        {
            // Act
            var stats = _processor.GetPerformanceStats();

            // Assert
            Assert.Equal(0, stats.Processed);
            Assert.Equal(0, stats.Failed);
            Assert.Equal(0, stats.Cancelled);
        }

        [Fact]
        public void TriggerCleanup_RemovesCompletedCommands()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var oldCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow.AddHours(-2), completionSource, cancellationTokenSource, CommandState.Completed);
            
            _activeCommands["cmd-1"] = oldCommand;

            // Act
            _processor.TriggerCleanup();

            // Assert
            Assert.False(_activeCommands.ContainsKey("cmd-1"));
        }

        [Fact]
        public void TriggerCleanup_KeepsRecentCommands()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var recentCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource, CommandState.Completed);
            
            _activeCommands["cmd-1"] = recentCommand;

            // Act
            _processor.TriggerCleanup();

            // Assert
            Assert.True(_activeCommands.ContainsKey("cmd-1"));
        }

        [Fact]
        public void TriggerCleanup_KeepsExecutingCommands()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var executingCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow.AddHours(-2), completionSource, cancellationTokenSource, CommandState.Executing);
            
            _activeCommands["cmd-1"] = executingCommand;

            // Act
            _processor.TriggerCleanup();

            // Assert
            Assert.True(_activeCommands.ContainsKey("cmd-1"));
        }

        [Fact]
        public void Dispose_DisposesResources()
        {
            // Act
            _processor.Dispose();

            // Assert
            // Should not throw
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            _processor.Dispose();
            _processor.Dispose(); // Should not throw
        }

        public void Dispose()
        {
            _processor?.Dispose();
        }
    }
}