namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Interface for stdio notification bridge - maintains compatibility with existing code.
    /// Provides methods for sending notifications via standard input/output streams.
    /// </summary>
    public interface IStdioNotificationBridge
    {
        /// <summary>
        /// Sends a notification via stdio asynchronously.
        /// </summary>
        /// <param name="notification">The notification object to send.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task SendNotificationAsync(object notification);

        /// <summary>
        /// Starts the bridge asynchronously.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task StartAsync();

        /// <summary>
        /// Stops the bridge asynchronously.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task StopAsync();

        /// <summary>
        /// Initializes the bridge asynchronously.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        Task InitializeAsync();
    }
}
