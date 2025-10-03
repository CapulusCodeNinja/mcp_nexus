using Microsoft.Extensions.Logging;

namespace mcp_nexus.Utilities;

/// <summary>
/// Provides conditional logging that respects both compilation symbols and environment settings.
/// Trace and Debug logging are only enabled in Development environment or Debug builds.
/// </summary>
public static class ConditionalLogger
{
    /// <summary>
    /// Logs a trace message only if trace logging is enabled.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">The arguments for the message.</param>
    public static void LogTrace(ILogger logger, string message, params object[] args)
    {
#if ENABLE_TRACE_LOGGING
        logger.LogTrace(message, args);
#endif
    }

    /// <summary>
    /// Logs a trace message with exception only if trace logging is enabled.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">The arguments for the message.</param>
    public static void LogTrace(ILogger logger, Exception exception, string message, params object[] args)
    {
#if ENABLE_TRACE_LOGGING
        logger.LogTrace(exception, message, args);
#endif
    }

    /// <summary>
    /// Logs a debug message only if debug logging is enabled.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">The arguments for the message.</param>
    public static void LogDebug(ILogger logger, string message, params object[] args)
    {
#if DEBUG
        logger.LogDebug(message, args);
#endif
    }

    /// <summary>
    /// Logs a debug message with exception only if debug logging is enabled.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">The arguments for the message.</param>
    public static void LogDebug(ILogger logger, Exception exception, string message, params object[] args)
    {
#if DEBUG
        logger.LogDebug(exception, message, args);
#endif
    }

    /// <summary>
    /// Checks if trace logging is enabled.
    /// </summary>
    /// <param name="logger">The logger to check.</param>
    /// <returns><c>true</c> if trace logging is enabled; otherwise, <c>false</c>.</returns>
    public static bool IsTraceEnabled(ILogger logger)
    {
#if ENABLE_TRACE_LOGGING
        return logger.IsEnabled(LogLevel.Trace);
#else
        return false;
#endif
    }

    /// <summary>
    /// Checks if debug logging is enabled.
    /// </summary>
    /// <param name="logger">The logger to check.</param>
    /// <returns><c>true</c> if debug logging is enabled; otherwise, <c>false</c>.</returns>
    public static bool IsDebugEnabled(ILogger logger)
    {
#if DEBUG
        return logger.IsEnabled(LogLevel.Debug);
#else
        return false;
#endif
    }
}
