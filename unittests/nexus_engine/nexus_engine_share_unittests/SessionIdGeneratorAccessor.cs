namespace Nexus.Engine.Share;

/// <summary>
/// Centralized session ID generator that ensures unique, timestamp-based session IDs.
/// </summary>
public class SessionIdGeneratorAccessor : SessionIdGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionIdGeneratorAccessor"/> class.
    /// </summary>
    public SessionIdGeneratorAccessor()
        : base()
    {
    }
}

