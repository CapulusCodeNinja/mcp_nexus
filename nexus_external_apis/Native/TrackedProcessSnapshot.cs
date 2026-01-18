namespace Nexus.External.Apis.Native;

/// <summary>
/// Snapshot of a tracked process for logging and diagnostics.
/// </summary>
public sealed class TrackedProcessSnapshot
{
    /// <summary>
    /// Gets or sets the process identifier.
    /// </summary>
    public int ProcessId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the best-effort process start time (local time).
    /// </summary>
    public DateTime? StartTime
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the best-effort process name.
    /// </summary>
    public string? ProcessName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the executable path (or file name) used to start the process, when available.
    /// </summary>
    public string? FileName
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the command line arguments used to start the process, when available.
    /// </summary>
    public string? Arguments
    {
        get; set;
    }
}

