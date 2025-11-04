using Nexus.Config;
using Nexus.Engine.Batch;
using Nexus.Engine.Internal;
using Nexus.Engine.Share.Models;

namespace Nexus.Engine.Tests.Internal;

/// <summary>
/// Test accessor for CommandQueue that exposes protected methods for unit testing.
/// </summary>
internal class CommandQueueTestAccessor : CommandQueue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandQueueTestAccessor"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="settings">The product settings.</param>
    /// <param name="batchProcessor">The batch processor.</param>
    public CommandQueueTestAccessor(string sessionId, ISettings settings, IBatchProcessor batchProcessor)
        : base(sessionId, settings, batchProcessor)
    {
    }

    /// <summary>
    /// Exposes the protected ValidateCdbSession method.
    /// </summary>
    public new void ValidateCdbSession()
    {
        base.ValidateCdbSession();
    }

    /// <summary>
    /// Exposes the protected LogCommandProcessing method.
    /// </summary>
    /// <param name="command">The command to log.</param>
    public new void LogCommandProcessing(QueuedCommand command)
    {
        base.LogCommandProcessing(command);
    }

    /// <summary>
    /// Exposes the protected ExecuteCommandWithCdbSession method.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task<string> ExecuteCommandWithCdbSession(QueuedCommand command, CancellationToken cancellationToken)
    {
        return base.ExecuteCommandWithCdbSession(command, cancellationToken);
    }

    /// <summary>
    /// Exposes the protected HandleSuccessfulCommandExecution method.
    /// </summary>
    /// <param name="command">The command that was executed.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="result">The result.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task HandleSuccessfulCommandExecution(QueuedCommand command, DateTime startTime, string result)
    {
        return base.HandleSuccessfulCommandExecution(command, startTime, result);
    }

    /// <summary>
    /// Exposes the protected HandleCancelledCommand method.
    /// </summary>
    /// <param name="command">The command that was cancelled.</param>
    /// <param name="startTime">The start time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task HandleCancelledCommand(QueuedCommand command, DateTime startTime)
    {
        return base.HandleCancelledCommand(command, startTime);
    }

    /// <summary>
    /// Exposes the protected HandleTimedOutCommand method.
    /// </summary>
    /// <param name="command">The command that timed out.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="ex">The timeout exception.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task HandleTimedOutCommand(QueuedCommand command, DateTime startTime, TimeoutException ex)
    {
        return base.HandleTimedOutCommand(command, startTime, ex);
    }

    /// <summary>
    /// Exposes the protected HandleFailedCommand method.
    /// </summary>
    /// <param name="command">The command that failed.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="ex">The exception.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task HandleFailedCommand(QueuedCommand command, DateTime startTime, Exception ex)
    {
        return base.HandleFailedCommand(command, startTime, ex);
    }

    /// <summary>
    /// Exposes the protected ProcessCommandAsync method.
    /// </summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task ProcessCommandAsync(QueuedCommand command, CancellationToken cancellationToken)
    {
        return base.ProcessCommandAsync(command, cancellationToken);
    }

    /// <summary>
    /// Exposes the protected UpdateCommandState method.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="newState">The new state.</param>
    public new void UpdateCommandState(QueuedCommand command, CommandState newState)
    {
        base.UpdateCommandState(command, newState);
    }

    /// <summary>
    /// Exposes the protected SetCommandResult method.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="result">The result.</param>
    public new void SetCommandResult(QueuedCommand command, CommandInfo result)
    {
        base.SetCommandResult(command, result);
    }
}

