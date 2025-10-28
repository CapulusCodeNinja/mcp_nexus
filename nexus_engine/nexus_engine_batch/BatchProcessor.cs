using System.Collections.Concurrent;

using Nexus.Config;
using Nexus.Engine.Batch.Internal;

using NLog;

namespace Nexus.Engine.Batch;
/// <summary>
/// Implements batch processing logic for commands and results.
/// </summary>
public class BatchProcessor : IBatchProcessor
{
    private readonly BatchCommandFilter m_Filter;
    private readonly BatchCommandBuilder m_Builder;
    private readonly BatchResultParser m_Parser;

    private readonly Logger m_Logger;

    /// <summary>
    /// Per-session batch caches for fast lookups and cleanup.
    /// </summary>
    private readonly ConcurrentDictionary<string, SessionBatchCache> m_SessionCaches = new();

    /// <summary>
    /// Gets the singleton instance of the batch processor.
    /// </summary>
    public static IBatchProcessor Instance { get; } = new BatchProcessor();

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessor"/> class.
    /// Creates internal batch processing components (filter, builder, parser).
    /// </summary>
    internal BatchProcessor()
    {
        m_Logger = LogManager.GetCurrentClassLogger();

        m_Filter = new BatchCommandFilter();
        m_Builder = new BatchCommandBuilder();
        m_Parser = new BatchResultParser(this);
    }

    /// <summary>
    /// Transforms commands (batches them or passes through).
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="commands">Commands to potentially batch.</param>
    /// <returns>Transformed commands (fewer if batched, same if not).</returns>
    public List<Command> BatchCommands(string sessionId, List<Command> commands)
    {
        if (commands == null || commands.Count == 0)
        {
            m_Logger.Debug("No commands to batch");
            return commands ?? new List<Command>();
        }

        // Check if batching should be applied
        if (!m_Filter.ShouldBatch(commands))
        {
            m_Logger.Trace("Batching not applicable, passing through {Count} commands", commands.Count);
            return commands;
        }

        // Group commands into batches respecting max batch size
        var batches = m_Builder.GroupIntoBatches(commands);

        // Build each batch
        var batchedCommands = new List<Command>();
        foreach (var batch in batches)
        {
            // Check if this specific batch should be batched
            // (may have commands that should be excluded)
            if (batch.Count >= Settings.GetInstance().Get().McpNexus.Batching.MinBatchSize &&
                batch.All(cmd => !m_Filter.IsCommandExcluded(cmd.CommandText)))
            {
                var batchedCommand = m_Builder.BuildBatch(sessionId, batch);

                // Get or create session cache
                var sessionCache = m_SessionCaches.GetOrAdd(sessionId, _ => new SessionBatchCache());

                // Store batch mappings in session cache
                var commandIds = new List<string>();
                foreach (var cmd in batch)
                {
                    commandIds.Add(cmd.CommandId);
                }

                sessionCache.AddBatch(batchedCommand.CommandId, commandIds);
                batchedCommands.Add(batchedCommand);

                m_Logger.Info("Batched {Count} commands into {BatchId}",
                    batch.Count, batchedCommand.CommandId);
            }
            else
            {
                // Pass through individually
                batchedCommands.AddRange(batch);
                m_Logger.Debug("Passing through {Count} commands individually", batch.Count);
            }
        }

        if (commands.Count == batchedCommands.Count)
        {
            m_Logger.Trace("Transformed {InputCount} commands into {OutputCount} batched commands",
                commands.Count, batchedCommands.Count);
        }
        else
        {
            m_Logger.Info("Transformed {InputCount} commands into {OutputCount} batched commands",
                commands.Count, batchedCommands.Count);
        }

        return batchedCommands;
    }

    /// <summary>
    /// Transforms results (unbatches them or passes through).
    /// </summary>
    /// <param name="results">Results from executed commands.</param>
    /// <returns>Transformed results (more if unbatched, same if not).</returns>
    public List<CommandResult> UnbatchResults(List<CommandResult> results)
    {
        if (results == null || results.Count == 0)
        {
            m_Logger.Debug("No results to unbatch");
            return results ?? new List<CommandResult>();
        }

        var unbatchedResults = new List<CommandResult>();

        foreach (var result in results)
        {
            var parsedResults = m_Parser.ParseResult(result);
            unbatchedResults.AddRange(parsedResults);

            if (parsedResults.Count > 1)
            {
                m_Logger.Info("Unbatched {BatchId} into {Count} individual results",
                    result.CommandId, parsedResults.Count);
            }
        }

        m_Logger.Info("Transformed {InputCount} results into {OutputCount} individual results",
            results.Count, unbatchedResults.Count);

        return unbatchedResults;
    }

    /// <summary>
    /// Gets the original command IDs that were batched into the given batch command ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="batchCommandId">The batch command ID or single command ID.</param>
    /// <returns>List of original command IDs (single item if not a batch).</returns>
    public List<string> GetOriginalCommandIds(string sessionId, string batchCommandId)
    {
        if (m_SessionCaches.TryGetValue(sessionId, out var cache))
        {
            return cache.GetOriginalCommandIds(batchCommandId);
        }

        // No cache = not a batch
        return new List<string> { batchCommandId };
    }

    /// <summary>
    /// Gets the batch command ID for a given individual command ID, if it was part of a batch.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="individualCommandId">The individual command ID.</param>
    /// <returns>The batch command ID if part of a batch, null otherwise.</returns>
    public string? GetBatchCommandId(string sessionId, string individualCommandId)
    {
        if (m_SessionCaches.TryGetValue(sessionId, out var cache))
        {
            return cache.GetBatchCommandId(individualCommandId);
        }

        // No cache = not part of any batch
        return null;
    }

    /// <summary>
    /// Clears all batch mappings for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID to clear mappings for.</param>
    public void ClearSessionBatchMappings(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return; // Handle null/empty session ID gracefully
        }

        // O(1) removal - just remove the entire session cache
        if (m_SessionCaches.TryRemove(sessionId, out var cache))
        {
            cache.Clear(); // Help GC
        }
    }
}

