namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Request model for extension callback command execution.
    /// </summary>
    public class ExtensionCallbackExecuteRequest
    {
        /// <summary>
        /// The WinDBG command to execute.
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Timeout in seconds (default: 300).
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;
    }
}


