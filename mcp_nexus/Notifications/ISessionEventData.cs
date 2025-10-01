namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Interface for session event data
    /// </summary>
    public interface ISessionEventData
    {
        /// <summary>Gets the event timestamp</summary>
        DateTime Timestamp { get; }

        /// <summary>Gets the event type</summary>
        string EventType { get; }

        /// <summary>Gets additional event data</summary>
        IReadOnlyDictionary<string, object> Data { get; }
    }
}
