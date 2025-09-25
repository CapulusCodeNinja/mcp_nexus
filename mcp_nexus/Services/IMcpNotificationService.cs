using mcp_nexus.Models;

namespace mcp_nexus.Services
{
    /// <summary>
    /// Interface for sending MCP server-initiated notifications
    /// </summary>
    public interface IMcpNotificationService
    {
        /// <summary>
        /// Send a command status notification to all connected clients
        /// </summary>
        Task NotifyCommandStatusAsync(string commandId, string command, string status, 
            int? progress = null, string? message = null, string? result = null, string? error = null);

        /// <summary>
        /// Send a heartbeat notification for a running command to show it's still active
        /// </summary>
        Task NotifyCommandHeartbeatAsync(string commandId, string command, TimeSpan elapsed, string? details = null);

        /// <summary>
        /// Send a session recovery notification to all connected clients
        /// </summary>
        Task NotifySessionRecoveryAsync(string reason, string recoveryStep, bool success, 
            string message, string[]? affectedCommands = null);

        /// <summary>
        /// Send a server health notification to all connected clients
        /// </summary>
        Task NotifyServerHealthAsync(string status, bool cdbSessionActive, int queueSize, 
            int activeCommands, TimeSpan? uptime = null);

        /// <summary>
        /// Send a tools list changed notification to all connected clients (standard MCP notification)
        /// </summary>
        Task NotifyToolsListChangedAsync();

        /// <summary>
        /// Send a custom notification to all connected clients
        /// </summary>
        Task SendNotificationAsync(string method, object? parameters = null);

        /// <summary>
        /// Register a notification handler for outgoing notifications
        /// </summary>
        void RegisterNotificationHandler(Func<McpNotification, Task> handler);

        /// <summary>
        /// Unregister a notification handler
        /// </summary>
        void UnregisterNotificationHandler(Func<McpNotification, Task> handler);
    }
}
