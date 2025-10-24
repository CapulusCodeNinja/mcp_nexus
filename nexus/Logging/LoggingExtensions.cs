using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.config;
using nexus.config.Internal;

namespace nexus.Logging;

/// <summary>
/// Extension methods for configuring logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds Nexus logging configuration to the logging builder.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddNexusLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        bool isServiceMode)
    {
        // Configure logging
        Settings.GetLoader().ConfigureLogging(logging, configuration, isServiceMode);
        
        return logging;
    }
}
