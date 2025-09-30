using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.CommandQueue;

namespace mcp_nexus_tests.CommandQueue
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
            var queueTime = DateTime.UtcNow;
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
            var queueTime = DateTime.UtcNow;
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
            var queueTime = DateTime.UtcNow;
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
            var queueTime = DateTime.UtcNow;
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
            var queueTime = DateTime.UtcNow;
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
            var queueTime = DateTime.UtcNow;
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
            var queueTime = DateTime.UtcNow;
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
            var queueTime = DateTime.UtcNow;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            var queuedCommand1 = new QueuedCommand(id1, command, queueTime, completionSource, cancellationTokenSource);
            var queuedCommand2 = new QueuedCommand(id2, command, queueTime, completionSource, cancellationTokenSource);

            // Assert
            Assert.NotEqual(queuedCommand1.GetHashCode(), queuedCommand2.GetHashCode());
        }

        [Fact]
        public void QueuedCommand_WithNullId_ThrowsArgumentNullException()
        {
            // Arrange
            var command = "!analyze -v";
            var queueTime = DateTime.UtcNow;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new QueuedCommand(null!, command, queueTime, completionSource, cancellationTokenSource));
        }

        [Fact]
        public void QueuedCommand_WithNullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var id = "cmd-123";
            var queueTime = DateTime.UtcNow;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new QueuedCommand(id, null!, queueTime, completionSource, cancellationTokenSource));
        }

        [Fact]
        public void QueuedCommand_WithNullCompletionSource_ThrowsArgumentNullException()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.UtcNow;
            var cancellationTokenSource = new CancellationTokenSource();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new QueuedCommand(id, command, queueTime, null!, cancellationTokenSource));
        }

        [Fact]
        public void QueuedCommand_WithNullCancellationTokenSource_ThrowsArgumentNullException()
        {
            // Arrange
            var id = "cmd-123";
            var command = "!analyze -v";
            var queueTime = DateTime.UtcNow;
            var completionSource = new TaskCompletionSource<string>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new QueuedCommand(id, command, queueTime, completionSource, null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void QueuedCommand_WithEmptyId_ThrowsArgumentException(string id)
        {
            // Arrange
            var command = "!analyze -v";
            var queueTime = DateTime.UtcNow;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void QueuedCommand_WithEmptyCommand_ThrowsArgumentException(string command)
        {
            // Arrange
            var id = "cmd-123";
            var queueTime = DateTime.UtcNow;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new QueuedCommand(id, command, queueTime, completionSource, cancellationTokenSource));
        }
    }
}