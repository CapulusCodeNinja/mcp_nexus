using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents server health notification parameters for MCP.
/// Reports overall server status and resource usage information.
/// </summary>
internal class McpServerHealthNotification
{
    /// <summary>
    /// Gets or sets the server health status (e.g., "healthy", "degraded", "unhealthy").
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the CDB debugging session is active.
    /// </summary>
    [JsonPropertyName("cdbSessionActive")]
    public bool CdbSessionActive
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the current command queue size.
    /// </summary>
    [JsonPropertyName("queueSize")]
    public int QueueSize
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the number of currently executing commands.
    /// </summary>
    [JsonPropertyName("activeCommands")]
    public int ActiveCommands
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the notification timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the server uptime since last restart.
    /// </summary>
    [JsonPropertyName("uptime")]
    public TimeSpan? Uptime
    {
        get; set;
    }
}

