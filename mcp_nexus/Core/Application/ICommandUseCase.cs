using mcp_nexus.Core.Domain;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Application layer interface for command use cases
    /// </summary>
    public interface ICommandUseCase
    {
        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="sessionId">Session identifier</param>
        /// <param name="commandText">Command to execute</param>
        /// <returns>Command result</returns>
        Task<ICommandResult> ExecuteCommandAsync(string sessionId, string commandText);

        /// <summary>
        /// Gets command status
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>Command status</returns>
        Task<CommandState> GetCommandStatusAsync(string commandId);

        /// <summary>
        /// Cancels a command
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>True if cancelled successfully</returns>
        Task<bool> CancelCommandAsync(string commandId);
    }
}
