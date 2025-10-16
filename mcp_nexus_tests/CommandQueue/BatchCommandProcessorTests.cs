using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
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
            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object);

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
                m_MockOptions.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                null!,
                m_MockOptions.Object));
        }

        [Fact]
        public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                null!));
        }

        #endregion

        #region ProcessCommandAsync Tests

        [Fact]
        public async Task ProcessCommandAsync_WithNullCommand_ShouldThrowArgumentNullException()
        {
            // Arrange
            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => processor.ProcessCommandAsync(null!));
        }

        [Fact]
        public async Task ProcessCommandAsync_WithExcludedCommand_ShouldExecuteImmediately()
        {
            // Arrange
            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object);

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
            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object);

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
            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object);

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

            // Wait a bit for batch processing
            await Task.Delay(100);

            // Assert
            m_MockCdbSession.Verify(x => x.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object);

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
            var processor = new BatchCommandProcessor(
                m_MockCdbSession.Object,
                m_ResultCache,
                m_MockLogger.Object,
                m_MockOptions.Object);

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

        #region IDisposable Implementation

        public void Dispose()
        {
            m_ResultCache?.Dispose();
        }

        #endregion
    }
}
