namespace Nexus.Engine.Batch;

/// <summary>
/// Represents a command execution result.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Gets or sets the command identifier.
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result text.
    /// </summary>
    public string ResultText { get; set; } = string.Empty;
}

