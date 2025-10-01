namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Interface for stdio notification bridge - maintains compatibility with existing code
    /// </summary>
    public interface IStdioNotificationBridge
    {
        /// <summary>
        /// Sends a notification via stdio
        /// </summary>
        /// <param name="notification">Notification to send</param>
        /// <returns>Task representing the operation</returns>
        Task SendNotificationAsync(object notification);

        /// <summary>
        /// Starts the bridge
        /// </summary>
        /// <returns>Task representing the operation</returns>
        Task StartAsync();

        /// <summary>
        /// Stops the bridge
        /// </summary>
        /// <returns>Task representing the operation</returns>
        Task StopAsync();

        /// <summary>
        /// Initializes the bridge
        /// </summary>
        /// <returns>Task representing the operation</returns>
        Task InitializeAsync();
    }
}
