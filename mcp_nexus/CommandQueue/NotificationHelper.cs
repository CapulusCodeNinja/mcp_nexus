using mcp_nexus.Notifications;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Helper class for fire-and-forget notifications to avoid blocking critical paths
    /// </summary>
    public static class NotificationHelper
    {
        /// <summary>
        /// Sends a command status notification in a fire-and-forget manner
        /// </summary>
        public static void NotifyCommandStatusFireAndForget(
            IMcpNotificationService notificationService,
            ILogger logger,
            string sessionId,
            string commandId,
            string command,
            string status,
            string? result = null,
            int progress = 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await notificationService.NotifyCommandStatusAsync(
                        sessionId, commandId, command, status, result, progress);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send {Status} notification for command {CommandId}", status, commandId);
                }
            }, CancellationToken.None);
        }

        /// <summary>
        /// Sends a command heartbeat notification in a fire-and-forget manner
        /// </summary>
        public static void NotifyCommandHeartbeatFireAndForget(
            IMcpNotificationService notificationService,
            ILogger logger,
            string sessionId,
            string commandId,
            string command,
            TimeSpan elapsed)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await notificationService.NotifyCommandHeartbeatAsync(
                        sessionId, commandId, command, elapsed);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send heartbeat for command {CommandId}", commandId);
                }
            }, CancellationToken.None);
        }

        /// <summary>
        /// Sends a command status notification in a fire-and-forget manner (without session ID)
        /// </summary>
        public static void NotifyCommandStatusFireAndForget(
            IMcpNotificationService notificationService,
            ILogger logger,
            string commandId,
            string command,
            string status,
            int progress = 0,
            string? message = null,
            string? result = null,
            string? error = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await notificationService.NotifyCommandStatusAsync(
                        commandId, command, status, progress, message, result, error);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send {Status} notification for command {CommandId}", status, commandId);
                }
            }, CancellationToken.None);
        }
    }
}
