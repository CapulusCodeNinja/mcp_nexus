namespace WinAiDbg.Config.Models;

/// <summary>
/// Automated recovery configuration settings.
/// </summary>
public class AutomatedRecoverySettings
{
    /// <summary>
    /// Gets or sets the default command timeout in minutes.
    /// </summary>
    public int DefaultCommandTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the complex command timeout in minutes.
    /// </summary>
    public int ComplexCommandTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum command timeout in minutes.
    /// </summary>
    public int MaxCommandTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum recovery attempts.
    /// </summary>
    public int MaxRecoveryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the recovery delay in seconds.
    /// </summary>
    public int RecoveryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets the default command timeout as a TimeSpan.
    /// </summary>
    /// <returns>The default command timeout.</returns>
    public TimeSpan GetDefaultCommandTimeout()
    {
        return TimeSpan.FromMinutes(DefaultCommandTimeoutMinutes);
    }
}
