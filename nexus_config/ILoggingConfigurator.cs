using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace nexus.config;

/// <summary>
/// Provides logging configuration functionality.
/// </summary>
public interface ILoggingConfigurator
{
    /// <summary>
    /// Configures logging for the application.
    /// </summary>
    /// <param name="logging">The logging builder to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    void ConfigureLogging(ILoggingBuilder logging, IConfiguration configuration, bool isServiceMode);
}
