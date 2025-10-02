using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Factory for creating notification services and related components
    /// </summary>
    public static class NotificationFactory
    {
        /// <summary>
        /// Creates a new MCP notification service
        /// </summary>
        /// <returns>New notification service instance</returns>
        public static IMcpNotificationService CreateNotificationService()
        {
            return new McpNotificationService();
        }

        /// <summary>
        /// Creates a new stdio notification bridge
        /// </summary>
        /// <param name="notificationService">Notification service to bridge</param>
        /// <returns>New stdio notification bridge instance</returns>
        public static IStdioNotificationBridge CreateStdioNotificationBridge(IMcpNotificationService notificationService)
        {
            return new StdioNotificationBridge(notificationService);
        }

        /// <summary>
        /// Creates a notification with the specified type and data
        /// </summary>
        /// <param name="method">Notification method</param>
        /// <param name="params">Notification parameters</param>
        /// <returns>New notification instance</returns>
        public static McpNotification CreateNotification(string method, object? @params = null)
        {
            return new McpNotification
            {
                Method = method,
                Params = @params
            };
        }

        /// <summary>
        /// Creates a command status notification
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <param name="status">Command status</param>
        /// <param name="result">Command result</param>
        /// <param name="error">Error message if any</param>
        /// <returns>New command status notification</returns>
        public static McpNotification CreateCommandStatusNotification(string commandId, string status, string result = "", string error = "")
        {
            return new McpNotification
            {
                Method = "notifications/commandStatus",
                Params = new { CommandId = commandId, Status = status, Result = result, Error = error }
            };
        }

        public static McpNotification CreateCommandStatusNotification(string sessionId, string commandId, string status, int progress, string message)
        {
            return new McpNotification
            {
                Method = "notifications/command_status",
                Params = new { SessionId = sessionId, CommandId = commandId, Status = status, Progress = progress, Message = message }
            };
        }

        public static McpNotification CreateCommandCompletionNotification(string commandId, string result, string error = "")
        {
            return new McpNotification
            {
                Method = "notifications/commandCompletion",
                Params = new { CommandId = commandId, Result = result, Error = error }
            };
        }

        /// <summary>
        /// Creates a session event notification
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventType">Event type</param>
        /// <param name="data">Event data</param>
        /// <returns>New session event notification</returns>
        public static McpNotification CreateSessionEventNotification(string sessionId, string eventType, object data)
        {
            return new McpNotification
            {
                Method = "notifications/sessionEvent",
                Params = new { SessionId = sessionId, EventType = eventType, Data = data }
            };
        }

        /// <summary>
        /// Creates a server health notification
        /// </summary>
        /// <param name="status">Health status</param>
        /// <param name="details">Health details</param>
        /// <returns>New server health notification</returns>
        public static McpNotification CreateServerHealthNotification(string status, object details)
        {
            return new McpNotification
            {
                Method = "notifications/serverHealth",
                Params = new { Status = status, Details = details }
            };
        }

        /// <summary>
        /// Creates a notification handler for testing
        /// </summary>
        /// <param name="action">Action to perform when notification is received</param>
        /// <returns>New notification handler</returns>
        public static Func<object, Task> CreateTestHandler(Action<object> action)
        {
            return (notification) =>
            {
                action(notification);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Creates a notification handler that collects notifications
        /// </summary>
        /// <param name="notifications">List to collect notifications into</param>
        /// <returns>New notification handler</returns>
        public static Func<object, Task> CreateCollectingHandler(List<object> notifications)
        {
            return (notification) =>
            {
                notifications.Add(notification);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Creates a notification handler that logs to console
        /// </summary>
        /// <param name="logger">Optional logger instance</param>
        /// <returns>New notification handler</returns>
        public static Func<object, Task> CreateLoggingHandler(ILogger? logger = null)
        {
            return (notification) =>
            {
                var message = $"Notification received: {notification}";
                if (logger != null)
                {
                    logger.LogInformation(message);
                }
                else
                {
                    Console.WriteLine(message);
                }
                return Task.CompletedTask;
            };
        }
    }

}
