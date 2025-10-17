using System.Text.Json.Serialization;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Response model for bulk command status checking.
    /// </summary>
    public class ExtensionCallbackBulkStatusResponse
    {
        /// <summary>
        /// Gets or sets the dictionary of command results keyed by command ID.
        /// </summary>
        [JsonPropertyName("results")]
        public Dictionary<string, ExtensionCallbackReadResponse> Results { get; set; } = new();
    }
}
