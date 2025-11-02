using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Callback;

/// <summary>
/// Request model for logging via callback.
/// </summary>
internal class LogRequest
{
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    [JsonPropertyName("level")]
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}


