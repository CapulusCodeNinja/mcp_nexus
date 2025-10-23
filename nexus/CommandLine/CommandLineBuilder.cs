using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nexus.setup;
using nexus.setup.Configuration;
using nexus.setup.Models;
using nexus.setup.Interfaces;
using nexus.utilities.ServiceManagement;

namespace nexus.CommandLine;

/// <summary>
/// Builds the command-line interface for the application.
/// </summary>
internal static class CommandLineBuilder
{
    /// <summary>
    /// Builds the root command with all subcommands.
    /// </summary>
    /// <returns>The root command.</returns>
    [SupportedOSPlatform("windows")]
    internal static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Nexus - Windows Debugging Server")
        {
            BuildHttpCommand(),
            BuildStdioCommand(),
            BuildServiceCommand(),
            BuildInstallCommand(),
            BuildUpdateCommand()
        };

        return rootCommand;
    }

    /// <summary>
    /// Builds the --http command.
    /// </summary>
    /// <returns>The http command.</returns>
    private static Command BuildHttpCommand()
    {
        var httpCommand = new Command("--http", "Run server in HTTP mode");
        
        var portOption = new Option<int>(
            name: "--port",
            description: "HTTP server port",
            getDefaultValue: () => 5000);
        httpCommand.AddOption(portOption);

        httpCommand.SetHandler(async (int port) =>
        {
            var host = Program.CreateHostBuilder(Array.Empty<string>(), ServerMode.Http).Build();
            await host.RunAsync();
        }, portOption);

        return httpCommand;
    }

    /// <summary>
    /// Builds the --stdio command.
    /// </summary>
    /// <returns>The stdio command.</returns>
    private static Command BuildStdioCommand()
    {
        var stdioCommand = new Command("--stdio", "Run server in standard input/output mode");

        stdioCommand.SetHandler(async () =>
        {
            var host = Program.CreateHostBuilder(Array.Empty<string>(), ServerMode.Stdio).Build();
            await host.RunAsync();
        });

        return stdioCommand;
    }

    /// <summary>
    /// Builds the --service command.
    /// </summary>
    /// <returns>The service command.</returns>
    private static Command BuildServiceCommand()
    {
        var serviceCommand = new Command("--service", "Run server as Windows Service");

        serviceCommand.SetHandler(async () =>
        {
            var host = Program.CreateHostBuilder(Array.Empty<string>(), ServerMode.Service).Build();
            await host.RunAsync();
        });

        return serviceCommand;
    }

    /// <summary>
    /// Builds the --install command.
    /// </summary>
    /// <returns>The install command.</returns>
    [SupportedOSPlatform("windows")]
    private static Command BuildInstallCommand()
    {
        var installCommand = new Command("--install", "Install Nexus as a Windows Service");

        var serviceNameOption = new Option<string>(
            name: "--service-name",
            description: "Name of the Windows service",
            getDefaultValue: () => "MCP-Nexus");
        installCommand.AddOption(serviceNameOption);

        var displayNameOption = new Option<string>(
            name: "--display-name",
            description: "Display name of the service",
            getDefaultValue: () => "MCP-Nexus Debugging Server");
        installCommand.AddOption(displayNameOption);

        var startModeOption = new Option<ServiceStartMode>(
            name: "--start-mode",
            description: "Service start mode (Automatic, Manual, Disabled)",
            getDefaultValue: () => ServiceStartMode.Automatic);
        installCommand.AddOption(startModeOption);

        installCommand.SetHandler(async (string serviceName, string displayName, ServiceStartMode startMode) =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddNexusSetupServices();
            var serviceProvider = services.BuildServiceProvider();

            var installationHandler = serviceProvider.GetRequiredService<IProductInstallation>();
            var success = await installationHandler.InstallServiceAsync(serviceName, displayName, startMode);
            
            if (!success)
            {
                Environment.Exit(1);
            }
        }, serviceNameOption, displayNameOption, startModeOption);

        return installCommand;
    }


    /// <summary>
    /// Builds the --update command.
    /// </summary>
    /// <returns>The update command.</returns>
    [SupportedOSPlatform("windows")]
    private static Command BuildUpdateCommand()
    {
        var updateCommand = new Command("--update", "Update an installed Windows Service");

        var serviceNameOption = new Option<string>(
            name: "--service-name",
            description: "Name of the Windows service to update",
            getDefaultValue: () => "MCP-Nexus");
        updateCommand.AddOption(serviceNameOption);

        updateCommand.SetHandler(async (string serviceName) =>
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddNexusSetupServices();
            var serviceProvider = services.BuildServiceProvider();

            var installationHandler = serviceProvider.GetRequiredService<IProductInstallation>();
            var success = await installationHandler.UpdateServiceAsync(serviceName);
            
            if (!success)
            {
                Environment.Exit(1);
            }
        }, serviceNameOption);

        return updateCommand;
    }
}

