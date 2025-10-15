using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Batching;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Batching
{
    /// <summary>
    /// Unit tests for CommandBatchBuilder
    /// </summary>
    public class CommandBatchBuilderTests
    {
        #region CreateBatchCommand Tests

        [Fact]
        public void CreateBatchCommand_WithValidCommands_ShouldCreateBatchCommand()
        {
            // Arrange
            var builder = new CommandBatchBuilder();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm"),
                CreateTestCommand("cmd-2", "!threads"),
                CreateTestCommand("cmd-3", "!peb")
            };

            // Act
            var batchCommand = builder.CreateBatchCommand(commands);

            // Assert
            Assert.NotNull(batchCommand);
            Assert.Contains("lm", batchCommand);
            Assert.Contains("!threads", batchCommand);
            Assert.Contains("!peb", batchCommand);
            Assert.Contains(CdbSentinels.CommandSeparator, batchCommand);
        }

        [Fact]
        public void CreateBatchCommand_WithNullCommands_ShouldThrowArgumentNullException()
        {
            // Arrange
            var builder = new CommandBatchBuilder();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.CreateBatchCommand(null!));
        }

        [Fact]
        public void CreateBatchCommand_WithEmptyCommands_ShouldThrowArgumentException()
        {
            // Arrange
            var builder = new CommandBatchBuilder();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.CreateBatchCommand(new List<QueuedCommand>()));
        }

        [Fact]
        public void CreateBatchCommand_WithSingleCommand_ShouldCreateValidBatch()
        {
            // Arrange
            var builder = new CommandBatchBuilder();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm")
            };

            // Act
            var batchCommand = builder.CreateBatchCommand(commands);

            // Assert
            Assert.NotNull(batchCommand);
            Assert.Contains("lm", batchCommand);
            Assert.Contains(CdbSentinels.CommandSeparator, batchCommand);
        }

        [Fact]
        public void CreateBatchCommand_WithEmptyCommand_ShouldSkipEmptyCommand()
        {
            // Arrange
            var builder = new CommandBatchBuilder();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm"),
                CreateTestCommand("cmd-2", ""),
                CreateTestCommand("cmd-3", "   "),
                CreateTestCommand("cmd-4", "!threads")
            };

            // Act
            var batchCommand = builder.CreateBatchCommand(commands);

            // Assert
            Assert.NotNull(batchCommand);
            Assert.Contains("lm", batchCommand);
            Assert.Contains("!threads", batchCommand);
            // Verify that empty commands were skipped by checking that only 2 commands are in the batch
            Assert.DoesNotContain("CMD-2", batchCommand);
            Assert.DoesNotContain("CMD-3", batchCommand);
            Assert.Contains("CMD-1", batchCommand);
            Assert.Contains("CMD-4", batchCommand);
        }

        [Fact]
        public void CreateBatchCommand_ShouldIncludeUniqueSeparators()
        {
            // Arrange
            var builder = new CommandBatchBuilder();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm"),
                CreateTestCommand("cmd-2", "!threads")
            };

            // Act
            var batchCommand = builder.CreateBatchCommand(commands);

            // Assert
            Assert.NotNull(batchCommand);

            // Should contain separators for each command
            var separatorCount = batchCommand.Split(CdbSentinels.CommandSeparator).Length - 1;
            Assert.Equal(4, separatorCount); // 2 commands * 2 separators each
        }

        [Fact]
        public void CreateBatchCommand_ShouldUseSemicolonSyntax()
        {
            // Arrange
            var builder = new CommandBatchBuilder();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm"),
                CreateTestCommand("cmd-2", "!threads")
            };

            // Act
            var batchCommand = builder.CreateBatchCommand(commands);

            // Assert - should use semicolons, not newlines, and no batch start/end wrappers
            Assert.DoesNotContain("\n", batchCommand);
            Assert.Contains("; ", batchCommand);
            Assert.DoesNotContain("MCP_NEXUS_BATCH_START", batchCommand);
            Assert.DoesNotContain("MCP_NEXUS_BATCH_END", batchCommand);
        }

        [Fact]
        public void CreateBatchCommand_ShouldUseUppercaseCommandIds()
        {
            // Arrange
            var builder = new CommandBatchBuilder();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-abc-123", "lm")
            };

            // Act
            var batchCommand = builder.CreateBatchCommand(commands);

            // Assert - command IDs should be uppercase
            Assert.Contains("MCP_NEXUS_CMD_SEP_CMD-ABC-123_START", batchCommand);
            Assert.Contains("MCP_NEXUS_CMD_SEP_CMD-ABC-123_END", batchCommand);
            Assert.DoesNotContain("cmd-abc-123", batchCommand);
        }

        [Fact]
        public void CreateBatchCommand_ShouldProduceValidCdbSyntax()
        {
            // Arrange
            var builder = new CommandBatchBuilder();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm"),
                CreateTestCommand("cmd-2", "!threads")
            };

            // Act
            var batchCommand = builder.CreateBatchCommand(commands);

            // Assert - verify expected per-command separators with semicolons and no batch wrappers
            Assert.StartsWith($".echo {CdbSentinels.CommandSeparator}_CMD-1_START; lm; ", batchCommand);
            Assert.Contains($".echo {CdbSentinels.CommandSeparator}_CMD-1_END; ", batchCommand);
            Assert.Contains($".echo {CdbSentinels.CommandSeparator}_CMD-2_START; !threads; ", batchCommand);
            Assert.EndsWith($".echo {CdbSentinels.CommandSeparator}_CMD-2_END", batchCommand);
        }

        #endregion

        #region Helper Methods

        private static QueuedCommand CreateTestCommand(string id, string command)
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
    }
}
