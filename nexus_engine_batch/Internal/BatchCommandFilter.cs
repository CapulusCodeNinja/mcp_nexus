using Microsoft.Extensions.Logging;
using nexus.engine.batch.Configuration;

namespace nexus.engine.batch.Internal;

/// <summary>
/// Filters commands to determine if they should be batched.
/// </summary>
internal class BatchCommandFilter
{
    private readonly BatchingConfiguration m_Configuration;
    private readonly ILogger<BatchCommandFilter> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCommandFilter"/> class.
    /// </summary>
    /// <param name="configuration">The batching configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public BatchCommandFilter(BatchingConfiguration configuration, ILogger<BatchCommandFilter> logger)
    {
        m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        m_Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("BatchCommandBuilder");
    }

    /// <summary>
    /// Determines if batching should be applied to the given commands.
    /// </summary>
    /// <param name="commands">The commands to check.</param>
    /// <returns>True if commands should be batched; otherwise, false.</returns>
    public bool ShouldBatch(List<Command> commands)
    {
        if (commands == null || commands.Count == 0)
        {
            m_Logger.LogDebug("No commands to batch");
            return false;
        }

        if (!m_Configuration.Enabled)
        {
            m_Logger.LogDebug("Batching is disabled");
            return false;
        }

        if (commands.Count < m_Configuration.MinBatchSize)
        {
            m_Logger.LogDebug("Not enough commands to batch (count: {Count}, min: {Min})",
                commands.Count, m_Configuration.MinBatchSize);
            return false;
        }

        // Check if any command is excluded
        foreach (var command in commands)
        {
            if (IsCommandExcluded(command.CommandText))
            {
                m_Logger.LogDebug("Command excluded from batching: {Command}", command.CommandText);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if a command should be excluded from batching.
    /// </summary>
    /// <param name="commandText">The command text to check.</param>
    /// <returns>True if the command should be excluded; otherwise, false.</returns>
    public bool IsCommandExcluded(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return false;

        var trimmedCommand = commandText.Trim();

        foreach (var excludedCommand in m_Configuration.ExcludedCommands)
        {
            // Prefix matching: if command starts with excluded prefix, it's excluded
            if (trimmedCommand.StartsWith(excludedCommand, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

