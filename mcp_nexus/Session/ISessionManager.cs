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
    /// Interface for managing multiple debugging sessions with thread-safe operations
    /// </summary>
    public interface ISessionManager : IDisposable
    {
        /// <summary>
        /// Create a new debugging session
        /// </summary>
        /// <param name="dumpPath">Path to the dump file</param>
        /// <param name="symbolsPath">Optional path to symbol files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unique session identifier</returns>
        Task<string> CreateSessionAsync(string dumpPath, string? symbolsPath = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Close a specific session
        /// </summary>
        /// <param name="sessionId">Session to close</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if session was found and closed</returns>
        Task<bool> CloseSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a session exists and is active
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>True if session exists and is active</returns>
        bool SessionExists(string sessionId);

        /// <summary>
        /// Get command queue for a specific session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Command queue service for the session</returns>
        /// <exception cref="SessionNotFoundException">Session not found</exception>
        ICommandQueueService GetCommandQueue(string sessionId);

        /// <summary>
        /// Get session context information for AI client guidance
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>Session context information</returns>
        /// <exception cref="SessionNotFoundException">Session not found</exception>
        SessionContext GetSessionContext(string sessionId);

        /// <summary>
        /// Update last activity time for a session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        void UpdateActivity(string sessionId);

        /// <summary>
        /// Get all active sessions (for monitoring/debugging)
        /// </summary>
        /// <returns>Collection of session contexts</returns>
        IEnumerable<SessionContext> GetActiveSessions();

        /// <summary>
        /// Get session statistics
        /// </summary>
        /// <returns>Session statistics</returns>
        SessionStatistics GetStatistics();

        /// <summary>
        /// Force cleanup of expired sessions
        /// </summary>
        /// <returns>Number of sessions cleaned up</returns>
        Task<int> CleanupExpiredSessionsAsync();
    }

    /// <summary>
    /// Session manager statistics
    /// </summary>
    public class SessionStatistics
    {
        /// <summary>Current number of active sessions</summary>
        public int ActiveSessions { get; set; }

        /// <summary>Total number of sessions created since startup</summary>
        public long TotalSessionsCreated { get; set; }

        /// <summary>Total number of sessions closed</summary>
        public long TotalSessionsClosed { get; set; }

        /// <summary>Total number of sessions that expired</summary>
        public long TotalSessionsExpired { get; set; }

        /// <summary>Total number of commands processed across all sessions</summary>
        public long TotalCommandsProcessed { get; set; }

        /// <summary>Average session lifetime</summary>
        public TimeSpan AverageSessionLifetime { get; set; }

        /// <summary>Session manager uptime</summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>Memory usage information</summary>
        public MemoryUsageInfo MemoryUsage { get; set; } = new();
    }

    /// <summary>
    /// Memory usage information
    /// </summary>
    public class MemoryUsageInfo
    {
        /// <summary>Working set memory in bytes</summary>
        public long WorkingSetBytes { get; set; }

        /// <summary>Private memory in bytes</summary>
        public long PrivateMemoryBytes { get; set; }

        /// <summary>GC total memory in bytes</summary>
        public long GCTotalMemoryBytes { get; set; }

        /// <summary>Number of Gen 0 collections</summary>
        public int Gen0Collections { get; set; }

        /// <summary>Number of Gen 1 collections</summary>
        public int Gen1Collections { get; set; }

        /// <summary>Number of Gen 2 collections</summary>
        public int Gen2Collections { get; set; }
    }
}

