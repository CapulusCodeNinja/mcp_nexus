namespace mcp_nexus.Session.Models
{
    /// <summary>
    /// Exception thrown when session limit is exceeded
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SessionLimitExceededException"/> class.
    /// </remarks>
    /// <param name="currentSessions">The current number of active sessions.</param>
    /// <param name="maxSessions">The maximum allowed number of concurrent sessions.</param>
    public class SessionLimitExceededException(int currentSessions, int maxSessions) : Exception($"Maximum concurrent sessions exceeded: {currentSessions}/{maxSessions}")
    {
        /// <summary>
        /// Gets the current number of active sessions.
        /// </summary>
        public int CurrentSessions { get; } = currentSessions;

        /// <summary>
        /// Gets the maximum allowed number of concurrent sessions.
        /// </summary>
        public int MaxSessions { get; } = maxSessions;
    }
}
