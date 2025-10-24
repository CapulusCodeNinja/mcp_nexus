using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.engine.batch.Internal;

namespace nexus.engine.batch;

using config;
using NLog;

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
        m_Parser = new BatchResultParser();
    }

    /// <summary>
    /// Transforms commands (batches them or passes through).
    /// </summary>
    /// <param name="commands">Commands to potentially batch.</param>
    /// <returns>Transformed commands (fewer if batched, same if not).</returns>
    public List<Command> BatchCommands(List<Command> commands)
    {
        if (commands == null || commands.Count == 0)
        {
            m_Logger.Debug("No commands to batch");
            return commands ?? new List<Command>();
        }

        // Check if batching should be applied
        if (!m_Filter.ShouldBatch(commands))
        {
            m_Logger.Debug("Batching not applicable, passing through {Count} commands", commands.Count);
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
                var batchedCommand = m_Builder.BuildBatch(batch);
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

        m_Logger.Info("Transformed {InputCount} commands into {OutputCount} batched commands",
            commands.Count, batchedCommands.Count);

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
}

