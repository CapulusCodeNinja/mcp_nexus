using Microsoft.Extensions.Logging;
using nexus.engine.batch.Configuration;

namespace nexus.engine.batch.Internal;

/// <summary>
/// Implements batch processing logic for commands and results.
/// </summary>
internal class BatchProcessor : IBatchProcessor
{
    private readonly BatchingConfiguration m_Configuration;
    private readonly BatchCommandFilter m_Filter;
    private readonly BatchCommandBuilder m_Builder;
    private readonly BatchResultParser m_Parser;
    private readonly ILogger<BatchProcessor> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessor"/> class.
    /// </summary>
    /// <param name="configuration">The batching configuration.</param>
    /// <param name="filter">The command filter.</param>
    /// <param name="builder">The command builder.</param>
    /// <param name="parser">The result parser.</param>
    /// <param name="logger">The logger instance.</param>
    public BatchProcessor(
        BatchingConfiguration configuration,
        BatchCommandFilter filter,
        BatchCommandBuilder builder,
        BatchResultParser parser,
        ILogger<BatchProcessor> logger)
    {
        m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        m_Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        m_Parser = parser ?? throw new ArgumentNullException(nameof(parser));
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (batch.Count >= m_Configuration.MinBatchSize && 
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

