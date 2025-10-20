using Microsoft.Extensions.Logging;

namespace mcp_nexus.Utilities
{
    /// <summary>
    /// Provides centralized, standardized emission of command performance statistics.
    /// </summary>
    public static class Statistics
    {
        /// <summary>
        /// Emits standardized command performance statistics at INFO level.
        /// </summary>
        /// <param name="logger">Logger to write the statistics to.</param>
        /// <param name="header">Short, human-readable label (for example, "Command completed").</param>
        /// <param name="sessionId">The session identifier associated with the command.</param>
        /// <param name="commandId">The unique command identifier.</param>
        /// <param name="command">The command text that was executed.</param>
        /// <param name="queuedAt">When the command was queued (local time).</param>
        /// <param name="startedAt">When execution started (local time).</param>
        /// <param name="completedAt">When execution completed (local time).</param>
        /// <param name="timeInQueueMs">Milliseconds spent in the queue before execution began.</param>
        /// <param name="timeExecutionMs">Milliseconds spent executing the command.</param>
        /// <param name="totalDurationMs">Total milliseconds from queue entry to completion.</param>
        public static void CommandStats(
            ILogger logger,
            string header,
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
            logger.LogInformation(
                "[STATISTICS] [{SessionId}] [{CommandId}] {Header}:" +
                "Command: {Command}\r\n" +
                "QueuedAt: {QueuedAt:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
                "StartedAt: {StartedAt:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
                "CompletedAt: {CompletedAt:yyyy-MM-dd HH:mm:ss.fff}\r\n" +
                "TimeInQueue: {TimeInQueueMs}ms\r\n" +
                "TimeExecution: {TimeExecutionMs}ms\r\n" +
                "TotalDuration: {TotalDurationMs}ms",
                header,
                commandId,
                sessionId,
                command,
                queuedAt,
                startedAt,
                completedAt,
                timeInQueueMs,
                timeExecutionMs,
                totalDurationMs);
        }
    }
}


