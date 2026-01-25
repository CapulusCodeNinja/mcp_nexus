using System.Text.Json.Serialization;

namespace WinAiDbg.Engine.Extensions.Callback;

/// <summary>
/// Response model for bulk status operations.
/// </summary>
internal class BulkStatusResponse
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
    /// Gets or sets the list of command statuses.
    /// </summary>
    [JsonPropertyName("commands")]
    public List<CommandStatus> Commands { get; set; } = new();

    /// <summary>
    /// Gets or sets the error message if any.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error
    {
        get;
        set;
    }
}


