using Nexus.Protocol.Models;

using NLog;

namespace Nexus.Protocol.Notifications;

/// <summary>
/// Implementation of the MCP notification service.
/// Handles creation and dispatching of server-initiated notifications to clients.
/// </summary>
internal class McpNotificationService : IMcpNotificationService
{
    private readonly Logger m_Logger;
    private readonly INotificationBridge m_NotificationBridge;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpNotificationService"/> class.
    /// </summary>
    /// <param name="notificationBridge">The bridge for sending notifications to clients.</param>
    public McpNotificationService(
        INotificationBridge notificationBridge)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_NotificationBridge = notificationBridge ?? throw new ArgumentNullException(nameof(notificationBridge));
    }

    /// <summary>
    /// Publishes a notification asynchronously.
    /// </summary>
    /// <param name="eventType">The type of event being notified.</param>
    /// <param name="data">The event data to include in the notification.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishNotificationAsync(string eventType, object data)
    {
        try
        {
            var notification = new McpNotification
            {
                JsonRpc = "2.0",
                Method = eventType,
                Params = data,
            };

            await m_NotificationBridge.SendNotificationAsync(notification);
            m_Logger.Debug("Published notification: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to publish notification: {EventType}", eventType);
        }
    }

    /// <summary>
    /// Notifies clients about command status changes.
    /// </summary>
    /// <param name="sessionId">The session identifier associated with the command.</param>
    /// <param name="commandId">The unique command identifier.</param>
    /// <param name="command">The command text that was executed.</param>
    /// <param name="status">The current status string.</param>
    /// <param name="result">Optional result payload for completed commands.</param>
    /// <param name="progress">Progress percentage (0-100) for in-flight commands.</param>
    /// <param name="message">Optional human-readable status message.</param>
    /// <param name="error">Optional error detail if the command failed.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public async Task NotifyCommandStatusAsync(
        string sessionId,
        string commandId,
        string command,
        string status,
        string? result,
        int progress,
        string? message,
        string? error)
    {
        var notification = new McpCommandStatusNotification
        {
            SessionId = sessionId,
            CommandId = commandId,
            Command = command,
            Status = status,
            Result = result,
            Progress = progress,
            Message = message,
            Error = error,
            Timestamp = DateTimeOffset.Now,
        };

        await PublishNotificationAsync("notifications/commandStatus", notification);
    }

    /// <summary>
    /// Notifies clients about command heartbeat (for long-running commands).
    /// </summary>
    /// <param name="sessionId">The session identifier owning the command.</param>
    /// <param name="commandId">The unique command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="elapsed">Elapsed time since command start.</param>
    /// <param name="details">Optional additional details about the heartbeat.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public async Task NotifyCommandHeartbeatAsync(
        string sessionId,
        string commandId,
        string command,
        TimeSpan elapsed,
        string? details)
    {
        var notification = new McpCommandHeartbeatNotification
        {
            SessionId = sessionId,
            CommandId = commandId,
            Command = command,
            ElapsedSeconds = elapsed.TotalSeconds,
            ElapsedDisplay = FormatTimeSpan(elapsed),
            Details = details,
            Timestamp = DateTimeOffset.Now,
        };

        await PublishNotificationAsync("notifications/commandHeartbeat", notification);
    }

    /// <summary>
    /// Notifies clients about session recovery events.
    /// </summary>
    /// <param name="reason">Reason for the recovery action.</param>
    /// <param name="recoveryStep">The recovery step performed.</param>
    /// <param name="success">True if recovery succeeded; otherwise false.</param>
    /// <param name="message">Human-readable message describing the outcome.</param>
    /// <param name="affectedCommands">Optional list of affected command IDs.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public async Task NotifySessionRecoveryAsync(
        string reason,
        string recoveryStep,
        bool success,
        string message,
        string[]? affectedCommands)
    {
        var notification = new McpSessionRecoveryNotification
        {
            Reason = reason,
            RecoveryStep = recoveryStep,
            Success = success,
            Message = message,
            AffectedCommands = affectedCommands,
            Timestamp = DateTimeOffset.Now,
        };

        await PublishNotificationAsync("notifications/sessionRecovery", notification);
    }

    /// <summary>
    /// Notifies clients about server health status.
    /// </summary>
    /// <param name="status">Overall server status string.</param>
    /// <param name="cdbSessionActive">Whether a CDB session is currently active.</param>
    /// <param name="queueSize">The current command queue size.</param>
    /// <param name="activeCommands">The number of actively executing commands.</param>
    /// <param name="uptime">Optional server uptime.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public async Task NotifyServerHealthAsync(
        string status,
        bool cdbSessionActive,
        int queueSize,
        int activeCommands,
        TimeSpan? uptime)
    {
        var notification = new McpServerHealthNotification
        {
            Status = status,
            CdbSessionActive = cdbSessionActive,
            QueueSize = queueSize,
            ActiveCommands = activeCommands,
            Uptime = uptime,
            Timestamp = DateTimeOffset.Now,
        };

        await PublishNotificationAsync("notifications/serverHealth", notification);
    }

    /// <summary>
    /// Notifies clients that the tools list has changed.
    /// </summary>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public async Task NotifyToolsListChangedAsync()
    {
        await PublishNotificationAsync("notifications/tools/listChanged", new { });
    }

    /// <summary>
    /// Notifies clients that the resources list has changed.
    /// </summary>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    public async Task NotifyResourcesListChangedAsync()
    {
        await PublishNotificationAsync("notifications/resources/listChanged", new { });
    }

    /// <summary>
    /// Formats a TimeSpan into a human-readable string (e.g., "2m 5s").
    /// </summary>
    /// <param name="timeSpan">The time span to format.</param>
    /// <returns>The formatted string.</returns>
    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        return timeSpan.TotalHours >= 1
            ? $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s"
            : timeSpan.TotalMinutes >= 1 ? $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s" : $"{timeSpan.Seconds}s";
    }
}
