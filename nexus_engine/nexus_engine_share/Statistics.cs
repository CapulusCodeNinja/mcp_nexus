using System.Text;

using Nexus.Engine.Share.Models;

using NLog;

namespace Nexus.Engine.Share;

/// <summary>
/// Provides centralized, standardized emission of command performance statistics.
/// </summary>
public static class Statistics
{
    /// <summary>
    /// Static lookup array for command state sort order.
    /// Indexed by CommandState enum value for O(1) access.
    /// </summary>
    private static readonly int[] m_StatusSortOrder;

    /// <summary>
    /// Static constructor to initialize the status sort order array.
    /// </summary>
    static Statistics()
    {
        // Initialize sort order array based on CommandState enum - dynamic sizing
        var maxEnumValue = (int)Enum.GetValues(typeof(CommandState)).Cast<CommandState>().Max();
        m_StatusSortOrder = new int[maxEnumValue + 1];
        m_StatusSortOrder[(int)CommandState.Completed] = 1;
        m_StatusSortOrder[(int)CommandState.Failed] = 2;
        m_StatusSortOrder[(int)CommandState.Cancelled] = 3;
        m_StatusSortOrder[(int)CommandState.Timeout] = 4;
        m_StatusSortOrder[(int)CommandState.Queued] = 5;
        m_StatusSortOrder[(int)CommandState.Executing] = 6;
    }

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
    /// <param name="commands">Collection of all commands in the session.</param>
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
        int timedOutCommands,
        IEnumerable<CommandInfo> commands)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine();
        _ = sb.AppendLine("    ╔═ Session Statistics ══════════════════════════════════════════════");
        _ = sb.AppendLine($"    ║ SessionId: {sessionId}");
        _ = sb.AppendLine($"    ║ OpenedAt: {openedAt:yyyy-MM-dd HH:mm:ss.fff}");
        _ = sb.AppendLine($"    ║ ClosedAt: {closedAt:yyyy-MM-dd HH:mm:ss.fff}");
        _ = sb.AppendLine($"    ║ TotalDuration: {totalDuration}");
        _ = sb.AppendLine("    ║ ────────────────────────────────────────────────────────────────────");
        _ = sb.AppendLine($"    ║ TotalCommands: {totalCommands}");
        _ = sb.AppendLine($"    ║ CompletedCommands: {completedCommands}");
        _ = sb.AppendLine($"    ║ FailedCommands: {failedCommands}");
        _ = sb.AppendLine($"    ║ CancelledCommands: {cancelledCommands}");
        _ = sb.AppendLine($"    ║ TimedOutCommands: {timedOutCommands}");

        // Add command table if there are any commands
        if (totalCommands > 0)
        {
            _ = sb.AppendLine("    ║ ────────────────────────────────────────────────────────────────────");
            _ = sb.AppendLine("    ║ CommandId | Command                  | Status    | ReadCount | TimeInQueue         | ExecutionTime         | TotalTime         |");
            _ = sb.AppendLine("    ║ ----------|--------------------------|-----------|-----------|---------------------|-----------------------|-------------------|");

            // Sort commands by status using static array: Completed, Failed, Cancelled, Timeout, Queued, Executing
            var sortedCommands = commands.OrderBy(c => m_StatusSortOrder[(int)c.State]).ThenBy(c => c.CommandNumber);

            foreach (var cmd in sortedCommands)
            {
                var totalTime = cmd.TotalTime.HasValue
                    ? cmd.TotalTime.Value.ToString()
                    : "N/A";

                var executionTime = cmd.ExecutionTime.HasValue
                    ? cmd.ExecutionTime.Value.ToString()
                    : "N/A";

                var queueTime = cmd.TimeInQueue.HasValue
                    ? cmd.TimeInQueue.Value.ToString()
                    : "N/A";

                var commandText = cmd.Command.Length <= 25
                    ? cmd.Command
                    : cmd.Command[..25];

                _ = sb.AppendLine($"    ║ {cmd.CommandNumber,-10} | {commandText,-26} | {cmd.State,-9} | {cmd.ReadCount,-9} | {queueTime,-18} | {executionTime,-18} | {totalTime,-18} |");
            }
        }

        _ = sb.Append("    ╚═══════════════════════════════════════════════════════════════════");

        logger.Info(sb.ToString());
    }
}

