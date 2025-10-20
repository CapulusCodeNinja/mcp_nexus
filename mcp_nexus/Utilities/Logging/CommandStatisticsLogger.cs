using Microsoft.Extensions.Logging;

namespace mcp_nexus.Utilities.Logging
{
    /// <summary>
    /// Centralized helper for emitting standardized command statistics logs.
    /// </summary>
    public static class CommandStatisticsLogger
    {
        /// <summary>
        /// Logs standardized statistics for a command lifecycle at Information level.
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="header">Short header describing the event, e.g., "Command completed".</param>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="queuedAt">When the command was queued.</param>
        /// <param name="startedAt">When execution started.</param>
        /// <param name="completedAt">When execution completed.</param>
        /// <param name="timeInQueueMs">Milliseconds spent in queue.</param>
        /// <param name="timeExecutionMs">Milliseconds spent executing.</param>
        /// <param name="totalDurationMs">Total milliseconds from queue to completion.</param>
        public static void Log(
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
                "[STATISTICS] {Header}: {CommandId}\r\n" +
                "SessionId: {SessionId}\r\n" +
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


