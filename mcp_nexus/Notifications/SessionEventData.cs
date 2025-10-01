namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Implementation of session event data
    /// </summary>
    public class SessionEventData : ISessionEventData
    {
        #region Private Fields

        private readonly DateTime m_timestamp;
        private readonly string m_eventType;
        private readonly Dictionary<string, object> m_data;

        #endregion

        #region Public Properties

        /// <summary>Gets the event timestamp</summary>
        public DateTime Timestamp => m_timestamp;

        /// <summary>Gets the event type</summary>
        public string EventType => m_eventType;

        /// <summary>Gets additional event data</summary>
        public IReadOnlyDictionary<string, object> Data => m_data.AsReadOnly();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new session event data
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="data">Additional event data</param>
        public SessionEventData(string eventType, Dictionary<string, object>? data = null)
        {
            m_timestamp = DateTime.UtcNow;
            m_eventType = eventType ?? string.Empty;
            m_data = data ?? new Dictionary<string, object>();
        }

        #endregion
    }
}
