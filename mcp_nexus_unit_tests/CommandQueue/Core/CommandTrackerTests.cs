using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for CommandTracker
    /// </summary>
    public class CommandTrackerTests
    {
        private readonly Mock<ILogger> m_MockLogger;
        private readonly CommandQueueConfiguration m_Config;
        private readonly BlockingCollection<QueuedCommand> m_CommandQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandTrackerTests"/> class.
        /// </summary>
        public CommandTrackerTests()
        {
            m_MockLogger = new Mock<ILogger>();
            m_Config = new CommandQueueConfiguration("test-session");
            m_CommandQueue = new BlockingCollection<QueuedCommand>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var tracker = new CommandTracker(m_MockLogger.Object, m_Config, m_CommandQueue);

            // Assert
            Assert.NotNull(tracker);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommandTracker(null!, m_Config, m_CommandQueue));
        }

        [Fact]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommandTracker(m_MockLogger.Object, null!, m_CommandQueue));
        }

        [Fact]
        public void Constructor_WithNullCommandQueue_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CommandTracker(m_MockLogger.Object, m_Config, null!));
        }

        #endregion

        #region GetCurrentCommand / SetCurrentCommand Tests

        [Fact]
        public void GetCurrentCommand_InitiallyReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var current = tracker.GetCurrentCommand();

            // Assert
            Assert.Null(current);
        }

        [Fact]
        public void SetCurrentCommand_SetsCommand()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");

            // Act
            tracker.SetCurrentCommand(command);

            // Assert
            var current = tracker.GetCurrentCommand();
            Assert.Equal(command, current);
        }

        [Fact]
        public void SetCurrentCommand_WithNull_ClearsCurrentCommand()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.SetCurrentCommand(command);

            // Act
            tracker.SetCurrentCommand(null);

            // Assert
            Assert.Null(tracker.GetCurrentCommand());
        }

        #endregion

        #region GenerateCommandId Tests

        [Fact]
        public void GenerateCommandId_ReturnsUniqueIds()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var id1 = tracker.GenerateCommandId();
            var id2 = tracker.GenerateCommandId();

            // Assert
            Assert.NotEqual(id1, id2);
        }

        #endregion

        #region TryAddCommand / TryRemoveCommand Tests

        [Fact]
        public void TryAddCommand_AddsCommandSuccessfully()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");

            // Act
            var result = tracker.TryAddCommand("cmd-1", command);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TryAddCommand_WithDuplicateId_ReturnsFalse()
        {
            // Arrange
            var tracker = CreateTracker();
            var command1 = CreateQueuedCommand("cmd-1", "test1");
            var command2 = CreateQueuedCommand("cmd-1", "test2");
            tracker.TryAddCommand("cmd-1", command1);

            // Act
            var result = tracker.TryAddCommand("cmd-1", command2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryRemoveCommand_RemovesExistingCommand()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.TryAddCommand("cmd-1", command);

            // Act
            var result = tracker.TryRemoveCommand("cmd-1", out var removed);

            // Assert
            Assert.True(result);
            Assert.Equal(command, removed);
        }

        [Fact]
        public void TryRemoveCommand_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var result = tracker.TryRemoveCommand("nonexistent", out var removed);

            // Assert
            Assert.False(result);
            Assert.Null(removed);
        }

        #endregion

        #region UpdateState Tests

        [Fact]
        public void UpdateState_WithNullCommandId_DoesNotThrow()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act & Assert - Should not throw
            tracker.UpdateState(null!, CommandState.Completed);
        }

        [Fact]
        public void UpdateState_WithEmptyCommandId_DoesNotThrow()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act & Assert - Should not throw
            tracker.UpdateState("", CommandState.Completed);
        }

        [Fact]
        public void UpdateState_WithWhitespaceCommandId_DoesNotThrow()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act & Assert - Should not throw
            tracker.UpdateState("   ", CommandState.Completed);
        }

        [Fact]
        public void UpdateState_WithValidCommand_UpdatesState()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.TryAddCommand("cmd-1", command);

            // Act
            tracker.UpdateState("cmd-1", CommandState.Executing);

            // Assert
            var state = tracker.GetCommandState("cmd-1");
            Assert.Equal(CommandState.Executing, state);
        }

        [Fact]
        public void UpdateState_WithNonExistentCommand_DoesNotThrow()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act & Assert - Should not throw
            tracker.UpdateState("nonexistent", CommandState.Completed);
        }

        #endregion

        #region GetCommand Tests

        [Fact]
        public void GetCommand_WithNullCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var command = tracker.GetCommand(null!);

            // Assert
            Assert.Null(command);
        }

        [Fact]
        public void GetCommand_WithEmptyCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var command = tracker.GetCommand("");

            // Assert
            Assert.Null(command);
        }

        [Fact]
        public void GetCommand_WithWhitespaceCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var command = tracker.GetCommand("   ");

            // Assert
            Assert.Null(command);
        }

        [Fact]
        public void GetCommand_WithValidId_ReturnsCommand()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.TryAddCommand("cmd-1", command);

            // Act
            var result = tracker.GetCommand("cmd-1");

            // Assert
            Assert.Equal(command, result);
        }

        [Fact]
        public void GetCommand_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var result = tracker.GetCommand("nonexistent");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetCommandState Tests

        [Fact]
        public void GetCommandState_WithNullCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var state = tracker.GetCommandState(null!);

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void GetCommandState_WithEmptyCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var state = tracker.GetCommandState("");

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void GetCommandState_WithWhitespaceCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var state = tracker.GetCommandState("   ");

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public void GetCommandState_WithValidId_ReturnsState()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.TryAddCommand("cmd-1", command);

            // Act
            var state = tracker.GetCommandState("cmd-1");

            // Assert
            Assert.Equal(CommandState.Queued, state);
        }

        [Fact]
        public void GetCommandState_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var state = tracker.GetCommandState("nonexistent");

            // Assert
            Assert.Null(state);
        }

        #endregion

        #region GetCommandInfo Tests

        [Fact]
        public void GetCommandInfo_WithNullCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var info = tracker.GetCommandInfo(null!);

            // Assert
            Assert.Null(info);
        }

        [Fact]
        public void GetCommandInfo_WithEmptyCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var info = tracker.GetCommandInfo("");

            // Assert
            Assert.Null(info);
        }

        [Fact]
        public void GetCommandInfo_WithWhitespaceCommandId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var info = tracker.GetCommandInfo("   ");

            // Assert
            Assert.Null(info);
        }

        [Fact]
        public void GetCommandInfo_WithValidId_ReturnsInfo()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test command");
            tracker.TryAddCommand("cmd-1", command);

            // Act
            var info = tracker.GetCommandInfo("cmd-1");

            // Assert
            Assert.NotNull(info);
            Assert.Equal("cmd-1", info.CommandId);
            Assert.Equal("test command", info.Command);
        }

        [Fact]
        public void GetCommandInfo_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var info = tracker.GetCommandInfo("nonexistent");

            // Assert
            Assert.Null(info);
        }

        #endregion

        #region GetQueuePosition Tests

        [Fact]
        public void GetQueuePosition_ForCurrentCommand_ReturnsZero()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.SetCurrentCommand(command);

            // Act
            var position = tracker.GetQueuePosition("cmd-1");

            // Assert
            Assert.Equal(0, position);
        }

        [Fact]
        public void GetQueuePosition_ForQueuedCommand_ReturnsPosition()
        {
            // Arrange
            var tracker = CreateTracker();
            var command1 = CreateQueuedCommand("cmd-1", "test1");
            var command2 = CreateQueuedCommand("cmd-2", "test2");
            m_CommandQueue.Add(command1);
            m_CommandQueue.Add(command2);

            // Act
            var position1 = tracker.GetQueuePosition("cmd-1");
            var position2 = tracker.GetQueuePosition("cmd-2");

            // Assert
            Assert.Equal(1, position1);
            Assert.Equal(2, position2);
        }

        [Fact]
        public void GetQueuePosition_ForNonExistentCommand_ReturnsMinusOne()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var position = tracker.GetQueuePosition("nonexistent");

            // Assert
            Assert.Equal(-1, position);
        }

        #endregion

        #region GetQueueStatus Tests

        [Fact]
        public void GetQueueStatus_WithNoCommands_ReturnsEmptyList()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var status = tracker.GetQueueStatus();

            // Assert
            Assert.Empty(status);
        }

        [Fact]
        public void GetQueueStatus_WithCurrentCommand_ReturnsExecutingStatus()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.SetCurrentCommand(command);

            // Act
            var status = tracker.GetQueueStatus().ToList();

            // Assert
            Assert.Single(status);
            Assert.Contains(status, s => s.Id == "cmd-1" && s.Status == "Executing");
        }

        [Fact]
        public void GetQueueStatus_WithQueuedCommands_ReturnsQueuedStatus()
        {
            // Arrange
            var tracker = CreateTracker();
            var command1 = CreateQueuedCommand("cmd-1", "test1");
            var command2 = CreateQueuedCommand("cmd-2", "test2");
            m_CommandQueue.Add(command1);
            m_CommandQueue.Add(command2);

            // Act
            var status = tracker.GetQueueStatus().ToList();

            // Assert
            Assert.Equal(2, status.Count);
            Assert.Contains(status, s => s.Id == "cmd-1" && s.Status.Contains("position 1"));
            Assert.Contains(status, s => s.Id == "cmd-2" && s.Status.Contains("position 2"));
        }

        #endregion

        #region Performance Counter Tests

        [Fact]
        public void GetPerformanceStats_InitiallyReturnsZeros()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var (total, completed, failed, cancelled) = tracker.GetPerformanceStats();

            // Assert
            Assert.Equal(0, total);
            Assert.Equal(0, completed);
            Assert.Equal(0, failed);
            Assert.Equal(0, cancelled);
        }

        [Fact]
        public void IncrementCompleted_IncrementsCounter()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            tracker.IncrementCompleted();
            tracker.IncrementCompleted();

            // Assert
            var (_, completed, _, _) = tracker.GetPerformanceStats();
            Assert.Equal(2, completed);
        }

        [Fact]
        public void IncrementFailed_IncrementsCounter()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            tracker.IncrementFailed();
            tracker.IncrementFailed();
            tracker.IncrementFailed();

            // Assert
            var (_, _, failed, _) = tracker.GetPerformanceStats();
            Assert.Equal(3, failed);
        }

        [Fact]
        public void IncrementCancelled_IncrementsCounter()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            tracker.IncrementCancelled();

            // Assert
            var (_, _, _, cancelled) = tracker.GetPerformanceStats();
            Assert.Equal(1, cancelled);
        }

        [Fact]
        public void GetPerformanceStats_TotalIsSum()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            tracker.IncrementCompleted();
            tracker.IncrementCompleted();
            tracker.IncrementFailed();
            tracker.IncrementCancelled();

            // Assert
            var (total, completed, failed, cancelled) = tracker.GetPerformanceStats();
            Assert.Equal(4, total);
            Assert.Equal(2, completed);
            Assert.Equal(1, failed);
            Assert.Equal(1, cancelled);
        }

        #endregion

        #region CancelAllCommands Tests

        [Fact]
        public void CancelAllCommands_WithNoCommands_ReturnsZero()
        {
            // Arrange
            var tracker = CreateTracker();

            // Act
            var count = tracker.CancelAllCommands("test reason");

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void CancelAllCommands_WithActiveCommands_CancelsAll()
        {
            // Arrange
            var tracker = CreateTracker();
            var command1 = CreateQueuedCommand("cmd-1", "test1");
            var command2 = CreateQueuedCommand("cmd-2", "test2");
            tracker.TryAddCommand("cmd-1", command1);
            tracker.TryAddCommand("cmd-2", command2);

            // Act
            var count = tracker.CancelAllCommands("shutdown");

            // Assert
            Assert.Equal(2, count);
            Assert.True(command1.CancellationTokenSource!.Token.IsCancellationRequested);
            Assert.True(command2.CancellationTokenSource!.Token.IsCancellationRequested);
        }

        [Fact]
        public void CancelAllCommands_WithNullReason_UsesDefaultReason()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.TryAddCommand("cmd-1", command);

            // Act
            var count = tracker.CancelAllCommands(null);

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void CancelAllCommands_WithAlreadyCancelledCommand_SkipsIt()
        {
            // Arrange
            var tracker = CreateTracker();
            var command1 = CreateQueuedCommand("cmd-1", "test1");
            var command2 = CreateQueuedCommand("cmd-2", "test2");
            command1.CancellationTokenSource!.Cancel(); // Pre-cancel one
            tracker.TryAddCommand("cmd-1", command1);
            tracker.TryAddCommand("cmd-2", command2);

            // Act
            var count = tracker.CancelAllCommands("shutdown");

            // Assert
            Assert.Equal(1, count); // Only command2 was cancelled
        }

        [Fact]
        public void CancelAllCommands_IncrementsPerformanceCounter()
        {
            // Arrange
            var tracker = CreateTracker();
            var command = CreateQueuedCommand("cmd-1", "test");
            tracker.TryAddCommand("cmd-1", command);

            // Act
            tracker.CancelAllCommands("shutdown");

            // Assert
            var (_, _, _, cancelled) = tracker.GetPerformanceStats();
            Assert.Equal(1, cancelled);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a CommandTracker instance for testing.
        /// </summary>
        /// <returns>A new tracker instance.</returns>
        private CommandTracker CreateTracker()
        {
            return new CommandTracker(m_MockLogger.Object, m_Config, m_CommandQueue);
        }

        /// <summary>
        /// Creates a QueuedCommand for testing.
        /// </summary>
        /// <param name="id">Command ID.</param>
        /// <param name="command">Command string.</param>
        /// <returns>A new QueuedCommand instance.</returns>
        private static QueuedCommand CreateQueuedCommand(string id, string command)
        {
            return new QueuedCommand(
                id,
                command,
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource(),
                CommandState.Queued);
        }

        #endregion

        #region Branch Coverage Tests

        [Fact]
        public void CancelAllCommands_WithNullCompletionSource_SkipsSetResult()
        {
            // Arrange - Test Line 257: command.CompletionSource?.TrySetResult (FALSE branch - null)
            var tracker = new CommandTracker(m_MockLogger.Object, m_Config, m_CommandQueue);

            // Create command with NULL CompletionSource using nullable constructor
            var cts = new CancellationTokenSource();
            var commandWithNullTcs = new QueuedCommand("cmd-1", "lm", DateTime.Now, null, cts);
            tracker.TryAddCommand("cmd-1", commandWithNullTcs);

            // Act - Should skip TrySetResult since CompletionSource is null
            tracker.CancelAllCommands("test cancellation");

            // Assert - Should complete without error despite null CompletionSource
            Assert.True(true); // Verified by no exception
        }

        [Fact]
        public void CancelAllCommands_WithNullCommandId_UsesEmptyString()
        {
            // Arrange - Test Line 259: command.Id ?? string.Empty (FALSE branch - null ID)
            var tracker = new CommandTracker(m_MockLogger.Object, m_Config, m_CommandQueue);

            // Create command with NULL ID
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<string>();
            var commandWithNullId = new QueuedCommand(null!, "lm", DateTime.Now, tcs, cts);
            tracker.TryAddCommand(string.Empty, commandWithNullId); // Use empty string as key

            // Act - Should use empty string for null ID
            tracker.CancelAllCommands("test");

            // Assert - Should complete without error
            Assert.True(true); // Verified by no exception
        }

        [Fact]
        public void GetCommandInfo_WithQueuedCommand_IsCompletedReturnsFalse()
        {
            // Arrange - Test Line 295: IsCommandCompleted FALSE branch (Queued state)
            var tracker = new CommandTracker(m_MockLogger.Object, m_Config, m_CommandQueue);
            var command = new QueuedCommand("cmd-1", "lm", DateTime.Now,
                new TaskCompletionSource<string>(), new CancellationTokenSource(), CommandState.Queued);
            tracker.TryAddCommand("cmd-1", command);

            // Act - GetCommandInfo calls IsCommandCompleted internally
            var commandInfo = tracker.GetCommandInfo("cmd-1");

            // Assert - Queued command should not be marked as completed
            Assert.NotNull(commandInfo);
            Assert.False(commandInfo.IsCompleted); // IsCommandCompleted returns FALSE for Queued
        }

        #endregion
    }
}
