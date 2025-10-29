using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Callback;

/// <summary>
/// Request model for getting command status via callback.
/// </summary>
public class StatusCommandRequest
{
    /// <summary>
    /// Gets or sets the command ID to get status for.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;
}


