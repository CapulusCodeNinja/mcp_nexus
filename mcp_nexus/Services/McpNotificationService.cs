using System.Collections.Concurrent;
using System.Text.Json;
using mcp_nexus.Models;

namespace mcp_nexus.Services
{
    /// <summary>
    /// Service for sending MCP server-initiated notifications to clients
    /// </summary>
    public class McpNotificationService : IMcpNotificationService, IDisposable
    {
        private readonly ILogger<McpNotificationService> m_logger;
        private readonly ConcurrentBag<Func<McpNotification, Task>> m_notificationHandlers = new();
        private readonly DateTime m_serverStartTime = DateTime.UtcNow;
        private bool m_disposed;

        public McpNotificationService(ILogger<McpNotificationService> logger)
        {
            m_logger = logger;
            m_logger.LogDebug("McpNotificationService initialized");
        }

        public async Task NotifyCommandStatusAsync(string commandId, string command, string status, 
            int? progress = null, string? message = null, string? result = null, string? error = null)
        {
            if (m_disposed) return;

            var notification = new McpCommandStatusNotification
            {
                CommandId = commandId,
                Command = command,
                Status = status,
                Progress = progress,
                Message = message,
                Result = result,
                Error = error,
                Timestamp = DateTime.UtcNow
            };

            await SendNotificationAsync("notifications/commandStatus", notification);
            
            m_logger.LogDebug("Sent command status notification: {CommandId} -> {Status}", commandId, status);
        }

        public async Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed, string? details = null)
        {
            if (m_disposed) return;

            var elapsedDisplay = elapsed.TotalMinutes >= 1 
                ? $"{elapsed.TotalMinutes.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}m"
                : $"{elapsed.TotalSeconds.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)}s";

            var notification = new McpCommandHeartbeatNotification
            {
                CommandId = commandId,
                Command = command,
                ElapsedSeconds = elapsed.TotalSeconds,
                ElapsedDisplay = elapsedDisplay,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            await SendNotificationAsync("notifications/commandHeartbeat", notification);
            
            m_logger.LogTrace("Sent command heartbeat: {CommandId} -> {Elapsed}", commandId, elapsedDisplay);
        }

        public async Task NotifySessionRecoveryAsync(string reason, string recoveryStep, bool success, 
            string message, string[]? affectedCommands = null)
        {
            if (m_disposed) return;

            var notification = new McpSessionRecoveryNotification
            {
                Reason = reason,
                RecoveryStep = recoveryStep,
                Success = success,
                Message = message,
                AffectedCommands = affectedCommands,
                Timestamp = DateTime.UtcNow
            };

            await SendNotificationAsync("notifications/sessionRecovery", notification);
            
            m_logger.LogInformation("Sent session recovery notification: {RecoveryStep} -> {Success}", recoveryStep, success);
        }

        public async Task NotifyServerHealthAsync(string status, bool cdbSessionActive, int queueSize, 
            int activeCommands, TimeSpan? uptime = null)
        {
            if (m_disposed) return;

            var notification = new McpServerHealthNotification
            {
                Status = status,
                CdbSessionActive = cdbSessionActive,
                QueueSize = queueSize,
                ActiveCommands = activeCommands,
                Uptime = uptime ?? (DateTime.UtcNow - m_serverStartTime),
                Timestamp = DateTime.UtcNow
            };

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

            var handlers = m_notificationHandlers.ToArray();
            if (handlers.Length == 0)
            {
                m_logger.LogDebug("No notification handlers registered - notification will be dropped: {Method}", method);
                return;
            }

            var tasks = new List<Task>();
            foreach (var handler in handlers)
            {
                try
                {
                    tasks.Add(handler(notification));
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Error invoking notification handler for method: {Method}", method);
                }
            }

            try
            {
                await Task.WhenAll(tasks);
                m_logger.LogTrace("Successfully sent notification to {HandlerCount} handlers: {Method}", handlers.Length, method);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error sending notification to handlers: {Method}", method);
            }
        }

        public void RegisterNotificationHandler(Func<McpNotification, Task> handler)
        {
            if (m_disposed) return;

            m_notificationHandlers.Add(handler);
            m_logger.LogDebug("Registered notification handler (Total: {HandlerCount})", m_notificationHandlers.Count);
        }

        public void UnregisterNotificationHandler(Func<McpNotification, Task> handler)
        {
            if (m_disposed) return;

            // ConcurrentBag doesn't support removal, so we'll need to track this differently
            // For now, we'll log the attempt - in a production system, you might use a different collection
            m_logger.LogTrace("UnregisterNotificationHandler called - ConcurrentBag doesn't support removal");
        }

        public void Dispose()
        {
            if (m_disposed) return;

            m_disposed = true;
            m_logger.LogDebug("McpNotificationService disposed");
        }
    }
}
