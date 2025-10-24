using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace nexus.engine.batch.Internal;

using config;

/// <summary>
/// Filters commands to determine if they should be batched.
/// </summary>
internal class BatchCommandFilter
{
    private readonly ILogger<BatchCommandFilter> m_Logger;

    public BatchCommandFilter(IServiceProvider serviceProvider)
    {
        m_Logger = serviceProvider.GetRequiredService<ILogger<BatchCommandFilter>>();
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

        if (!Settings.GetInstance().Get().McpNexus.Batching.Enabled)
        {
            m_Logger.LogDebug("Batching is disabled");
            return false;
        }

        if (commands.Count < Settings.GetInstance().Get().McpNexus.Batching.MinBatchSize)
        {
            m_Logger.LogDebug("Not enough commands to batch (count: {Count}, min: {Min})",
                commands.Count, Settings.GetInstance().Get().McpNexus.Batching.MinBatchSize);
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

        foreach (var excludedCommand in Settings.GetInstance().Get().McpNexus.Batching.ExcludedCommands)
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

