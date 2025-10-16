namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Global sentinel markers used to delineate command boundaries in CDB output.
    /// These markers are used to identify the start and end of command execution in CDB output streams.
    /// </summary>
    public static class CdbSentinels
    {
        /// <summary>
        /// Start-of-command diagnostic marker. Kept stable for log filtering and tooling.
        /// This marker is placed at the beginning of each command to identify command boundaries.
        /// </summary>
        public const string StartMarker = "MCP_NEXUS_SENTINEL_COMMAND_START";

        /// <summary>
        /// End-of-command completion marker. Stable string so consumers can filter.
        /// This marker is placed at the end of each command to identify command completion.
        /// If uniqueness per-command is desired later, we can append a commandId suffix.
        /// </summary>
        public const string EndMarker = "MCP_NEXUS_SENTINEL_COMMAND_END";

        /// <summary>
        /// Batch start marker for command batching functionality.
        /// Used to identify the beginning of a batch command execution.
        /// </summary>
        public const string BatchStart = "MCP_NEXUS_BATCH_START";

        /// <summary>
        /// Batch end marker for command batching functionality.
        /// Used to identify the end of a batch command execution.
        /// </summary>
        public const string BatchEnd = "MCP_NEXUS_BATCH_END";

        /// <summary>
        /// Command separator marker for command batching functionality.
        /// Used to separate individual command outputs within a batch.
        /// </summary>
        public const string CommandSeparator = "MCP_NEXUS_CMD_SEP";
    }
}


