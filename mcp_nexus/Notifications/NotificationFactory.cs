using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Factory for creating notification services and related components.
    /// Provides static methods for creating various types of notifications and notification handlers.
    /// </summary>
    public static class NotificationFactory
    {
        /// <summary>
        /// Creates a new MCP notification service instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="IMcpNotificationService"/> instance.
        /// </returns>
        public static IMcpNotificationService CreateNotificationService()
        {
            return new McpNotificationService();
        }

        /// <summary>
        /// Creates a new stdio notification bridge instance.
        /// </summary>
        /// <param name="notificationService">The notification service to bridge.</param>
        /// <returns>
        /// A new <see cref="IStdioNotificationBridge"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="notificationService"/> is null.</exception>
        public static IStdioNotificationBridge CreateStdioNotificationBridge(IMcpNotificationService notificationService)
        {
            return new StdioNotificationBridge(notificationService);
        }

        /// <summary>
        /// Creates a notification with the specified method and parameters.
        /// </summary>
        /// <param name="method">The notification method.</param>
        /// <param name="params">The notification parameters. Can be null.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance.
        /// </returns>
        public static McpNotification CreateNotification(string method, object? @params = null)
        {
            return new McpNotification
            {
                Method = method,
                Params = @params
            };
        }

        /// <summary>
        /// Creates a command status notification.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result. Default is empty string.</param>
        /// <param name="error">The error message, if any. Default is empty string.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for command status.
        /// </returns>
        public static McpNotification CreateCommandStatusNotification(string commandId, string status, string result = "", string error = "")
        {
            return new McpNotification
            {
                Method = "notifications/command_status",
                Params = new { CommandId = commandId, Status = status, Result = result, Error = error }
            };
        }

        /// <summary>
        /// Creates a command status notification with session information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="progress">The progress percentage.</param>
        /// <param name="message">The status message.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for command status.
        /// </returns>
        public static McpNotification CreateCommandStatusNotification(string sessionId, string commandId, string status, int progress, string message)
        {
            return new McpNotification
            {
                Method = "notifications/command_status",
                Params = new { SessionId = sessionId, CommandId = commandId, Status = status, Progress = progress, Message = message }
            };
        }

        /// <summary>
        /// Creates a command completion notification.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any. Default is empty string.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for command completion.
        /// </returns>
        public static McpNotification CreateCommandCompletionNotification(string commandId, string result, string error = "")
        {
            return new McpNotification
            {
                Method = "notifications/command_completion",
                Params = new { CommandId = commandId, Result = result, Error = error }
            };
        }

        /// <summary>
        /// Creates a session event notification.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="data">The event data.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for session events.
        /// </returns>
        public static McpNotification CreateSessionEventNotification(string sessionId, string eventType, object data)
        {
            return new McpNotification
            {
                Method = "notifications/session_event",
                Params = new { SessionId = sessionId, EventType = eventType, Data = data }
            };
        }

        /// <summary>
        /// Creates a server health notification.
        /// </summary>
        /// <param name="status">The health status.</param>
        /// <param name="details">The health details.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for server health.
        /// </returns>
        public static McpNotification CreateServerHealthNotification(string status, object details)
        {
            return new McpNotification
            {
                Method = "notifications/server_health",
                Params = new { Status = status, Details = details }
            };
        }

        /// <summary>
        /// Creates a notification handler for testing purposes.
        /// </summary>
        /// <param name="action">The action to perform when a notification is received.</param>
        /// <returns>
        /// A new notification handler function.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
        public static Func<object, Task> CreateTestHandler(Action<object> action)
        {
            return (notification) =>
            {
                action(notification);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Creates a notification handler that collects notifications into a list.
        /// </summary>
        /// <param name="notifications">The list to collect notifications into.</param>
        /// <returns>
        /// A new notification handler function that adds notifications to the list.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="notifications"/> is null.</exception>
        public static Func<object, Task> CreateCollectingHandler(List<object> notifications)
        {
            return (notification) =>
            {
                notifications.Add(notification);
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Creates a notification handler that logs notifications to console or logger.
        /// </summary>
        /// <param name="logger">Optional logger instance. If null, logs to console.</param>
        /// <returns>
        /// A new notification handler function that logs notifications.
        /// </returns>
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

        /// <summary>
        /// Creates a command failure notification.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="error">The error message.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for command failure.
        /// </returns>
        public static McpNotification CreateCommandFailureNotification(string commandId, string error)
        {
            return new McpNotification
            {
                Method = "notifications/command_failure",
                Params = new { CommandId = commandId, Error = error }
            };
        }

        /// <summary>
        /// Creates a command heartbeat notification.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for command heartbeat.
        /// </returns>
        public static McpNotification CreateCommandHeartbeatNotification(string commandId, TimeSpan elapsed)
        {
            return new McpNotification
            {
                Method = "notifications/command_heartbeat",
                Params = new { CommandId = commandId, Elapsed = elapsed.TotalMilliseconds }
            };
        }

        /// <summary>
        /// Creates a queue event notification.
        /// </summary>
        /// <param name="eventType">The event type.</param>
        /// <param name="data">The event data.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for queue events.
        /// </returns>
        public static McpNotification CreateQueueEventNotification(string eventType, object data)
        {
            return new McpNotification
            {
                Method = "notifications/queue_event",
                Params = new { EventType = eventType, Data = data }
            };
        }

        /// <summary>
        /// Creates a recovery notification.
        /// </summary>
        /// <param name="reason">The recovery reason.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance for recovery events.
        /// </returns>
        public static McpNotification CreateRecoveryNotification(string reason, bool success)
        {
            return new McpNotification
            {
                Method = "notifications/recovery",
                Params = new { Reason = reason, Success = success }
            };
        }

        /// <summary>
        /// Creates a custom notification with the specified method and data.
        /// </summary>
        /// <param name="method">The notification method.</param>
        /// <param name="data">The notification data.</param>
        /// <returns>
        /// A new <see cref="McpNotification"/> instance with the specified method and data.
        /// </returns>
        public static McpNotification CreateCustomNotification(string method, object data)
        {
            return new McpNotification
            {
                Method = method,
                Params = data
            };
        }
    }

}
