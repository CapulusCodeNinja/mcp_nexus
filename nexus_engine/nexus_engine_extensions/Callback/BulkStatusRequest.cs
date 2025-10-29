using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Callback;

/// <summary>
/// Request model for getting bulk command status via callback.
/// </summary>
public class BulkStatusRequest
{
    /// <summary>
    /// Gets or sets the list of command IDs to get status for.
    /// </summary>
    [JsonPropertyName("commandIds")]
    public List<string> CommandIds { get; set; } = new();
}


