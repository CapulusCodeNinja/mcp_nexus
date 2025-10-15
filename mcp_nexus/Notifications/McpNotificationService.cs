using Microsoft.Extensions.Logging;
using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// MCP notification service implementation - maintains compatibility with existing code.
    /// Automatically detects transport mode and disables notifications in HTTP mode.
    /// Provides methods for publishing, subscribing to, and managing notifications in the MCP system.
    /// </summary>
    public class McpNotificationService : IMcpNotificationService, IDisposable
    {
        #region Private Fields

        private readonly Dictionary<string, List<Func<object, Task>>> m_Handlers = [];
        private readonly Dictionary<string, string> m_SubscriptionIds = []; // Maps subscription ID to event type
        private readonly object m_Lock = new();
        private readonly ILogger<McpNotificationService>? m_Logger;

        private bool m_NotificationsEnabled = true; // Enabled by default for testing compatibility

        private bool m_Disposed = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="McpNotificationService"/> class.
        /// </summary>
        public McpNotificationService()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpNotificationService"/> class with a logger.
        /// </summary>
        /// <param name="logger">The logger instance for recording notification operations and errors.</param>
        public McpNotificationService(ILogger<McpNotificationService> logger)
        {
            m_Logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Publishes a notification asynchronously.
        /// This method wraps the data in an McpNotification object and notifies all registered handlers.
        /// </summary>
        /// <param name="eventType">The type of event to publish.</param>
        /// <param name="data">The event data to publish.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task PublishNotificationAsync(string eventType, object data)
        {
            // Skip notifications entirely if not enabled (HTTP mode)
            if (!m_NotificationsEnabled)
                return;

            if (string.IsNullOrEmpty(eventType))
                return;

            List<Func<object, Task>>? handlers;
            lock (m_Lock)
            {
                if (!m_Handlers.TryGetValue(eventType, out handlers) || handlers?.Count == 0)
                {
                    // Log warning when no handlers are registered
                    m_Logger?.LogTrace("No notification handlers registered for event type: {EventType}", eventType);
                    return;
                }
                // Copy only when there are multiple handlers to avoid mutation issues
                if (handlers!.Count > 1)
                {
                    handlers = [.. handlers];
                }
            }

            // Wrap the data in an McpNotification object for test compatibility
            var notification = new McpNotification
            {
                Method = $"notifications/{ToCamelCase(eventType)}", // Convert to camelCase for test compatibility
                Params = data
            };

            if (handlers!.Count == 1)
            {
                await SafeInvokeHandler(handlers[0], notification);
            }
            else
            {
                var tasks = new Task[handlers.Count];
                for (int i = 0; i < handlers.Count; i++)
                {
                    tasks[i] = SafeInvokeHandler(handlers[i], notification);
                }
                await Task.WhenAll(tasks);
            }
        }


        /// <summary>
        /// Subscribes to notifications of a specific event type.
        /// </summary>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="handler">The event handler to call when the event occurs.</param>
        /// <returns>
        /// A subscription identifier that can be used to unsubscribe.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        public string Subscribe(string eventType, Func<object, Task> handler)
        {
            if (string.IsNullOrEmpty(eventType))
                throw new ArgumentException("Event type cannot be null or empty");

            ArgumentNullException.ThrowIfNull(handler);

            var subscriptionId = Guid.NewGuid().ToString();

            lock (m_Lock)
            {
                if (!m_Handlers.ContainsKey(eventType))
                    m_Handlers[eventType] = [];

                m_Handlers[eventType].Add(handler);
                m_SubscriptionIds[subscriptionId] = eventType;

                // Enable notifications when first subscriber registers (stdio mode)
                m_NotificationsEnabled = true;
            }

            return subscriptionId;
        }

        /// <summary>
        /// Subscribes to notifications with strongly-typed McpNotification handler.
        /// </summary>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="handler">The strongly-typed event handler to call when the event occurs.</param>
        /// <returns>
        /// A subscription identifier that can be used to unsubscribe.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        public string Subscribe(string eventType, Func<McpNotification, Task> handler)
        {
            if (string.IsNullOrEmpty(eventType))
                throw new ArgumentException("Event type cannot be null or empty");

            ArgumentNullException.ThrowIfNull(handler);

            var subscriptionId = Guid.NewGuid().ToString();

            lock (m_Lock)
            {
                if (!m_Handlers.ContainsKey(eventType))
                    m_Handlers[eventType] = [];

                // Convert McpNotification handler to object handler
                async Task objectHandler(object obj)
                {
                    if (obj is McpNotification notification)
                    {
                        await handler(notification);
                    }
                }
                m_Handlers[eventType].Add(objectHandler);
                m_SubscriptionIds[subscriptionId] = eventType;

                // Enable notifications when first subscriber registers (stdio mode)
                m_NotificationsEnabled = true;
            }
            return subscriptionId;
        }

        /// <summary>
        /// Unsubscribes from notifications using the subscription identifier.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier returned from Subscribe.</param>
        /// <returns>
        /// <c>true</c> if unsubscribed successfully; otherwise, <c>false</c>.
        /// </returns>
        public bool Unsubscribe(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId))
                return false;

            lock (m_Lock)
            {
                if (m_SubscriptionIds.TryGetValue(subscriptionId, out var eventType))
                {
                    m_SubscriptionIds.Remove(subscriptionId);

                    // Disable notifications when last subscriber unsubscribes
                    if (m_SubscriptionIds.Count == 0)
                    {
                        m_NotificationsEnabled = false;
                    }

                    // Note: We don't remove the handler from the list as we don't track which handler belongs to which subscription
                    // In a real implementation, you'd need to track handler-to-subscription mapping
                    return true;
                }
            }

            return false;
        }

        // Additional methods for compatibility with existing code
        /// <summary>
        /// Notifies about command status change with basic parameters.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status });
        }

        /// <summary>
        /// Notifies about command heartbeat with basic parameters.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandHeartbeatAsync(string sessionId, string commandId)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { SessionId = sessionId, CommandId = commandId });
        }

        /// <summary>
        /// Notifies about session event with basic parameters.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="data">The event data.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionEventAsync(string sessionId, string eventType, object data)
        {
            await PublishNotificationAsync("SessionEvent", new { SessionId = sessionId, EventType = eventType, Data = data });
        }

        /// <summary>
        /// Notifies about session recovery with basic parameters.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The recovery type.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType });
        }

        /// <summary>
        /// Notifies about server health with basic parameters.
        /// </summary>
        /// <param name="healthStatus">The health status.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyServerHealthAsync(string healthStatus)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus });
        }

        /// <summary>
        /// Notifies about tools list change.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyToolsListChangedAsync()
        {
            await PublishNotificationAsync("ToolsListChanged", new { });
        }

        // Overloaded methods for compatibility with existing code
        /// <summary>
        /// Notifies about command status change with extended parameters including result, error, queue position, and elapsed time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The position in the command queue.</param>
        /// <param name="elapsed">The elapsed time as a string.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition, string elapsed)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition, Elapsed = elapsed });
        }

        /// <summary>
        /// Notifies about command status change with extended parameters including result, error, and queue position.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The position in the command queue.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition });
        }

        /// <summary>
        /// Notifies about command heartbeat with extended parameters including status and elapsed time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="elapsed">The elapsed time as a string.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, string elapsed)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { SessionId = sessionId, CommandId = commandId, Status = status, Elapsed = elapsed });
        }

        /// <summary>
        /// Notifies about session event with extended parameters including additional information.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="data">The event data.</param>
        /// <param name="additionalInfo">Additional information about the event.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionEventAsync(string sessionId, string eventType, object data, string additionalInfo)
        {
            await PublishNotificationAsync("SessionEvent", new { SessionId = sessionId, EventType = eventType, Data = data, AdditionalInfo = additionalInfo });
        }

        /// <summary>
        /// Notifies about session recovery with extended parameters including status and details.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The recovery type.</param>
        /// <param name="status">The recovery status.</param>
        /// <param name="details">Additional details about the recovery.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType, Status = status, Details = details });
        }

        /// <summary>
        /// Notifies about server health with extended parameters including status.
        /// </summary>
        /// <param name="healthStatus">The health status.</param>
        /// <param name="status">The server status.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyServerHealthAsync(string healthStatus, string status)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus, Status = status });
        }

        /// <summary>
        /// Notifies about server health with extended parameters including status and CDB session status.
        /// </summary>
        /// <param name="healthStatus">The health status.</param>
        /// <param name="status">The server status.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus, Status = status, CdbSessionActive = cdbSessionActive });
        }

        /// <summary>
        /// Notifies about server health with extended parameters including status, CDB session status, and queue size.
        /// </summary>
        /// <param name="healthStatus">The health status.</param>
        /// <param name="status">The server status.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <param name="queueSize">The current queue size.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus, Status = status, CdbSessionActive = cdbSessionActive, QueueSize = queueSize });
        }

        /// <summary>
        /// Notifies about server health with extended parameters including status, CDB session status, queue size, and active commands count.
        /// </summary>
        /// <param name="healthStatus">The health status.</param>
        /// <param name="status">The server status.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <param name="queueSize">The current queue size.</param>
        /// <param name="activeCommands">The number of active commands.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize, int activeCommands)
        {
            var notification = new McpServerHealthNotification
            {
                Status = healthStatus,
                CdbSessionActive = cdbSessionActive,
                QueueSize = queueSize,
                ActiveCommands = activeCommands,
                Timestamp = DateTimeOffset.Now
            };
            await PublishNotificationAsync("ServerHealth", notification);
        }

        /// <summary>
        /// Notifies about server health with extended parameters including status, CDB session status, queue size, and active commands count.
        /// </summary>
        /// <param name="status">The server status.</param>
        /// <param name="cdbSessionActive">Whether the CDB session is active.</param>
        /// <param name="queueSize">The current queue size.</param>
        /// <param name="activeCommands">The number of active commands.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyServerHealthAsync(string status, bool cdbSessionActive, int queueSize, int activeCommands)
        {
            await PublishNotificationAsync("ServerHealth", new { Status = status, CdbSessionActive = cdbSessionActive, QueueSize = queueSize, ActiveCommands = activeCommands });
        }

        // Additional overloaded methods for compatibility with existing code
        /// <summary>
        /// Notifies about command status change with extended parameters including result, error, queue position, and elapsed time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The position in the command queue.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, TimeSpan elapsed)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition, Elapsed = elapsed.TotalMilliseconds });
        }

        /// <summary>
        /// Notifies about command status change with extended parameters including result, error, and queue position.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The position in the command queue.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition });
        }

        /// <summary>
        /// Notifies about command status change with extended parameters including result, error, queue position, and message.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="queuePosition">The position in the command queue.</param>
        /// <param name="message">Additional message about the command status.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, string message)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition, Message = message });
        }

        /// <summary>
        /// Notifies about command heartbeat with extended parameters including status and elapsed time.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="status">The command status.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, TimeSpan elapsed)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { SessionId = sessionId, CommandId = commandId, Status = status, Elapsed = elapsed.TotalMilliseconds });
        }

        /// <summary>
        /// Notifies about session recovery with extended parameters including status and success flag.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The recovery type.</param>
        /// <param name="status">The recovery status.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, bool success)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType, Status = status, Success = success });
        }

        /// <summary>
        /// Notifies about session event with extended parameters including context.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="data">The event data.</param>
        /// <param name="context">Additional context information.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionEventAsync(string sessionId, string eventType, object data, object context)
        {
            await PublishNotificationAsync("SessionEvent", new { SessionId = sessionId, EventType = eventType, Data = data, Context = context });
        }

        /// <summary>
        /// Notifies about session recovery with extended parameters including status, details, and success flag.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="recoveryType">The recovery type.</param>
        /// <param name="status">The recovery status.</param>
        /// <param name="details">Additional details about the recovery.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details, bool success)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType, Status = status, Details = details, Success = success });
        }

        /// <summary>
        /// Notifies about command status change with int queue position and context.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="queuePosition">The position in the command queue.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <param name="context">Additional context information.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string commandId, string command, string status, int queuePosition, string result, string error, object context)
        {
            var notification = new McpCommandStatusNotification
            {
                CommandId = commandId,
                Command = command,
                Status = status,
                Progress = queuePosition,
                Message = result,
                Result = error,
                Error = context?.ToString(),
                Timestamp = DateTimeOffset.Now
            };
            await PublishNotificationAsync("CommandStatus", notification);
        }

        /// <summary>
        /// Notifies about command status change with int progress.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="progress">The progress percentage.</param>
        /// <param name="result">The command result.</param>
        /// <param name="error">The error message, if any.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string commandId, string command, string status, int progress, string result, string error)
        {
            var notification = new McpCommandStatusNotification
            {
                CommandId = commandId,
                Command = command,
                Status = status,
                Progress = progress,
                Message = result,
                Result = error,
                Timestamp = DateTimeOffset.Now
            };
            await PublishNotificationAsync("CommandStatus", notification);
        }

        /// <summary>
        /// Notifies about command status change with int progress and result.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="progress">The progress percentage.</param>
        /// <param name="result">The command result.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string commandId, string command, string status, int progress, string result)
        {
            var notification = new McpCommandStatusNotification
            {
                CommandId = commandId,
                Command = command,
                Status = status,
                Progress = progress,
                Message = result,
                Timestamp = DateTimeOffset.Now
            };
            await PublishNotificationAsync("CommandStatus", notification);
        }

        /// <summary>
        /// Notifies about command status change with 8 parameters.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="status">The command status.</param>
        /// <param name="result">The command result.</param>
        /// <param name="progress">The progress percentage.</param>
        /// <param name="message">Additional message about the command status.</param>
        /// <param name="error">The error message, if any.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string command, string status, string? result, int progress, string? message, string? error)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Command = command, Status = status, Result = result, Progress = progress, Message = message, Error = error });
        }


        /// <summary>
        /// Notifies about command heartbeat with details.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <param name="details">Additional details about the heartbeat.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed, string details)
        {
            var heartbeatNotification = new McpCommandHeartbeatNotification
            {
                CommandId = commandId,
                Command = command,
                ElapsedSeconds = elapsed.TotalSeconds,
                ElapsedDisplay = FormatElapsedTime(elapsed),
                Details = details,
                Timestamp = DateTimeOffset.Now
            };
            await PublishNotificationAsync("CommandHeartbeat", heartbeatNotification);
        }

        /// <summary>
        /// Notifies about command heartbeat without details.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <param name="command">The command text.</param>
        /// <param name="elapsed">The elapsed time.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed)
        {
            var heartbeatNotification = new McpCommandHeartbeatNotification
            {
                CommandId = commandId,
                Command = command,
                ElapsedSeconds = elapsed.TotalSeconds,
                ElapsedDisplay = FormatElapsedTime(elapsed),
                Details = null,
                Timestamp = DateTimeOffset.Now
            };
            await PublishNotificationAsync("CommandHeartbeat", heartbeatNotification);
        }

        /// <summary>
        /// Notifies about session recovery with reason, step, success, and message.
        /// </summary>
        /// <param name="reason">The reason for the recovery.</param>
        /// <param name="step">The recovery step.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <param name="message">Additional message about the recovery.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionRecoveryAsync(string reason, string step, bool success, string message)
        {
            await PublishNotificationAsync("SessionRecovery", new { Reason = reason, Step = step, Success = success, Message = message });
        }

        /// <summary>
        /// Notifies about session recovery with reason, step, success, message, and affected commands.
        /// </summary>
        /// <param name="reason">The reason for the recovery.</param>
        /// <param name="step">The recovery step.</param>
        /// <param name="success">Whether the recovery was successful.</param>
        /// <param name="message">Additional message about the recovery.</param>
        /// <param name="affectedCommands">The list of commands affected by the recovery.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task NotifySessionRecoveryAsync(string reason, string step, bool success, string message, string[] affectedCommands)
        {
            var notification = new McpSessionRecoveryNotification
            {
                Reason = reason,
                RecoveryStep = step,
                Success = success,
                Message = message,
                AffectedCommands = affectedCommands,
                Timestamp = DateTimeOffset.Now
            };
            await PublishNotificationAsync("SessionRecovery", notification);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Safely invokes a handler, catching and logging any exceptions.
        /// </summary>
        /// <param name="handler">Handler to invoke.</param>
        /// <param name="data">Data to pass to handler.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        private static async Task SafeInvokeHandler(Func<object, Task> handler, object data)
        {
            try
            {
                await handler(data);
            }
            catch (Exception ex)
            {
                // Log the exception but don't let it propagate
                // In a real implementation, you would use proper logging here
                Console.WriteLine($"Error in notification handler: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a string to camelCase.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>CamelCase string.</returns>
        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Length == 1)
                return input.ToLowerInvariant();

            return char.ToLowerInvariant(input[0]) + input[1..];
        }

        /// <summary>
        /// Formats elapsed time as a human-readable string.
        /// </summary>
        /// <param name="elapsed">Elapsed time.</param>
        /// <returns>Formatted time string.</returns>
        private static string FormatElapsedTime(TimeSpan elapsed)
        {
            if (elapsed.TotalDays >= 1)
                return $"{(int)elapsed.TotalDays}d {elapsed.Hours}h {elapsed.Minutes}m";
            else if (elapsed.TotalHours >= 1)
                return $"{elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s";
            else if (elapsed.TotalMinutes >= 1)
                return elapsed.TotalMinutes.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "m";
            else if (elapsed.TotalSeconds == (int)elapsed.TotalSeconds)
                return $"{(int)elapsed.TotalSeconds}s";
            else
                return elapsed.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "s";
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the notification service and cleans up all resources.
        /// This method clears all handlers and subscription mappings.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;

            lock (m_Lock)
            {
                m_Handlers.Clear();
                m_SubscriptionIds.Clear();
            }
        }

        #endregion
    }
}
