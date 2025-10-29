using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents a Model Context Protocol (MCP) JSON-RPC 2.0 response.
/// Contains either a result or an error, never both.
/// </summary>
internal class McpResponse
{
    /// <summary>
    /// Gets or sets the JSON-RPC protocol version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the response identifier, matching the request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the result data on success.
    /// Mutually exclusive with <see cref="Error"/>.
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the error information on failure.
    /// Mutually exclusive with <see cref="Result"/>.
    /// </summary>
    [JsonPropertyName("error")]
    public McpError? Error
    {
        get; set;
    }
}
