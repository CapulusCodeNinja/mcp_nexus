using System.Text.Json.Serialization;

namespace Nexus.Engine.Extensions.Callback;

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

/// <summary>
/// Request model for queuing a command via callback.
/// </summary>
public class QueueCommandRequest
{
    /// <summary>
    /// Gets or sets the command to queue.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Request model for reading a command result via callback.
/// </summary>
public class ReadCommandRequest
{
    /// <summary>
    /// Gets or sets the command ID to read.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for getting command status via callback.
/// </summary>
public class StatusCommandRequest
{
    /// <summary>
    /// Gets or sets the command ID to get status for.
    /// </summary>
    [JsonPropertyName("commandId")]
    public string CommandId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for getting bulk command status via callback.
/// </summary>
public class BulkStatusRequest
{
    /// <summary>
    /// Gets or sets the list of command IDs to get status for.
    /// </summary>
    [JsonPropertyName("commandIds")]
    public List<string> CommandIds { get; set; } = new();
}

/// <summary>
/// Request model for logging via callback.
/// </summary>
public class LogRequest
{
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    [JsonPropertyName("level")]
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response model for command operations.
/// </summary>
public class CommandResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success
    {
        get; set;
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
        get; set;
    }

    /// <summary>
    /// Gets or sets the command state.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Response model for bulk status operations.
/// </summary>
public class BulkStatusResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success
    {
        get; set;
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
        get; set;
    }
}

/// <summary>
/// Model for command status information.
/// </summary>
public class CommandStatus
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
        get; set;
    }

    /// <summary>
    /// Gets or sets the time when the command started executing.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime? StartTime
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the time when the command completed.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime
    {
        get; set;
    }
}
