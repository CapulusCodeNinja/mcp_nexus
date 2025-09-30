using NLog.Web;
using NLog;

namespace mcp_nexus.Configuration
{
    /// <summary>
    /// Handles logging configuration for different environments
    /// </summary>
    public static class LoggingSetup
    {
        /// <summary>
        /// Configures logging for the application
        /// </summary>
        public static void ConfigureLogging(ILoggingBuilder logging, bool isServiceMode, IConfiguration configuration)
        {
            LogConfigurationStart(isServiceMode);
            
            var logLevel = GetLogLevelFromConfiguration(configuration);
            ConfigureNLogDynamically(configuration, logLevel);
            ConfigureMicrosoftLogging(logging, logLevel);
            
            LogConfigurationComplete(isServiceMode, logLevel);
        }

        /// <summary>
        /// Logs the start of logging configuration
        /// </summary>
        private static void LogConfigurationStart(bool isServiceMode)
        {
            var logMessage = "Configuring logging...";
            if (isServiceMode)
            {
                Console.WriteLine(logMessage);
            }
            else
            {
                Console.Error.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Gets the log level from configuration
        /// </summary>
        private static Microsoft.Extensions.Logging.LogLevel GetLogLevelFromConfiguration(IConfiguration configuration)
        {
            var logLevelString = configuration["Logging:LogLevel"] ?? "Information";
            return ParseLogLevel(logLevelString);
        }

        /// <summary>
        /// Configures NLog dynamically based on application settings
        /// </summary>
        private static void ConfigureNLogDynamically(IConfiguration configuration, Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            var nlogConfig = LogManager.Configuration;
            if (nlogConfig != null)
            {
                var nlogLevel = GetNLogLevel(logLevel);
                foreach (var rule in nlogConfig.LoggingRules)
                {
                    rule.SetLoggingLevels(nlogLevel, NLog.LogLevel.Fatal);
                }
                LogManager.Configuration = nlogConfig;
            }
        }

        /// <summary>
        /// Configures Microsoft.Extensions.Logging
        /// </summary>
        private static void ConfigureMicrosoftLogging(ILoggingBuilder logging, Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            logging.ClearProviders();
            logging.AddNLogWeb();
            logging.SetMinimumLevel(logLevel);
        }

        /// <summary>
        /// Logs the completion of logging configuration
        /// </summary>
        private static void LogConfigurationComplete(bool isServiceMode, Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            var completeMessage = $"Logging configured with NLog (Level: {logLevel})";
            if (isServiceMode)
            {
                Console.WriteLine(completeMessage);
            }
            else
            {
                Console.Error.WriteLine(completeMessage);
            }
        }

        /// <summary>
        /// Parses log level string to LogLevel enum
        /// </summary>
        private static Microsoft.Extensions.Logging.LogLevel ParseLogLevel(string logLevelString)
        {
            return logLevelString.ToLowerInvariant() switch
            {
                "trace" => Microsoft.Extensions.Logging.LogLevel.Trace,
                "debug" => Microsoft.Extensions.Logging.LogLevel.Debug,
                "information" or "info" => Microsoft.Extensions.Logging.LogLevel.Information,
                "warning" or "warn" => Microsoft.Extensions.Logging.LogLevel.Warning,
                "error" => Microsoft.Extensions.Logging.LogLevel.Error,
                "critical" => Microsoft.Extensions.Logging.LogLevel.Critical,
                "none" => Microsoft.Extensions.Logging.LogLevel.None,
                _ => Microsoft.Extensions.Logging.LogLevel.Information
            };
        }

        /// <summary>
        /// Converts Microsoft LogLevel to NLog LogLevel
        /// </summary>
        private static NLog.LogLevel GetNLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Trace => NLog.LogLevel.Trace,
                Microsoft.Extensions.Logging.LogLevel.Debug => NLog.LogLevel.Debug,
                Microsoft.Extensions.Logging.LogLevel.Information => NLog.LogLevel.Info,
                Microsoft.Extensions.Logging.LogLevel.Warning => NLog.LogLevel.Warn,
                Microsoft.Extensions.Logging.LogLevel.Error => NLog.LogLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Critical => NLog.LogLevel.Fatal,
                Microsoft.Extensions.Logging.LogLevel.None => NLog.LogLevel.Off,
                _ => NLog.LogLevel.Info
            };
        }
    }
}
