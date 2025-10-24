namespace Nexus.Engine.Batch.Internal;

/// <summary>
/// Constants for batch sentinel markers used to identify command boundaries in batched output.
/// </summary>
internal static class BatchSentinels
{
    /// <summary>
    /// Command separator marker prefix for batch commands.
    /// </summary>
    public const string CommandSeparator = "MCP_NEXUS_COMMAND_SEPARATOR";

    /// <summary>
    /// Gets the start marker for a specific command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>The start marker string.</returns>
    public static string GetStartMarker(string commandId)
    {
        return $"{CommandSeparator}_{commandId}_START";
    }

    /// <summary>
    /// Gets the end marker for a specific command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>The end marker string.</returns>
    public static string GetEndMarker(string commandId)
    {
        return $"{CommandSeparator}_{commandId}_END";
    }
}

