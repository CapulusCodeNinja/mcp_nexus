using System.Runtime.Versioning;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Nexus.CommandLine;
using Nexus.Config;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Logging;
using Nexus.Startup;

using NLog;

namespace Nexus;

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

            var settings = new Settings();
            var filesystem = new FileSystem();
            var processManager = new ProcessManager();
            var serviceController = new ServiceControllerWrapper();

            // Use hosted service for ALL command types
            var host = CreateHostBuilder(cmd, filesystem, processManager, serviceController, settings).Build();
            await host.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");
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
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="serviceController">Service controller abstraction.</param>
    /// <param name="settings">The product settings.</param>
    /// <returns>Configured host builder.</returns>
    [SupportedOSPlatform("windows")]
    internal static IHostBuilder CreateHostBuilder(CommandLineContext cmd, IFileSystem fileSystem, IProcessManager processManager, IServiceController serviceController, ISettings settings)
    {
        var builder = Host.CreateDefaultBuilder(cmd.Args)
            .ConfigureLogging((context, logging) => logging.AddNexusLogging(
                    settings,
                    cmd.IsServiceMode || cmd.IsInstallMode || cmd.IsUpdateMode || cmd.IsUninstallMode))
            .ConfigureServices((_, services) =>
            {
                // Register mode
                var ununsed = services.AddSingleton(cmd);

                // Register ONLY the main hosted service (no others)
                var ununsedHost = services.AddHostedService(sp =>
                    new MainHostedService(cmd, fileSystem, processManager, serviceController, settings));
                });

        // Configure Windows Service support if in service mode
        if (cmd.IsServiceMode)
        {
            _ = builder.UseWindowsService(options => options.ServiceName = "MCP-Nexus");
        }

        return builder;
    }
}
