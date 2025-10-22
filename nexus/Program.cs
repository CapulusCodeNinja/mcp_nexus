using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using nexus.CommandLine;
using nexus.Hosting;

namespace nexus;

/// <summary>
/// Main entry point for the Nexus application.
/// </summary>
internal static class Program
{
    private static Logger? m_Logger;

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    [SupportedOSPlatform("windows")]
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Initialize logging first
            InitializeLogging();
            m_Logger = LogManager.GetCurrentClassLogger();
            m_Logger.Info("=== Nexus Starting ===");
            m_Logger.Info($"Command line: {string.Join(" ", args)}");

            // Parse command line and run
            var rootCommand = CommandLineBuilder.BuildRootCommand();
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            m_Logger?.Fatal(ex, "Fatal error during startup");
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
        finally
        {
            m_Logger?.Info("=== Nexus Shutdown ===");
            LogManager.Shutdown();
        }
    }

    /// <summary>
    /// Initializes NLog logging system.
    /// </summary>
    private static void InitializeLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();

        // Console target
        var consoleTarget = new NLog.Targets.ConsoleTarget("console")
        {
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
        };
        config.AddTarget(consoleTarget);
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget);

        // File target
        var fileTarget = new NLog.Targets.FileTarget("file")
        {
            FileName = "${basedir}/logs/nexus-${shortdate}.log",
            Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}",
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
            MaxArchiveFiles = 7
        };
        config.AddTarget(fileTarget);
        config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget);

        LogManager.Configuration = config;
    }

    /// <summary>
    /// Creates and configures a host builder for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="mode">Server mode (http, stdio, service).</param>
    /// <returns>Configured host builder.</returns>
    internal static IHostBuilder CreateHostBuilder(string[] args, ServerMode mode)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddNLog();
            })
            .ConfigureServices((context, services) =>
            {
                // Register mode
                services.AddSingleton(new ServerModeContext(mode));

                // Register hosted service based on mode
                switch (mode)
                {
                    case ServerMode.Http:
                        services.AddHostedService<HttpServerHostedService>();
                        break;
                    case ServerMode.Stdio:
                        services.AddHostedService<StdioServerHostedService>();
                        break;
                    case ServerMode.Service:
                        services.AddHostedService<ServiceHostedService>();
                        break;
                }
            });

        // Configure Windows Service support if in service mode
        if (mode == ServerMode.Service)
        {
            builder.UseWindowsService(options =>
            {
                options.ServiceName = "Nexus";
            });
        }

        return builder;
    }
}

