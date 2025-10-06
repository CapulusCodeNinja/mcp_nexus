using System;
using System.Threading;
using System.Threading.Tasks;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Defines the contract for CDB debugging session operations.
    /// Provides methods for starting, stopping, and executing commands in CDB debugging sessions.
    /// </summary>
    public interface ICdbSession : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the CDB session is currently active and ready for commands.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the process ID of the CDB debugger process.
        /// </summary>
        int? ProcessId { get; }

        /// <summary>
        /// Starts a debugging session with the specified target.
        /// </summary>
        /// <param name="target">The target to debug (dump file path or process ID).</param>
        /// <param name="arguments">Optional additional arguments for the CDB process.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the session was started successfully; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> StartSession(string target, string? arguments);

        /// <summary>
        /// Stops the debugging session gracefully.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the session was stopped successfully; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> StopSession();

        /// <summary>
        /// Executes a command in the debugging session without cancellation support.
        /// </summary>
        /// <param name="command">The CDB command to execute.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the command output as a string, or an error message if execution fails.
        /// </returns>
        Task<string> ExecuteCommand(string command);

        /// <summary>
        /// Executes a command in the debugging session with cancellation support.
        /// </summary>
        /// <param name="command">The CDB command to execute.</param>
        /// <param name="externalCancellationToken">The cancellation token for external cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the command output as a string, or an error message if execution fails.
        /// </returns>
        Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken);

        /// <summary>
        /// Executes a command in the debugging session with cancellation support and command ID.
        /// </summary>
        /// <param name="command">The CDB command to execute.</param>
        /// <param name="commandId">The unique command ID from the command queue.</param>
        /// <param name="externalCancellationToken">The cancellation token for external cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the command output as a string, or an error message if execution fails.
        /// </returns>
        Task<string> ExecuteCommand(string command, string commandId, CancellationToken externalCancellationToken);

        /// <summary>
        /// Cancels the currently executing command operation.
        /// </summary>
        void CancelCurrentOperation();
    }
}

