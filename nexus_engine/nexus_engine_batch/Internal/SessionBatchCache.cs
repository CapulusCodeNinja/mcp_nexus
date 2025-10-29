using System.Collections.Concurrent;

namespace Nexus.Engine.Batch.Internal;

/// <summary>
/// Manages bidirectional batch-to-command mappings for a single session.
/// Provides O(1) lookups in both directions.
/// </summary>
internal class SessionBatchCache
{
    /// <summary>
    /// Forward mapping: batch ID to list of individual command IDs.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<string>> m_BatchToCommands = new();

    /// <summary>
    /// Reverse mapping: individual command ID to batch ID.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> m_CommandToBatch = new();

    /// <summary>
    /// Adds a batch mapping.
    /// </summary>
    /// <param name="batchId">The batch command ID.</param>
    /// <param name="commandIds">The individual command IDs in this batch.</param>
    /// <exception cref="ArgumentNullException">Thrown when batchId or commandIds is null.</exception>
    public void AddBatch(string batchId, List<string> commandIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);
        ArgumentNullException.ThrowIfNull(commandIds);

        // Store forward mapping
        m_BatchToCommands[batchId] = commandIds;

        // Store reverse mappings
        foreach (var commandId in commandIds)
        {
            m_CommandToBatch[commandId] = batchId;
        }
    }

    /// <summary>
    /// Gets the original command IDs for a batch.
    /// </summary>
    /// <param name="batchId">The batch command ID.</param>
    /// <returns>List of command IDs, or single-item list if not a batch.</returns>
    /// <exception cref="ArgumentNullException">Thrown when batchId is null.</exception>
    public List<string> GetOriginalCommandIds(string batchId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

        return m_BatchToCommands.TryGetValue(batchId, out var commandIds)
            ? new List<string>(commandIds)
            : new List<string> { batchId };
    }

    /// <summary>
    /// Gets the batch ID for an individual command.
    /// </summary>
    /// <param name="commandId">The individual command ID.</param>
    /// <returns>The batch ID if part of a batch, null otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when commandId is null.</exception>
    public string? GetBatchCommandId(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        return m_CommandToBatch.TryGetValue(commandId, out var batchId) ? batchId : null;
    }

    /// <summary>
    /// Clears all mappings in this cache.
    /// </summary>
    public void Clear()
    {
        m_BatchToCommands.Clear();
        m_CommandToBatch.Clear();
    }
}
