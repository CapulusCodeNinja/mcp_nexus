using Microsoft.Extensions.Logging;
using mcp_nexus.Utilities.Json;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Stdio notification bridge implementation - maintains compatibility with existing code.
    /// Provides a bridge between the notification service and standard input/output streams.
    /// </summary>
    public class StdioNotificationBridge : IStdioNotificationBridge, IDisposable
    {
        private bool m_IsRunning = false;
        private readonly IMcpNotificationService m_NotificationService;
        private readonly List<string> m_SubscriptionIds = [];
        private bool m_IsInitialized = false;
        private readonly ILogger<StdioNotificationBridge>? m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StdioNotificationBridge"/> class.
        /// </summary>
        /// <param name="notificationService">The notification service to subscribe to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="notificationService"/> is null.</exception>
        public StdioNotificationBridge(IMcpNotificationService notificationService)
        {
            m_NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StdioNotificationBridge"/> class with a logger.
        /// </summary>
        /// <param name="logger">The logger instance for recording bridge operations and errors.</param>
        /// <param name="notificationService">The notification service to subscribe to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="notificationService"/> is null.</exception>
        public StdioNotificationBridge(ILogger<StdioNotificationBridge> logger, IMcpNotificationService notificationService)
        {
            m_NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
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
            if (!m_IsRunning) return;

            try
            {
                // Always write through Console.Out to respect test harness redirections.
                // Program startup guarantees UTF-8 encoding for stdio.
                var json = System.Text.Json.JsonSerializer.Serialize(notification, JsonOptions.JsonCompact);
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
            m_IsRunning = true;
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
            m_IsRunning = false;
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
            if (!m_IsInitialized)
            {
                // Subscribe to all notification types
                m_SubscriptionIds.Add(m_NotificationService.Subscribe("CommandStatus", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                // Subscribe to other notification types
                m_SubscriptionIds.Add(m_NotificationService.Subscribe("CommandHeartbeat", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_SubscriptionIds.Add(m_NotificationService.Subscribe("SessionEvent", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_SubscriptionIds.Add(m_NotificationService.Subscribe("SessionRecovery", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_SubscriptionIds.Add(m_NotificationService.Subscribe("ServerHealth", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_SubscriptionIds.Add(m_NotificationService.Subscribe("ToolsListChanged", async notification =>
                {
                    await SendNotificationAsync(notification);
                }));

                m_IsRunning = true; // Start the bridge when initialized
                m_IsInitialized = true;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the notification bridge.
        /// This method unsubscribes from all notifications and stops the bridge.
        /// </summary>
        public void Dispose()
        {
            foreach (var subscriptionId in m_SubscriptionIds)
            {
                m_NotificationService.Unsubscribe(subscriptionId);
            }
            m_SubscriptionIds.Clear();
            m_IsRunning = false;
        }
    }
}
