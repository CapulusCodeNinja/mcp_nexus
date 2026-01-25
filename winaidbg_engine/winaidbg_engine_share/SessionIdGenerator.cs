namespace WinAiDbg.Engine.Share;

/// <summary>
/// Centralized session ID generator that ensures unique, timestamp-based session IDs.
/// </summary>
public class SessionIdGenerator
{
    private static long m_LastIssuedTicks;

    /// <summary>
    /// Gets singleton instance of the session ID generator.
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
    /// Format: sess-YYYY-MM-DD-HH-mm-ss-fffffff (7-digit ticks within second).
    /// </summary>
    /// <returns>A unique session ID in the format sess-YYYY-MM-DD-HH-mm-ss-fffffff.</returns>
    public string GenerateSessionId()
    {
        var nowTicks = DateTime.Now.Ticks;
        var uniqueTicks = GetNextUniqueTicks(nowTicks);

        var uniqueNow = new DateTime(uniqueTicks, DateTimeKind.Local);
        return $"sess-{uniqueNow:yyyy-MM-dd-HH-mm-ss-fffffff}";
    }

    /// <summary>
    /// Gets the next unique tick value that is guaranteed to be strictly increasing.
    /// </summary>
    /// <param name="nowTicks">The current local time tick value.</param>
    /// <returns>A unique tick value that is greater than any previously returned value.</returns>
    private static long GetNextUniqueTicks(long nowTicks)
    {
        while (true)
        {
            var lastTicks = Interlocked.Read(ref m_LastIssuedTicks);
            var candidate = nowTicks > lastTicks ? nowTicks : lastTicks + 1;

            var original = Interlocked.CompareExchange(ref m_LastIssuedTicks, candidate, lastTicks);
            if (original == lastTicks)
            {
                return candidate;
            }
        }
    }
}
