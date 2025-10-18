using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Debugger;
using System.Collections.Concurrent;

namespace mcp_nexus_tests.CommandQueue.Core
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

        public void Dispose()
        {
            m_CommandQueue?.Dispose();
            m_ProcessingCts?.Dispose();
            m_ResultCache?.Dispose();
        }
    }
}
