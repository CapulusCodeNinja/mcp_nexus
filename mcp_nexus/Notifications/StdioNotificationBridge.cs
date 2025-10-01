namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Stdio notification bridge implementation - maintains compatibility with existing code
    /// </summary>
    public class StdioNotificationBridge : IStdioNotificationBridge
    {
        private bool m_isRunning = false;

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
            // Initialize any required resources
            return Task.CompletedTask;
        }
    }
}
