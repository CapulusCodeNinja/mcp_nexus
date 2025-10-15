using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;

namespace mcp_nexus.Startup
{
    /// <summary>
    /// Handles Windows service installation, uninstallation, and update commands.
    /// </summary>
    public static class ServiceCommandHandler
    {
        /// <summary>
        /// Handles the service installation command.
        /// </summary>
        /// <returns>A completed task.</returns>
        public static async Task HandleInstallCommandAsync()
        {
            if (OperatingSystem.IsWindows())
            {
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.ClearProviders();
                    builder.AddNLogWeb();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");

                var installResult = await mcp_nexus.Infrastructure.Installation.WindowsServiceInstaller.InstallServiceAsync(logger);
                Environment.Exit(installResult ? 0 : 1);
            }
            else
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                await Console.Error.WriteLineAsync("ERROR: Service installation is only supported on Windows.");
                logger.Error("Service installation is only supported on Windows");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Handles the service uninstallation command.
        /// </summary>
        /// <returns>A completed task.</returns>
        public static async Task HandleUninstallCommandAsync()
        {
            Console.WriteLine("Uninstall command detected");
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    Console.WriteLine("Starting service uninstallation...");
                    using var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddNLogWeb();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                    var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");

                    var uninstallResult = await mcp_nexus.Infrastructure.Installation.WindowsServiceInstaller.UninstallServiceAsync(logger);
                    Console.WriteLine($"Uninstall result: {uninstallResult}");
                    Environment.Exit(uninstallResult ? 0 : 1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during uninstall: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    Environment.Exit(1);
                }
            }
            else
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                await Console.Error.WriteLineAsync("ERROR: Service uninstallation is only supported on Windows.");
                logger.Error("Service uninstallation is only supported on Windows");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Handles the service update command.
        /// </summary>
        /// <returns>A completed task.</returns>
        public static async Task HandleUpdateCommandAsync()
        {
            Console.Error.WriteLine($" Update command detected");

            if (OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine($" Creating logger for update process...");

                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.ClearProviders();
                    builder.AddNLogWeb();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");

                Console.Error.WriteLine($" Starting update service call...");
                var updateResult = await mcp_nexus.Infrastructure.Installation.WindowsServiceInstaller.UpdateServiceAsync(logger);
                Console.Error.WriteLine($" Update service call completed with result: {updateResult}");

                Environment.Exit(updateResult ? 0 : 1);
            }
            else
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                await Console.Error.WriteLineAsync("ERROR: Service update is only supported on Windows.");
                logger.Error("Service update is only supported on Windows");
            }
        }
    }
}

