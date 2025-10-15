namespace mcp_nexus.Session.Core.Models
{
    /// <summary>
    /// Represents the current status of a debugging session
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>Session is initializing</summary>
        Initializing,
        /// <summary>Session is active and ready for commands</summary>
        Active,
        /// <summary>Session is being cleaned up</summary>
        Disposing,
        /// <summary>Session has been disposed</summary>
        Disposed,
        /// <summary>Session encountered an error</summary>
        Error
    }
}
