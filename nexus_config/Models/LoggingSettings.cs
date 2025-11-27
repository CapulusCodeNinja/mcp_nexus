namespace Nexus.Config.Models;

/// <summary>
/// Logging configuration settings.
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets the maximum number of archived log files to retain.
    /// Each archived file typically represents one day of logs.
    /// </summary>
    public int RetentionDays { get; set; } = 7;
}
