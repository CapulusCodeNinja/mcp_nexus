using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents session recovery notification parameters for MCP.
/// Sent when the debugging session undergoes automatic recovery procedures.
/// </summary>
internal class McpSessionRecoveryNotification
{
    /// <summary>
    /// Gets or sets the reason for the recovery operation.
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recovery step description.
    /// </summary>
    [JsonPropertyName("recoveryStep")]
    public string RecoveryStep { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether the recovery was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the recovery message providing additional details.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the array of command IDs affected by the recovery.
    /// </summary>
    [JsonPropertyName("affectedCommands")]
    public string[]? AffectedCommands
    {
        get; set;
    }
}
