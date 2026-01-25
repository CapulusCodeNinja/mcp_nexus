namespace WinAiDbg.Engine.Share.Models;

/// <summary>
/// Represents the result of a request to create a new debug session,
/// including the assigned session identifier and dump check details.
/// </summary>
public class CreateSessionResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the created debug session.
    /// </summary>
    public required string SessionId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the dump check result describing how the dump file
    /// was validated and whether dumpchk was executed.
    /// </summary>
    public required DumpCheckResult DumpCheck
    {
        get; set;
    }
}


