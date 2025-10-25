namespace Nexus.Engine.Share;

/// <summary>
/// Centralized session ID generator that ensures unique, sequential session IDs.
/// </summary>
public class SessionIdGenerator
{
    /// <summary>
    /// Global counter for generating sequential session IDs.
    /// </summary>
    private int m_SessionCounter;

    /// <summary>
    /// Singleton instance of the session ID generator.
    /// </summary>
    public static SessionIdGenerator Instance { get; } = new SessionIdGenerator();

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionIdGenerator"/> class.
    /// </summary>
    private SessionIdGenerator()
    {
        m_SessionCounter = 0;
    }

    /// <summary>
    /// Generates a unique session ID.
    /// Format: sess-{sequentialNumber}
    /// </summary>
    /// <returns>A unique session ID in the format sess-{number}.</returns>
    public string GenerateSessionId()
    {
        // Atomically increment the counter and get the new value
        var sessionNumber = Interlocked.Increment(ref m_SessionCounter);
        return $"sess-{sessionNumber}";
    }

    /// <summary>
    /// Gets the current session counter value without incrementing it.
    /// </summary>
    /// <returns>The current session counter value.</returns>
    internal int GetCurrentCount()
    {
        return m_SessionCounter;
    }

    /// <summary>
    /// Resets the session counter.
    /// This is primarily for testing purposes.
    /// </summary>
    internal void Reset()
    {
        _ = Interlocked.Exchange(ref m_SessionCounter, 0);
    }
}

