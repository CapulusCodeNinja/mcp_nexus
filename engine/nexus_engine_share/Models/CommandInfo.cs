namespace Nexus.Engine.Share.Models;

/// <summary>
/// Represents information about a debug command at any stage of its lifecycle.
/// </summary>
public class CommandInfo
{
    /// <summary>
    /// Gets the unique identifier of the command.
    /// </summary>
    public required string CommandId
    {
        get; init;
    }

    /// <summary>
    /// Gets the command text that was executed.
    /// </summary>
    public required string Command
    {
        get; init;
    }

    /// <summary>
    /// Gets the current state of the command.
    /// </summary>
    public required CommandState State
    {
        get; init;
    }

    /// <summary>
    /// Gets the time when the command was queued.
    /// </summary>
    public required DateTime QueuedTime
    {
        get; init;
    }

    /// <summary>
    /// Gets the time when the command started executing, or null if not started.
    /// </summary>
    public DateTime? StartTime
    {
        get; init;
    }

    /// <summary>
    /// Gets the time when the command completed, or null if not completed.
    /// </summary>
    public DateTime? EndTime
    {
        get; init;
    }

    /// <summary>
    /// Gets the output from the command execution, or null if not completed.
    /// </summary>
    public string? Output
    {
        get; init;
    }

    /// <summary>
    /// Gets a value indicating whether the command executed successfully, or null if not completed.
    /// </summary>
    public bool? IsSuccess
    {
        get; init;
    }

    /// <summary>
    /// Gets the error message if the command failed, or null if not failed.
    /// </summary>
    public string? ErrorMessage
    {
        get; init;
    }

    /// <summary>
    /// Gets the execution time if the command has completed, otherwise null.
    /// </summary>
    public TimeSpan? ExecutionTime => EndTime.HasValue && StartTime.HasValue ? EndTime.Value - StartTime.Value : null;

    /// <summary>
    /// Gets the total time from queuing to completion, or null if not completed.
    /// </summary>
    public TimeSpan? TotalTime => EndTime.HasValue ? EndTime.Value - QueuedTime : null;

    /// <summary>
    /// Creates a command info for a queued command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <returns>A command info for a queued command.</returns>
    public static CommandInfo Queued(string commandId, string command, DateTime queuedTime)
    {
        return new CommandInfo
        {
            CommandId = commandId,
            Command = command,
            State = CommandState.Queued,
            QueuedTime = queuedTime
        };
    }

    /// <summary>
    /// Creates a command info for an executing command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <returns>A command info for an executing command.</returns>
    public static CommandInfo Executing(string commandId, string command, DateTime queuedTime, DateTime startTime)
    {
        return new CommandInfo
        {
            CommandId = commandId,
            Command = command,
            State = CommandState.Executing,
            QueuedTime = queuedTime,
            StartTime = startTime
        };
    }

    /// <summary>
    /// Creates a command info for a completed command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command completed.</param>
    /// <param name="output">The command output.</param>
    /// <param name="isSuccess">Whether the command succeeded.</param>
    /// <param name="errorMessage">The error message if failed.</param>
    /// <returns>A command info for a completed command.</returns>
    public static CommandInfo Completed(
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime startTime,
        DateTime endTime,
        string output,
        bool isSuccess,
        string? errorMessage = null)
    {
        return new CommandInfo
        {
            CommandId = commandId,
            Command = command,
            State = isSuccess ? CommandState.Completed : CommandState.Failed,
            QueuedTime = queuedTime,
            StartTime = startTime,
            EndTime = endTime,
            Output = output,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a command info for a cancelled command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command was cancelled.</param>
    /// <returns>A command info for a cancelled command.</returns>
    public static CommandInfo Cancelled(
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        return new CommandInfo
        {
            CommandId = commandId,
            Command = command,
            State = CommandState.Cancelled,
            QueuedTime = queuedTime,
            StartTime = startTime,
            EndTime = endTime,
            IsSuccess = false,
            ErrorMessage = "Command was cancelled"
        };
    }

    /// <summary>
    /// Creates a command info for a timed out command.
    /// </summary>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command timed out.</param>
    /// <param name="errorMessage">The timeout error message.</param>
    /// <returns>A command info for a timed out command.</returns>
    public static CommandInfo TimedOut(
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime startTime,
        DateTime endTime,
        string errorMessage)
    {
        return new CommandInfo
        {
            CommandId = commandId,
            Command = command,
            State = CommandState.Timeout,
            QueuedTime = queuedTime,
            StartTime = startTime,
            EndTime = endTime,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

