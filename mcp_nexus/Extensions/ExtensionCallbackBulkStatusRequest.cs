using System.Text.Json.Serialization;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Request model for bulk command status checking.
    /// </summary>
    public class ExtensionCallbackBulkStatusRequest
    {
        /// <summary>
        /// Gets or sets the list of command IDs to check.
        /// </summary>
        [JsonPropertyName("commandIds")]
        public List<string> CommandIds { get; set; } = new();
    }
}

