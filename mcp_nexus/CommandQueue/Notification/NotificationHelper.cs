using mcp_nexus.Notifications;

namespace mcp_nexus.CommandQueue.Notification
{
    /// <summary>
    /// Helper class for fire-and-forget notifications to avoid blocking critical paths
    /// </summary>
    public static class NotificationHelper
    {
        /// <summary>
        /// Sends a command status notification in a fire-and-forget manner
        /// </summary>
        /// <param name="notificationService">The notification service to use for sending the notification.</param>
        /// <param name="logger">The logger for recording any errors that occur.</param>
        /// <param name="sessionId">The ID of the session the command belongs to.</param>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <param name="command">The command text that was executed.</param>
        /// <param name="status">The current status of the command (e.g., "Queued", "Executing", "Completed").</param>
        /// <param name="result">Optional result of the command execution.</param>
        /// <param name="progress">Optional progress percentage (0-100).</param>
        /// <param name="cancellationToken">Cancellation token for the notification task.</param>
        public static void NotifyCommandStatusFireAndForget(
            IMcpNotificationService notificationService,
            ILogger logger,
            string sessionId,
            string commandId,
            string command,
            string status,
            string? result = null,
            int progress = 0,
            CancellationToken cancellationToken = default)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await notificationService.NotifyCommandStatusAsync(
                        sessionId, commandId, status, result ?? string.Empty, string.Empty, progress);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    logger.LogDebug("Command status notification cancelled for {CommandId}", commandId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send {Status} notification for command {CommandId}", status, commandId);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Sends a command heartbeat notification in a fire-and-forget manner
        /// </summary>
        /// <param name="notificationService">The notification service to use for sending the notification.</param>
        /// <param name="logger">The logger for recording any errors that occur.</param>
        /// <param name="sessionId">The ID of the session the command belongs to.</param>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <param name="command">The command text that is being executed.</param>
        /// <param name="elapsed">The elapsed time since the command started executing.</param>
        /// <param name="cancellationToken">Cancellation token for the notification task.</param>
        public static void NotifyCommandHeartbeatFireAndForget(
            IMcpNotificationService notificationService,
            ILogger logger,
            string sessionId,
            string commandId,
            string command,
            TimeSpan elapsed,
            CancellationToken cancellationToken = default)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var status = $"Executing for {elapsed.TotalMinutes:F1} minutes...";
                    await notificationService.NotifyCommandHeartbeatAsync(
                        sessionId, commandId, status, elapsed);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    logger.LogDebug("Command heartbeat notification cancelled for {CommandId}", commandId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send heartbeat for command {CommandId}", commandId);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Sends a command status notification in a fire-and-forget manner (with detailed parameters)
        /// </summary>
        /// <param name="notificationService">The notification service to use for sending the notification.</param>
        /// <param name="logger">The logger for recording any errors that occur.</param>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <param name="command">The command text that was executed.</param>
        /// <param name="status">The current status of the command (e.g., "Queued", "Executing", "Completed").</param>
        /// <param name="progress">Optional progress percentage (0-100).</param>
        /// <param name="message">Optional message describing the current state.</param>
        /// <param name="result">Optional result of the command execution.</param>
        /// <param name="error">Optional error message if the command failed.</param>
        /// <param name="cancellationToken">Cancellation token for the notification task.</param>
        public static void NotifyCommandStatusDetailedFireAndForget(
            IMcpNotificationService notificationService,
            ILogger logger,
            string commandId,
            string command,
            string status,
            int progress = 0,
            string? message = null,
            string? result = null,
            string? error = null,
            CancellationToken cancellationToken = default)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await notificationService.NotifyCommandStatusAsync(
                        commandId, command, status, progress, message ?? string.Empty, result ?? string.Empty, error ?? string.Empty);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    logger.LogDebug("Command status notification cancelled for {CommandId}", commandId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send {Status} notification for command {CommandId}", status, commandId);
                }
            }, cancellationToken);
        }
    }
}
