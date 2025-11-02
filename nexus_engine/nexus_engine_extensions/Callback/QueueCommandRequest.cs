using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Callback;

/// <summary>
/// Request model for queuing a command via callback.
/// </summary>
internal class QueueCommandRequest
{
    /// <summary>
    /// Gets or sets the command to queue.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;
}


