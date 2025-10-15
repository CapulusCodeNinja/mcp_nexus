namespace mcp_nexus.Session.Core.Models
{
    /// <summary>
    /// Exception thrown when a session is not found
    /// </summary>
    public class SessionNotFoundException : Exception
    {
        /// <summary>
        /// Gets the session ID that was not found.
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionNotFoundException"/> class.
        /// </summary>
        /// <param name="sessionId">The session ID that was not found.</param>
        public SessionNotFoundException(string sessionId)
            : base($"Session '{sessionId}' not found or has expired")
        {
            SessionId = sessionId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionNotFoundException"/> class.
        /// </summary>
        /// <param name="sessionId">The session ID that was not found.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public SessionNotFoundException(string sessionId, string message)
            : base(message)
        {
            SessionId = sessionId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionNotFoundException"/> class.
        /// </summary>
        /// <param name="sessionId">The session ID that was not found.</param>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SessionNotFoundException(string sessionId, string message, Exception innerException)
            : base(message, innerException)
        {
            SessionId = sessionId;
        }
    }
}
