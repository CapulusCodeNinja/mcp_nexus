using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents command status notification parameters for MCP.
/// Sent when debugger commands change state during execution.
/// Status progression: "queued" → "executing" → "completed" (or "cancelled"/"failed").
/// </summary>
internal class McpCommandStatusNotification
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
    /// Gets or sets the command text that was executed.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current command status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    [JsonPropertyName("progress")]
    public int? Progress
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the status message providing additional context.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the notification timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the command result when completed.
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the error message if the command failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error
    {
        get; set;
    }
}
