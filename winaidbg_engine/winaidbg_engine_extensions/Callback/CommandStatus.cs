using System.Text.Json.Serialization;

namespace WinAiDbg.Engine.Extensions.Callback;

/// <summary>
/// Model for command status information.
/// </summary>
internal class CommandStatus
{
    /// <summary>
    /// Gets or sets the command ID.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command text.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command state.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time when the command was queued.
    /// </summary>
    [JsonPropertyName("queuedTime")]
    public DateTime QueuedTime
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the time when the command started executing.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime? StartTime
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the time when the command completed.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime
    {
        get;
        set;
    }
}


