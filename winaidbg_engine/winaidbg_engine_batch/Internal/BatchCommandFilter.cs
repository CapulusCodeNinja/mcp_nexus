using NLog;

using WinAiDbg.Config;

namespace WinAiDbg.Engine.Batch.Internal;

/// <summary>
/// Filters commands to determine if they should be batched.
/// </summary>
internal class BatchCommandFilter
{
    private readonly Logger m_Logger;
    private readonly ISettings m_Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCommandFilter"/> class.
    /// </summary>
    /// <param name="settings">The product settings.</param>
    public BatchCommandFilter(ISettings settings)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_Settings = settings;
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
            m_Logger.Debug("No commands to batch");
            return false;
        }

        if (!m_Settings.Get().WinAiDbg.Batching.Enabled)
        {
            m_Logger.Debug("Batching is disabled");
            return false;
        }

        if (commands.Count < m_Settings.Get().WinAiDbg.Batching.MinBatchSize)
        {
            m_Logger.Trace("Not enough commands to batch (count: {Count}, min: {Min})", commands.Count, m_Settings.Get().WinAiDbg.Batching.MinBatchSize);
            return false;
        }

        // Check if any command is excluded
        foreach (var command in commands)
        {
            if (IsCommandExcluded(command.CommandText))
            {
                m_Logger.Debug("Command excluded from batching: {Command}", command.CommandText);
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
        {
            return false;
        }

        var trimmedCommand = commandText.Trim();

        foreach (var excludedCommand in m_Settings.Get().WinAiDbg.Batching.ExcludedCommands)
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
