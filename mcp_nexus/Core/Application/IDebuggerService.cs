using mcp_nexus.Core.Domain;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Debugger service interface - major connection interface
    /// </summary>
    public interface IDebuggerService
    {
        /// <summary>
        /// Initializes a debugger session
        /// </summary>
        /// <param name="dumpPath">Path to dump file</param>
        /// <param name="symbolsPath">Optional symbols path</param>
        /// <returns>Session identifier</returns>
        Task<string> InitializeSessionAsync(string dumpPath, string? symbolsPath = null);

        /// <summary>
        /// Executes a debugger command
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="command">Command to execute</param>
        /// <returns>Command output</returns>
        Task<string> ExecuteCommandAsync(string sessionId, string command);

        /// <summary>
        /// Closes a debugger session
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>True if closed successfully</returns>
        Task<bool> CloseSessionAsync(string sessionId);

        /// <summary>
        /// Checks if a session is active
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <returns>True if session is active</returns>
        Task<bool> IsSessionActiveAsync(string sessionId);
    }
}
