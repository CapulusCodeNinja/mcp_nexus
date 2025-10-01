using System.Collections.Concurrent;
using mcp_nexus.Session;

namespace mcp_nexus.Repositories
{
    /// <summary>
    /// In-memory implementation of session repository using Repository Pattern
    /// </summary>
    public class InMemorySessionRepository : ISessionRepository
    {
        #region Private Fields

        private readonly ConcurrentDictionary<string, ISessionInfo> m_sessions = new();
        private readonly object m_lock = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Session info or null if not found</returns>
        public Task<ISessionInfo?> GetByIdAsync(string sessionId)
        {
            m_sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        /// <summary>
        /// Gets all sessions
        /// </summary>
        /// <returns>Collection of all sessions</returns>
        public Task<IEnumerable<ISessionInfo>> GetAllAsync()
        {
            return Task.FromResult(m_sessions.Values.AsEnumerable());
        }

        /// <summary>
        /// Gets active sessions
        /// </summary>
        /// <returns>Collection of active sessions</returns>
        public Task<IEnumerable<ISessionInfo>> GetActiveSessionsAsync()
        {
            var activeSessions = m_sessions.Values
                .Where(s => s.Status == SessionStatus.Active && !s.IsDisposed)
                .AsEnumerable();
            
            return Task.FromResult(activeSessions);
        }

        /// <summary>
        /// Adds a new session
        /// </summary>
        /// <param name="session">Session to add</param>
        /// <returns>Task representing the operation</returns>
        public Task AddAsync(ISessionInfo session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            m_sessions.TryAdd(session.SessionId, session);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates an existing session
        /// </summary>
        /// <param name="session">Session to update</param>
        /// <returns>Task representing the operation</returns>
        public Task UpdateAsync(ISessionInfo session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            m_sessions.AddOrUpdate(session.SessionId, session, (key, existing) => session);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Task representing the operation</returns>
        public Task RemoveAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));

            m_sessions.TryRemove(sessionId, out _);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks if a session exists
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>True if session exists, false otherwise</returns>
        public Task<bool> ExistsAsync(string sessionId)
        {
            return Task.FromResult(m_sessions.ContainsKey(sessionId));
        }

        /// <summary>
        /// Gets the count of sessions
        /// </summary>
        /// <returns>Number of sessions</returns>
        public Task<int> GetCountAsync()
        {
            return Task.FromResult(m_sessions.Count);
        }

        #endregion
    }
}
