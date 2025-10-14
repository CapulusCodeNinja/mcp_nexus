namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Request model for reading command result.
    /// </summary>
    public class ExtensionCallbackReadRequest
    {
        /// <summary>
        /// The command ID to read.
        /// </summary>
        public string CommandId { get; set; } = string.Empty;
    }
}


