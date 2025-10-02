namespace mcp_nexus.Session.Models
{
    /// <summary>
    /// Exception thrown when a session is not found
    /// </summary>
    public class SessionNotFoundException : Exception
    {
        public string SessionId { get; }

        public SessionNotFoundException(string sessionId)
            : base($"Session '{sessionId}' not found or has expired")
        {
            SessionId = sessionId;
        }

        public SessionNotFoundException(string sessionId, string message)
            : base(message)
        {
            SessionId = sessionId;
        }

        public SessionNotFoundException(string sessionId, string message, Exception innerException)
            : base(message, innerException)
        {
            SessionId = sessionId;
        }
    }
}
