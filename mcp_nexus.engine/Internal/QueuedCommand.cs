using mcp_nexus.Engine.Models;

namespace mcp_nexus.Engine.Internal;

/// <summary>
/// Represents a command that has been queued for execution.
/// </summary>
internal class QueuedCommand
{
    /// <summary>
    /// Gets or sets the unique identifier of the command.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the command text to execute.
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// Gets or sets the time when the command was queued.
    /// </summary>
    public required DateTime QueuedTime { get; set; }

    /// <summary>
    /// Gets or sets the current state of the command.
    /// </summary>
    public CommandState State { get; set; } = CommandState.Queued;

    /// <summary>
    /// Gets or sets the completion source for the command result.
    /// </summary>
    public TaskCompletionSource<CommandInfo> CompletionSource { get; set; } = new();

    /// <summary>
    /// Gets or sets the cancellation token source for the command.
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();
}
