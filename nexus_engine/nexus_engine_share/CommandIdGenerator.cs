using System.Collections.Concurrent;

namespace Nexus.Engine.Share;

/// <summary>
/// Centralized command ID generator that ensures unique, sequential command IDs per session.
/// </summary>
public class CommandIdGenerator
{
    /// <summary>
    /// Per-session counters for generating sequential command IDs.
    /// </summary>
    private readonly ConcurrentDictionary<string, int> m_SessionCounters = new();

    /// <summary>
    /// Singleton instance of the command ID generator.
    /// </summary>
    public static CommandIdGenerator Instance { get; } = new CommandIdGenerator();

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandIdGenerator"/> class.
    /// </summary>
    internal CommandIdGenerator()
    {
    }

    /// <summary>
    /// Generates a unique command ID for the specified session.
    /// Format: cmd-{sessionId}-{sequentialNumber}
    /// </summary>
    /// <param name="sessionId">The session ID to generate a command ID for.</param>
    /// <returns>A unique command ID in the format cmd-{sessionId}-{number}.</returns>
    /// <exception cref="ArgumentException">Thrown when sessionId is null or whitespace.</exception>
    public string GenerateCommandId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        // Atomically increment the counter for this session
        var commandNumber = m_SessionCounters.AddOrUpdate(
            sessionId,
            1, // Initial value if key doesn't exist
            (_, currentValue) => currentValue + 1); // Increment if key exists

        return $"cmd-{sessionId}-{commandNumber}";
    }

    /// <summary>
    /// Resets the counter for a specific session.
    /// This should be called when a session is closed to free memory.
    /// </summary>
    /// <param name="sessionId">The session ID to reset the counter for.</param>
    /// <returns>True if the counter was found and removed, false otherwise.</returns>
    public bool ResetSession(string sessionId)
    {
        return !string.IsNullOrWhiteSpace(sessionId) && m_SessionCounters.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Gets the current counter value for a session without incrementing it.
    /// </summary>
    /// <param name="sessionId">The session ID to query.</param>
    /// <returns>The current counter value, or 0 if the session has no counter yet.</returns>
    internal int GetCurrentCount(string sessionId)
    {
        return string.IsNullOrWhiteSpace(sessionId) ? 0 : (m_SessionCounters.TryGetValue(sessionId, out var count) ? count : 0);
    }

    /// <summary>
    /// Gets the total number of sessions being tracked.
    /// </summary>
    /// <returns>The number of active session counters.</returns>
    internal int GetActiveSessionCount()
    {
        return m_SessionCounters.Count;
    }

    /// <summary>
    /// Clears all session counters.
    /// This is primarily for testing purposes.
    /// </summary>
    internal void Clear()
    {
        m_SessionCounters.Clear();
    }
}

