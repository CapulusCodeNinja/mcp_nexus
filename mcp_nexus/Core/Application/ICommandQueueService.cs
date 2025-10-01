using mcp_nexus.Core.Domain;

namespace mcp_nexus.Core.Application
{
    /// <summary>
    /// Command queue service interface - major connection interface
    /// </summary>
    public interface ICommandQueueService
    {
        /// <summary>
        /// Enqueues a command for execution
        /// </summary>
        /// <param name="command">Command to enqueue</param>
        /// <returns>Command identifier</returns>
        Task<string> EnqueueCommandAsync(ICommand command);

        /// <summary>
        /// Gets command status
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>Command state</returns>
        Task<CommandState> GetCommandStatusAsync(string commandId);

        /// <summary>
        /// Gets command result
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>Command result</returns>
        Task<ICommandResult> GetCommandResultAsync(string commandId);

        /// <summary>
        /// Cancels a command
        /// </summary>
        /// <param name="commandId">Command identifier</param>
        /// <returns>True if cancelled successfully</returns>
        Task<bool> CancelCommandAsync(string commandId);
    }
}
