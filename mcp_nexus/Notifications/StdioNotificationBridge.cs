using Microsoft.Extensions.Logging;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Stdio notification bridge implementation - maintains compatibility with existing code
    /// </summary>
    public class StdioNotificationBridge : IStdioNotificationBridge, IDisposable
    {
        private bool m_isRunning = false;
        private readonly IMcpNotificationService m_notificationService;
        private string? m_subscriptionId;
        private bool m_isInitialized = false;

        /// <summary>
        /// Initializes a new instance of the StdioNotificationBridge
        /// </summary>
        /// <param name="notificationService">Notification service to subscribe to</param>
        public StdioNotificationBridge(IMcpNotificationService notificationService)
        {
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// Initializes a new instance of the StdioNotificationBridge with logger
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="notificationService">Notification service to subscribe to</param>
        public StdioNotificationBridge(ILogger<StdioNotificationBridge> logger, IMcpNotificationService notificationService)
        {
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            // Logger parameter for compatibility with tests
        }

        /// <summary>
        /// Sends a notification via stdio
        /// </summary>
        /// <param name="notification">Notification to send</param>
        /// <returns>Task representing the operation</returns>
        public async Task SendNotificationAsync(object notification)
        {
            if (!m_isRunning) return;

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(notification);
                await Console.Out.WriteLineAsync(json);
                await Console.Out.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error sending notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the bridge
        /// </summary>
        /// <returns>Task representing the operation</returns>
        public Task StartAsync()
        {
            m_isRunning = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the bridge
        /// </summary>
        /// <returns>Task representing the operation</returns>
        public Task StopAsync()
        {
            m_isRunning = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the bridge
        /// </summary>
        /// <returns>Task representing the operation</returns>
        public Task InitializeAsync()
        {
            if (!m_isInitialized)
            {
                m_subscriptionId = m_notificationService.Subscribe("notification", async (mcp_nexus.Models.McpNotification notification) =>
                {
                    await SendNotificationAsync(notification);
                });
                m_isRunning = true; // Start the bridge when initialized
                m_isInitialized = true;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the bridge
        /// </summary>
        public void Dispose()
        {
            if (m_subscriptionId != null)
            {
                m_notificationService.Unsubscribe(m_subscriptionId);
                m_subscriptionId = null;
            }
            m_isRunning = false;
        }
    }
}
