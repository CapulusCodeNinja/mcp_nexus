using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue.Core;
using mcp_nexus.Session.Core.Models;

namespace mcp_nexus.Session.Core
{
    /// <summary>
    /// Factory for creating session information objects using Factory Pattern.
    /// Provides methods for creating session info instances with various configurations.
    /// </summary>
    public class SessionInfoFactory
    {
        /// <summary>
        /// Creates a new session info instance with the specified parameters.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="cdbSession">The CDB session.</param>
        /// <param name="commandQueue">The command queue service.</param>
        /// <param name="dumpPath">The dump file path.</param>
        /// <param name="symbolsPath">The symbols path. Can be null.</param>
        /// <param name="processId">The process ID. Can be null.</param>
        /// <returns>
        /// A new <see cref="SessionInfo"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        public SessionInfo CreateSessionInfo(string sessionId, ICdbSession cdbSession, ICommandQueueService commandQueue,
            string dumpPath, string? symbolsPath = null, int? processId = null)
        {
            return new SessionInfo(sessionId, cdbSession, commandQueue, dumpPath, symbolsPath, processId);
        }

        /// <summary>
        /// Creates a new SessionInfo instance with default values.
        /// </summary>
        /// <returns>
        /// A new <see cref="SessionInfo"/> instance with default values.
        /// </returns>
        public SessionInfo CreateDefaultSessionInfo()
        {
            return new SessionInfo();
        }
    }
}
