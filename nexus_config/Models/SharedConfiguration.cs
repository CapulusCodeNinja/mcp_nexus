namespace Nexus.Config.Models;

/// <summary>
/// Shared configuration settings for all Nexus libraries.
/// </summary>
public class SharedConfiguration
{
    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the MCP Nexus specific settings.
    /// </summary>
    public McpNexusSettings McpNexus { get; set; } = new();

    /// <summary>
    /// Gets or sets the IP rate limiting settings.
    /// </summary>
    public IpRateLimitingSettings IpRateLimiting { get; set; } = new();
}
