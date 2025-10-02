namespace mcp_nexus.Session.Models
{
    /// <summary>
    /// Exception thrown when session limit is exceeded
    /// </summary>
    public class SessionLimitExceededException : Exception
    {
        public int CurrentSessions { get; }
        public int MaxSessions { get; }

        public SessionLimitExceededException(int currentSessions, int maxSessions)
            : base($"Maximum concurrent sessions exceeded: {currentSessions}/{maxSessions}")
        {
            CurrentSessions = currentSessions;
            MaxSessions = maxSessions;
        }
    }
}
