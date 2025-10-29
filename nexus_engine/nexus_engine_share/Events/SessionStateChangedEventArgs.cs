using Nexus.Engine.Share.Models;

namespace Nexus.Engine.Share.Events;

/// <summary>
/// Event arguments for session state change events.
/// </summary>
public class SessionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public required string SessionId
    {
        get; init;
    }

    /// <summary>
    /// Gets the previous state of the session.
    /// </summary>
    public required SessionState OldState
    {
        get; init;
    }

    /// <summary>
    /// Gets the new state of the session.
    /// </summary>
    public required SessionState NewState
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
    /// Gets additional information about the state change.
    /// </summary>
    public string? Message
    {
        get; init;
    }
}
