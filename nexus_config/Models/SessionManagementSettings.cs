namespace Nexus.Config.Models;

/// <summary>
/// Session management configuration settings.
/// </summary>
public class SessionManagementSettings
{
    /// <summary>
    /// Gets or sets the maximum concurrent sessions.
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the session timeout in minutes.
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the cleanup interval in seconds.
    /// </summary>
    public int CleanupIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the disposal timeout in seconds.
    /// </summary>
    public int DisposalTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the default command timeout in minutes.
    /// </summary>
    public int DefaultCommandTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the memory cleanup threshold in MB.
    /// </summary>
    public int MemoryCleanupThresholdMB { get; set; } = 1024;

    /// <summary>
    /// Gets the cleanup interval as a TimeSpan.
    /// </summary>
    /// <returns>The cleanup interval.</returns>
    public TimeSpan GetCleanupInterval()
    {
        return TimeSpan.FromSeconds(CleanupIntervalSeconds);
    }
}
