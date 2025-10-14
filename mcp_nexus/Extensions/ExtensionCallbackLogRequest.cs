namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Request model for logging from extension scripts.
    /// </summary>
    public class ExtensionCallbackLogRequest
    {
        /// <summary>
        /// The log message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The log level (Debug, Information, Warning, Error). Defaults to Information.
        /// </summary>
        public string? Level { get; set; }
    }
}


