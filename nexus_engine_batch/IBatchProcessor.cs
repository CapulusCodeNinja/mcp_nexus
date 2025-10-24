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
    /// <param name="commands">Commands to potentially batch.</param>
    /// <returns>Transformed commands (fewer if batched, same if not).</returns>
    /// <remarks>
    /// ALWAYS call this. Library decides whether to batch:
    /// - If batching enabled: returns fewer commands (batched with sentinels)
    /// - If disabled/excluded: returns same commands (1:1 pass-through)
    /// </remarks>
    List<Command> BatchCommands(List<Command> commands);

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
}

