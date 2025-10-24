using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.engine.batch.Internal;

namespace nexus.engine.batch;

using config;

/// <summary>
/// Implements batch processing logic for commands and results.
/// </summary>
public class BatchProcessor : IBatchProcessor
{
    private readonly BatchCommandFilter m_Filter;
    private readonly BatchCommandBuilder m_Builder;
    private readonly BatchResultParser m_Parser;

    private readonly ILogger<BatchProcessor> m_Logger;

    private static IBatchProcessor? m_Instance;

    public static IBatchProcessor GetInstance(IServiceProvider serviceProvider)
    {
        return m_Instance ??= new BatchProcessor(serviceProvider);
    }

    internal BatchProcessor(IServiceProvider serviceProvider)
    {
        m_Logger = serviceProvider.GetRequiredService<ILogger<BatchProcessor>>();

        m_Filter = new BatchCommandFilter(serviceProvider);
        m_Builder = new BatchCommandBuilder(serviceProvider);
        m_Parser = new BatchResultParser(serviceProvider);
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
            m_Logger.LogDebug("No commands to batch");
            return commands ?? new List<Command>();
        }

        // Check if batching should be applied
        if (!m_Filter.ShouldBatch(commands))
        {
            m_Logger.LogDebug("Batching not applicable, passing through {Count} commands", commands.Count);
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
                m_Logger.LogInformation("Batched {Count} commands into {BatchId}",
                    batch.Count, batchedCommand.CommandId);
            }
            else
            {
                // Pass through individually
                batchedCommands.AddRange(batch);
                m_Logger.LogDebug("Passing through {Count} commands individually", batch.Count);
            }
        }

        m_Logger.LogInformation("Transformed {InputCount} commands into {OutputCount} batched commands",
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
            m_Logger.LogDebug("No results to unbatch");
            return results ?? new List<CommandResult>();
        }

        var unbatchedResults = new List<CommandResult>();

        foreach (var result in results)
        {
            var parsedResults = m_Parser.ParseResult(result);
            unbatchedResults.AddRange(parsedResults);

            if (parsedResults.Count > 1)
            {
                m_Logger.LogInformation("Unbatched {BatchId} into {Count} individual results",
                    result.CommandId, parsedResults.Count);
            }
        }

        m_Logger.LogInformation("Transformed {InputCount} results into {OutputCount} individual results",
            results.Count, unbatchedResults.Count);

        return unbatchedResults;
    }
}

