using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using nexus.CommandLine;
using nexus.Hosting;
using nexus.config;
using nexus.config.ServiceRegistration;

namespace nexus;

/// <summary>
/// Main entry point for the Nexus application.
/// </summary>
internal static class Program
{
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
            // Parse command line and run
            var rootCommand = CommandLineBuilder.BuildRootCommand();
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
        finally
        {
            LogManager.Shutdown();
        }
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
            .ConfigureLogging((context, logging) =>
            {
                // Use sophisticated logging setup from nexus_config library
                var isServiceMode = mode == ServerMode.Service;
                logging.AddNexusLogging(context.Configuration, isServiceMode);
            })
            .ConfigureServices((context, services) =>
            {
                // Display startup banner before starting any hosted services
                var isServiceMode = mode == ServerMode.Service;
                var startupBanner = new Startup.StartupBanner(context, isServiceMode);
                
                startupBanner.DisplayBanner();

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
                options.ServiceName = "MCP-Nexus";
            });
        }

        return builder;
    }
}

