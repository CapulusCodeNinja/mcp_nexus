namespace mcp_nexus.Core.Domain
{
    /// <summary>
    /// Core domain implementation of session - no external dependencies
    /// </summary>
    public class Session : ISession
    {
        #region Private Fields

        private readonly string m_sessionId;
        private readonly DateTime m_createdAt;
        private readonly string m_dumpPath;
        private readonly string? m_symbolsPath;
        private SessionStatus m_status;
        private DateTime m_lastActivity;
        private bool m_isDisposed;

        #endregion

        #region Public Properties

        /// <summary>Gets the unique session identifier</summary>
        public string SessionId => m_sessionId;

        /// <summary>Gets the session creation time</summary>
        public DateTime CreatedAt => m_createdAt;

        /// <summary>Gets the path to the dump file being debugged</summary>
        public string DumpPath => m_dumpPath;

        /// <summary>Gets the optional path to symbol files</summary>
        public string? SymbolsPath => m_symbolsPath;

        /// <summary>Gets the current session status</summary>
        public SessionStatus Status => m_status;

        /// <summary>Gets the last activity time</summary>
        public DateTime LastActivity => m_lastActivity;

        /// <summary>Gets whether the session is disposed</summary>
        public bool IsDisposed => m_isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="dumpPath">Path to dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        public Session(string sessionId, string dumpPath, string? symbolsPath = null)
        {
            m_sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            m_dumpPath = dumpPath ?? throw new ArgumentNullException(nameof(dumpPath));
            m_symbolsPath = symbolsPath;
            m_createdAt = DateTime.UtcNow;
            m_lastActivity = m_createdAt;
            m_status = SessionStatus.Initializing;
            m_isDisposed = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the session status
        /// </summary>
        /// <param name="status">New status</param>
        public void UpdateStatus(SessionStatus status)
        {
            if (m_isDisposed)
                throw new InvalidOperationException("Cannot update status of disposed session");

            m_status = status;
            UpdateActivity();
        }

        /// <summary>
        /// Updates the last activity time
        /// </summary>
        public void UpdateActivity()
        {
            if (m_isDisposed)
                throw new InvalidOperationException("Cannot update activity of disposed session");

            m_lastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Disposes the session
        /// </summary>
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                m_status = SessionStatus.Disposed;
                m_isDisposed = true;
            }
        }

        #endregion
    }
}
