using System.Text.Json;
using Microsoft.Extensions.Logging;
using mcp_nexus.Protocol.Models;

namespace mcp_nexus.Protocol.Notifications;

/// <summary>
/// Implementation of the notification bridge that sends notifications to stdout.
/// Used for stdio-based MCP transport where notifications are written to standard output.
/// </summary>
internal class StdioNotificationBridge : INotificationBridge
{
    private readonly ILogger<StdioNotificationBridge> m_Logger;
    private readonly SemaphoreSlim m_WriteSemaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="StdioNotificationBridge"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording bridge events.</param>
    public StdioNotificationBridge(ILogger<StdioNotificationBridge> logger)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a notification through stdout.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendNotificationAsync(McpNotification notification)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        await m_WriteSemaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(notification);
            await Console.Out.WriteLineAsync(json);
            await Console.Out.FlushAsync();

            m_Logger.LogTrace("Sent notification via stdio: {Method}", notification.Method);
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to send notification via stdio: {Method}", notification.Method);
            throw;
        }
        finally
        {
            m_WriteSemaphore.Release();
        }
    }
}

