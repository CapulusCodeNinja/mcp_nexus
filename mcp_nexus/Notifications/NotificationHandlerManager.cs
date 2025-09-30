using System.Collections.Concurrent;
using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Manages notification handlers with thread-safe registration and invocation
    /// </summary>
    public class NotificationHandlerManager
    {
        private readonly ILogger m_logger;
        private readonly ConcurrentDictionary<Guid, Func<McpNotification, Task>> m_handlers = new();
        
        public NotificationHandlerManager(ILogger logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Registers a notification handler
        /// </summary>
        /// <param name="handler">The handler function</param>
        /// <returns>Handler ID for later removal</returns>
        public Guid RegisterHandler(Func<McpNotification, Task> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            var id = Guid.NewGuid();
            m_handlers[id] = handler;
            m_logger.LogDebug("📢 Registered notification handler {HandlerId} (Total: {HandlerCount})", id, m_handlers.Count);
            return id;
        }
        
        /// <summary>
        /// Unregisters a notification handler by ID
        /// </summary>
        /// <param name="handlerId">The handler ID</param>
        public void UnregisterHandler(Guid handlerId)
        {
            if (m_handlers.TryRemove(handlerId, out _))
            {
                m_logger.LogDebug("📢 Unregistered notification handler {HandlerId} (Remaining: {HandlerCount})", handlerId, m_handlers.Count);
            }
        }
        
        /// <summary>
        /// Unregisters a notification handler by reference
        /// </summary>
        /// <param name="handler">The handler function</param>
        public void UnregisterHandler(Func<McpNotification, Task> handler)
        {
            if (handler == null) return;
            
            var toRemove = m_handlers.Where(kvp => ReferenceEquals(kvp.Value, handler)).Select(kvp => kvp.Key).ToList();
            foreach (var id in toRemove)
            {
                UnregisterHandler(id);
            }
        }
        
        /// <summary>
        /// Sends a notification to all registered handlers
        /// </summary>
        /// <param name="notification">The notification to send</param>
        /// <returns>Task representing the send operation</returns>
        public async Task SendNotificationAsync(McpNotification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));
            
            // Performance: Check count first to avoid unnecessary ToArray() allocation
            if (m_handlers.IsEmpty)
            {
                m_logger.LogTrace("📢 No notification handlers registered - notification will be dropped: {Method}", notification.Method);
                return;
            }
            
            // Performance: Only create array when we know there are handlers
            var handlers = m_handlers.Values.ToArray();
            
            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                try
                {
                    tasks.Add(handler(notification));
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "📢 Error invoking notification handler for method: {Method}", notification.Method);
                }
            }
            
            try
            {
                await Task.WhenAll(tasks);
                m_logger.LogTrace("📢 Successfully sent notification to {HandlerCount} handlers: {Method}", handlers.Length, notification.Method);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "📢 Error sending notification to handlers: {Method}", notification.Method);
            }
        }
        
        /// <summary>
        /// Gets all registered handler IDs
        /// </summary>
        /// <returns>List of handler IDs</returns>
        public IReadOnlyList<Guid> GetRegisteredHandlerIds()
        {
            return m_handlers.Keys.ToList();
        }
        
        /// <summary>
        /// Gets the number of registered handlers
        /// </summary>
        /// <returns>Handler count</returns>
        public int GetHandlerCount()
        {
            return m_handlers.Count;
        }
        
        /// <summary>
        /// Clears all registered handlers
        /// </summary>
        public void ClearAllHandlers()
        {
            var count = m_handlers.Count;
            m_handlers.Clear();
            m_logger.LogInformation("📢 Cleared {Count} notification handlers", count);
        }
    }
}
