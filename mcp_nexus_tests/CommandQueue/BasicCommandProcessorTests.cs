using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;
using mcp_nexus.Debugger;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using mcp_nexus_tests.Helpers;

namespace mcp_nexus_tests.CommandQueue
{
    /// <summary>
    /// Tests for BasicCommandProcessor
    /// </summary>
    public class BasicCommandProcessorTests : IDisposable
    {
        private readonly ICdbSession m_RealisticCdbSession;
        private readonly Mock<ILogger<BasicCommandProcessor>> m_MockLogger;
        private readonly BasicQueueConfiguration m_Config;
        private readonly ConcurrentDictionary<string, QueuedCommand> m_ActiveCommands;
        private readonly BasicCommandProcessor m_Processor;

        public BasicCommandProcessorTests()
        {
            m_RealisticCdbSession = RealisticCdbTestHelper.CreateBugSimulatingCdbSession(Mock.Of<ILogger>());
            m_MockLogger = new Mock<ILogger<BasicCommandProcessor>>();
            m_Config = new BasicQueueConfiguration();
            m_ActiveCommands = new ConcurrentDictionary<string, QueuedCommand>();
            m_Processor = new BasicCommandProcessor(m_RealisticCdbSession, m_MockLogger.Object, m_Config, m_ActiveCommands);
        }

        [Fact]
        public void Constructor_WithNullCdbSession_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BasicCommandProcessor(null!, m_MockLogger.Object, m_Config, m_ActiveCommands));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BasicCommandProcessor(m_RealisticCdbSession.Object, null!, m_Config, m_ActiveCommands));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BasicCommandProcessor(m_RealisticCdbSession.Object, m_MockLogger.Object, null!, m_ActiveCommands));
        }

        [Fact]
        public void Constructor_WithNullActiveCommands_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new BasicCommandProcessor(m_RealisticCdbSession.Object, m_MockLogger.Object, m_Config, null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act
            var processor = new BasicCommandProcessor(m_RealisticCdbSession.Object, m_MockLogger.Object, m_Config, m_ActiveCommands);

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
            await m_Processor.ProcessCommandQueueAsync(commandQueue, cancellationToken);

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

            m_RealisticCdbSession.Setup(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Analysis completed");

            // Act
            await m_Processor.ProcessCommandQueueAsync(commandQueue, cancellationTokenSource.Token);

            // Assert
            m_RealisticCdbSession.Verify(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(completionSource.Task.IsCompleted);
            var stats = m_Processor.GetPerformanceStats();
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

            m_RealisticCdbSession.Setup(x => x.ExecuteCommand("!invalid", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Command failed"));

            // Act
            await m_Processor.ProcessCommandQueueAsync(commandQueue, cancellationTokenSource.Token);

            // Assert
            m_RealisticCdbSession.Verify(x => x.ExecuteCommand("!invalid", It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(completionSource.Task.IsCompleted);
            var stats = m_Processor.GetPerformanceStats();
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

            m_RealisticCdbSession.Setup(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            await m_Processor.ProcessCommandQueueAsync(commandQueue, cancellationTokenSource.Token);

            // Assert
            var stats = m_Processor.GetPerformanceStats();
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
            m_RealisticCdbSession.Setup(x => x.ExecuteCommand("!analyze -v", It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>(async (cmd, token) =>
                {
                    // Simulate a long-running command that checks for cancellation
                    await Task.Delay(200, token); // This will throw OperationCanceledException when token is cancelled
                    return "Command completed";
                });

            // Act - Start processing and then cancel the service
            var processingTask = m_Processor.ProcessCommandQueueAsync(commandQueue, serviceCancellationTokenSource.Token);

            // Cancel the service after a short delay to allow processing to start
            await Task.Delay(100);
            serviceCancellationTokenSource.Cancel();

            await processingTask;

            // Assert
            var stats = m_Processor.GetPerformanceStats();
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
            var currentCommand = m_Processor.GetCurrentCommand();

            // Assert
            Assert.Null(currentCommand);
        }

        [Fact]
        public void GetPerformanceStats_Initially_ReturnsZeroStats()
        {
            // Act
            var stats = m_Processor.GetPerformanceStats();

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

            m_ActiveCommands["cmd-1"] = oldCommand;

            // Act
            m_Processor.TriggerCleanup();

            // Assert
            Assert.False(m_ActiveCommands.ContainsKey("cmd-1"));
        }

        [Fact]
        public void TriggerCleanup_KeepsRecentCommands()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var recentCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow, completionSource, cancellationTokenSource, CommandState.Completed);

            m_ActiveCommands["cmd-1"] = recentCommand;

            // Act
            m_Processor.TriggerCleanup();

            // Assert
            Assert.True(m_ActiveCommands.ContainsKey("cmd-1"));
        }

        [Fact]
        public void TriggerCleanup_KeepsExecutingCommands()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var executingCommand = new QueuedCommand("cmd-1", "!analyze -v", DateTime.UtcNow.AddHours(-2), completionSource, cancellationTokenSource, CommandState.Executing);

            m_ActiveCommands["cmd-1"] = executingCommand;

            // Act
            m_Processor.TriggerCleanup();

            // Assert
            Assert.True(m_ActiveCommands.ContainsKey("cmd-1"));
        }

        [Fact]
        public void Dispose_DisposesResources()
        {
            // Act
            m_Processor.Dispose();

            // Assert
            // Should not throw
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            m_Processor.Dispose();
            m_Processor.Dispose(); // Should not throw
        }

        public void Dispose()
        {
            m_Processor?.Dispose();
            m_RealisticCdbSession?.Dispose();
        }
    }
}