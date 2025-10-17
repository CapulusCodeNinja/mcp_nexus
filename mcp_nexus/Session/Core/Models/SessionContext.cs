namespace mcp_nexus.Session.Core.Models
{
    /// <summary>
    /// Context information about a session for AI client guidance
    /// </summary>
    public class SessionContext
    {
        /// <summary>Session identifier</summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Human-readable session description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Path to the dump file being debugged</summary>
        public string? DumpPath { get; set; }

        /// <summary>Session creation time</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Last activity time</summary>
        public DateTime LastActivity { get; set; }

        /// <summary>Current session status</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Number of commands processed in this session</summary>
        public int CommandsProcessed { get; set; }

        /// <summary>Number of active/pending commands</summary>
        public int ActiveCommands { get; set; }

        /// <summary>Time until session expires due to inactivity</summary>
        public TimeSpan? TimeUntilExpiry { get; set; }

        /// <summary>Helpful hints for AI client about session usage</summary>
        public List<string> UsageHints { get; set; } = [];
    }
}
