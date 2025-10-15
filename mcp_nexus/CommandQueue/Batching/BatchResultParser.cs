using System.Text.RegularExpressions;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.CommandQueue.Batching
{
    /// <summary>
    /// Parses batch command output and splits it into individual command results
    /// </summary>
    public class BatchResultParser
    {
        /// <summary>
        /// Splits batch command output into individual command results
        /// </summary>
        /// <param name="batchOutput">The combined output from the batch command</param>
        /// <param name="commands">The original commands that were batched</param>
        /// <returns>A list of CommandResult objects matching the input command order</returns>
        /// <exception cref="ArgumentNullException">Thrown when batchOutput or commands is null</exception>
        public List<ICommandResult> SplitBatchResults(string batchOutput, List<QueuedCommand> commands)
        {
            if (batchOutput == null)
                throw new ArgumentNullException(nameof(batchOutput));

            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            var results = new List<ICommandResult>();

            foreach (var command in commands)
            {
                try
                {
                    var commandOutput = ExtractCommandOutput(batchOutput, command.Id ?? string.Empty);
                    var result = CommandResult.Success(commandOutput, TimeSpan.Zero);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    // If parsing fails for this command, return an error result
                    var errorResult = CommandResult.Failure($"Failed to parse batch result for command {command.Id}: {ex.Message}");
                    results.Add(errorResult);
                }
            }

            return results;
        }

        /// <summary>
        /// Extracts the output for a specific command from the batch output
        /// </summary>
        /// <param name="batchOutput">The combined batch output</param>
        /// <param name="commandId">The ID of the command to extract</param>
        /// <returns>The output for the specific command</returns>
        private string ExtractCommandOutput(string batchOutput, string commandId)
        {
            var upperCommandId = commandId.ToUpperInvariant();
            var startMarker = $"{CdbSentinels.CommandSeparator}_{upperCommandId}_START";
            var endMarker = $"{CdbSentinels.CommandSeparator}_{upperCommandId}_END";

            var startIndex = batchOutput.IndexOf(startMarker);
            if (startIndex == -1)
            {
                throw new InvalidOperationException($"Start marker for command {commandId} not found in batch output.");
            }

            var endIndex = batchOutput.IndexOf(endMarker, startIndex + startMarker.Length);
            if (endIndex == -1)
            {
                throw new InvalidOperationException($"End marker for command {commandId} not found in batch output.");
            }

            // Extract the content between the start and end markers
            var contentStartIndex = startIndex + startMarker.Length;
            var rawOutput = batchOutput.Substring(contentStartIndex, endIndex - contentStartIndex).Trim();

            // Remove any leading/trailing newlines or carriage returns that might be part of the echo command
            return Regex.Replace(rawOutput, @"^[\r\n]+|[\r\n]+$", "");
        }
    }
}
