using System;
using System.Collections.Generic;
using System.Text.Json;
using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Builder for creating notification messages
    /// </summary>
    public class NotificationMessageBuilder
    {
        private readonly McpNotification _notification;

        public NotificationMessageBuilder()
        {
            _notification = new McpNotification();
        }

        /// <summary>
        /// Sets the method for the notification
        /// </summary>
        /// <param name="method">Method name</param>
        /// <returns>Builder instance</returns>
        public NotificationMessageBuilder SetMethod(string method)
        {
            _notification.Method = method;
            return this;
        }

        /// <summary>
        /// Sets the parameters for the notification
        /// </summary>
        /// <param name="parameters">Parameters object</param>
        /// <returns>Builder instance</returns>
        public NotificationMessageBuilder SetParameters(object parameters)
        {
            _notification.Params = parameters;
            return this;
        }

        /// <summary>
        /// Builds the notification message
        /// </summary>
        /// <returns>McpNotification instance</returns>
        public McpNotification Build()
        {
            return _notification;
        }

        /// <summary>
        /// Builds the notification message as JSON string
        /// </summary>
        /// <returns>JSON string representation</returns>
        public string BuildJson()
        {
            return JsonSerializer.Serialize(_notification);
        }

        /// <summary>
        /// Creates a command status notification
        /// </summary>
        /// <param name="commandId">Command ID</param>
        /// <param name="status">Command status</param>
        /// <param name="result">Command result</param>
        /// <returns>Builder instance</returns>
        public static NotificationMessageBuilder CreateCommandStatusNotification(string commandId, string status, string result = null)
        {
            return new NotificationMessageBuilder()
                .SetMethod("notifications/commandStatus")
                .SetParameters(new
                {
                    commandId,
                    status,
                    result
                });
        }

        /// <summary>
        /// Creates a session event notification
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="eventType">Event type</param>
        /// <param name="data">Event data</param>
        /// <returns>Builder instance</returns>
        public static NotificationMessageBuilder CreateSessionEventNotification(string sessionId, string eventType, object data)
        {
            return new NotificationMessageBuilder()
                .SetMethod("notifications/sessionEvent")
                .SetParameters(new
                {
                    sessionId,
                    eventType,
                    data
                });
        }

        /// <summary>
        /// Creates a server health notification
        /// </summary>
        /// <param name="healthStatus">Health status</param>
        /// <param name="status">Status details</param>
        /// <returns>Builder instance</returns>
        public static NotificationMessageBuilder CreateServerHealthNotification(string healthStatus, string status = null)
        {
            return new NotificationMessageBuilder()
                .SetMethod("notifications/serverHealth")
                .SetParameters(new
                {
                    healthStatus,
                    status
                });
        }
    }
}
