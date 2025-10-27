using Nexus.Engine.Share.Models;

using NLog;

namespace Nexus.Engine.Share;

/// <summary>
/// Provides centralized, standardized emission of command performance statistics.
/// </summary>
public static class Statistics
{
    /// <summary>
    /// Emits standardized command performance statistics at INFO level.
    /// </summary>
    /// <param name="logger">Logger to write the statistics to.</param>
    /// <param name="status">The current status of the command.</param>
    /// <param name="sessionId">The session identifier associated with the command.</param>
    /// <param name="commandId">The unique command identifier.</param>
    /// <param name="batchCommandId">The unique batch command identifier.</param>
    /// <param name="command">The command text that was executed.</param>
    /// <param name="queuedAt">When the command was queued (local time).</param>
    /// <param name="startedAt">When execution started (local time).</param>
    /// <param name="completedAt">When execution completed (local time).</param>
    /// <param name="timeInQueue">Time spent in the queue before execution began.</param>
    /// <param name="timeExecution">Time spent executing the command.</param>
    /// <param name="totalDuration">Total time from queue entry to completion.</param>
    public static void EmitCommandStats(
        Logger logger,
        CommandState status,
        string sessionId,
        string? commandId,
        string? batchCommandId,
        string? command,
        DateTime queuedAt,
        DateTime startedAt,
        DateTime completedAt,
        TimeSpan timeInQueue,
        TimeSpan timeExecution,
        TimeSpan totalDuration)
    {
        logger.Info("\r\n" +
            "    ┌─ Command Statistics ──────────────────────────────────────────────\r\n" +
            "    │ SessionId: {0}\r\n" +
            "    │ CommandId: {1}\r\n" +
            "    │ BatchCommandId: {2}\r\n" +
            "    │ Status: {3}\r\n" +
            "    │ Command: {4}\r\n" +
            "    │ QueuedAt: {5:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    │ StartedAt: {6:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    │ CompletedAt: {7:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    │ TimeInQueue: {8}\r\n" +
            "    │ TimeExecution: {9}\r\n" +
            "    │ TotalDuration: {10}\r\n" +
            "    └───────────────────────────────────────────────────────────────────",
            sessionId,
            commandId,
            batchCommandId,
            status,
            command,
            queuedAt,
            startedAt,
            completedAt,
            timeInQueue,
            timeExecution,
            totalDuration);
    }

    /// <summary>
    /// Emits standardized session statistics at INFO level.
    /// </summary>
    /// <param name="logger">Logger to write the statistics to.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="openedAt">When the session was opened (local time).</param>
    /// <param name="closedAt">When the session was closed (local time).</param>
    /// <param name="totalDuration">Total session duration</param>
    /// <param name="totalCommands">Total number of commands executed in the session.</param>
    /// <param name="completedCommands">Number of successfully completed commands.</param>
    /// <param name="failedCommands">Number of failed commands.</param>
    /// <param name="cancelledCommands">Number of cancelled commands.</param>
    /// <param name="timedOutCommands">Number of timed out commands.</param>
    public static void EmitSessionStats(
        Logger logger,
        string sessionId,
        DateTime openedAt,
        DateTime closedAt,
        TimeSpan totalDuration,
        int totalCommands,
        int completedCommands,
        int failedCommands,
        int cancelledCommands,
        int timedOutCommands)
    {
        logger.Info("\r\n" +
            "    ╔═ Session Statistics ══════════════════════════════════════════════\r\n" +
            "    ║ SessionId: {0}\r\n" +
            "    ║ OpenedAt: {1:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    ║ ClosedAt: {2:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    ║ TotalDuration: {3}\r\n" +
            "    ║ ────────────────────────────────────────────────────────────────────\r\n" +
            "    ║ TotalCommands: {4}\r\n" +
            "    ║ CompletedCommands: {5}\r\n" +
            "    ║ FailedCommands: {6}\r\n" +
            "    ║ CancelledCommands: {7}\r\n" +
            "    ║ TimedOutCommands: {8}\r\n" +
            "    ╚═══════════════════════════════════════════════════════════════════",
            sessionId,
            openedAt,
            closedAt,
            totalDuration,
            totalCommands,
            completedCommands,
            failedCommands,
            cancelledCommands,
            timedOutCommands);
    }
}

