using mcp_nexus.Session;

namespace mcp_nexus.Repositories
{
    /// <summary>
    /// Repository interface for session data access using Repository Pattern
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Session info or null if not found</returns>
        Task<ISessionInfo?> GetByIdAsync(string sessionId);

        /// <summary>
        /// Gets all sessions
        /// </summary>
        /// <returns>Collection of all sessions</returns>
        Task<IEnumerable<ISessionInfo>> GetAllAsync();

        /// <summary>
        /// Gets active sessions
        /// </summary>
        /// <returns>Collection of active sessions</returns>
        Task<IEnumerable<ISessionInfo>> GetActiveSessionsAsync();

        /// <summary>
        /// Adds a new session
        /// </summary>
        /// <param name="session">Session to add</param>
        /// <returns>Task representing the operation</returns>
        Task AddAsync(ISessionInfo session);

        /// <summary>
        /// Updates an existing session
        /// </summary>
        /// <param name="session">Session to update</param>
        /// <returns>Task representing the operation</returns>
        Task UpdateAsync(ISessionInfo session);

        /// <summary>
        /// Removes a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Task representing the operation</returns>
        Task RemoveAsync(string sessionId);

        /// <summary>
        /// Checks if a session exists
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>True if session exists, false otherwise</returns>
        Task<bool> ExistsAsync(string sessionId);

        /// <summary>
        /// Gets the count of sessions
        /// </summary>
        /// <returns>Number of sessions</returns>
        Task<int> GetCountAsync();
    }
}
