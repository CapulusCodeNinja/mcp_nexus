using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents the schema definition for a Model Context Protocol (MCP) tool.
/// Defines the tool's name, description, and input parameter schema.
/// </summary>
public class McpToolSchema
{
    /// <summary>
    /// Gets or sets the unique tool name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool description explaining its purpose and usage.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema defining the tool's input parameters.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };
}

