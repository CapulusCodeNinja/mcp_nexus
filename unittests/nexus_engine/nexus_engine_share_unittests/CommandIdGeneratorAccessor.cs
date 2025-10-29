namespace Nexus.Engine.Share;

/// <summary>
/// Centralized command ID generator that ensures unique, sequential command IDs per session.
/// </summary>
public class CommandIdGeneratorAccessor : CommandIdGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandIdGeneratorAccessor"/> class.
    /// </summary>
    public CommandIdGeneratorAccessor()
        : base()
    {
    }
}
