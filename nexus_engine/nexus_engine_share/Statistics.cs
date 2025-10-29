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
    /// Initializes static members of the <see cref="Statistics"/> class.
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
    /// <param name="batchSize">Optional batch size when command is part of a batch; null or 1 for single.</param>
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
        TimeSpan totalDuration,
        int? batchSize = null)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine();
        _ = sb.AppendLine("    ┌─ Command Statistics ──────────────────────────────────────────────");
        _ = sb.AppendLine($"    │ SessionId: {sessionId}");
        _ = sb.AppendLine($"    │ CommandId: {commandId}");
        _ = sb.AppendLine($"    │ BatchCommandId: {batchCommandId}");
        _ = sb.AppendLine($"    │ Command: {command}");
        _ = sb.AppendLine($"    │ Status: {status}");
        _ = sb.AppendLine("    │ ──────────────────────────────────────────────────────────────────");

        // Context header clarifying execution scope
        if (!string.IsNullOrWhiteSpace(batchCommandId))
        {
            var size = (batchSize.HasValue && batchSize.Value > 1) ? batchSize.Value : 0;
            var scopeLine = size > 1
                ? $"    │ Batched execution ({size} commands) — ExecutionTime = batch wall time"
                : "    │ Batched execution — ExecutionTime = batch wall time";
            _ = sb.AppendLine(scopeLine);
        }
        else
        {
            _ = sb.AppendLine("    │ Single-command execution");
        }

        _ = sb.AppendLine($"    │ QueuedAt: {queuedAt:yyyy-MM-dd HH:mm:ss.fff}");
        _ = sb.AppendLine($"    │ StartedAt: {startedAt:yyyy-MM-dd HH:mm:ss.fff}");
        _ = sb.AppendLine($"    │ CompletedAt: {completedAt:yyyy-MM-dd HH:mm:ss.fff}");
        _ = sb.AppendLine($"    │ TimeInQueue: {timeInQueue}");
        _ = sb.AppendLine($"    │ TimeExecution: {timeExecution}");
        _ = sb.AppendLine($"    │ TotalDuration: {totalDuration}");
        _ = sb.Append("    └───────────────────────────────────────────────────────────────────");

        logger.Info(sb.ToString());
    }

    /// <summary>
    /// Emits standardized session statistics at INFO level.
    /// </summary>
    /// <param name="logger">Logger to write the statistics to.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="openedAt">When the session was opened (local time).</param>
    /// <param name="closedAt">When the session was closed (local time).</param>
    /// <param name="totalDuration">Total session duration.</param>
    /// <param name="totalCommands">Total number of commands executed in the session.</param>
    /// <param name="completedCommands">Number of successfully completed commands.</param>
    /// <param name="failedCommands">Number of failed commands.</param>
    /// <param name="cancelledCommands">Number of cancelled commands.</param>
    /// <param name="timedOutCommands">Number of timed out commands.</param>
    /// <param name="commands">Collection of all commands in the session.</param>
    /// <param name="commandIdToBatchId">Optional mapping of individual command IDs to batch command IDs for summary metrics.</param>
    /// <param name="closeReason">Optional reason for session closure (e.g., IdleTimeout, UserRequest).</param>
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
        IEnumerable<CommandInfo> commands,
        IDictionary<string, string?>? commandIdToBatchId = null,
        string? closeReason = null)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine();
        _ = sb.AppendLine("    ╔═ Session Statistics ═════════════════════════════════════════════════════════════════════════════════════════════════════");
        _ = sb.AppendLine($"    ║ SessionId: {sessionId}");
        _ = sb.AppendLine($"    ║ OpenedAt: {openedAt:yyyy-MM-dd HH:mm:ss.fff}");
        _ = sb.AppendLine($"    ║ ClosedAt: {closedAt:yyyy-MM-dd HH:mm:ss.fff}");
        _ = sb.AppendLine($"    ║ TotalDuration: {totalDuration}");
        if (!string.IsNullOrWhiteSpace(closeReason))
        {
            _ = sb.AppendLine($"    ║ CloseReason: {closeReason}");
        }

        _ = sb.AppendLine("    ║ ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
        _ = sb.AppendLine($"    ║ TotalCommands: {totalCommands}");
        _ = sb.AppendLine($"    ║ CompletedCommands: {completedCommands}");
        _ = sb.AppendLine($"    ║ FailedCommands: {failedCommands}");
        _ = sb.AppendLine($"    ║ CancelledCommands: {cancelledCommands}");
        _ = sb.AppendLine($"    ║ TimedOutCommands: {timedOutCommands}");

        // Add command table if there are any commands
        if (totalCommands > 0)
        {
            _ = sb.AppendLine("    ║ ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
            _ = sb.AppendLine("    ║ Legend: ExecutionTime = batch wall time when BatchCommandId is present; single-command otherwise.");
            _ = sb.AppendLine("    ║ ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
            _ = sb.AppendLine("    ║ CommandId  | Command                    | Status    | RC | TimeInQueue        | ExecutionTime      | TotalTime          |");
            _ = sb.AppendLine("    ║ -----------|----------------------------|-----------|----|--------------------|--------------------|--------------------|");

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

                _ = sb.AppendLine($"    ║ {cmd.CommandNumber,-10} | {commandText,-26} | {cmd.State,-9} | {cmd.ReadCount,-2} | {queueTime,-18} | {executionTime,-18} | {totalTime,-18} |");
            }

            // Append compact Batch Summary block
            _ = sb.AppendLine("    ║ ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────");
            _ = sb.AppendLine("    ║ Batch Summary");

            // Compute overall
            var allQueue = commands.Select(c => c.TimeInQueue).Where(t => t.HasValue).Select(t => t!.Value).ToList();
            var allExec = commands.Select(c => c.ExecutionTime).Where(t => t.HasValue).Select(t => t!.Value).ToList();

            // Compute batched vs single using provided mapping
            var hasMap = commandIdToBatchId != null && commandIdToBatchId.Count > 0;
            var batchedGroups = new Dictionary<string, List<CommandInfo>>();
            var singleCommands = new List<CommandInfo>();

            if (hasMap)
            {
                foreach (var cmd in commands)
                {
                    if (commandIdToBatchId!.TryGetValue(cmd.CommandId, out var bId) && !string.IsNullOrWhiteSpace(bId))
                    {
                        if (!batchedGroups.TryGetValue(bId!, out var list))
                        {
                            list = new List<CommandInfo>();
                            batchedGroups[bId!] = list;
                        }

                        list.Add(cmd);
                    }
                    else
                    {
                        singleCommands.Add(cmd);
                    }
                }
            }

            var batchedCount = hasMap ? batchedGroups.Values.Sum(g => g.Count) : 0;
            var singleCount = hasMap ? singleCommands.Count : totalCommands;
            var batchCount = hasMap ? batchedGroups.Count : 0;
            var avgBatchSize = hasMap && batchCount > 0 ? (batchedCount / (double)batchCount) : 0.0;
            var batchedPct = totalCommands > 0 ? (batchedCount * 100.0 / totalCommands) : 0.0;

            _ = sb.AppendLine(hasMap
                ? $"    ║   Commands: total={totalCommands}, batched={batchedCount} ({batchedPct:F1}%), single={singleCount} ({100.0 - batchedPct:F1}%), avg batch size={avgBatchSize:F1}"
                : $"    ║   Commands: total={totalCommands} (batch details unavailable)");

            // Percentiles helpers
            static TimeSpan? Pctl(IReadOnlyList<TimeSpan> values, double p)
            {
                if (values == null || values.Count == 0)
                {
                    return null;
                }

                var ordered = values.OrderBy(v => v).ToList();
                var idx = (int)Math.Ceiling(p * ordered.Count) - 1;
                if (idx < 0)
                {
                    idx = 0;
                }

                if (idx >= ordered.Count)
                {
                    idx = ordered.Count - 1;
                }

                return ordered[idx];
            }

            var allQ50 = Pctl(allQueue, 0.50);
            var allQ95 = Pctl(allQueue, 0.95);
            _ = sb.AppendLine($"    ║   TimeInQueue p50/p95 (all): {allQ50?.ToString() ?? "N/A"} / {allQ95?.ToString() ?? "N/A"}");

            if (hasMap)
            {
                var batchedQueue = batchedGroups.Values.SelectMany(g => g).Select(c => c.TimeInQueue).Where(t => t.HasValue).Select(t => t!.Value).ToList();
                var singleQueue = singleCommands.Select(c => c.TimeInQueue).Where(t => t.HasValue).Select(t => t!.Value).ToList();
                var bQ50 = Pctl(batchedQueue, 0.50);
                var bQ95 = Pctl(batchedQueue, 0.95);
                var sQ50 = Pctl(singleQueue, 0.50);
                var sQ95 = Pctl(singleQueue, 0.95);
                _ = sb.AppendLine($"    ║   TimeInQueue p50/p95 (batched vs single): {bQ50?.ToString() ?? "N/A"} / {bQ95?.ToString() ?? "N/A"}  |  {sQ50?.ToString() ?? "N/A"} / {sQ95?.ToString() ?? "N/A"}");

                // ExecutionTime percentiles
                var batchWallTimes = new List<TimeSpan>();
                foreach (var kvp in batchedGroups)
                {
                    var rep = kvp.Value.FirstOrDefault(c => c.ExecutionTime.HasValue);
                    if (rep != null && rep.ExecutionTime.HasValue)
                    {
                        batchWallTimes.Add(rep.ExecutionTime!.Value);
                    }
                }

                var singleExec = singleCommands.Select(c => c.ExecutionTime).Where(t => t.HasValue).Select(t => t!.Value).ToList();
                var bE50 = Pctl(batchWallTimes, 0.50);
                var bE95 = Pctl(batchWallTimes, 0.95);
                var sE50 = Pctl(singleExec, 0.50);
                var sE95 = Pctl(singleExec, 0.95);
                _ = sb.AppendLine($"    ║   ExecutionTime p50/p95 (batched wall): {bE50?.ToString() ?? "N/A"} / {bE95?.ToString() ?? "N/A"}");
                _ = sb.AppendLine($"    ║   ExecutionTime p50/p95 (single):       {sE50?.ToString() ?? "N/A"} / {sE95?.ToString() ?? "N/A"}");

                // Top 3 slowest batches
                var topBatches = batchedGroups
                    .Select(g => new
                    {
                        BatchId = g.Key,
                        Size = g.Value.Count,
                        Wall = g.Value.FirstOrDefault(c => c.ExecutionTime.HasValue)?.ExecutionTime ?? TimeSpan.Zero,
                    })
                    .OrderByDescending(x => x.Wall)
                    .Take(3)
                    .ToList();

                if (topBatches.Count > 0)
                {
                    _ = sb.AppendLine("    ║   Slowest batches:");
                    foreach (var b in topBatches)
                    {
                        _ = sb.AppendLine($"    ║     - BatchId: {b.BatchId}, size={b.Size}, ExecutionTime={b.Wall}");
                    }
                }
            }
        }

        _ = sb.Append("    ╚══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════");

        logger.Info(sb.ToString());
    }
}
