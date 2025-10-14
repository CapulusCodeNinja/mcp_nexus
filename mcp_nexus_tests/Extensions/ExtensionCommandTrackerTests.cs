using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.CommandQueue;
using mcp_nexus.Extensions;
using Xunit;

namespace mcp_nexus_tests.Extensions
{
    /// <summary>
    /// Tests for the ExtensionCommandTracker class.
    /// </summary>
    public class ExtensionCommandTrackerTests
    {
        private readonly Mock<ILogger<ExtensionCommandTracker>> m_MockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionCommandTrackerTests"/> class.
        /// </summary>
        public ExtensionCommandTrackerTests()
        {
            m_MockLogger = new Mock<ILogger<ExtensionCommandTracker>>();
        }

        [Fact]
        public void Constructor_WithValidLogger_Succeeds()
        {
            // Act
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);

            // Assert
            Assert.NotNull(tracker);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ExtensionCommandTracker(null!));
        }

        [Fact]
        public void TrackExtension_CreatesCommandInfo()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            var sessionId = "session-1";
            var extensionName = "test_extension";
            var parameters = new { param1 = "value1" };

            // Act
            tracker.TrackExtension(commandId, sessionId, extensionName, parameters);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.Equal(commandId, commandInfo.Id);
            Assert.Equal(sessionId, commandInfo.SessionId);
            Assert.Equal(extensionName, commandInfo.ExtensionName);
            Assert.Equal(CommandState.Queued, commandInfo.State);
        }

        [Fact]
        public void TrackExtension_WithNullParameters_Succeeds()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            var sessionId = "session-1";
            var extensionName = "test_extension";

            // Act
            tracker.TrackExtension(commandId, sessionId, extensionName, null);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.Null(commandInfo.Parameters);
        }

        [Fact]
        public void UpdateState_ChangesCommandState()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);

            // Act
            tracker.UpdateState(commandId, CommandState.Executing);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.Equal(CommandState.Executing, commandInfo.State);
        }

        [Fact]
        public void UpdateState_ToExecuting_SetsStartedAt()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);

            // Act
            tracker.UpdateState(commandId, CommandState.Executing);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.True(commandInfo.StartedAt.HasValue);
        }

        [Fact]
        public void UpdateState_ToCompleted_SetsCompletedAt()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);
            tracker.UpdateState(commandId, CommandState.Executing);

            // Act
            tracker.UpdateState(commandId, CommandState.Completed);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.NotNull(commandInfo.CompletedAt);
            Assert.True(commandInfo.IsCompleted);
        }

        [Fact]
        public void UpdateState_WithNonexistentCommand_DoesNotThrow()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);

            // Act & Assert - Should not throw
            tracker.UpdateState("nonexistent-command", CommandState.Completed);
        }

        [Fact]
        public void UpdateProgress_SetsProgressMessage()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);
            var progressMessage = "Processing step 1 of 3";

            // Act
            tracker.UpdateProgress(commandId, progressMessage);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.Equal(progressMessage, commandInfo.ProgressMessage);
        }

        [Fact]
        public void UpdateProgress_WithNonexistentCommand_DoesNotThrow()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);

            // Act & Assert - Should not throw
            tracker.UpdateProgress("nonexistent-command", "Progress message");
        }

        [Fact]
        public void IncrementCallbackCount_IncrementsCounter()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);

            // Act
            tracker.IncrementCallbackCount(commandId);
            tracker.IncrementCallbackCount(commandId);
            tracker.IncrementCallbackCount(commandId);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.Equal(3, commandInfo.CallbackCount);
        }

        [Fact]
        public void IncrementCallbackCount_WithNonexistentCommand_DoesNotThrow()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);

            // Act & Assert - Should not throw
            tracker.IncrementCallbackCount("nonexistent-command");
        }

        [Fact]
        public void StoreResult_CreatesCommandResult()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);

            var result = new ExtensionResult
            {
                Success = true,
                Output = "Test output",
                Error = null,
                ExitCode = 0,
                ExecutionTime = TimeSpan.FromSeconds(5)
            };

            // Act
            tracker.StoreResult(commandId, result);
            var commandResult = tracker.GetCommandResult(commandId);

            // Assert
            Assert.NotNull(commandResult);
            Assert.Equal("Test output", commandResult.Output);
            Assert.True(commandResult.IsSuccess);
        }

        [Fact]
        public void StoreResult_WithFailure_StoresError()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);

            var result = new ExtensionResult
            {
                Success = false,
                Output = string.Empty,
                Error = "Extension failed",
                ExitCode = 1,
                ExecutionTime = TimeSpan.FromSeconds(2)
            };

            // Act
            tracker.StoreResult(commandId, result);
            var commandResult = tracker.GetCommandResult(commandId);

            // Assert
            Assert.NotNull(commandResult);
            Assert.False(commandResult.IsSuccess);
            Assert.Equal("Extension failed", commandResult.ErrorMessage);
        }

        [Fact]
        public void GetCommandInfo_WithNonexistentCommand_ReturnsNull()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);

            // Act
            var commandInfo = tracker.GetCommandInfo("nonexistent-command");

            // Assert
            Assert.Null(commandInfo);
        }

        [Fact]
        public void GetCommandResult_WithNonexistentCommand_ReturnsNull()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);

            // Act
            var commandResult = tracker.GetCommandResult("nonexistent-command");

            // Assert
            Assert.Null(commandResult);
        }

        [Fact]
        public void ExtensionCommandInfo_ImplementsICommandInfo()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);

            // Act
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Assert
            Assert.NotNull(commandInfo);
            Assert.IsAssignableFrom<ICommandInfo>(commandInfo);
            Assert.Equal(commandId, commandInfo.CommandId);
            // QueueTime should be set when command is tracked
            Assert.True(commandInfo.QueueTime > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public void ExtensionCommandInfo_UpdateTiming_UpdatesTimingValues()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Act
            commandInfo!.UpdateTiming(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(10), commandInfo.Elapsed);
            Assert.Equal(TimeSpan.FromSeconds(5), commandInfo.Remaining);
        }

        [Fact]
        public void ExtensionCommandInfo_MarkCompleted_MarksAsCompleted()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Act
            commandInfo!.MarkCompleted();

            // Assert
            Assert.True(commandInfo.IsCompleted);
            Assert.True(commandInfo.CompletedAt.HasValue);
        }

        [Fact]
        public void ExtensionCommandInfo_UpdateQueuePosition_UpdatesPosition()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var commandId = "ext-test-123";
            tracker.TrackExtension(commandId, "session-1", "test_extension", null);
            var commandInfo = tracker.GetCommandInfo(commandId);

            // Act
            commandInfo!.UpdateQueuePosition(5);

            // Assert
            Assert.Equal(5, commandInfo.QueuePosition);
        }

        [Fact]
        public async Task ConcurrentOperations_ThreadSafe()
        {
            // Arrange
            var tracker = new ExtensionCommandTracker(m_MockLogger.Object);
            var tasks = new List<Task>();

            // Act - Perform concurrent operations
            for (int i = 0; i < 100; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    var commandId = $"ext-{index}";
                    tracker.TrackExtension(commandId, $"session-{index % 10}", $"extension-{index}", null);
                    tracker.UpdateState(commandId, CommandState.Executing);
                    tracker.IncrementCallbackCount(commandId);
                    tracker.UpdateProgress(commandId, $"Progress {index}");
                }));
            }

            await Task.WhenAll([.. tasks]);

            // Assert - All commands should be tracked
            for (int i = 0; i < 100; i++)
            {
                var commandInfo = tracker.GetCommandInfo($"ext-{i}");
                Assert.NotNull(commandInfo);
                Assert.Equal(CommandState.Executing, commandInfo.State);
            }
        }
    }
}

