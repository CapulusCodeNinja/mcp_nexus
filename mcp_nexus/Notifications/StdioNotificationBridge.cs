using Microsoft.Extensions.Logging;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Stdio notification bridge implementation - maintains compatibility with existing code.
    /// Provides a bridge between the notification service and standard input/output streams.
    /// </summary>
    public class StdioNotificationBridge : IStdioNotificationBridge, IDisposable
    {
        private bool m_isRunning = false;
        private readonly IMcpNotificationService m_notificationService;
        private readonly List<string> m_subscriptionIds = [];
        private bool m_isInitialized = false;
        private readonly ILogger<StdioNotificationBridge>? m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StdioNotificationBridge"/> class.
        /// </summary>
        /// <param name="notificationService">The notification service to subscribe to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="notificationService"/> is null.</exception>
        public StdioNotificationBridge(IMcpNotificationService notificationService)
        {
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StdioNotificationBridge"/> class with a logger.
        /// </summary>
        /// <param name="logger">The logger instance for recording bridge operations and errors.</param>
        /// <param name="notificationService">The notification service to subscribe to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="notificationService"/> is null.</exception>
        public StdioNotificationBridge(ILogger<StdioNotificationBridge> logger, IMcpNotificationService notificationService)
        {
            m_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            m_Logger = logger;
        }

        /// <summary>
        /// Sends a notification via stdio asynchronously.
        /// This method serializes the notification to JSON and writes it to the standard output stream.
        /// </summary>
        /// <param name="notification">The notification object to send.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
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
                m_Logger?.LogError(ex, "Error sending notification");
            }
        }

        /// <summary>
        /// Starts the bridge asynchronously.
        /// This method enables the bridge to send notifications via stdio.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task StartAsync()
        {
            m_isRunning = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the bridge asynchronously.
        /// This method disables the bridge from sending notifications via stdio.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task StopAsync()
        {
            m_isRunning = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the bridge asynchronously.
        /// This method subscribes to all notification types and enables the bridge.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public Task InitializeAsync()
        {
            if (!m_isInitialized)
            {
                // Subscribe to all notification types
                m_subscriptionIds.Add(m_notificationService.Subscribe("CommandStatus", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                // Subscribe to other notification types
                m_subscriptionIds.Add(m_notificationService.Subscribe("CommandHeartbeat", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_subscriptionIds.Add(m_notificationService.Subscribe("SessionEvent", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_subscriptionIds.Add(m_notificationService.Subscribe("SessionRecovery", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_subscriptionIds.Add(m_notificationService.Subscribe("ServerHealth", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_subscriptionIds.Add(m_notificationService.Subscribe("ToolsListChanged", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_isRunning = true; // Start the bridge when initialized
                m_isInitialized = true;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the notification bridge.
        /// This method unsubscribes from all notifications and stops the bridge.
        /// </summary>
        public void Dispose()
        {
            foreach (var subscriptionId in m_subscriptionIds)
            {
                m_notificationService.Unsubscribe(subscriptionId);
            }
            m_subscriptionIds.Clear();
            m_isRunning = false;
        }
    }
}
