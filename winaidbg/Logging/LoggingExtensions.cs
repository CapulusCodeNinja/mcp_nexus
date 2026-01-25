using Microsoft.Extensions.Logging;

using WinAiDbg.Config;

namespace WinAiDbg.Logging;

/// <summary>
/// Extension methods for configuring logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds WinAiDbg logging configuration to the logging builder.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <param name="settings">The product settings.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddWinAiDbgLogging(
        this ILoggingBuilder logging,
        ISettings settings,
        bool isServiceMode)
    {
        // Configure logging
        settings.ConfigureLogging(logging, isServiceMode);

        return logging;
    }
}
