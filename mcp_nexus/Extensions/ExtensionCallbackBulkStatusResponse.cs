namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Response model for bulk command status checking.
    /// </summary>
    public class ExtensionCallbackBulkStatusResponse
    {
        /// <summary>
        /// Gets or sets the dictionary of command statuses keyed by command ID.
        /// </summary>
        public Dictionary<string, ExtensionCallbackReadResponse> Results { get; set; } = new();

        /// <summary>
        /// Gets or sets any error message if the request failed.
        /// </summary>
        public string? Error { get; set; }
    }
}

