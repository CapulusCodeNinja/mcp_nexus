namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Response model for extension callback command queueing.
    /// </summary>
    public class ExtensionCallbackQueueResponse
    {
        /// <summary>
        /// The command ID assigned to this command.
        /// </summary>
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// The status of the command.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Optional message providing additional context.
        /// </summary>
        public string? Message { get; set; }
    }
}

