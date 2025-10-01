namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Notification service interface - major connection interface
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Publishes an event
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="data">Event data</param>
        /// <returns>Task representing the operation</returns>
        Task PublishEventAsync(string eventType, object data);

        /// <summary>
        /// Subscribes to an event type
        /// </summary>
        /// <param name="eventType">Event type to subscribe to</param>
        /// <param name="handler">Event handler</param>
        /// <returns>Subscription identifier</returns>
        string Subscribe(string eventType, Func<object, Task> handler);

        /// <summary>
        /// Unsubscribes from an event
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <returns>True if unsubscribed successfully</returns>
        bool Unsubscribe(string subscriptionId);
    }
}
