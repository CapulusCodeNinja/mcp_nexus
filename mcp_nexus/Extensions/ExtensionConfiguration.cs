namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Configuration settings for extension execution.
    /// </summary>
    public class ExtensionConfiguration
    {
        /// <summary>
        /// Whether extensions are enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Path to the extensions directory.
        /// </summary>
        public string ExtensionsPath { get; set; } = "extensions";

        /// <summary>
        /// Port for extension callbacks (0 = use MCP server port).
        /// </summary>
        public int CallbackPort { get; set; } = 0;

        /// <summary>
        /// Timeout in milliseconds for graceful process termination.
        /// </summary>
        public int GracefulTerminationTimeoutMs { get; set; } = 2000;
    }
}
