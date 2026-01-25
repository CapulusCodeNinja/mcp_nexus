using Microsoft.Extensions.Logging;

using WinAiDbg.Config;
using WinAiDbg.Config.Internal;

namespace WinAiDbg.Config_unittests.Internal;

/// <summary>
/// Test accessor for LoggingConfiguration to expose protected methods for testing.
/// </summary>
internal class LoggingConfigurationTestAccessor : LoggingConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingConfigurationTestAccessor"/> class.
    /// </summary>
    /// <param name="settings">The product settings.</param>
    public LoggingConfigurationTestAccessor(ISettings settings)
        : base(settings)
    {
    }

    /// <summary>
    /// Exposes the GetLogLevelFromConfiguration method for testing.
    /// </summary>
    /// <returns>The configured log level.</returns>
    public LogLevel TestGetLogLevelFromConfiguration()
    {
        return GetLogLevelFromConfiguration();
    }

    /// <summary>
    /// Exposes the ConfigureNLogDynamically method for testing.
    /// </summary>
    /// <param name="logLevel">The log level to configure.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    public void TestConfigureNLogDynamically(LogLevel logLevel, bool isServiceMode)
    {
        ConfigureNLogDynamically(logLevel, isServiceMode);
    }

    /// <summary>
    /// Exposes the ConfigureLogPaths method for testing.
    /// </summary>
    /// <param name="nlogConfig">The NLog configuration to update.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    public void TestConfigureLogPaths(NLog.Config.LoggingConfiguration nlogConfig, bool isServiceMode)
    {
        ConfigureLogPaths(nlogConfig, isServiceMode);
    }

    /// <summary>
    /// Exposes the SetInternalLogFile method for testing.
    /// </summary>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    public void TestSetInternalLogFile(bool isServiceMode)
    {
        SetInternalLogFile(isServiceMode);
    }

    /// <summary>
    /// Exposes the ConfigureNLogProvider method for testing.
    /// </summary>
    /// <param name="logging">The logging builder to configure.</param>
    /// <param name="logLevel">The log level to set.</param>
    public void TestConfigureNLogProvider(ILoggingBuilder logging, LogLevel logLevel)
    {
        ConfigureNLogProvider(logging, logLevel);
    }

    /// <summary>
    /// Exposes the ParseLogLevel method for testing.
    /// </summary>
    /// <param name="logLevelString">The log level string to parse.</param>
    /// <returns>The corresponding LogLevel enum value.</returns>
    public LogLevel TestParseLogLevel(string logLevelString)
    {
        return ParseLogLevel(logLevelString);
    }

    /// <summary>
    /// Exposes the GetNLogLevel method for testing.
    /// </summary>
    /// <param name="logLevel">The Microsoft LogLevel to convert.</param>
    /// <returns>The corresponding NLog LogLevel.</returns>
    public NLog.LogLevel TestGetNLogLevel(LogLevel logLevel)
    {
        return GetNLogLevel(logLevel);
    }
}
