namespace Nexus.Engine.Share.Models;

/// <summary>
/// Represents the current state of a debug command.
/// </summary>
public enum CommandState
{
    /// <summary>
    /// Command has been queued but not yet started.
    /// </summary>
    Queued,

    /// <summary>
    /// Command is currently being executed by the debugger.
    /// </summary>
    Executing,

    /// <summary>
    /// Command completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Command failed due to an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Command was cancelled by user request.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Command timed out during execution.
    /// </summary>
    Timeout,
}
