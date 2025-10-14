using NLog.Web;
using NLog;
using System.IO;
using System.Linq;

namespace mcp_nexus.Configuration
{
    /// <summary>
    /// Handles logging configuration for different environments
    /// </summary>
    public static class LoggingSetup
    {
        /// <summary>
        /// Configures logging for the application.
        /// Sets up NLog and Microsoft.Extensions.Logging based on configuration and service mode.
        /// </summary>
        /// <param name="logging">The logging builder to configure.</param>
        /// <param name="isServiceMode">Whether the application is running in service mode.</param>
        /// <param name="configuration">The application configuration.</param>
        public static void ConfigureLogging(ILoggingBuilder logging, bool isServiceMode, IConfiguration configuration)
        {
            LogConfigurationStart(isServiceMode);

            var logLevel = GetLogLevelFromConfiguration(configuration);
            ConfigureNLogDynamically(configuration, logLevel, isServiceMode);
            ConfigureMicrosoftLogging(logging, logLevel);

            LogConfigurationComplete(isServiceMode, logLevel);
        }

        /// <summary>
        /// Logs the start of logging configuration.
        /// </summary>
        /// <param name="isServiceMode">Whether the application is running in service mode.</param>
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
        /// <param name="configuration">The application configuration</param>
        /// <returns>The configured log level</returns>
        private static Microsoft.Extensions.Logging.LogLevel GetLogLevelFromConfiguration(IConfiguration configuration)
        {
            if (configuration == null)
                return Microsoft.Extensions.Logging.LogLevel.Information;

            var logLevelString = configuration["Logging:LogLevel"] ?? "Information";
            return ParseLogLevel(logLevelString);
        }

        /// <summary>
        /// Configures NLog dynamically based on application settings.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logLevel">The log level to configure.</param>
        /// <param name="isServiceMode">Whether the application is running in service mode.</param>
        private static void ConfigureNLogDynamically(IConfiguration configuration, Microsoft.Extensions.Logging.LogLevel logLevel, bool isServiceMode)
        {
            // Build or augment NLog configuration entirely from appsettings + code (no external nlog.json)
            var nlogConfig = LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();

            // Ensure main file target exists
            if (nlogConfig.FindTargetByName("mainFile") is not NLog.Targets.FileTarget fileTarget)
            {
                fileTarget = new NLog.Targets.FileTarget("mainFile")
                {
                    FileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcp-nexus.log"),
                    ArchiveFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "archive", "mcp-nexus-${shortdate}-{##}.log"),
                    ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                    ArchiveSuffixFormat = "{#}",
                    MaxArchiveFiles = 30,
                    KeepFileOpen = false,
                    AutoFlush = true,
                    CreateDirs = true,
                    Layout = "${longdate} [${level:uppercase=true}] ${message} ${exception:format=ToString}"
                };
                nlogConfig.AddTarget(fileTarget);
                nlogConfig.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget));
            }

            // Ensure stderr console target exists
            if (nlogConfig.FindTargetByName("stderr") is not NLog.Targets.ConsoleTarget stderrTarget)
            {
                stderrTarget = new NLog.Targets.ConsoleTarget("stderr")
                {
                    StdErr = true,
                    Layout = "${longdate}|${uppercase:${level}}|${logger}|${message} ${exception:format=ToString}"
                };
                nlogConfig.AddTarget(stderrTarget);
                nlogConfig.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, NLog.LogLevel.Fatal, stderrTarget));
            }

            // Apply service-mode paths (ProgramData vs app dir)
            ConfigureLogPaths(nlogConfig, isServiceMode);

            // Update levels from appsettings
            var nlogLevel = GetNLogLevel(logLevel);
            foreach (var rule in nlogConfig.LoggingRules)
            {
                rule.SetLoggingLevels(nlogLevel, NLog.LogLevel.Fatal);
            }

            // Apply the configuration
            LogManager.Configuration = nlogConfig;

            // Set internal log file path after configuration is applied
            SetInternalLogFile(isServiceMode);
        }

        /// <summary>
        /// Configures log paths based on service mode.
        /// </summary>
        /// <param name="nlogConfig">The NLog configuration to update.</param>
        /// <param name="isServiceMode">Whether the application is running in service mode.</param>
        private static void ConfigureLogPaths(NLog.Config.LoggingConfiguration nlogConfig, bool isServiceMode)
        {
            string logDirectory;
            if (isServiceMode)
            {
                // Use ProgramData for service mode
                var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                logDirectory = Path.Combine(programDataPath, "MCP-Nexus", "Logs");
                _ = Path.Combine(logDirectory, "mcp-nexus-internal.log");

                // Ensure ProgramData directories exist
                try
                {
                    Directory.CreateDirectory(logDirectory);
                    Directory.CreateDirectory(Path.Combine(logDirectory, "archive"));
                }
                catch (Exception)
                {
                    // If directory creation fails, continue - NLog will handle it
                }
            }
            else
            {
                // Use application directory for non-service mode
                logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                _ = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcp-nexus-internal.log");
            }

            // Update internal log file path - this needs to be set before applying the configuration
            // We'll set it through the LogManager after the configuration is applied

            // Update file target paths
            foreach (var target in nlogConfig.AllTargets.OfType<NLog.Targets.FileTarget>())
            {
                if (target.Name == "mainFile")
                {
                    // Update main log file path
                    target.FileName = Path.Combine(logDirectory, "mcp-nexus.log");
                    // Update archive path to use the new log directory
                    target.ArchiveFileName = Path.Combine(logDirectory, "archive", "mcp-nexus-${shortdate}-{##}.log");
                }
            }
        }

        /// <summary>
        /// Sets the internal log file path based on service mode
        /// </summary>
        /// <param name="isServiceMode">Whether the application is running in service mode</param>
        private static void SetInternalLogFile(bool isServiceMode)
        {
            try
            {
                string internalLogFile;

                if (isServiceMode)
                {
                    // Use ProgramData for service mode
                    var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    internalLogFile = Path.Combine(programDataPath, "MCP-Nexus", "Logs", "mcp-nexus-internal.log");
                }
                else
                {
                    // Use application directory for non-service mode
                    internalLogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcp-nexus-internal.log");
                }

                // Set the internal log file through LogManager
                if (LogManager.Configuration != null)
                {
                    LogManager.Configuration.Variables["internalLogFile"] = internalLogFile;
                }
            }
            catch (Exception)
            {
                // If setting internal log file fails, continue without it
                // This is not critical for the application to function
            }
        }

        /// <summary>
        /// Configures Microsoft.Extensions.Logging
        /// </summary>
        /// <param name="logging">The logging builder to configure</param>
        /// <param name="logLevel">The log level to set</param>
        private static void ConfigureMicrosoftLogging(ILoggingBuilder logging, Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            logging.ClearProviders();
            logging.AddNLogWeb();
            logging.SetMinimumLevel(logLevel);
        }

        /// <summary>
        /// Logs the completion of logging configuration
        /// </summary>
        /// <param name="isServiceMode">Whether the application is running in service mode</param>
        /// <param name="logLevel">The configured log level</param>
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
        /// <param name="logLevelString">The log level string to parse</param>
        /// <returns>The corresponding LogLevel enum value</returns>
        private static Microsoft.Extensions.Logging.LogLevel ParseLogLevel(string logLevelString)
        {
            if (string.IsNullOrEmpty(logLevelString))
                return Microsoft.Extensions.Logging.LogLevel.Information;

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
        /// <param name="logLevel">The Microsoft LogLevel to convert</param>
        /// <returns>The corresponding NLog LogLevel</returns>
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
