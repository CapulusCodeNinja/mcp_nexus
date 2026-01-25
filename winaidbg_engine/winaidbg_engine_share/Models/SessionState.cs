namespace WinAiDbg.Engine.Share.Models;

/// <summary>
/// Represents the current state of a debug session.
/// </summary>
public enum SessionState
{
    /// <summary>
    /// Session is being initialized.
    /// </summary>
    Initializing,

    /// <summary>
    /// Session is active and ready to accept commands.
    /// </summary>
    Active,

    /// <summary>
    /// Session is being closed.
    /// </summary>
    Closing,

    /// <summary>
    /// Session has been closed.
    /// </summary>
    Closed,

    /// <summary>
    /// Session encountered an error and is in a faulted state.
    /// </summary>
    Faulted,
}
