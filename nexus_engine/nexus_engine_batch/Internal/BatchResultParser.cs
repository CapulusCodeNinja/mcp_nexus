using NLog;

namespace Nexus.Engine.Batch.Internal;

/// <summary>
/// Parses batched command results and splits them into individual command results.
/// </summary>
internal class BatchResultParser
{
    private readonly Logger m_Logger;
    private readonly BatchProcessor m_BatchProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchResultParser"/> class.
    /// </summary>
    /// <param name="batchProcessor">The owning batch processor used for cache lookups.</param>
    public BatchResultParser(BatchProcessor batchProcessor)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_BatchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
    }

    /// <summary>
    /// Parses a command result and extracts individual command results if batched.
    /// </summary>
    /// <param name="result">The command result to parse.</param>
    /// <returns>List of individual command results.</returns>
    public List<CommandResult> ParseResult(CommandResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        // Check if this is a batch result by looking it up in the cache
        var originalCommandIds = m_BatchProcessor.GetOriginalCommandIds(result.SessionId, result.CommandId);

        // If we get more than one command ID back, it's a batch
        if (originalCommandIds.Count <= 1)
        {
            // Not a batch, pass through as-is
            m_Logger.Trace("Result {CommandId} is not a batch, passing through", result.CommandId);
            return new List<CommandResult> { result };
        }

        // Check if result text contains sentinels
        if (!ContainsSentinels(result.ResultText))
        {
            m_Logger.Warn("Batch result does not contain sentinels: {BatchId}", result.CommandId);

            // Return single result for first command (fallback behavior)
            return new List<CommandResult>
            {
                new CommandResult
                {
                    CommandId = originalCommandIds[0],
                    SessionId = result.SessionId,
                    ResultText = result.ResultText,
                },
            };
        }

        // Split by sentinels using the original command IDs from cache
        return SplitBatchResult(result.SessionId, originalCommandIds, result.ResultText);
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
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commandIds">The command IDs in order.</param>
    /// <param name="resultText">The batched result text.</param>
    /// <returns>List of individual command results.</returns>
    private List<CommandResult> SplitBatchResult(string sessionId, List<string> commandIds, string resultText)
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
                SessionId = sessionId,
                ResultText = individualResult,
            });

            m_Logger.Trace("Extracted result for command {CommandId} ({Length} chars)", commandId, individualResult.Length);
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
            return resultText[startIndex..].Trim();
        }

        // Find the last newline before the end marker (to exclude the echo command)
        if (startIndex < endIndex)
        {
            var lastNewlineBeforeEnd = resultText.LastIndexOf('\n', endIndex - 1, endIndex - startIndex);
            if (lastNewlineBeforeEnd >= startIndex)
            {
                endIndex = lastNewlineBeforeEnd;
            }
        }

        return resultText[startIndex..endIndex].Trim();
    }
}
