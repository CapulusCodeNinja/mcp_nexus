namespace Nexus.Engine.Batch;

/// <summary>
/// Stateless batch processing utility.
/// Transforms commands and results based on batching configuration.
/// </summary>
public interface IBatchProcessor
{
    /// <summary>
    /// Transforms commands (batches them or passes through).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commands">Commands to potentially batch.</param>
    /// <returns>Transformed commands (fewer if batched, same if not).</returns>
    /// <remarks>
    /// ALWAYS call this. Library decides whether to batch:
    /// - If batching enabled: returns fewer commands (batched with sentinels)
    /// - If disabled/excluded: returns same commands (1:1 pass-through)
    /// </remarks>
    List<Command> BatchCommands(string sessionId, List<Command> commands);

    /// <summary>
    /// Transforms results (unbatches them or passes through).
    /// </summary>
    /// <param name="results">Results from executed commands.</param>
    /// <returns>Transformed results (more if unbatched, same if not).</returns>
    /// <remarks>
    /// ALWAYS call this. Library decides whether to unbatch:
    /// - If was batched: splits by sentinels, returns more results
    /// - If not batched: passes through as-is (1:1 pass-through)
    /// </remarks>
    List<CommandResult> UnbatchResults(List<CommandResult> results);

    /// <summary>
    /// Gets the original command IDs that were batched into the given batch command ID.
    /// </summary>
    /// <param name="batchCommandId">The batch command ID or single command ID.</param>
    /// <returns>List of original command IDs (single item if not a batch).</returns>
    List<string> GetOriginalCommandIds(string batchCommandId);

    /// <summary>
    /// Gets the batch command ID for a given individual command ID, if it was part of a batch.
    /// </summary>
    /// <param name="individualCommandId">The individual command ID.</param>
    /// <returns>The batch command ID if part of a batch, null otherwise.</returns>
    string? GetBatchCommandId(string individualCommandId);

    /// <summary>
    /// Clears all batch mappings for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to clear mappings for.</param>
    void ClearSessionBatchMappings(string sessionId);
}

