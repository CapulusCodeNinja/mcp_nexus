namespace Nexus.Engine.Internal;

/// <summary>
/// Constants for CDB sentinel markers used to identify command boundaries in output.
/// </summary>
internal static class CdbSentinels
{
    /// <summary>
    /// Start marker for command execution.
    /// </summary>
    public const string StartMarker = "MCP_NEXUS_SENTINEL_COMMAND_START";

    /// <summary>
    /// End marker for command execution.
    /// </summary>
    public const string EndMarker = "MCP_NEXUS_SENTINEL_COMMAND_END";

    /// <summary>
    /// Command separator marker for batch commands.
    /// </summary>
    public const string CommandSeparator = "MCP_NEXUS_COMMAND_SEPERATOR";
}
