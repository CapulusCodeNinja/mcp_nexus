using mcp_nexus.Models;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;

namespace mcp_nexus.Session
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
    }

    /// <summary>
    /// Session manager statistics.
    /// Contains information about session usage, performance, and resource consumption.
    /// </summary>
    public class SessionStatistics
    {
        /// <summary>
        /// Gets or sets the current number of active sessions.
        /// </summary>
        public int ActiveSessions { get; set; }

        /// <summary>
        /// Gets or sets the total number of sessions created since startup.
        /// </summary>
        public long TotalSessionsCreated { get; set; }

        /// <summary>
        /// Gets or sets the total number of sessions closed.
        /// </summary>
        public long TotalSessionsClosed { get; set; }

        /// <summary>
        /// Gets or sets the total number of sessions that expired.
        /// </summary>
        public long TotalSessionsExpired { get; set; }

        /// <summary>
        /// Gets or sets the total number of commands processed across all sessions.
        /// </summary>
        public long TotalCommandsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the average session lifetime.
        /// </summary>
        public TimeSpan AverageSessionLifetime { get; set; }

        /// <summary>
        /// Gets or sets the session manager uptime.
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Gets or sets the memory usage information.
        /// </summary>
        public MemoryUsageInfo MemoryUsage { get; set; } = new();
    }

    /// <summary>
    /// Memory usage information.
    /// Contains details about memory consumption and garbage collection statistics.
    /// </summary>
    public class MemoryUsageInfo
    {
        /// <summary>
        /// Gets or sets the working set memory in bytes.
        /// </summary>
        public long WorkingSetBytes { get; set; }

        /// <summary>
        /// Gets or sets the private memory in bytes.
        /// </summary>
        public long PrivateMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets the GC total memory in bytes.
        /// </summary>
        public long GCTotalMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets the number of Gen 0 collections.
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// Gets or sets the number of Gen 1 collections.
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// Gets or sets the number of Gen 2 collections.
        /// </summary>
        public int Gen2Collections { get; set; }
    }
}

