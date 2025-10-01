using mcp_nexus.Core.Domain;
using DomainSession = mcp_nexus.Core.Domain.ISession;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Application layer interface for session use cases
    /// </summary>
    public interface ISessionUseCase
    {
        /// <summary>
        /// Creates a new session
        /// </summary>
        /// <param name="dumpPath">Path to dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <returns>Created session</returns>
        Task<DomainSession> CreateSessionAsync(string dumpPath, string? symbolsPath = null);

        /// <summary>
        /// Gets a session by ID
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Session or null if not found</returns>
        Task<DomainSession?> GetSessionAsync(string sessionId);

        /// <summary>
        /// Closes a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>True if closed successfully</returns>
        Task<bool> CloseSessionAsync(string sessionId);

        /// <summary>
        /// Gets all active sessions
        /// </summary>
        /// <returns>Collection of active sessions</returns>
        Task<IEnumerable<DomainSession>> GetActiveSessionsAsync();
    }
}
