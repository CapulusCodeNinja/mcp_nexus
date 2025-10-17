using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;

namespace mcp_nexus.Session.Core
{
    /// <summary>
    /// Interface for session information with proper encapsulation.
    /// Provides access to session-related data and services for debugging operations.
    /// </summary>
    public interface ISessionInfo : IDisposable
    {
        /// <summary>
        /// Gets the unique session identifier.
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// Gets the CDB session for this debugging session.
        /// </summary>
        ICdbSession CdbSession { get; }

        /// <summary>
        /// Gets the command queue service for this session.
        /// </summary>
        ICommandQueueService CommandQueue { get; }

        /// <summary>
        /// Gets the session creation time (local).
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Gets the path to the dump file being debugged.
        /// </summary>
        string DumpPath { get; }

        /// <summary>
        /// Gets the optional path to symbol files.
        /// </summary>
        string? SymbolsPath { get; }

        /// <summary>
        /// Gets the process ID of the CDB debugger process.
        /// </summary>
        int? ProcessId { get; }

        /// <summary>
        /// Gets or sets the current session status.
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// Gets or sets the last activity time.
        /// </summary>
        DateTime LastActivity { get; set; }

        /// <summary>
        /// Gets whether the session is disposed.
        /// </summary>
        bool IsDisposed { get; }
    }
}
