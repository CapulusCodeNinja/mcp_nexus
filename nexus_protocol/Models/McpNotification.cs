using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents a server-initiated Model Context Protocol (MCP) notification.
/// Notifications are one-way messages that do not expect a response.
/// </summary>
public class McpNotification
{
    /// <summary>
    /// Gets or sets the JSON-RPC protocol version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the notification method name.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification parameters.
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params
    {
        get; set;
    }
}
