using mcp_nexus.Models;
using mcp_nexus.Session.Core.Models;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.CommandQueue.Recovery;
using mcp_nexus.Infrastructure.Adapters;
using mcp_nexus.Session.Core;
using mcp_nexus.Session.Statistics;

namespace mcp_nexus.Session.Lifecycle
{
    /// <summary>
    /// Interface for managing multiple debugging sessions with thread-safe operations.
    /// Provides methods for creating, managing, and monitoring debugging sessions.
    /// </summary>
    public interface ISessionManager : IDisposable
    {
        /// <summary>
        /// Creates a new debugging session asynchronously.
        /// </summary>
        /// <param name="dumpPath">The path to the dump file.</param>
        /// <param name="symbolsPath">The optional path to symbol files.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the unique session identifier.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dumpPath"/> is null or empty.</exception>
        /// <exception cref="SessionLimitExceededException">Thrown when the session limit is exceeded.</exception>
        Task<string> CreateSessionAsync(string dumpPath, string? symbolsPath = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes a specific session asynchronously.
        /// </summary>
        /// <param name="sessionId">The session to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the session was found and closed; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a session exists and is active.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>
        /// <c>true</c> if the session exists and is active; otherwise, <c>false</c>.
        /// </returns>
        bool SessionExists(string sessionId);

        /// <summary>
        /// Gets the command queue for a specific session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>
        /// The command queue service for the session.
        /// </returns>
        /// <exception cref="SessionNotFoundException">Thrown when the session is not found.</exception>
        ICommandQueueService GetCommandQueue(string sessionId);

        /// <summary>
        /// Tries to get the command queue for a specific session without throwing when unavailable.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandQueue">The command queue service for the session when available.</param>
        /// <returns>
        /// <c>true</c> when a non-disposed, active session with a queue exists; otherwise, <c>false</c>.
        /// </returns>
        bool TryGetCommandQueue(string sessionId, out ICommandQueueService? commandQueue);

        /// <summary>
        /// Gets session context information for AI client guidance.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>
        /// The session context information.
        /// </returns>
        /// <exception cref="SessionNotFoundException">Thrown when the session is not found.</exception>
        SessionContext GetSessionContext(string sessionId);

        /// <summary>
        /// Updates the last activity time for a session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        void UpdateActivity(string sessionId);

        /// <summary>
        /// Gets all active sessions for monitoring and debugging.
        /// </summary>
        /// <returns>
        /// A collection of session contexts.
        /// </returns>
        IEnumerable<SessionContext> GetActiveSessions();

        /// <summary>
        /// Gets all sessions with full session information.
        /// </summary>
        /// <returns>
        /// A collection of session info objects.
        /// </returns>
        IEnumerable<SessionInfo> GetAllSessions();

        /// <summary>
        /// Gets session statistics.
        /// </summary>
        /// <returns>
        /// The session statistics.
        /// </returns>
        SessionStatistics GetStatistics();

        /// <summary>
        /// Forces cleanup of expired sessions asynchronously.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the number of sessions cleaned up.
        /// </returns>
        Task<int> CleanupExpiredSessionsAsync();

        /// <summary>
        /// Gets command information and result by checking both the command queue tracker and result cache.
        /// This method provides a unified way to retrieve command status and results efficiently.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="commandId">The command identifier.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns command information and result if found; otherwise, (null, null).
        /// </returns>
        Task<(CommandInfo? CommandInfo, ICommandResult? Result)> GetCommandInfoAndResultAsync(string sessionId, string commandId);
    }

}

