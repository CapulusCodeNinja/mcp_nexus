using Microsoft.Extensions.Logging;

using NLog;
using NLog.Web;

namespace Nexus.Config.Internal;

/// <summary>
/// Handles logging configuration for different environments.
/// </summary>
/// <param name="settings">The product settings.</param>
internal class LoggingConfiguration(ISettings settings)
{
    private readonly ISettings m_Settings = settings;

    /// <summary>
    /// Configures logging for the application.
    /// Sets up NLog and Microsoft.Extensions.Logging based on configuration and service mode.
    /// </summary>
    /// <param name="logging">The logging builder to configure.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    public virtual void ConfigureLogging(ILoggingBuilder logging, bool isServiceMode)
    {
        var logLevel = GetLogLevelFromConfiguration();
        ConfigureNLogDynamically(logLevel, isServiceMode);
        ConfigureNLogProvider(logging, logLevel);
    }

    /// <summary>
    /// Gets the log level from configuration.
    /// </summary>
    /// <returns>The configured log level.</returns>
    protected virtual Microsoft.Extensions.Logging.LogLevel GetLogLevelFromConfiguration()
    {
        var logLevelString = m_Settings.Get().Logging.LogLevel;
        return ParseLogLevel(logLevelString);
    }

    /// <summary>
    /// Configures NLog dynamically based on application settings.
    /// </summary>
    /// <param name="logLevel">The log level to configure.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    protected virtual void ConfigureNLogDynamically(Microsoft.Extensions.Logging.LogLevel logLevel, bool isServiceMode)
    {
        // Build or augment NLog configuration entirely from appsettings + code (no external nlog.json)
        var nlogConfig = LogManager.Configuration ?? new NLog.Config.LoggingConfiguration();

        // Ensure main file target exists
        var retentionDays = m_Settings.Get().Logging.RetentionDays;
        if (nlogConfig.FindTargetByName("mainFile") is not NLog.Targets.FileTarget)
        {
            var fileTarget = new NLog.Targets.FileTarget("mainFile")
            {
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mcp-nexus.log"),
                ArchiveFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "archive", "mcp-nexus-${shortdate}-{##}.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveSuffixFormat = "{#}",
                MaxArchiveFiles = Math.Max(1, retentionDays),
                KeepFileOpen = true,
                AutoFlush = true,
                CreateDirs = true,
                WriteBom = true,
                Layout = "${longdate} [${level:uppercase=true}] ${message} ${exception:format=ToString}",
                Encoding = System.Text.Encoding.UTF8,
            };
            nlogConfig.AddTarget(fileTarget);
            nlogConfig.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget));
        }

        // Ensure stderr console target exists
        if (nlogConfig.FindTargetByName("stderr") is not NLog.Targets.ConsoleTarget)
        {
            var stderrTarget = new NLog.Targets.ConsoleTarget("stderr")
            {
                StdErr = true,
                Layout = "${longdate} [${level:uppercase=true}] ${message} ${exception:format=ToString}",
                Encoding = System.Text.Encoding.UTF8,
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
    protected virtual void ConfigureLogPaths(NLog.Config.LoggingConfiguration nlogConfig, bool isServiceMode)
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
                _ = Directory.CreateDirectory(logDirectory);
                _ = Directory.CreateDirectory(Path.Combine(logDirectory, "archive"));
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
    /// Sets the internal log file path based on service mode.
    /// </summary>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    protected virtual void SetInternalLogFile(bool isServiceMode)
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
            if (LogManager.Configuration is { } config)
            {
                config.Variables["internalLogFile"] = internalLogFile;
            }
        }
        catch (Exception)
        {
            // If setting internal log file fails, continue without it
            // This is not critical for the application to function
        }
    }

    /// <summary>
    /// Configures the NLog provider for Microsoft.Extensions.Logging.
    /// </summary>
    /// <param name="logging">The logging builder to configure.</param>
    /// <param name="logLevel">The log level to set.</param>
    protected virtual void ConfigureNLogProvider(ILoggingBuilder logging, Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        // Only configure NLog for Microsoft.Extensions.Logging when Trace is enabled
        if (logLevel != Microsoft.Extensions.Logging.LogLevel.Trace)
        {
            return;
        }

        _ = logging.ClearProviders();
        _ = logging.AddNLogWeb();
        _ = logging.SetMinimumLevel(logLevel);
    }

    /// <summary>
    /// Parses log level string to LogLevel enum.
    /// </summary>
    /// <param name="logLevelString">The log level string to parse.</param>
    /// <returns>The corresponding LogLevel enum value.</returns>
    protected virtual Microsoft.Extensions.Logging.LogLevel ParseLogLevel(string logLevelString)
    {
        return string.IsNullOrEmpty(logLevelString)
            ? Microsoft.Extensions.Logging.LogLevel.Information
            : logLevelString.ToLowerInvariant() switch
            {
                "trace" => Microsoft.Extensions.Logging.LogLevel.Trace,
                "debug" => Microsoft.Extensions.Logging.LogLevel.Debug,
                "information" or "info" => Microsoft.Extensions.Logging.LogLevel.Information,
                "warning" or "warn" => Microsoft.Extensions.Logging.LogLevel.Warning,
                "error" => Microsoft.Extensions.Logging.LogLevel.Error,
                "critical" => Microsoft.Extensions.Logging.LogLevel.Critical,
                "none" => Microsoft.Extensions.Logging.LogLevel.None,
                _ => Microsoft.Extensions.Logging.LogLevel.Information,
            };
    }

    /// <summary>
    /// Converts Microsoft LogLevel to NLog LogLevel.
    /// </summary>
    /// <param name="logLevel">The Microsoft LogLevel to convert.</param>
    /// <returns>The corresponding NLog LogLevel.</returns>
    protected virtual NLog.LogLevel GetNLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
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
            _ => NLog.LogLevel.Info,
        };
    }
}
