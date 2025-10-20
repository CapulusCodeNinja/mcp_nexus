using mcp_nexus.CommandQueue.Core;
using System;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_unit_tests.CommandQueue.Core
{
    public class CommandInfoFactoryTests
    {
        #region CreateCommandInfo Tests

        [Fact]
        public void CreateCommandInfo_WithAllParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;
            var queuePosition = 5;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime, queuePosition);

            // Assert
            Assert.Equal(commandId, result.CommandId);
            Assert.Equal(command, result.Command);
            Assert.Equal(state, result.State);
            Assert.Equal(queueTime, result.QueueTime);
            Assert.Equal(queuePosition, result.QueuePosition);
            Assert.False(result.IsCompleted);
            Assert.Equal(TimeSpan.Zero, result.Elapsed);
            Assert.Equal(TimeSpan.Zero, result.Remaining);
        }

        [Fact]
        public void CreateCommandInfo_WithDefaultQueuePosition_SetsZero()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-456";
            var command = "test-command";
            var state = CommandState.Executing;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(0, result.QueuePosition);
        }

        [Fact]
        public void CreateCommandInfo_WithNullCommandId_ThrowsArgumentNullException()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            string? commandId = null;
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateCommandInfo(commandId!, command, state, queueTime));
        }

        [Fact]
        public void CreateCommandInfo_WithNullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            string? command = null;
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateCommandInfo(commandId, command!, state, queueTime));
        }

        [Fact]
        public void CreateCommandInfo_WithEmptyCommandId_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = string.Empty;
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(string.Empty, result.CommandId);
            Assert.Equal(command, result.Command);
        }

        [Fact]
        public void CreateCommandInfo_WithEmptyCommand_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = string.Empty;
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(commandId, result.CommandId);
            Assert.Equal(string.Empty, result.Command);
        }

        [Fact]
        public void CreateCommandInfo_WithDifferentStates_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = "test-command";
            var queueTime = DateTime.Now;

            // Act & Assert
            foreach (CommandState state in Enum.GetValues<CommandState>())
            {
                var result = factory.CreateCommandInfo(commandId, command, state, queueTime);
                Assert.Equal(state, result.State);
            }
        }

        [Fact]
        public void CreateCommandInfo_WithNegativeQueuePosition_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;
            var queuePosition = -1;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime, queuePosition);

            // Assert
            Assert.Equal(-1, result.QueuePosition);
        }

        [Fact]
        public void CreateCommandInfo_WithLargeQueuePosition_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;
            var queuePosition = int.MaxValue;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime, queuePosition);

            // Assert
            Assert.Equal(int.MaxValue, result.QueuePosition);
        }

        [Fact]
        public void CreateCommandInfo_WithMinDateTime_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.MinValue;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(DateTime.MinValue, result.QueueTime);
        }

        [Fact]
        public void CreateCommandInfo_WithMaxDateTime_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.MaxValue;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(DateTime.MaxValue, result.QueueTime);
        }

        #endregion

        #region CreateCommandInfoFromQueuedCommand Tests

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithValidQueuedCommand_SetsPropertiesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            var command = "test-command";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var state = CommandState.Executing;
            var queuePosition = 3;

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource, state);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand, queuePosition);

            // Assert
            Assert.Equal(commandId, result.CommandId);
            Assert.Equal(command, result.Command);
            Assert.Equal(state, result.State);
            Assert.Equal(queueTime, result.QueueTime);
            Assert.Equal(queuePosition, result.QueuePosition);
            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithDefaultQueuePosition_SetsZero()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            var command = "test-command";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var state = CommandState.Queued;

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource, state);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(0, result.QueuePosition);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithNullId_UsesEmptyString()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            string? commandId = null;
            var command = "test-command";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(string.Empty, result.CommandId);
            Assert.Equal(command, result.Command);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithNullCommand_UsesEmptyString()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            string? command = null;
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(commandId, result.CommandId);
            Assert.Equal(string.Empty, result.Command);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithNullIdAndCommand_UsesEmptyStrings()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            string? commandId = null;
            string? command = null;
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(string.Empty, result.CommandId);
            Assert.Equal(string.Empty, result.Command);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithDifferentStates_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            var command = "test-command";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            // Act & Assert
            foreach (CommandState state in Enum.GetValues<CommandState>())
            {
                var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource, state);
                var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);
                Assert.Equal(state, result.State);
            }
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithNegativeQueuePosition_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            var command = "test-command";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var state = CommandState.Queued;
            var queuePosition = -5;

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource, state);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand, queuePosition);

            // Assert
            Assert.Equal(-5, result.QueuePosition);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithLargeQueuePosition_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            var command = "test-command";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var state = CommandState.Queued;
            var queuePosition = int.MaxValue;

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource, state);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand, queuePosition);

            // Assert
            Assert.Equal(int.MaxValue, result.QueuePosition);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithMinDateTime_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            var command = "test-command";
            var queueTime = DateTime.MinValue;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var state = CommandState.Queued;

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource, state);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(DateTime.MinValue, result.QueueTime);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithMaxDateTime_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-789";
            var command = "test-command";
            var queueTime = DateTime.MaxValue;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();
            var state = CommandState.Queued;

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource, state);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(DateTime.MaxValue, result.QueueTime);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithNullQueuedCommand_ThrowsNullReferenceException()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            QueuedCommand? queuedCommand = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() =>
                factory.CreateCommandInfoFromQueuedCommand(queuedCommand!));
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void CreateCommandInfo_WithVeryLongCommandId_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = new string('A', 10000);
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(commandId, result.CommandId);
        }

        [Fact]
        public void CreateCommandInfo_WithVeryLongCommand_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = new string('B', 10000);
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(command, result.Command);
        }

        [Fact]
        public void CreateCommandInfo_WithUnicodeCommandId_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "命令-123-测试";
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(commandId, result.CommandId);
        }

        [Fact]
        public void CreateCommandInfo_WithUnicodeCommand_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = "测试命令-你好世界";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(command, result.Command);
        }

        [Fact]
        public void CreateCommandInfo_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123!@#$%^&*()";
            var command = "test-command !@#$%^&*()_+-=[]{}|;':\",./<>?";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result = factory.CreateCommandInfo(commandId, command, state, queueTime);

            // Assert
            Assert.Equal(commandId, result.CommandId);
            Assert.Equal(command, result.Command);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithVeryLongCommandId_UsesEmptyString()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = new string('A', 10000);
            var command = "test-command";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(commandId, result.CommandId);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithVeryLongCommand_UsesEmptyString()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123";
            var command = new string('B', 10000);
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(command, result.Command);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithUnicodeValues_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "命令-123-测试";
            var command = "测试命令-你好世界";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(commandId, result.CommandId);
            Assert.Equal(command, result.Command);
        }

        [Fact]
        public void CreateCommandInfoFromQueuedCommand_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var factory = new CommandInfoFactory();
            var commandId = "cmd-123!@#$%^&*()";
            var command = "test-command !@#$%^&*()_+-=[]{}|;':\",./<>?";
            var queueTime = DateTime.Now;
            var completionSource = new TaskCompletionSource<string>();
            var cancellationTokenSource = new CancellationTokenSource();

            var queuedCommand = new QueuedCommand(commandId, command, queueTime, completionSource, cancellationTokenSource);

            // Act
            var result = factory.CreateCommandInfoFromQueuedCommand(queuedCommand);

            // Assert
            Assert.Equal(commandId, result.CommandId);
            Assert.Equal(command, result.Command);
        }

        #endregion

        #region Factory Pattern Tests

        [Fact]
        public void CommandInfoFactory_IsNotStatic()
        {
            // Arrange & Act
            var factory = new CommandInfoFactory();

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void CommandInfoFactory_CanCreateMultipleInstances()
        {
            // Arrange & Act
            var factory1 = new CommandInfoFactory();
            var factory2 = new CommandInfoFactory();

            // Assert
            Assert.NotSame(factory1, factory2);
        }

        [Fact]
        public void CommandInfoFactory_MultipleInstances_WorkIndependently()
        {
            // Arrange
            var factory1 = new CommandInfoFactory();
            var factory2 = new CommandInfoFactory();
            var commandId1 = "cmd-1";
            var commandId2 = "cmd-2";
            var command = "test-command";
            var state = CommandState.Queued;
            var queueTime = DateTime.Now;

            // Act
            var result1 = factory1.CreateCommandInfo(commandId1, command, state, queueTime);
            var result2 = factory2.CreateCommandInfo(commandId2, command, state, queueTime);

            // Assert
            Assert.Equal(commandId1, result1.CommandId);
            Assert.Equal(commandId2, result2.CommandId);
            Assert.NotEqual(result1.CommandId, result2.CommandId);
        }

        #endregion
    }
}
