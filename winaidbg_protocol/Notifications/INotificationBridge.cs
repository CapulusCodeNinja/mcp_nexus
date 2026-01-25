using WinAiDbg.Protocol.Models;

namespace WinAiDbg.Protocol.Notifications;

/// <summary>
/// Interface for bridging MCP notifications to a transport mechanism.
/// Implementations handle the actual delivery of notifications (stdio, HTTP, etc.).
/// </summary>
public interface INotificationBridge
{
    /// <summary>
    /// Sends a notification through the transport mechanism.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNotificationAsync(McpNotification notification);
}
