using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Batching;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus_unit_tests.CommandQueue.Batching
{
    /// <summary>
    /// Unit tests for BatchResultParser
    /// </summary>
    public class BatchResultParserTests
    {
        #region SplitBatchResults Tests

        [Fact]
        public void SplitBatchResults_WithValidBatchOutput_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new BatchResultParser();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm"),
                CreateTestCommand("cmd-2", "!threads"),
                CreateTestCommand("cmd-3", "!peb")
            };

            var batchOutput = CreateMockBatchOutput(commands);

            // Act
            var results = parser.SplitBatchResults(batchOutput, commands);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(3, results.Count);
            Assert.True(results.All(r => r.IsSuccess));
        }

        [Fact]
        public void SplitBatchResults_WithNullBatchOutput_ShouldThrowArgumentNullException()
        {
            // Arrange
            var parser = new BatchResultParser();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm")
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => parser.SplitBatchResults(null!, commands));
        }

        [Fact]
        public void SplitBatchResults_WithNullCommands_ShouldThrowArgumentNullException()
        {
            // Arrange
            var parser = new BatchResultParser();
            var batchOutput = "Some batch output";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => parser.SplitBatchResults(batchOutput, null!));
        }

        [Fact]
        public void SplitBatchResults_WithEmptyBatchOutput_ShouldReturnEmptyResults()
        {
            // Arrange
            var parser = new BatchResultParser();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm")
            };

            // Act
            var results = parser.SplitBatchResults("", commands);

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.False(results[0].IsSuccess);
        }

        [Fact]
        public void SplitBatchResults_WithMissingSeparators_ShouldReturnErrorResults()
        {
            // Arrange
            var parser = new BatchResultParser();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm"),
                CreateTestCommand("cmd-2", "!threads")
            };

            var batchOutput = "Some output without proper separators";

            // Act
            var results = parser.SplitBatchResults(batchOutput, commands);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.True(results.All(r => !r.IsSuccess));
        }

        [Fact]
        public void SplitBatchResults_WithSingleCommand_ShouldParseCorrectly()
        {
            // Arrange
            var parser = new BatchResultParser();
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd-1", "lm")
            };

            var batchOutput = CreateMockBatchOutput(commands);

            // Act
            var results = parser.SplitBatchResults(batchOutput, commands);

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.True(results[0].IsSuccess);
            Assert.Contains("Module list", results[0].Output);
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

        private static string CreateMockBatchOutput(List<QueuedCommand> commands)
        {
            var output = new List<string>();

            foreach (var command in commands)
            {
                // Create start and end markers for this command
                var startMarker = $"{CdbSentinels.CommandSeparator}_{command.Id?.ToUpperInvariant() ?? "UNKNOWN"}_START";
                var endMarker = $"{CdbSentinels.CommandSeparator}_{command.Id?.ToUpperInvariant() ?? "UNKNOWN"}_END";

                output.Add($"echo {startMarker}");

                // Add mock output based on command
                switch (command.Command)
                {
                    case "lm":
                        output.Add("Module list output");
                        break;
                    case "!threads":
                        output.Add("Thread information");
                        break;
                    case "!peb":
                        output.Add("Process Environment Block");
                        break;
                    default:
                        output.Add($"Output for {command.Command}");
                        break;
                }

                output.Add($"echo {endMarker}");
            }

            return string.Join("\n", output);
        }

        #region Branch Coverage Tests - Missing Branches

        [Fact]
        public void ExtractCommandOutput_WithMissingEndMarker_ReturnsError()
        {
            // Arrange
            var parser = new BatchResultParser();
            var batchOutput = $"{CdbSentinels.CommandSeparator}_CMD123_START\nCommand output without end marker";
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd123", "test")
            };

            // Act
            var results = parser.SplitBatchResults(batchOutput, commands);

            // Assert - should contain error result for missing end marker
            Assert.Single(results);
            var result = results[0];
            Assert.False(result.IsSuccess);
            Assert.Contains("End marker for command", result.ErrorMessage);
        }

        [Fact]
        public void ExtractCommandOutput_WithMissingStartMarker_ReturnsError()
        {
            // Arrange
            var parser = new BatchResultParser();
            var batchOutput = "Some output without start marker";
            var commands = new List<QueuedCommand>
            {
                CreateTestCommand("cmd123", "test")
            };

            // Act
            var results = parser.SplitBatchResults(batchOutput, commands);

            // Assert - should contain error result for missing start marker
            Assert.Single(results);
            var result = results[0];
            Assert.False(result.IsSuccess);
            Assert.Contains("Start marker", result.ErrorMessage);
        }

        [Fact]
        public void SplitBatchResults_WithNullCommandId_UsesEmptyString()
        {
            // Arrange - Test Line 33: command.Id ?? string.Empty (FALSE branch - null ID)
            var parser = new BatchResultParser();

            // Create command with NULL ID using the nullable constructor
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<string>();
            var commandWithNullId = new QueuedCommand(null!, "lm", DateTime.Now, tcs, cts);

            var commands = new List<QueuedCommand> { commandWithNullId };

            var batchOutput = $"{CdbSentinels.BatchStart}\n" +
                             $"{CdbSentinels.CommandSeparator}\n" +
                             "test output\n" +
                             $"{CdbSentinels.CommandSeparator}_END\n" +
                             $"{CdbSentinels.BatchEnd}";

            // Act - Should use empty string for null ID
            var results = parser.SplitBatchResults(batchOutput, commands);

            // Assert - Should parse successfully despite null ID
            Assert.NotNull(results);
            Assert.Single(results);
        }

        #endregion

        #endregion
    }
}
