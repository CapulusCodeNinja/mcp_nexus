using Microsoft.Extensions.Logging;

using Nexus.Config;

namespace Nexus.Logging;

/// <summary>
/// Extension methods for configuring logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds Nexus logging configuration to the logging builder.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddNexusLogging(
        this ILoggingBuilder logging,
        bool isServiceMode)
    {
        // Configure logging
        Settings.GetInstance().ConfigureLogging(logging, isServiceMode);

        return logging;
    }
}
