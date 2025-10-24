using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using nexus.CommandLine;
using nexus.Hosting;
using nexus.Startup;
using nexus.config;
using nexus.ServiceRegistration;

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
            var cmd = new CommandLineContext(args);

            // Use hosted service for ALL command types
            var host = CreateHostBuilder(cmd).Build();
            await host.RunAsync();

            return 0;
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
    /// <param name="cmd">Command line context.</param>
    /// <returns>Configured host builder.</returns>
    [SupportedOSPlatform("windows")]
    internal static IHostBuilder CreateHostBuilder(CommandLineContext cmd)
    {
        var builder = Host.CreateDefaultBuilder(cmd.Args)
            .ConfigureLogging((context, logging) =>
            {
                logging.AddNexusLogging(context.Configuration,
                    cmd.IsServiceMode ||
                    cmd.IsInstallMode ||
                    cmd.IsUpdateMode ||
                    cmd.IsUninstallMode);
            })
            .ConfigureServices((context, services) =>
            {
                // Register mode
                services.AddSingleton(cmd);

                // Register ALL services for ALL modes (consistent architecture)
                services.AddNexusServices(context.Configuration);

                // Register ONLY the main hosted service (no others)
                services.AddHostedService<MainHostedService>();
            });

        // Configure Windows Service support if in service mode
        if (cmd.IsServiceMode)
        {
            builder.UseWindowsService(options =>
            {
                options.ServiceName = "MCP-Nexus";
            });
        }

        return builder;
    }
}
