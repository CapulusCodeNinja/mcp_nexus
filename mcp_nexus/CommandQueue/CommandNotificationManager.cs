using mcp_nexus.Notifications;

namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Manages notifications and heartbeat for command queue operations
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CommandNotificationManager"/> class.
    /// </remarks>
    /// <param name="notificationService">The notification service for sending notifications.</param>
    /// <param name="logger">The logger instance for recording notification operations.</param>
    /// <param name="config">The command queue configuration settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
    public class CommandNotificationManager(
        IMcpNotificationService notificationService,
        ILogger logger,
        CommandQueueConfiguration config)
    {
        private readonly IMcpNotificationService m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        private readonly ILogger m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly CommandQueueConfiguration m_config = config ?? throw new ArgumentNullException(nameof(config));

        /// <summary>
        /// Sends a command status notification (fire and forget).
        /// </summary>
        /// <param name="command">The command to notify about.</param>
        /// <param name="status">The current status of the command.</param>
        /// <param name="result">Optional result of the command execution.</param>
        /// <param name="progress">Optional progress percentage (0-100).</param>
        public void NotifyCommandStatusFireAndForget(QueuedCommand command, string status, string? result = null, int progress = 0)
        {
            NotifyCommandStatusFireAndForget(command.Id ?? string.Empty, command.Command ?? string.Empty, status, result, progress);
        }

        /// <summary>
        /// Sends a command status notification (fire and forget).
        /// </summary>
        /// <param name="commandId">The ID of the command to notify about.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The current status of the command.</param>
        /// <param name="result">Optional result of the command execution.</param>
        /// <param name="progress">Optional progress percentage (0-100).</param>
        public void NotifyCommandStatusFireAndForget(string commandId, string command, string status, string? result = null, int progress = 0)
        {
            try
            {
                // Fire and forget - don't wait for notification to complete
                Task.Run(async () =>
                {
                    try
                    {
                        await m_notificationService.NotifyCommandStatusAsync(commandId, command, status, progress, result ?? string.Empty, string.Empty);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send command status notification for {CommandId}", commandId);
                    }
                });
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error starting command status notification task for {CommandId}", commandId);
            }
        }

        /// <summary>
        /// Sends a command heartbeat notification (fire and forget).
        /// </summary>
        /// <param name="command">The command to send heartbeat for.</param>
        /// <param name="elapsed">The elapsed time since the command started.</param>
        public void NotifyCommandHeartbeatFireAndForget(QueuedCommand command, TimeSpan elapsed)
        {
            try
            {
                // Fire and forget - don't wait for notification to complete
                Task.Run(async () =>
                {
                    try
                    {
                        var status = $"Executing for {elapsed.TotalMinutes:F1} minutes...";
                        var progress = Math.Min(95, (int)(elapsed.TotalMinutes * 10)); // Rough progress based on time

                        await m_notificationService.NotifyCommandStatusAsync(
                            command.Id ?? string.Empty,
                            command.Command ?? string.Empty,
                            status,
                            progress,
                            string.Empty,
                            string.Empty,
                            string.Empty);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogTrace(ex, "Failed to send heartbeat notification for {CommandId}", command.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                m_logger.LogTrace(ex, "Error starting heartbeat notification task for {CommandId}", command.Id);
            }
        }

        /// <summary>
        /// Creates a status message for a queued command.
        /// </summary>
        /// <param name="queuePosition">The position of the command in the queue.</param>
        /// <param name="elapsed">The elapsed time since the command was queued.</param>
        /// <returns>A formatted status message for the queued command.</returns>
        public string CreateQueuedStatusMessage(int queuePosition, TimeSpan elapsed)
        {
            var remainingMinutes = Math.Max(3, queuePosition * 2); // Estimate
            var remainingSeconds = Math.Max(5, queuePosition * 10); // Estimate

            return m_config.GetQueuedStatusMessage(queuePosition, elapsed, remainingMinutes, remainingSeconds);
        }

        /// <summary>
        /// Calculates progress percentage for a queued command
        /// </summary>
        public int CalculateQueueProgress(int queuePosition, TimeSpan elapsed)
        {
            return m_config.CalculateProgressPercentage(queuePosition, elapsed);
        }

        /// <summary>
        /// Sends notifications for command queue events
        /// </summary>
        public void NotifyQueueEvent(string eventType, string message, object? data = null)
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        // This could be extended to send queue-level notifications
                        m_logger.LogInformation("Queue Event [{EventType}]: {Message}", eventType, message);

                        // If we had a queue event notification method, we'd call it here
                        // await m_notificationService.NotifyQueueEventAsync(eventType, message, data);
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send queue event notification: {EventType}", eventType);
                    }
                });
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error starting queue event notification task: {EventType}", eventType);
            }
        }

        /// <summary>
        /// Notifies about service shutdown
        /// </summary>
        public void NotifyServiceShutdown(string reason)
        {
            NotifyQueueEvent("ServiceShutdown", $"Command queue service shutting down: {reason}");
        }

        /// <summary>
        /// Notifies about service startup
        /// </summary>
        public void NotifyServiceStartup()
        {
            NotifyQueueEvent("ServiceStartup", $"Command queue service started for session {m_config.SessionId}");
        }

        /// <summary>
        /// Notifies about command cancellation
        /// </summary>
        public void NotifyCommandCancellation(string commandId, string reason)
        {
            NotifyQueueEvent("CommandCancelled", $"Command {commandId} cancelled: {reason}");
        }

        /// <summary>
        /// Notifies about multiple command cancellation
        /// </summary>
        public void NotifyBulkCommandCancellation(int count, string reason)
        {
            NotifyQueueEvent("BulkCommandCancellation", $"Cancelled {count} commands: {reason}");
        }
    }
}
