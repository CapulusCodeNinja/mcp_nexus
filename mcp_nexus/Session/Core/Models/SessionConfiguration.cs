namespace mcp_nexus.Session.Core.Models
{
    /// <summary>
    /// Configuration for session management
    /// </summary>
    public class SessionConfiguration
    {
        /// <summary>Maximum number of concurrent sessions</summary>
        public int MaxConcurrentSessions { get; set; } = 1000;

        /// <summary>Session timeout due to inactivity</summary>
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>How often to check for expired sessions</summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>How long to wait for session disposal during cleanup</summary>
        public TimeSpan DisposalTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Default command timeout per session</summary>
        public TimeSpan DefaultCommandTimeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Memory threshold in bytes for triggering cleanup (default: 1GB)</summary>
        public long MemoryCleanupThresholdBytes { get; set; } = 1_000_000_000; // 1GB

        /// <summary>Whether the application is running in service mode</summary>
        public bool ServiceMode { get; set; } = false;
    }
}
