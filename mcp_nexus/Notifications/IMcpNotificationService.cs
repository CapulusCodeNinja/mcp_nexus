using System;
using System.Threading.Tasks;
using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Interface for MCP notification service - maintains compatibility with existing code.
    /// Provides methods for publishing, subscribing to, and managing notifications in the MCP system.
    /// </summary>
    public interface IMcpNotificationService
    {
        /// <summary>
        /// Publishes a notification asynchronously.
        /// </summary>
        /// <param name="eventType">The type of event to publish.</param>
        /// <param name="data">The event data to publish.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task PublishNotificationAsync(string eventType, object data);


        /// <summary>
        /// Subscribes to notifications of a specific event type.
        /// </summary>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="handler">The event handler to call when the event occurs.</param>
        /// <returns>
        /// A subscription identifier that can be used to unsubscribe.
        /// </returns>
        string Subscribe(string eventType, Func<object, Task> handler);

        /// <summary>
        /// Subscribes to notifications of a specific event type with strongly-typed handler.
        /// </summary>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="handler">The strongly-typed event handler to call when the event occurs.</param>
        /// <returns>
        /// A subscription identifier that can be used to unsubscribe.
        /// </returns>
        string Subscribe(string eventType, Func<McpNotification, Task> handler);

        /// <summary>
        /// Unsubscribes from notifications using the subscription identifier.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier returned from Subscribe.</param>
        /// <returns>
        /// <c>true</c> if unsubscribed successfully; otherwise, <c>false</c>.
        /// </returns>
        bool Unsubscribe(string subscriptionId);

        // Additional methods for compatibility with existing code

        /// <summary>
        /// Notifies about command status changes.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The queue position as a string.</param>
        /// <param name="elapsed">The elapsed time as a string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition, string elapsed);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The queue position as a string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The queue position as an integer.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, TimeSpan elapsed);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The queue position as an integer.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The queue position as an integer.</param>
        /// <param name="message">Additional message information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, string message);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="queuePosition">The queue position as an integer.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="context">Additional context information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string commandId, string command, string status, int queuePosition, string result, string error, object context);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="progress">The progress percentage.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string commandId, string command, string status, int progress, string result, string error);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="progress">The progress percentage.</param>
        /// <param name="result">The command result.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string commandId, string command, string status, int progress, string result);

        /// <summary>
        /// Notifies about command status changes with detailed information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result. Can be null.</param>
        /// <param name="progress">The progress percentage.</param>
        /// <param name="message">Additional message information. Can be null.</param>
        /// <param name="error">The error message, if any. Can be null.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string command, string status, string? result, int progress, string? message, string? error);
        /// <summary>
        /// Notifies about command heartbeat events.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandHeartbeatAsync(string sessionId, string commandId);

        /// <summary>
        /// Notifies about command heartbeat events with status and elapsed time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="elapsed">The elapsed time as a string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, string elapsed);

        /// <summary>
        /// Notifies about command heartbeat events with status and elapsed time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, TimeSpan elapsed);

        /// <summary>
        /// Notifies about command heartbeat events with elapsed time and details.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <param name="details">Additional details about the heartbeat.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed, string details);

        /// <summary>
        /// Notifies about command heartbeat events with elapsed time.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed);
        /// <summary>
        /// Notifies about session events.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="eventType">The type of event.</param>
        /// <param name="data">The event data.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionEventAsync(string sessionId, string eventType, object data);

        /// <summary>
        /// Notifies about session events with additional information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="eventType">The type of event.</param>
        /// <param name="data">The event data.</param>
        /// <param name="additionalInfo">Additional information about the event.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionEventAsync(string sessionId, string eventType, object data, string additionalInfo);

        /// <summary>
        /// Notifies about session events with context information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="eventType">The type of event.</param>
        /// <param name="data">The event data.</param>
        /// <param name="context">Additional context information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionEventAsync(string sessionId, string eventType, object data, object context);
        /// <summary>
        /// Notifies about session recovery events.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The type of recovery operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType);

        /// <summary>
        /// Notifies about session recovery events with status and details.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The type of recovery operation.</param>
        /// <param name="status">The recovery status.</param>
        /// <param name="details">Additional details about the recovery.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details);

        /// <summary>
        /// Notifies about session recovery events with status and success indicator.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The type of recovery operation.</param>
        /// <param name="status">The recovery status.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, bool success);

        /// <summary>
        /// Notifies about session recovery events with status, details, and success indicator.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The type of recovery operation.</param>
        /// <param name="status">The recovery status.</param>
        /// <param name="details">Additional details about the recovery.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details, bool success);

        /// <summary>
        /// Notifies about session recovery events with reason, step, success, and message.
        /// </summary>
        /// <param name="reason">The reason for the recovery.</param>
        /// <param name="step">The recovery step.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <param name="message">Additional message about the recovery.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionRecoveryAsync(string reason, string step, bool success, string message);

        /// <summary>
        /// Notifies about session recovery events with reason, step, success, message, and affected commands.
        /// </summary>
        /// <param name="reason">The reason for the recovery.</param>
        /// <param name="step">The recovery step.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <param name="message">Additional message about the recovery.</param>
        /// <param name="affectedCommands">Array of command identifiers affected by the recovery.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifySessionRecoveryAsync(string reason, string step, bool success, string message, string[] affectedCommands);
        /// <summary>
        /// Notifies about server health status.
        /// </summary>
        /// <param name="healthStatus">The health status of the server.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyServerHealthAsync(string healthStatus);

        /// <summary>
        /// Notifies about server health status with additional status information.
        /// </summary>
        /// <param name="healthStatus">The health status of the server.</param>
        /// <param name="status">Additional status information.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyServerHealthAsync(string healthStatus, string status);

        /// <summary>
        /// Notifies about server health status with CDB session status.
        /// </summary>
        /// <param name="healthStatus">The health status of the server.</param>
        /// <param name="status">Additional status information.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive);

        /// <summary>
        /// Notifies about server health status with CDB session status and queue size.
        /// </summary>
        /// <param name="healthStatus">The health status of the server.</param>
        /// <param name="status">Additional status information.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <param name="queueSize">The current queue size.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize);

        /// <summary>
        /// Notifies about server health status with CDB session status, queue size, and active commands count.
        /// </summary>
        /// <param name="healthStatus">The health status of the server.</param>
        /// <param name="status">Additional status information.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <param name="queueSize">The current queue size.</param>
        /// <param name="activeCommands">The number of active commands.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize, int activeCommands);

        /// <summary>
        /// Notifies about server health status with status, CDB session status, queue size, and active commands count.
        /// </summary>
        /// <param name="status">The status information.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <param name="queueSize">The current queue size.</param>
        /// <param name="activeCommands">The number of active commands.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyServerHealthAsync(string status, bool cdbSessionActive, int queueSize, int activeCommands);

        /// <summary>
        /// Notifies about changes to the tools list.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task NotifyToolsListChangedAsync();
    }
}