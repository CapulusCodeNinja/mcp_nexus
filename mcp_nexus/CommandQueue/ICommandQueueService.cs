namespace mcp_nexus.CommandQueue
{
    /// <summary>
    /// Interface for command queue service operations.
    /// Provides methods for queuing, executing, and managing commands in a thread-safe manner.
    /// </summary>
    public interface ICommandQueueService : IDisposable
    {
        /// <summary>
        /// Queues a command for execution.
        /// </summary>
        /// <param name="command">The command to queue.</param>
        /// <returns>The unique identifier for the queued command.</returns>
        string QueueCommand(string command);

        /// <summary>
        /// Gets the result of a command execution.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>A task that represents the asynchronous operation and contains the command result.</returns>
        Task<string> GetCommandResult(string commandId);

        /// <summary>
        /// Cancels a specific command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command to cancel.</param>
        /// <returns><c>true</c> if the command was cancelled; otherwise, <c>false</c>.</returns>
        bool CancelCommand(string commandId);

        /// <summary>
        /// Cancels all queued commands.
        /// </summary>
        /// <param name="reason">Optional reason for cancellation.</param>
        /// <returns>The number of commands that were cancelled.</returns>
        int CancelAllCommands(string? reason = null);

        /// <summary>
        /// Gets the current status of all commands in the queue.
        /// </summary>
        /// <returns>A collection of command status information.</returns>
        IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus();

        /// <summary>
        /// Gets the currently executing command.
        /// </summary>
        /// <returns>The currently executing command, or <c>null</c> if no command is executing.</returns>
        QueuedCommand? GetCurrentCommand();

        /// <summary>
        /// Gets the state of a specific command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>The command state, or <c>null</c> if the command is not found.</returns>
        CommandState? GetCommandState(string commandId);

        /// <summary>
        /// Gets detailed information about a specific command.
        /// </summary>
        /// <param name="commandId">The unique identifier of the command.</param>
        /// <returns>Detailed command information, or <c>null</c> if the command is not found.</returns>
        CommandInfo? GetCommandInfo(string commandId);
    }
}


