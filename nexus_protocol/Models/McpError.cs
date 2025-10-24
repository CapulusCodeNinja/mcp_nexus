using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Nexus.Protocol.Models;

/// <summary>
/// Represents an error in a Model Context Protocol (MCP) response.
/// Contains error code, message, and optional additional data.
/// </summary>
internal class McpError
{
    /// <summary>
    /// Gets or sets the error code indicating the error type.
    /// </summary>
    [JsonPropertyName("code")]
    [Required(ErrorMessage = "error code is required")]
    public int Code
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the error message providing a short description.
    /// </summary>
    [JsonPropertyName("message")]
    [Required(ErrorMessage = "error message is required")]
    [StringLength(1000, ErrorMessage = "error message cannot exceed 1000 characters")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional additional error data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data
    {
        get; set;
    }
}

