namespace mcp_nexus.Engine.Configuration;

/// <summary>
/// Configuration settings for the debug engine.
/// </summary>
public class DebugEngineConfiguration
{
    /// <summary>
    /// Gets or sets the path to the CDB executable. If null, the system will attempt to find it automatically.
    /// </summary>
    public string? CdbPath { get; set; }

    /// <summary>
    /// Gets or sets the default timeout for command execution.
    /// </summary>
    public TimeSpan DefaultCommandTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of concurrent debug sessions.
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 10;

    /// <summary>
    /// Gets or sets the timeout for session initialization.
    /// </summary>
    public TimeSpan SessionInitializationTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the timeout for session cleanup.
    /// </summary>
    public TimeSpan SessionCleanupTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the interval for heartbeat monitoring of long-running commands.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the batching configuration.
    /// </summary>
    public BatchingConfiguration Batching { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum number of commands that can be queued per session.
    /// </summary>
    public int MaxQueuedCommandsPerSession { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum number of cached results per session.
    /// </summary>
    public int MaxCachedResultsPerSession { get; set; } = 10000;
}
