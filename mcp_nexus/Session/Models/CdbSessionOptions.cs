namespace mcp_nexus.Session.Models
{
    /// <summary>
    /// Configuration for CDB session creation
    /// </summary>
    public class CdbSessionOptions
    {
        /// <summary>Command timeout in milliseconds</summary>
        public int CommandTimeoutMs { get; set; } = 30000;

        /// <summary>Idle timeout in milliseconds (how long CDB can be silent before timing out)</summary>
        public int IdleTimeoutMs { get; set; } = 180000;

        /// <summary>Symbol server timeout in milliseconds</summary>
        public int SymbolServerTimeoutMs { get; set; } = 30000;

        /// <summary>Maximum symbol server retries</summary>
        public int SymbolServerMaxRetries { get; set; } = 1;

        /// <summary>Symbol search path</summary>
        public string? SymbolSearchPath { get; set; }

        /// <summary>Startup delay in milliseconds (how long to wait after CDB starts before reading output)</summary>
        public int StartupDelayMs { get; set; } = 1000;

        /// <summary>Output reading timeout in milliseconds (how long to wait for command output)</summary>
        public int OutputReadingTimeoutMs { get; set; } = 300000;

        /// <summary>Whether to enable command preprocessing (path conversion and directory creation)</summary>
        public bool EnableCommandPreprocessing { get; set; } = true;

        /// <summary>Custom CDB executable path</summary>
        public string? CustomCdbPath { get; set; }
    }
}
