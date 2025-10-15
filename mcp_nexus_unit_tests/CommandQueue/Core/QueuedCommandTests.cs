using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    /// <summary>
    /// Tests for QueuedCommand record
    /// </summary>
    public class QueuedCommandTests
    {
        [Fact]
        public void QueuedCommand_Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.Equal(id, queuedCommand.Id);
            Assert.Equal(command, queuedCommand.Command);
            Assert.Equal(queueTime, queuedCommand.QueueTime);
            Assert.Equal(completionSource, queuedCommand.CompletionSource);
            Assert.Equal(cancellationTokenSource, queuedCommand.CancellationTokenSource);
            Assert.Equal(CommandState.Queued, queuedCommand.State);
        }

        [Fact]
        public void QueuedCommand_Constructor_WithCustomState_CreatesInstance()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var state = CommandState.Executing;

            // Act
            var queuedCommand = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource, state);

            // Assert
            Assert.Equal(state, queuedCommand.State);
        }

        [Fact]
        public void QueuedCommand_RecordEquality_WithSameValues_ReturnsTrue()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand1 = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);
            var queuedCommand2 = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.Equal(queuedCommand1, queuedCommand2);
            Assert.True(queuedCommand1 == queuedCommand2);
            Assert.False(queuedCommand1 != queuedCommand2);
        }

        [Fact]
        public void QueuedCommand_RecordEquality_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var id1 = "cmd-123";
            var id2 = "cmd-456";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand1 = new QueuedCommand(id1, command, queueTime, completionSource, cancellationTokenSource);
            var queuedCommand2 = new QueuedCommand(id2, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.NotEqual(queuedCommand1, queuedCommand2);
            Assert.False(queuedCommand1 == queuedCommand2);
            Assert.True(queuedCommand1 != queuedCommand2);
        }

        [Fact]
        public void QueuedCommand_RecordEquality_WithDifferentStates_ReturnsFalse()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand1 = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource, CommandState.Queued);
            var queuedCommand2 = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource, CommandState.Executing);

            // Assert
            Assert.NotEqual(queuedCommand1, queuedCommand2);
        }

        [Fact]
        public void QueuedCommand_ToString_ReturnsFormattedString()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);
            var result = queuedCommand.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(id, result);
            Assert.Contains(command, result);
            Assert.Contains(CommandState.Queued.ToString(), result);
        }

        [Fact]
        public void QueuedCommand_GetHashCode_WithSameValues_ReturnsSameHashCode()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand1 = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);
            var queuedCommand2 = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.Equal(queuedCommand1.GetHashCode(), queuedCommand2.GetHashCode());
        }

        [Fact]
        public void QueuedCommand_GetHashCode_WithDifferentValues_ReturnsDifferentHashCodes()
        {
            // Arrange
            var id1 = "cmd-123";
            var id2 = "cmd-456";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand1 = new QueuedCommand(id1, command, queueTime, completionSource, cancellationTokenSource);
            var queuedCommand2 = new QueuedCommand(id2, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.NotEqual(queuedCommand1.GetHashCode(), queuedCommand2.GetHashCode());
        }

        [Fact]
        public void QueuedCommand_WithNullId_CreatesInstance()
        {
            // Arrange
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand = new QueuedCommand(null!, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.NotNull(queuedCommand);
            Assert.Null(queuedCommand.Id);
        }

        [Fact]
        public void QueuedCommand_WithNullCommand_CreatesInstance()
        {
            // Arrange
            var id = "cmd-123";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand = new QueuedCommand(id, null!, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.NotNull(queuedCommand);
            Assert.Null(queuedCommand.Command);
        }

        [Fact]
        public void QueuedCommand_WithNullCompletionSource_CreatesInstance()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand = new QueuedCommand(id, command, queueTime, null!, cancellationTokenSource);

            // Assert
            Assert.NotNull(queuedCommand);
            Assert.Null(queuedCommand.CompletionSource);
        }

        [Fact]
        public void QueuedCommand_WithNullCancellationTokenSource_CreatesInstance()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();

            // Act
            var queuedCommand = new QueuedCommand(id, command, queueTime, completionSource, null!);

            // Assert
            Assert.NotNull(queuedCommand);
            Assert.Null(queuedCommand.CancellationTokenSource);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void QueuedCommand_WithEmptyId_CreatesInstance(string id)
        {
            // Arrange
            var command = "!analyze -v";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.NotNull(queuedCommand);
            Assert.Equal(id, queuedCommand.Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void QueuedCommand_WithEmptyCommand_CreatesInstance(string command)
        {
            // Arrange
            var id = "cmd-123";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand = new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.NotNull(queuedCommand);
            Assert.Equal(command, queuedCommand.Command);
        }

        #region UpdateState Tests

        [Fact]
        public void UpdateState_WithNewState_UpdatesState()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();

            // Act
            queuedCommand.UpdateState(CommandState.Executing);

            // Assert
            Assert.Equal(CommandState.Executing, queuedCommand.State);
        }

        [Fact]
        public void UpdateState_AfterDisposal_DoesNotUpdate()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();
            queuedCommand.Dispose();

            // Act
            queuedCommand.UpdateState(CommandState.Executing);

            // Assert
            Assert.Equal(CommandState.Queued, queuedCommand.State); // State should remain as initially set
        }

        #endregion

        #region Cancel Tests

        [Fact]
        public void Cancel_SetsStateAndCancelsToken()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();

            // Act
            queuedCommand.Cancel();

            // Assert
            Assert.Equal(CommandState.Cancelled, queuedCommand.State);
            Assert.True(queuedCommand.CancellationTokenSource!.Token.IsCancellationRequested);
        }

        [Fact]
        public void Cancel_AfterDisposal_DoesNotCancel()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();
            queuedCommand.Dispose();

            // Act
            queuedCommand.Cancel();

            // Assert
            Assert.Equal(CommandState.Queued, queuedCommand.State); // State should remain as initially set
        }

        [Fact]
        public void Cancel_WithNullCancellationTokenSource_DoesNotThrow()
        {
            // Arrange
            var queuedCommand = new QueuedCommand("cmd-1", "test", DateTime.Now, null, null);

            // Act & Assert
            queuedCommand.Cancel(); // Should not throw
        }

        #endregion

        #region SetResult Tests

        [Fact]
        public void SetResult_SetsStateAndCompletesTask()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();
            var result = "Test Result";

            // Act
            queuedCommand.SetResult(result);

            // Assert
            Assert.Equal(CommandState.Completed, queuedCommand.State);
            Assert.True(queuedCommand.CompletionSource!.Task.IsCompletedSuccessfully);
        }

        [Fact]
        public void SetResult_AfterDisposal_DoesNotSetResult()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();
            queuedCommand.Dispose();

            // Act
            queuedCommand.SetResult("Test");

            // Assert
            Assert.Equal(CommandState.Queued, queuedCommand.State);
            Assert.False(queuedCommand.CompletionSource!.Task.IsCompleted);
        }

        [Fact]
        public void SetResult_WithNullCompletionSource_DoesNotThrow()
        {
            // Arrange
            var queuedCommand = new QueuedCommand("cmd-1", "test", DateTime.Now, null, null);

            // Act & Assert
            queuedCommand.SetResult("test"); // Should not throw
        }

        #endregion

        #region SetException Tests

        [Fact]
        public void SetException_SetsStateAndException()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();
            var exception = new InvalidOperationException("Test error");

            // Act
            queuedCommand.SetException(exception);

            // Assert
            Assert.Equal(CommandState.Failed, queuedCommand.State);
            Assert.True(queuedCommand.CompletionSource!.Task.IsFaulted);
            Assert.Equal(exception, queuedCommand.CompletionSource.Task.Exception!.InnerException);
        }

        [Fact]
        public void SetException_AfterDisposal_DoesNotSetException()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();
            queuedCommand.Dispose();

            // Act
            queuedCommand.SetException(new Exception("Test"));

            // Assert
            Assert.Equal(CommandState.Queued, queuedCommand.State);
            Assert.False(queuedCommand.CompletionSource!.Task.IsFaulted);
        }

        [Fact]
        public void SetException_WithNullCompletionSource_DoesNotThrow()
        {
            // Arrange
            var queuedCommand = new QueuedCommand("cmd-1", "test", DateTime.Now, null, null);

            // Act & Assert
            queuedCommand.SetException(new Exception("test")); // Should not throw
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_DisposesResources()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();

            // Act
            queuedCommand.Dispose();

            // Assert
            Assert.True(queuedCommand.IsDisposed);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();

            // Act & Assert
            queuedCommand.Dispose();
            queuedCommand.Dispose(); // Should not throw
        }

        #endregion

        #region Equality Edge Cases

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();

            // Act & Assert
            Assert.False(queuedCommand.Equals(null));
        }

        [Fact]
        public void Equals_WithNonQueuedCommand_ReturnsFalse()
        {
            // Arrange
            var queuedCommand = CreateQueuedCommand();

            // Act & Assert
            Assert.False(queuedCommand.Equals("not a queued command"));
        }

        [Fact]
        public void OperatorEquals_WithBothNull_ReturnsTrue()
        {
            // Arrange
            QueuedCommand? left = null;
            QueuedCommand? right = null;

            // Act & Assert
            Assert.True(left == right);
        }

        [Fact]
        public void OperatorEquals_WithLeftNull_ReturnsFalse()
        {
            // Arrange
            QueuedCommand? left = null;
            var right = CreateQueuedCommand();

            // Act & Assert
            Assert.False(left == right);
        }

        [Fact]
        public void OperatorEquals_WithRightNull_ReturnsFalse()
        {
            // Arrange
            var left = CreateQueuedCommand();
            QueuedCommand? right = null;

            // Act & Assert
            Assert.False(left == right);
        }

        [Fact]
        public void OperatorNotEquals_WithBothNull_ReturnsFalse()
        {
            // Arrange
            QueuedCommand? left = null;
            QueuedCommand? right = null;

            // Act & Assert
            Assert.False(left != right);
        }

        #endregion

        #region With Methods Tests

        [Fact]
        public void With_WithNewId_ReturnsNewInstanceWithUpdatedId()
        {
            // Arrange
            var original = CreateQueuedCommand();
            var newId = "new-id";

            // Act
            var updated = original.With(id: newId);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Equal(newId, updated.Id);
            Assert.Equal(original.Command, updated.Command);
        }

        [Fact]
        public void WithState_WithNewState_ReturnsNewInstanceWithUpdatedState()
        {
            // Arrange
            var original = CreateQueuedCommand();

            // Act
            var updated = original.WithState(CommandState.Executing);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Equal(CommandState.Executing, updated.State);
        }

        [Fact]
        public void WithCompletionSource_WithNewCompletionSource_ReturnsNewInstanceWithUpdatedSource()
        {
            // Arrange
            var original = CreateQueuedCommand();
            var newSource = new TaskCompletionSource<string>();

            // Act
            var updated = original.WithCompletionSource(newSource);

            // Assert
            Assert.NotSame(original, updated);
            Assert.Same(newSource, updated.CompletionSource);
        }

        [Fact]
        public void WithState_WithNullFields_UsesDefaultValues()
        {
            // Arrange - Test null-coalescing operators in WithState (lines 236-241)
            var commandWithNulls = new QueuedCommand(null, null, DateTime.Now, null, null);

            // Act - WithState should use ?? operators to handle nulls
            var updated = commandWithNulls.WithState(CommandState.Executing);

            // Assert - Should use empty strings and create new TaskCompletionSource/CancellationTokenSource
            Assert.Equal(string.Empty, updated.Id);
            Assert.Equal(string.Empty, updated.Command);
            Assert.NotNull(updated.CompletionSource);
            Assert.NotNull(updated.CancellationTokenSource);
            Assert.Equal(CommandState.Executing, updated.State);
        }

        [Fact]
        public void WithCompletionSource_WithNullFields_UsesDefaultValues()
        {
            // Arrange - Test null-coalescing operators in WithCompletionSource (lines 250-254)
            var commandWithNulls = new QueuedCommand(null, null, DateTime.Now, null, null);
            var newSource = new TaskCompletionSource<string>();

            // Act - WithCompletionSource should use ?? operators to handle nulls
            var updated = commandWithNulls.WithCompletionSource(newSource);

            // Assert - Should use empty strings and create new CancellationTokenSource
            Assert.Equal(string.Empty, updated.Id);
            Assert.Equal(string.Empty, updated.Command);
            Assert.Same(newSource, updated.CompletionSource);
            Assert.NotNull(updated.CancellationTokenSource);
        }

        [Fact]
        public void With_WithNullFields_UsesDefaultValues()
        {
            // Arrange - Test null-coalescing operators in With (lines 221-225)
            var commandWithNulls = new QueuedCommand(null, null, DateTime.Now, null, null);

            // Act - With() without parameters should use ?? operators to handle nulls
            var updated = commandWithNulls.With();

            // Assert - Should use empty strings and create new instances
            Assert.Equal(string.Empty, updated.Id);
            Assert.Equal(string.Empty, updated.Command);
            Assert.NotNull(updated.CompletionSource);
            Assert.NotNull(updated.CancellationTokenSource);
        }

        #endregion

        #region Helper Methods

        private static QueuedCommand CreateQueuedCommand()
        {
            return new QueuedCommand(
                "cmd-123",
                "test command",
                DateTime.Now,
                new TaskCompletionSource<string>(),
                new CancellationTokenSource());
        }

        #endregion
    }
}