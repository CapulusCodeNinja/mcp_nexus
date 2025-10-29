using Nexus.Engine.Share.Models;

namespace Nexus.Engine.Share.Events;

/// <summary>
/// Event arguments for command state change events.
/// </summary>
public class CommandStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public required string SessionId
    {
        get; init;
    }

    /// <summary>
    /// Gets the command identifier.
    /// </summary>
    public required string CommandId
    {
        get; init;
    }

    /// <summary>
    /// Gets the previous state of the command.
    /// </summary>
    public required CommandState OldState
    {
        get; init;
    }

    /// <summary>
    /// Gets the new state of the command.
    /// </summary>
    public required CommandState NewState
    {
        get; init;
    }

    /// <summary>
    /// Gets the timestamp when the state change occurred.
    /// </summary>
    public required DateTime Timestamp
    {
        get; init;
    }

    /// <summary>
    /// Gets the command text that was executed.
    /// </summary>
    public string? Command
    {
        get; init;
    }
}
