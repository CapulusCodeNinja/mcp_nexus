namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Response model for reading command result.
    /// </summary>
    public class ExtensionCallbackReadResponse
    {
        /// <summary>
        /// The command ID.
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// The status of the command.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Whether the command is completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// The command output.
        /// </summary>
        public string? Output { get; set; }

        /// <summary>
        /// Error message if the command failed.
        /// </summary>
        public string? Error { get; set; }
    }
}


