using mcp_nexus.Models;
using System.Text.Json;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Factory for creating standardized MCP notifications
    /// </summary>
    public static class NotificationFactory
    {
        /// <summary>
        /// Creates a command status notification
        /// </summary>
        public static McpNotification CreateCommandStatusNotification(string sessionId, string commandId, string status, int? progress = null, string? message = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["commandId"] = commandId,
                ["status"] = status
            };

            if (progress.HasValue)
                parameters["progress"] = progress.Value;

            if (!string.IsNullOrEmpty(message))
                parameters["message"] = message;

            return new McpNotification
            {
                Method = "notifications/command_status",
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }

        /// <summary>
        /// Creates a command completion notification
        /// </summary>
        public static McpNotification CreateCommandCompletionNotification(string sessionId, string commandId, object result)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["commandId"] = commandId,
                ["result"] = result
            };

            return new McpNotification
            {
                Method = "notifications/command_completion",
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }

        /// <summary>
        /// Creates a command failure notification
        /// </summary>
        public static McpNotification CreateCommandFailureNotification(string sessionId, string commandId, string error, string? details = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["commandId"] = commandId,
                ["error"] = error
            };

            if (!string.IsNullOrEmpty(details))
                parameters["details"] = details;

            return new McpNotification
            {
                Method = "notifications/command_failure",
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }

        /// <summary>
        /// Creates a command heartbeat notification
        /// </summary>
        public static McpNotification CreateCommandHeartbeatNotification(string sessionId, string commandId, string status, int? progress = null, string? message = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["commandId"] = commandId,
                ["status"] = status
            };

            if (progress.HasValue)
                parameters["progress"] = progress.Value;

            if (!string.IsNullOrEmpty(message))
                parameters["message"] = message;

            return new McpNotification
            {
                Method = "notifications/command_heartbeat",
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }

        /// <summary>
        /// Creates a session event notification
        /// </summary>
        public static McpNotification CreateSessionEventNotification(string sessionId, string eventType, object? eventData = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["eventType"] = eventType
            };

            if (eventData != null)
                parameters["eventData"] = eventData;

            return new McpNotification
            {
                Method = "notifications/session_event",
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }

        /// <summary>
        /// Creates a queue event notification
        /// </summary>
        public static McpNotification CreateQueueEventNotification(string sessionId, string eventType, object? eventData = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["eventType"] = eventType
            };

            if (eventData != null)
                parameters["eventData"] = eventData;

            return new McpNotification
            {
                Method = "notifications/queue_event",
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }

        /// <summary>
        /// Creates a recovery notification
        /// </summary>
        public static McpNotification CreateRecoveryNotification(string sessionId, string recoveryType, string status, object? details = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sessionId"] = sessionId,
                ["recoveryType"] = recoveryType,
                ["status"] = status
            };

            if (details != null)
                parameters["details"] = details;

            return new McpNotification
            {
                Method = "notifications/recovery",
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }

        /// <summary>
        /// Creates a generic notification with custom method and parameters
        /// </summary>
        public static McpNotification CreateCustomNotification(string method, object parameters)
        {
            return new McpNotification
            {
                Method = method,
                Params = JsonSerializer.SerializeToElement(parameters)
            };
        }
    }
}
