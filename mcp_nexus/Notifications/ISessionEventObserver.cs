namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Observer interface for session events using Observer Pattern
    /// </summary>
    public interface ISessionEventObserver
    {
        /// <summary>
        /// Handles session created event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        Task OnSessionCreatedAsync(string sessionId, ISessionEventData eventData);

        /// <summary>
        /// Handles session closed event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        Task OnSessionClosedAsync(string sessionId, ISessionEventData eventData);

        /// <summary>
        /// Handles session error event
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="eventData">Event data</param>
        Task OnSessionErrorAsync(string sessionId, ISessionEventData eventData);
    }
}
