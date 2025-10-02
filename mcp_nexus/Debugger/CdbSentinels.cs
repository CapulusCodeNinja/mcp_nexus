namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Global sentinel markers used to delineate command boundaries in CDB output
    /// </summary>
    public static class CdbSentinels
    {
        // Start-of-command diagnostic marker. Kept stable for log filtering and tooling.
        public const string StartMarker = "MCP_NEXUS_COMMAND_SENTINEL_START";

        // End-of-command completion marker. Stable string so consumers can filter.
        // If uniqueness per-command is desired later, we can append a commandId suffix.
        public const string EndMarker = "MCP_NEXUS_SENTINEL_COMMAND_END";
    }
}


