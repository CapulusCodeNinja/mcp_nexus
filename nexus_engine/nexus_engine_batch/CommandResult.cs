namespace Nexus.Engine.Batch;

/// <summary>
/// Represents a command execution result.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Gets or sets the process identifier.
    /// </summary>
    public int? ProcessId
    {
        get; set;
    }

    /// <summary>
    /// Gets the command identifier.
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
    /// Gets the result text.
    /// </summary>
    public required string ResultText
    {
        get; init;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the command was cancelled.
    /// </summary>
    public bool IsCancelled
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the command timed out.
    /// </summary>
    public bool IsTimeout
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the command failed.
    /// </summary>
    public bool IsFailed
    {
        get; set;
    }
}
