namespace WinAiDbg.Engine.Share.Models;

/// <summary>
/// Represents the outcome of a dumpchk execution request, including whether
/// dumpchk was enabled, whether it was executed, and the combined textual output.
/// </summary>
public class DumpCheckResult
{
    /// <summary>
    /// Gets or sets a value indicating whether dumpchk integration was enabled
    /// in configuration at the time of the request.
    /// </summary>
    public required bool IsEnabled
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dumpchk process was actually started.
    /// </summary>
    public required bool WasExecuted
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exit code returned by the dumpchk process when it was executed.
    /// A value of zero typically indicates success, while non-zero values indicate errors.
    /// </summary>
    public required int ExitCode
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets an optional informational message describing why dumpchk
    /// was not executed, such as being disabled in configuration.
    /// </summary>
    public required string Message
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dumpchk process timed out.
    /// When true, the validation was skipped but session creation can continue safely.
    /// </summary>
    public bool TimedOut
    {
        get; set;
    }
}


