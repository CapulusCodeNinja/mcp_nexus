namespace Nexus.Engine.Extensions.Models;

/// <summary>
/// Represents the result of an extension script execution.
/// </summary>
internal class ExtensionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the extension execution was successful.
    /// </summary>
    public bool Success
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the output from the extension script.
    /// </summary>
    public string? Output
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the error message if the execution failed.
    /// </summary>
    public string? Error
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the exit code from the extension script.
    /// </summary>
    public int ExitCode
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the timestamp when the execution started.
    /// </summary>
    public DateTime StartTime
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the timestamp when the execution completed.
    /// </summary>
    public DateTime EndTime
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the process ID of the running extension (if still running).
    /// </summary>
    public int? ProcessId
    {
        get; set;
    }
}
