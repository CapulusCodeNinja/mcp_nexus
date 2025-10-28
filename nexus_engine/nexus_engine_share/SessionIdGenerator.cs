namespace Nexus.Engine.Share;

/// <summary>
/// Centralized session ID generator that ensures unique, timestamp-based session IDs.
/// </summary>
public class SessionIdGenerator
{
    /// <summary>
    /// Singleton instance of the session ID generator.
    /// </summary>
    public static SessionIdGenerator Instance { get; } = new SessionIdGenerator();

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionIdGenerator"/> class.
    /// </summary>
    protected SessionIdGenerator()
    {
    }

    /// <summary>
    /// Generates a unique session ID based on the current timestamp with tick precision.
    /// Format: sess-YYYY-MM-DD-HH-mm-ss-fffffff (7-digit ticks within second)
    /// </summary>
    /// <returns>A unique session ID in the format sess-YYYY-MM-DD-HH-mm-ss-fffffff.</returns>
    public string GenerateSessionId()
    {
        var now = DateTime.Now;
        return $"sess-{now:yyyy-MM-dd-HH-mm-ss-fffffff}";
    }
}

