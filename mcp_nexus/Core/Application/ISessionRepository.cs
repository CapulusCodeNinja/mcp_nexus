using mcp_nexus.Core.Domain;
using DomainSession = mcp_nexus.Core.Domain.ISession;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Repository interface for session persistence - major connection interface
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Session or null if not found</returns>
        Task<DomainSession?> GetByIdAsync(string sessionId);

        /// <summary>
        /// Gets all sessions
        /// </summary>
        /// <returns>Collection of all sessions</returns>
        Task<IEnumerable<DomainSession>> GetAllAsync();

        /// <summary>
        /// Gets active sessions
        /// </summary>
        /// <returns>Collection of active sessions</returns>
        Task<IEnumerable<DomainSession>> GetActiveSessionsAsync();

        /// <summary>
        /// Saves a session
        /// </summary>
        /// <param name="session">Session to save</param>
        /// <returns>Task representing the operation</returns>
        Task SaveAsync(DomainSession session);

        /// <summary>
        /// Deletes a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Task representing the operation</returns>
        Task DeleteAsync(string sessionId);
    }
}
