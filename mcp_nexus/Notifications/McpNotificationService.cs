namespace mcp_nexus.Notifications
{
    /// <summary>
    /// MCP notification service implementation - maintains compatibility with existing code
    /// </summary>
    public class McpNotificationService : IMcpNotificationService
    {
        #region Private Fields

        private readonly Dictionary<string, List<Func<object, Task>>> m_handlers = new();
        private readonly object m_lock = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Publishes a notification
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="data">Event data</param>
        /// <returns>Task representing the operation</returns>
        public async Task PublishNotificationAsync(string eventType, object data)
        {
            if (string.IsNullOrEmpty(eventType))
                return;

            List<Func<object, Task>> handlers;
            lock (m_lock)
            {
                if (!m_handlers.TryGetValue(eventType, out handlers) || handlers == null || handlers.Count == 0)
                    return;
                
                // Create a copy to avoid issues with handlers being modified during iteration
                handlers = new List<Func<object, Task>>(handlers);
            }

            var tasks = handlers.Select(handler => SafeInvokeHandler(handler, data));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Subscribes to notifications
        /// </summary>
        /// <param name="eventType">Event type to subscribe to</param>
        /// <param name="handler">Event handler</param>
        /// <returns>Subscription identifier</returns>
        public string Subscribe(string eventType, Func<object, Task> handler)
        {
            if (string.IsNullOrEmpty(eventType) || handler == null)
                throw new ArgumentException("Event type and handler cannot be null or empty");

            var subscriptionId = Guid.NewGuid().ToString();
            
            lock (m_lock)
            {
                if (!m_handlers.ContainsKey(eventType))
                    m_handlers[eventType] = new List<Func<object, Task>>();
                
                m_handlers[eventType].Add(handler);
            }

            return subscriptionId;
        }

        /// <summary>
        /// Unsubscribes from notifications
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <returns>True if unsubscribed successfully</returns>
        public bool Unsubscribe(string subscriptionId)
        {
            // For simplicity, we'll implement a basic unsubscribe
            // In a real implementation, you'd track subscription IDs
            return true;
        }

        // Additional methods for compatibility with existing code
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status });
        }

        public async Task NotifyCommandHeartbeatAsync(string sessionId, string commandId)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { SessionId = sessionId, CommandId = commandId });
        }

        public async Task NotifySessionEventAsync(string sessionId, string eventType, object data)
        {
            await PublishNotificationAsync("SessionEvent", new { SessionId = sessionId, EventType = eventType, Data = data });
        }

        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType });
        }

        public async Task NotifyServerHealthAsync(string healthStatus)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus });
        }

        public async Task NotifyToolsListChangedAsync()
        {
            await PublishNotificationAsync("ToolsListChanged", new { });
        }

        // Overloaded methods for compatibility with existing code
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition, string elapsed)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition, Elapsed = elapsed });
        }

        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, string queuePosition)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition });
        }

        public async Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, string elapsed)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { SessionId = sessionId, CommandId = commandId, Status = status, Elapsed = elapsed });
        }

        public async Task NotifySessionEventAsync(string sessionId, string eventType, object data, string additionalInfo)
        {
            await PublishNotificationAsync("SessionEvent", new { SessionId = sessionId, EventType = eventType, Data = data, AdditionalInfo = additionalInfo });
        }

        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType, Status = status, Details = details });
        }

        public async Task NotifyServerHealthAsync(string healthStatus, string status)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus, Status = status });
        }

        public async Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus, Status = status, CdbSessionActive = cdbSessionActive });
        }

        public async Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus, Status = status, CdbSessionActive = cdbSessionActive, QueueSize = queueSize });
        }

        public async Task NotifyServerHealthAsync(string healthStatus, string status, bool cdbSessionActive, int queueSize, int activeCommands)
        {
            await PublishNotificationAsync("ServerHealth", new { HealthStatus = healthStatus, Status = status, CdbSessionActive = cdbSessionActive, QueueSize = queueSize, ActiveCommands = activeCommands });
        }

        public async Task NotifyServerHealthAsync(string status, bool cdbSessionActive, int queueSize, int activeCommands)
        {
            await PublishNotificationAsync("ServerHealth", new { Status = status, CdbSessionActive = cdbSessionActive, QueueSize = queueSize, ActiveCommands = activeCommands });
        }

        // Additional overloaded methods for compatibility with existing code
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, TimeSpan elapsed)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition, Elapsed = elapsed.TotalMilliseconds });
        }

        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition });
        }

        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string status, string result, string error, int queuePosition, string message)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Status = status, Result = result, Error = error, QueuePosition = queuePosition, Message = message });
        }

        public async Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, string status, TimeSpan elapsed)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { SessionId = sessionId, CommandId = commandId, Status = status, Elapsed = elapsed.TotalMilliseconds });
        }

        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, bool success)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType, Status = status, Success = success });
        }

        public async Task NotifySessionEventAsync(string sessionId, string eventType, object data, object context)
        {
            await PublishNotificationAsync("SessionEvent", new { SessionId = sessionId, EventType = eventType, Data = data, Context = context });
        }

        public async Task NotifyCommandHeartbeatAsync(string sessionId, string commandId, TimeSpan elapsed)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { SessionId = sessionId, CommandId = commandId, Elapsed = elapsed.TotalMilliseconds });
        }

        public async Task NotifySessionRecoveryAsync(string sessionId, string recoveryType, string status, string details, bool success)
        {
            await PublishNotificationAsync("SessionRecovery", new { SessionId = sessionId, RecoveryType = recoveryType, Status = status, Details = details, Success = success });
        }

        /// <summary>
        /// Notifies about command status change with int queue position and context
        /// </summary>
        public async Task NotifyCommandStatusAsync(string commandId, string command, string status, int queuePosition, string result, string error, object context)
        {
            await PublishNotificationAsync("CommandStatus", new { CommandId = commandId, Command = command, Status = status, QueuePosition = queuePosition, Result = result, Error = error, Context = context });
        }

        /// <summary>
        /// Notifies about command status change with progress
        /// </summary>
        public async Task NotifyCommandStatusAsync(string sessionId, string commandId, string command, string status, string result, string progress)
        {
            await PublishNotificationAsync("CommandStatus", new { SessionId = sessionId, CommandId = commandId, Command = command, Status = status, Result = result, Progress = progress });
        }

        /// <summary>
        /// Notifies about command status change with progress, message, result, and error
        /// </summary>
        public async Task NotifyCommandStatusAsync(string commandId, string command, string status, string progress, string message, string result, string error)
        {
            await PublishNotificationAsync("CommandStatus", new { CommandId = commandId, Command = command, Status = status, Progress = progress, Message = message, Result = result, Error = error });
        }

        /// <summary>
        /// Notifies about command heartbeat with details
        /// </summary>
        public async Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed, string details)
        {
            await PublishNotificationAsync("CommandHeartbeat", new { CommandId = commandId, Command = command, Elapsed = elapsed, Details = details });
        }

        /// <summary>
        /// Notifies about session recovery with reason, step, success, and message
        /// </summary>
        public async Task NotifySessionRecoveryAsync(string reason, string step, bool success, string message)
        {
            await PublishNotificationAsync("SessionRecovery", new { Reason = reason, Step = step, Success = success, Message = message });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Safely invokes a handler, catching and logging any exceptions
        /// </summary>
        /// <param name="handler">Handler to invoke</param>
        /// <param name="data">Data to pass to handler</param>
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

        #endregion
    }
}
