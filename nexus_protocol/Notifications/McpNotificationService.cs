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
                Params = data
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
            Timestamp = DateTimeOffset.Now
        };

        await PublishNotificationAsync("notifications/commandStatus", notification);
    }

    /// <summary>
    /// Notifies clients about command heartbeat (for long-running commands).
    /// </summary>
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
            Timestamp = DateTimeOffset.Now
        };

        await PublishNotificationAsync("notifications/commandHeartbeat", notification);
    }

    /// <summary>
    /// Notifies clients about session recovery events.
    /// </summary>
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
            Timestamp = DateTimeOffset.Now
        };

        await PublishNotificationAsync("notifications/sessionRecovery", notification);
    }

    /// <summary>
    /// Notifies clients about server health status.
    /// </summary>
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
            Timestamp = DateTimeOffset.Now
        };

        await PublishNotificationAsync("notifications/serverHealth", notification);
    }

    /// <summary>
    /// Notifies clients that the tools list has changed.
    /// </summary>
    public async Task NotifyToolsListChangedAsync()
    {
        await PublishNotificationAsync("notifications/tools/listChanged", new { });
    }

    /// <summary>
    /// Notifies clients that the resources list has changed.
    /// </summary>
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

