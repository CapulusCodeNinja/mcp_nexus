using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;

namespace mcp_nexus.Session
{
    /// <summary>
    /// Factory interface for creating session information objects.
    /// Provides methods for creating session info instances with various configurations.
    /// </summary>
    public interface ISessionInfoFactory
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
        /// A new <see cref="ISessionInfo"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
        ISessionInfo CreateSessionInfo(string sessionId, ICdbSession cdbSession, ICommandQueueService commandQueue,
            string dumpPath, string? symbolsPath = null, int? processId = null);

        /// <summary>
        /// Creates a default session info instance with default values.
        /// </summary>
        /// <returns>
        /// A new <see cref="ISessionInfo"/> instance with default values.
        /// </returns>
        ISessionInfo CreateDefaultSessionInfo();
    }
}
