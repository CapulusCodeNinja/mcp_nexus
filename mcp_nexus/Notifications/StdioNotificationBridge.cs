using System.Text.Json;
using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Bridge that connects McpNotificationService to stdio MCP server transport
    /// This enables server-initiated notifications to reach MCP clients over stdio
    /// 
    /// NOTE: This implementation sends notifications through stdout in MCP JSON-RPC format
    /// for stdio transport. The MCP client will receive these as proper notifications.
    /// </summary>
    public class StdioNotificationBridge : IStdioNotificationBridge, IDisposable
    {
        private readonly ILogger<StdioNotificationBridge> m_logger;
        private readonly IMcpNotificationService m_notificationService;
        private readonly object m_stdoutLock = new();
        private bool m_disposed;
        private bool m_initialized;
        // FIXED: Track handler function for proper cleanup
        private Func<McpNotification, Task>? m_notificationHandler;

        public StdioNotificationBridge(
            ILogger<StdioNotificationBridge> logger,
            IMcpNotificationService notificationService)
        {
            m_logger = logger;
            m_notificationService = notificationService;
        }

        public Task InitializeAsync()
        {
            if (m_disposed || m_initialized)
                return Task.CompletedTask;

            try
            {
                m_logger.LogInformation("Initializing stdio notification bridge...");

                // FIXED: Register our notification handler with the notification service and track function
                m_notificationHandler = HandleNotification;
                m_notificationService.RegisterNotificationHandler(m_notificationHandler);

                m_initialized = true;
                m_logger.LogInformation("Stdio notification bridge initialized - notifications will be sent to MCP clients via stdout");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to initialize stdio notification bridge");
                throw;
            }
        }

        private Task HandleNotification(McpNotification notification)
        {
            if (m_disposed || notification == null)
                return Task.CompletedTask;

            try
            {
                // Create MCP JSON-RPC notification message
                var mcpMessage = new
                {
                    jsonrpc = "2.0",
                    method = notification.Method,
                    @params = notification.Params
                };

                // Serialize to JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                var jsonMessage = JsonSerializer.Serialize(mcpMessage, jsonOptions);

                // Send via stdout (MCP protocol requirement for stdio transport)
                // Use lock to ensure atomic writes to stdout
                lock (m_stdoutLock)
                {
                    Console.WriteLine(jsonMessage);
                    Console.Out.Flush();
                }

                m_logger.LogDebug("Successfully sent MCP notification via stdio: {Method}", notification.Method);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to send MCP notification via stdio: {Method}", notification.Method);
                return Task.CompletedTask;
            }
        }

        public void Dispose()
        {
            if (m_disposed)
                return;

            try
            {
                m_logger.LogDebug("Disposing stdio notification bridge...");

                // FIXED: Properly unregister notification handler
                if (m_initialized && m_notificationHandler != null)
                {
                    m_notificationService.UnregisterNotificationHandler(m_notificationHandler);
                }

                m_disposed = true;

                m_logger.LogDebug("Stdio notification bridge disposed");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error disposing stdio notification bridge");
            }
        }
    }
}

