using Microsoft.Extensions.Logging;
using nexus.engine.batch.Configuration;

namespace nexus.engine.batch.Internal;

/// <summary>
/// Builds batched commands with sentinel markers.
/// </summary>
internal class BatchCommandBuilder
{
    private readonly BatchingConfiguration m_Configuration;
    private readonly ILogger<BatchCommandBuilder> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCommandBuilder"/> class.
    /// </summary>
    /// <param name="configuration">The batching configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public BatchCommandBuilder(BatchingConfiguration configuration, ILogger<BatchCommandBuilder> logger)
    {
        m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        m_Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BatchProcessor");
    }

    /// <summary>
    /// Builds a batch command from multiple commands.
    /// </summary>
    /// <param name="commands">The commands to batch.</param>
    /// <returns>A single command containing all batched commands with sentinels.</returns>
    public Command BuildBatch(List<Command> commands)
    {
        if (commands == null || commands.Count == 0)
            throw new ArgumentException("Commands list cannot be null or empty", nameof(commands));

        if (commands.Count == 1)
        {
            // Single command, no batching needed
            return commands[0];
        }

        // Create batch ID with embedded command IDs
        var batchId = CreateBatchId(commands);

        // Build batched command text with sentinels
        var batchedCommandText = BuildBatchedCommandText(commands);

        m_Logger.LogDebug("Built batch {BatchId} with {Count} commands", batchId, commands.Count);

        return new Command
        {
            CommandId = batchId,
            CommandText = batchedCommandText
        };
    }

    /// <summary>
    /// Groups commands into batches respecting maximum batch size.
    /// </summary>
    /// <param name="commands">The commands to group.</param>
    /// <returns>List of command batches.</returns>
    public List<List<Command>> GroupIntoBatches(List<Command> commands)
    {
        var batches = new List<List<Command>>();
        var currentBatch = new List<Command>();

        foreach (var command in commands)
        {
            currentBatch.Add(command);

            if (currentBatch.Count >= m_Configuration.MaxBatchSize)
            {
                batches.Add(currentBatch);
                currentBatch = new List<Command>();
            }
        }

        // Add remaining commands
        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// Creates a batch ID that embeds all command IDs.
    /// </summary>
    /// <param name="commands">The commands in the batch.</param>
    /// <returns>A batch ID containing all command IDs.</returns>
    private static string CreateBatchId(List<Command> commands)
    {
        var commandIds = string.Join("_", commands.Select(c => c.CommandId));
        return $"batch_{commandIds}";
    }

    /// <summary>
    /// Builds the batched command text with sentinel markers.
    /// </summary>
    /// <param name="commands">The commands to batch.</param>
    /// <returns>The batched command text.</returns>
    private static string BuildBatchedCommandText(List<Command> commands)
    {
        var parts = new List<string>();

        foreach (var command in commands)
        {
            var startMarker = BatchSentinels.GetStartMarker(command.CommandId);
            var endMarker = BatchSentinels.GetEndMarker(command.CommandId);

            parts.Add($".echo {startMarker}");
            parts.Add(command.CommandText);
            parts.Add($".echo {endMarker}");
        }

        return string.Join("; ", parts);
    }
}

