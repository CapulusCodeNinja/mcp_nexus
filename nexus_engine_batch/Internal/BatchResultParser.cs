using Microsoft.Extensions.Logging;

namespace nexus.engine.batch.Internal;

/// <summary>
/// Parses batched command results and splits them into individual command results.
/// </summary>
internal class BatchResultParser
{
    private readonly ILogger<BatchResultParser> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchResultParser"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public BatchResultParser(ILogger<BatchResultParser> logger)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses a command result and extracts individual command results if batched.
    /// </summary>
    /// <param name="result">The command result to parse.</param>
    /// <returns>List of individual command results.</returns>
    public List<CommandResult> ParseResult(CommandResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        // Check if this is a batch result by parsing the CommandId
        if (!IsBatchResult(result.CommandId))
        {
            // Not a batch, pass through as-is
            m_Logger.LogDebug("Result {CommandId} is not a batch, passing through", result.CommandId);
            return new List<CommandResult> { result };
        }

        // Extract command IDs from batch ID
        var commandIds = ExtractCommandIdsFromBatchId(result.CommandId);
        if (commandIds.Count == 0)
        {
            m_Logger.LogWarning("Failed to extract command IDs from batch ID: {BatchId}", result.CommandId);
            return new List<CommandResult> { result };
        }

        // Check if result text contains sentinels
        if (!ContainsSentinels(result.ResultText))
        {
            m_Logger.LogWarning("Batch result does not contain sentinels: {BatchId}", result.CommandId);
            // Return single result for first command (fallback behavior)
            return new List<CommandResult>
            {
                new CommandResult
                {
                    CommandId = commandIds[0],
                    ResultText = result.ResultText
                }
            };
        }

        // Split by sentinels
        return SplitBatchResult(commandIds, result.ResultText);
    }

    /// <summary>
    /// Determines if a command ID represents a batch.
    /// </summary>
    /// <param name="commandId">The command ID to check.</param>
    /// <returns>True if the command ID represents a batch; otherwise, false.</returns>
    private static bool IsBatchResult(string commandId)
    {
        return commandId.StartsWith("batch_", StringComparison.Ordinal);
    }

    /// <summary>
    /// Extracts individual command IDs from a batch ID.
    /// </summary>
    /// <param name="batchId">The batch ID (format: batch_cmd-1_cmd-2_cmd-3).</param>
    /// <returns>List of command IDs.</returns>
    private static List<string> ExtractCommandIdsFromBatchId(string batchId)
    {
        if (!batchId.StartsWith("batch_"))
            return new List<string>();

        // Remove "batch_" prefix
        var idsString = batchId.Substring(6);

        // Split by underscore to get command IDs
        // Note: This assumes command IDs don't contain underscores themselves
        var ids = idsString.Split('_').ToList();

        return ids;
    }

    /// <summary>
    /// Checks if result text contains batch sentinel markers.
    /// </summary>
    /// <param name="resultText">The result text to check.</param>
    /// <returns>True if sentinels are present; otherwise, false.</returns>
    private static bool ContainsSentinels(string resultText)
    {
        return resultText.Contains(BatchSentinels.CommandSeparator, StringComparison.Ordinal);
    }

    /// <summary>
    /// Splits a batched result into individual command results.
    /// </summary>
    /// <param name="commandIds">The command IDs in order.</param>
    /// <param name="resultText">The batched result text.</param>
    /// <returns>List of individual command results.</returns>
    private List<CommandResult> SplitBatchResult(List<string> commandIds, string resultText)
    {
        var results = new List<CommandResult>();

        foreach (var commandId in commandIds)
        {
            var startMarker = BatchSentinels.GetStartMarker(commandId);
            var endMarker = BatchSentinels.GetEndMarker(commandId);

            var individualResult = ExtractCommandResult(resultText, startMarker, endMarker);

            results.Add(new CommandResult
            {
                CommandId = commandId,
                ResultText = individualResult
            });

            m_Logger.LogDebug("Extracted result for command {CommandId} ({Length} chars)",
                commandId, individualResult.Length);
        }

        return results;
    }

    /// <summary>
    /// Extracts the result text between start and end markers.
    /// </summary>
    /// <param name="resultText">The full result text.</param>
    /// <param name="startMarker">The start marker.</param>
    /// <param name="endMarker">The end marker.</param>
    /// <returns>The extracted result text.</returns>
    private static string ExtractCommandResult(string resultText, string startMarker, string endMarker)
    {
        var startIndex = resultText.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return string.Empty;
        }

        // Move past the start marker and any echo output
        startIndex = resultText.IndexOf('\n', startIndex);
        if (startIndex < 0)
        {
            return string.Empty;
        }
        startIndex++; // Skip the newline

        var endIndex = resultText.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            // No end marker found, take everything until end
            return resultText.Substring(startIndex).Trim();
        }

        // Find the last newline before the end marker (to exclude the echo command)
        var lastNewlineBeforeEnd = resultText.LastIndexOf('\n', endIndex - 1, endIndex - startIndex);
        if (lastNewlineBeforeEnd >= startIndex)
        {
            endIndex = lastNewlineBeforeEnd;
        }

        return resultText.Substring(startIndex, endIndex - startIndex).Trim();
    }
}

