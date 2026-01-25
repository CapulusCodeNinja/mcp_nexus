using Microsoft.Extensions.Logging;

using NLog;
using NLog.Web;

namespace WinAiDbg.Config.Internal;

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

        // Prevent any console-based providers from emitting noisy logs that would interfere
        // with MCP stdio communication. These filters ensure that even if a console logger
        // is added by hosting infrastructure, the most verbose categories are suppressed.
        _ = logging.AddFilter("ModelContextProtocol.Server.StdioServerTransport", Microsoft.Extensions.Logging.LogLevel.None);
        _ = logging.AddFilter("ModelContextProtocol.Server.McpServer", Microsoft.Extensions.Logging.LogLevel.None);
        _ = logging.AddFilter("Microsoft.Hosting.Lifetime", Microsoft.Extensions.Logging.LogLevel.None);
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
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinAiDbg.log"),
                ArchiveFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "archive", "WinAiDbg-${shortdate}-{##}.log"),
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
            logDirectory = Path.Combine(programDataPath, "WinAiDbg", "Logs");
            _ = Path.Combine(logDirectory, "WinAiDbg-internal.log");

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
            _ = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinAiDbg-internal.log");
        }

        // Update file target paths
        foreach (var target in nlogConfig.AllTargets.OfType<NLog.Targets.FileTarget>())
        {
            if (target.Name == "mainFile")
            {
                // Update main log file path
                target.FileName = Path.Combine(logDirectory, "WinAiDbg.log");

                // Update archive path to use the new log directory
                target.ArchiveFileName = Path.Combine(logDirectory, "archive", "WinAiDbg-${shortdate}-{##}.log");
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
                internalLogFile = Path.Combine(programDataPath, "WinAiDbg", "Logs", "WinAiDbg-internal.log");
            }
            else
            {
                // Use application directory for non-service mode
                internalLogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinAiDbg-internal.log");
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
    /// Ensures that all framework logging is routed through NLog so that no logs
    /// are written directly to stdout. This is critical for MCP stdio mode where
    /// stdout must be reserved exclusively for JSON-RPC messages and all human-readable
    /// logging must go to file targets (or explicitly configured sinks).
    /// </summary>
    /// <param name="logging">The logging builder to configure.</param>
    /// <param name="logLevel">The log level to set.</param>
    protected virtual void ConfigureNLogProvider(ILoggingBuilder logging, Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        ArgumentNullException.ThrowIfNull(logging);

        // Remove default console/debug providers so nothing writes directly to stdout/stderr.
        _ = logging.ClearProviders();

        // Route Microsoft.Extensions.Logging through NLog (using NLog.Web for compatibility
        // with both generic hosts and ASP.NET Core).
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
