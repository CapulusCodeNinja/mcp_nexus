using mcp_nexus.Models;
using mcp_nexus.Session.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Service for sending MCP server-initiated notifications to clients
    /// </summary>
    public class McpNotificationService : IMcpNotificationService, IDisposable
    {
        private readonly ILogger<McpNotificationService> m_logger;
        private readonly NotificationHandlerManager m_handlerManager;
        private readonly NotificationMessageBuilder m_messageBuilder;
        private readonly DateTime m_serverStartTime = DateTime.UtcNow;
        private bool m_disposed;

        public McpNotificationService(ILogger<McpNotificationService> logger)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_handlerManager = new NotificationHandlerManager(logger);
            m_messageBuilder = new NotificationMessageBuilder(logger, m_serverStartTime);
            m_logger.LogDebug("McpNotificationService initialized");
        }

        public async Task NotifyCommandStatusAsync(
            string commandId,
            string command,
            string status,
            int? progress = null,
            string? message = null,
            string? result = null,
            string? error = null)
        {
            if (m_disposed) return;

            var notification = m_messageBuilder.CreateCommandStatusNotification(
                commandId, command, status, progress, message, result, error);

            await SendNotificationAsync("notifications/commandStatus", notification);

            m_logger.LogDebug("Sent command status notification: {CommandId} -> {Status}", commandId, status);
        }

        public async Task NotifyCommandStatusAsync(
            string sessionId,
            string commandId,
            string command,
            string status,
            string? result = null,
            int? progress = null,
            string? message = null,
            string? error = null)
        {
            if (m_disposed) return;

            var notification = m_messageBuilder.CreateSessionCommandStatusNotification(
                sessionId, commandId, command, status, result, progress, message, error);

            await SendNotificationAsync("notifications/commandStatus", notification);

            m_logger.LogDebug("Sent session-aware command status notification: {SessionId}/{CommandId} -> {Status}", sessionId, commandId, status);
        }

        public async Task NotifyCommandHeartbeatAsync(
            string commandId,
            string command,
            TimeSpan elapsed,
            string? details = null)
        {
            if (m_disposed) return;

            var notification = m_messageBuilder.CreateCommandHeartbeatNotification(
                commandId, command, elapsed, details);

            await SendNotificationAsync("notifications/commandHeartbeat", notification);

            m_logger.LogTrace("Sent command heartbeat: {CommandId} -> {Elapsed}", commandId, notification.ElapsedDisplay);
        }

        public async Task NotifyCommandHeartbeatAsync(
            string sessionId,
            string commandId,
            string command,
            TimeSpan elapsed,
            string? details = null)
        {
            if (m_disposed) return;

            var notification = m_messageBuilder.CreateSessionCommandHeartbeatNotification(
                sessionId, commandId, command, elapsed, details);

            await SendNotificationAsync("notifications/commandHeartbeat", notification);

            m_logger.LogTrace("Sent session-aware command heartbeat: {SessionId}/{CommandId} -> {Elapsed}", sessionId, commandId, notification.ElapsedDisplay);
        }

        public async Task NotifySessionRecoveryAsync(string reason, string recoveryStep, bool success,
            string message, string[]? affectedCommands = null)
        {
            if (m_disposed) return;

            var notification = m_messageBuilder.CreateSessionRecoveryNotification(
                reason, recoveryStep, success, message, affectedCommands);

            await SendNotificationAsync("notifications/sessionRecovery", notification);

            m_logger.LogInformation("Sent session recovery notification: {RecoveryStep} -> {Success}", recoveryStep, success);
        }

        public async Task NotifySessionEventAsync(string sessionId, string eventType, string message, SessionContext? context = null)
        {
            if (m_disposed) return;

            var notification = m_messageBuilder.CreateSessionEventNotification(
                sessionId, eventType, message, context);

            await SendNotificationAsync("notifications/sessionEvent", notification);

            m_logger.LogInformation("Sent session event notification: {SessionId} -> {EventType}: {Message}", sessionId, eventType, message);
        }

        public async Task NotifyServerHealthAsync(string status, bool cdbSessionActive, int queueSize,
            int activeCommands, TimeSpan? uptime = null)
        {
            if (m_disposed) return;

            var notification = m_messageBuilder.CreateServerHealthNotification(
                status, cdbSessionActive, queueSize, activeCommands, uptime);

            await SendNotificationAsync("notifications/serverHealth", notification);

            m_logger.LogDebug("Sent server health notification: {Status} (Queue: {QueueSize}, Active: {ActiveCommands})",
                status, queueSize, activeCommands);
        }

        public async Task NotifyToolsListChangedAsync()
        {
            if (m_disposed) return;

            // Standard MCP notification - no parameters needed for tools/list_changed
            await SendNotificationAsync("notifications/tools/list_changed", null);

            m_logger.LogDebug("Sent standard MCP tools list changed notification");
        }

        public async Task SendNotificationAsync(string method, object? parameters = null)
        {
            if (m_disposed) return;

            var notification = new McpNotification
            {
                Method = method,
                Params = parameters
            };

            await m_handlerManager.SendNotificationAsync(notification);
        }

        public Guid RegisterNotificationHandler(Func<McpNotification, Task> handler)
        {
            if (m_disposed) return Guid.Empty;
            return m_handlerManager.RegisterHandler(handler);
        }

        public void UnregisterNotificationHandler(Guid handlerId)
        {
            if (m_disposed) return;
            m_handlerManager.UnregisterHandler(handlerId);
        }

        public void UnregisterNotificationHandler(Func<McpNotification, Task> handler)
        {
            if (m_disposed) return;
            m_handlerManager.UnregisterHandler(handler);
        }

        public IReadOnlyList<Guid> GetRegisteredHandlerIds()
        {
            if (m_disposed) return Array.Empty<Guid>();
            return m_handlerManager.GetRegisteredHandlerIds();
        }

        public int GetHandlerCount()
        {
            if (m_disposed) return 0;
            return m_handlerManager.GetHandlerCount();
        }

        public void ClearAllHandlers()
        {
            if (m_disposed) return;
            m_handlerManager.ClearAllHandlers();
        }

        public void Dispose()
        {
            if (m_disposed) return;

            m_disposed = true;
            m_handlerManager.ClearAllHandlers();
            m_logger.LogDebug("McpNotificationService disposed");
        }
    }
}

