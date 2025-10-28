namespace Nexus.Engine.Share.Models;

/// <summary>
/// Represents information about a debug command at any stage of its lifecycle.
/// </summary>
public class CommandInfo
{
    /// <summary>
    /// Gets the process identifier.
    /// </summary>
    public int? ProcessId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public string SessionId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the unique identifier of the command.
    /// </summary>
    public string CommandId
    {
        get; private set;
    }

    /// <summary>
    /// Gets the command number.
    /// </summary>
    public int CommandNumber
    {
        get; private set;
    }

    /// <summary>
    /// Gets the command text that was executed.
    /// </summary>
    public string Command
    {
        get; private set;
    }

    /// <summary>
    /// Gets the current state of the command.
    /// </summary>
    public CommandState State
    {
        get; private set;
    }

    /// <summary>
    /// Gets the time when the command was queued.
    /// </summary>
    public DateTime QueuedTime
    {
        get; private set;
    }

    /// <summary>
    /// Gets the time when the command started executing, or null if not started.
    /// </summary>
    public DateTime? StartTime
    {
        get; private set;
    }

    /// <summary>
    /// Gets the time when the command completed, or null if not completed.
    /// </summary>
    public DateTime? EndTime
    {
        get; private set;
    }

    /// <summary>
    /// Gets the output stream from the command execution, or null if not completed.
    /// </summary>
    public string? AggregatedOutput
    {
        get; private set;
    }

    /// <summary>
    /// Gets how often this command was read from external APIs.
    /// </summary>
    public uint ReadCount
    {
        get; set;
    } = 0;

    /// <summary>
    /// Gets a value indicating whether the command executed successfully, or null if not completed.
    /// </summary>
    public bool? IsSuccess => State == CommandState.Completed;

    /// <summary>
    /// Gets the error message if the command failed, or null if not failed.
    /// </summary>
    public string? ErrorMessage
    {
        get; private set;
    }

    /// <summary>
    /// Gets the time spent in the queue before execution started, or null if not started.
    /// </summary>
    public TimeSpan? TimeInQueue => StartTime.HasValue ? StartTime.Value - QueuedTime : null;

    /// <summary>
    /// Gets the execution time if the command has completed, otherwise null.
    /// </summary>
    public TimeSpan? ExecutionTime => EndTime.HasValue && StartTime.HasValue ? EndTime.Value - StartTime.Value : null;

    /// <summary>
    /// Gets the total time from queuing to completion, or null if not completed.
    /// </summary>
    public TimeSpan? TotalTime => EndTime.HasValue ? EndTime.Value - QueuedTime : null;

    /// <summary>
    /// Gets the command number from the command identifier.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <returns>The command number.</returns>
    private static int GetCommandNumber(string sessionId, string commandId)
    {
        var prefix = $"cmd-{sessionId}-";
        var indexString = commandId.Replace(prefix, "");

        return int.TryParse(indexString, out var index) ? index : 0;
    }

    /// <summary>
    /// Creates a command info for a completed command.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="state">The state of the command.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="processId">The process identifier.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command completed.</param>
    /// <param name="aggregatedOutput">The aggregated output.</param>
    /// <param name="errorMessage">The error message if failed.</param>
    public CommandInfo(string sessionId, string commandId, string command, CommandState state, DateTime queuedTime, int? processId, 
        DateTime? startTime, DateTime? endTime, string? aggregatedOutput, string? errorMessage)
    {
        SessionId = sessionId;
        CommandId = commandId;
        CommandNumber = GetCommandNumber(sessionId, commandId);
        ProcessId = processId;
        Command = command;
        State = state;
        QueuedTime = queuedTime;
        StartTime = startTime ?? null;
        EndTime = endTime ?? null;
        AggregatedOutput = aggregatedOutput ?? string.Empty;
        ErrorMessage = errorMessage ?? string.Empty;
    }

    /// <summary>
    /// Creates a command info for a queued command.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="processId">The process identifier.</param>
    /// <returns>A command info for a queued command.</returns>
    public static CommandInfo Enqueued(
        string sessionId,
        string commandId,
        string command,
        DateTime queuedTime,
        int? processId)
    {
        return new CommandInfo(sessionId, commandId, command, CommandState.Queued, queuedTime, processId, null, null, string.Empty, string.Empty);
    }

    /// <summary>
    /// Creates a command info for an executing command.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="processId">The process identifier.</param>
    /// <returns>A command info for an executing command.</returns>
    public static CommandInfo Executing(
        string sessionId,
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime startTime,
        int? processId)
    {
        return new CommandInfo(sessionId, commandId, command, CommandState.Executing, queuedTime, processId, startTime, null, string.Empty, string.Empty);
    }

    /// <summary>
    /// Creates a command info for a completed command.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command completed.</param>
    /// <param name="aggregatedOutput">The output stream.</param>
    /// <param name="errorMessage">The error message if failed.</param>
    /// <param name="processId">The process identifier.</param>
    /// <returns>A command info for a completed command.</returns>
    public static CommandInfo Completed(
        string sessionId,
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime startTime,
        DateTime endTime,
        string aggregatedOutput,
        string errorMessage,
        int? processId)
    {
        return new CommandInfo(sessionId, commandId, command, CommandState.Completed, queuedTime, processId, startTime, endTime, aggregatedOutput, errorMessage);
    }

    /// <summary>
    /// Creates a command info for a completed command.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command completed.</param>
    /// <param name="aggregatedOutput">The aggregated output.</param>
    /// <param name="errorMessage">The error message if failed.</param>
    /// <param name="processId">The process identifier.</param>
    /// <returns>A command info for a completed command.</returns>
    public static CommandInfo Failed(
        string sessionId,
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime startTime,
        DateTime endTime,
        string aggregatedOutput,
        string errorMessage,
        int? processId)
    {
        return new CommandInfo(sessionId, commandId, command, CommandState.Failed, queuedTime, processId, startTime, endTime, aggregatedOutput, errorMessage);
    }

    /// <summary>
    /// Creates a command info for a cancelled command.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command was cancelled.</param>
    /// <param name="aggregatedOutput">The aggregated output.</param>
    /// <param name="errorMessage">The error message if failed.</param>
    /// <param name="processId">The process identifier.</param>
    /// <returns>A command info for a cancelled command.</returns>
    public static CommandInfo Cancelled(
        string sessionId,
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime startTime,
        DateTime endTime,
        string aggregatedOutput,
        string errorMessage,
        int? processId)  
    {
        return new CommandInfo(sessionId, commandId, command, CommandState.Cancelled, queuedTime, processId, startTime, endTime, aggregatedOutput, $"Command was cancelled: {errorMessage}");
    }

    /// <summary>
    /// Creates a command info for a timed out command.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="queuedTime">The time when the command was queued.</param>
    /// <param name="startTime">The time when the command started.</param>
    /// <param name="endTime">The time when the command timed out.</param>
    /// <param name="aggregatedOutput">The aggregated output.</param>
    /// <param name="errorMessage">The error message if failed.</param>
    /// <param name="processId">The process identifier.</param>
    /// <returns>A command info for a timed out command.</returns>
    public static CommandInfo TimedOut(
        string sessionId,
        string commandId,
        string command,
        DateTime queuedTime,
        DateTime startTime,
        DateTime endTime,
        string aggregatedOutput,
        string errorMessage,
        int? processId)
    {
        return new CommandInfo(sessionId, commandId, command, CommandState.Timeout, queuedTime, processId, startTime, endTime, aggregatedOutput, $"Command timed out: {errorMessage}");
    }
}

