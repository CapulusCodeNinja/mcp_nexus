using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Callback;

/// <summary>
/// Request model for reading a command result via callback.
/// </summary>
public class ReadCommandRequest
{
    /// <summary>
    /// Gets or sets the command ID to read.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;
}


