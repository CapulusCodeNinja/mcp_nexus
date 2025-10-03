namespace mcp_nexus.Session.Models
{
    /// <summary>
    /// Exception thrown when session limit is exceeded
    /// </summary>
    public class SessionLimitExceededException : Exception
    {
        /// <summary>
        /// Gets the current number of active sessions.
        /// </summary>
        public int CurrentSessions { get; }
        
        /// <summary>
        /// Gets the maximum allowed number of concurrent sessions.
        /// </summary>
        public int MaxSessions { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLimitExceededException"/> class.
        /// </summary>
        /// <param name="currentSessions">The current number of active sessions.</param>
        /// <param name="maxSessions">The maximum allowed number of concurrent sessions.</param>
        public SessionLimitExceededException(int currentSessions, int maxSessions)
            : base($"Maximum concurrent sessions exceeded: {currentSessions}/{maxSessions}")
        {
            CurrentSessions = currentSessions;
            MaxSessions = maxSessions;
        }
    }
}
