namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Subject interface for session events using Observer Pattern
    /// </summary>
    public interface ISessionEventSubject
    {
        /// <summary>
        /// Attaches an observer to receive session events
        /// </summary>
        /// <param name="observer">Observer to attach</param>
        void Attach(ISessionEventObserver observer);

        /// <summary>
        /// Detaches an observer from receiving session events
        /// </summary>
        /// <param name="observer">Observer to detach</param>
        void Detach(ISessionEventObserver observer);

        /// <summary>
        /// Notifies all observers of a session created event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        Task NotifySessionCreatedAsync(string sessionId, ISessionEventData eventData);

        /// <summary>
        /// Notifies all observers of a session closed event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        Task NotifySessionClosedAsync(string sessionId, ISessionEventData eventData);

        /// <summary>
        /// Notifies all observers of a session error event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        Task NotifySessionErrorAsync(string sessionId, ISessionEventData eventData);
    }
}
