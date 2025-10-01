namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Interface for MCP notification service - maintains compatibility with existing code
    /// </summary>
    public interface IMcpNotificationService
    {
        /// <summary>
        /// Publishes a notification
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="data">Event data</param>
        /// <returns>Task representing the operation</returns>
        Task PublishNotificationAsync(string eventType, object data);

        /// <summary>
        /// Subscribes to notifications
        /// </summary>
        /// <param name="eventType">Event type to subscribe to</param>
        /// <param name="handler">Event handler</param>
        /// <returns>Subscription identifier</returns>
        string Subscribe(string eventType, Func<object, Task> handler);

        /// <summary>
        /// Unsubscribes from notifications
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <returns>True if unsubscribed successfully</returns>
        bool Unsubscribe(string subscriptionId);

        // Additional methods for compatibility with existing code
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status);
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition, string elapsed);
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition);
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, TimeSpan elapsed);
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition);
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, string message);
        Task NotifyCommandStatusAsync(string commandId, string command, string status, int queuePosition, string result, string error, object context);
        Task NotifyCommandStatusAsync(string sessionId, string commandId, string command, string status, string result, string progress);
        Task NotifyCommandStatusAsync(string commandId, string command, string status, string progress, string message, string result, string error);
        Task NotifyCommandHeartbeatAsync(string sessionId, string commandId);
        Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, string elapsed);
        Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, TimeSpan elapsed);
        Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, TimeSpan elapsed);
        Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed, string details);
        Task NotifySessionEventAsync(string sessionId, string eventType, object data);
        Task NotifySessionEventAsync(string sessionId, string eventType, object data, string additionalInfo);
        Task NotifySessionEventAsync(string sessionId, string eventType, object data, object context);
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType);
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details);
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, bool success);
        Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details, bool success);
        Task NotifySessionRecoveryAsync(string reason, string step, bool success, string message);
        Task NotifyServerHealthAsync(string healthStatus);
        Task NotifyServerHealthAsync(string healthStatus, string status);
        Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive);
        Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize);
        Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize, int activeCommands);
        Task NotifyServerHealthAsync(string status, bool cdbSessionActive, int queueSize, int activeCommands);
        Task NotifyToolsListChangedAsync();
    }
}