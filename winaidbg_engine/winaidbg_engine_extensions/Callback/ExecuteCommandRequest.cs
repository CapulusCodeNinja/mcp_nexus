using System.Text.Json.Serialization;

namespace WinAiDbg.Engine.Extensions.Callback;

/// <summary>
/// Request model for executing a command via callback.
/// </summary>
internal class ExecuteCommandRequest
{
    /// <summary>
    /// Gets or sets the command to execute.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;
}


