namespace Nexus.Engine.Batch;

/// <summary>
/// Represents a command to be executed.
/// </summary>
public class Command
{
    /// <summary>
    /// Gets or sets the command identifier.
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command text.
    /// </summary>
    public string CommandText { get; set; } = string.Empty;
}
