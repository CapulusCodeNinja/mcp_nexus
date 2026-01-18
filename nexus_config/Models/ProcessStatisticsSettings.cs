namespace Nexus.Config.Models;

/// <summary>
/// Configuration settings for periodic process tracking statistics emission.
/// </summary>
public class ProcessStatisticsSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether periodic process statistics logging is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval (in minutes) at which process statistics are emitted.
    /// </summary>
    public int IntervalMinutes { get; set; } = 30;
}

