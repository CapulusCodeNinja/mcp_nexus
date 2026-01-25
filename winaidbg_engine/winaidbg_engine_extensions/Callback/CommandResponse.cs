using System.Text.Json.Serialization;

namespace WinAiDbg.Engine.Extensions.Callback;

/// <summary>
/// Response model for command operations.
/// </summary>
internal class CommandResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the command ID.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command output.
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if any.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the command state.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}


