namespace Nexus.Protocol.Notifications;

/// <summary>
/// Interface for the Model Context Protocol (MCP) notification service.
/// Provides methods for publishing server-initiated notifications to clients.
/// </summary>
public interface IMcpNotificationService
{
    /// <summary>
    /// Publishes a notification asynchronously.
    /// </summary>
    /// <param name="eventType">The type of event being notified.</param>
    /// <param name="data">The event data to include in the notification.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishNotificationAsync(string eventType, object data);

    /// <summary>
    /// Notifies clients about command status changes.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="status">The current command status.</param>
    /// <param name="result">The command result, if available.</param>
    /// <param name="progress">The progress percentage (0-100).</param>
    /// <param name="message">Additional status message.</param>
    /// <param name="error">Error message, if failed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyCommandStatusAsync(
        string sessionId,
        string commandId,
        string command,
        string status,
        string? result,
        int progress,
        string? message,
        string? error);

    /// <summary>
    /// Notifies clients about command heartbeat (for long-running commands).
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="commandId">The command identifier.</param>
    /// <param name="command">The command text.</param>
    /// <param name="elapsed">The elapsed time since command started.</param>
    /// <param name="details">Additional heartbeat details.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyCommandHeartbeatAsync(
        string sessionId,
        string commandId,
        string command,
        TimeSpan elapsed,
        string? details);

    /// <summary>
    /// Notifies clients about session recovery events.
    /// </summary>
    /// <param name="reason">The reason for recovery.</param>
    /// <param name="recoveryStep">The recovery step description.</param>
    /// <param name="success">Whether recovery was successful.</param>
    /// <param name="message">Additional recovery message.</param>
    /// <param name="affectedCommands">Command IDs affected by recovery.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifySessionRecoveryAsync(
        string reason,
        string recoveryStep,
        bool success,
        string message,
        string[]? affectedCommands);

    /// <summary>
    /// Notifies clients about server health status.
    /// </summary>
    /// <param name="status">The health status.</param>
    /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
    /// <param name="queueSize">The current command queue size.</param>
    /// <param name="activeCommands">The number of active commands.</param>
    /// <param name="uptime">The server uptime.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyServerHealthAsync(
        string status,
        bool cdbSessionActive,
        int queueSize,
        int activeCommands,
        TimeSpan? uptime);

    /// <summary>
    /// Notifies clients that the tools list has changed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyToolsListChangedAsync();

    /// <summary>
    /// Notifies clients that the resources list has changed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyResourcesListChangedAsync();
}

