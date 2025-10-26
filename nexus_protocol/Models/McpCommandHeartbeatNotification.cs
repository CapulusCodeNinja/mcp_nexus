using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents command heartbeat notification parameters for MCP.
/// Sent periodically for long-running commands to show they are still active.
/// </summary>
internal class McpCommandHeartbeatNotification
{
    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the command identifier.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command text being executed.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the elapsed time in seconds since command started.
    /// </summary>
    [JsonPropertyName("elapsedSeconds")]
    public double ElapsedSeconds
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the human-readable elapsed time display (e.g., "2m 5s").
    /// </summary>
    [JsonPropertyName("elapsedDisplay")]
    public string ElapsedDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional details about the command execution.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the notification timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}

