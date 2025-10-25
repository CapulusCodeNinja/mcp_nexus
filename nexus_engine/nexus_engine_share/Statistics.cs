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
    /// <param name="command">The command text that was executed.</param>
    /// <param name="queuedAt">When the command was queued (local time).</param>
    /// <param name="startedAt">When execution started (local time).</param>
    /// <param name="completedAt">When execution completed (local time).</param>
    /// <param name="timeInQueueMs">Milliseconds spent in the queue before execution began.</param>
    /// <param name="timeExecutionMs">Milliseconds spent executing the command.</param>
    /// <param name="totalDurationMs">Total milliseconds from queue entry to completion.</param>
    public static void EmitCommandStats(
        Logger logger,
        CommandState status,
        string sessionId,
        string? commandId,
        string? command,
        DateTime queuedAt,
        DateTime startedAt,
        DateTime completedAt,
        double timeInQueueMs,
        double timeExecutionMs,
        double totalDurationMs)
    {
        logger.Info("\r\n" +
            "    ┌─ Command Statistics ──────────────────────────────────────────────\r\n" +
            "    │ SessionId: {0}\r\n" +
            "    │ CommandId: {1}\r\n" +
            "    │ Status: {2}\r\n" +
            "    │ Command: {3}\r\n" +
            "    │ QueuedAt: {4:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    │ StartedAt: {5:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    │ CompletedAt: {6:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
            "    │ TimeInQueue: {7}ms\r\n" +
            "    │ TimeExecution: {8}ms\r\n" +
            "    │ TotalDuration: {9}ms\r\n" +
            "    └───────────────────────────────────────────────────────────────────",
            sessionId,
            commandId,
            status,
            command,
            queuedAt,
            startedAt,
            completedAt,
            timeInQueueMs,
            timeExecutionMs,
            totalDurationMs);
    }

    /// <summary>
    /// Emits standardized session statistics at INFO level.
    /// </summary>
    /// <param name="logger">Logger to write the statistics to.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="openedAt">When the session was opened (local time).</param>
    /// <param name="closedAt">When the session was closed (local time).</param>
    /// <param name="totalDurationMs">Total session duration in milliseconds.</param>
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
        double totalDurationMs,
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
            "    ║ TotalDuration: {3}ms\r\n" +
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
            totalDurationMs,
            totalCommands,
            completedCommands,
            failedCommands,
            cancelledCommands,
            timedOutCommands);
    }
}

