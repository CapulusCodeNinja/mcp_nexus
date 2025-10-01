using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Session.Models;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Factory for creating session information objects using Factory Pattern
    /// </summary>
    public class SessionInfoFactory
    {
        /// <summary>
        /// Creates a new session info instance
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="cdbSession">CDB session</param>
        /// <param name="commandQueue">Command queue service</param>
        /// <param name="dumpPath">Dump file path</param>
        /// <param name="symbolsPath">Symbols path (optional)</param>
        /// <param name="processId">Process ID (optional)</param>
        /// <returns>New session info instance</returns>
        public SessionInfo CreateSessionInfo(string sessionId, ICdbSession cdbSession, ICommandQueueService commandQueue,
            string dumpPath, string? symbolsPath = null, int? processId = null)
        {
            return new SessionInfo(sessionId, cdbSession, commandQueue, dumpPath, symbolsPath, processId);
        }

        /// <summary>
        /// Creates a default session info instance
        /// </summary>
        /// <returns>Default session info instance</returns>
        public SessionInfo CreateDefaultSessionInfo()
        {
            return new SessionInfo();
        }
    }
}
