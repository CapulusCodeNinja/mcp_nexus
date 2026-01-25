namespace WinAiDbg.Config.Models;

/// <summary>
/// Shared configuration settings for all WinAiDbg libraries.
/// </summary>
public class SharedConfiguration
{
    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the WinAiDbg specific settings.
    /// </summary>
    public WinAiDbgSettings WinAiDbg { get; set; } = new();

    /// <summary>
    /// Gets or sets the IP rate limiting settings.
    /// </summary>
    public IpRateLimitingSettings IpRateLimiting { get; set; } = new();
}
