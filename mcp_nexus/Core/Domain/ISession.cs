namespace mcp_nexus.Core.Domain
{
    /// <summary>
    /// Core domain interface for session - no external dependencies
    /// </summary>
    public interface ISession
    {
        /// <summary>Gets the unique session identifier</summary>
        string SessionId { get; }

        /// <summary>Gets the session creation time</summary>
        DateTime CreatedAt { get; }

        /// <summary>Gets the path to the dump file being debugged</summary>
        string DumpPath { get; }

        /// <summary>Gets the optional path to symbol files</summary>
        string? SymbolsPath { get; }

        /// <summary>Gets the current session status</summary>
        SessionStatus Status { get; }

        /// <summary>Gets the last activity time</summary>
        DateTime LastActivity { get; }

        /// <summary>Gets whether the session is disposed</summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Updates the session status
        /// </summary>
        /// <param name="status">New status</param>
        void UpdateStatus(SessionStatus status);

        /// <summary>
        /// Updates the last activity time
        /// </summary>
        void UpdateActivity();
    }
}
