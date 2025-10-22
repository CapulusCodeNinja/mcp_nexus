using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nexus.setup;
using nexus.setup.Configuration;
using nexus.setup.Models;

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
            Console.WriteLine($"Starting HTTP server on port {port}...");
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
            Console.WriteLine("Starting stdio server...");
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
            Console.WriteLine("Starting as Windows Service...");
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
            getDefaultValue: () => "Nexus");
        installCommand.AddOption(serviceNameOption);

        var displayNameOption = new Option<string>(
            name: "--display-name",
            description: "Display name of the service",
            getDefaultValue: () => "Nexus Debugging Server");
        installCommand.AddOption(displayNameOption);

        var startModeOption = new Option<ServiceStartMode>(
            name: "--start-mode",
            description: "Service start mode (Automatic, Manual, Disabled)",
            getDefaultValue: () => ServiceStartMode.Automatic);
        installCommand.AddOption(startModeOption);

        installCommand.SetHandler(async (string serviceName, string displayName, ServiceStartMode startMode) =>
        {
            Console.WriteLine($"Installing {serviceName} as Windows Service...");

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddNexusSetupServices();
            var serviceProvider = services.BuildServiceProvider();

            var installer = serviceProvider.GetRequiredService<IServiceInstaller>();
            
            var executablePath = Path.Combine(AppContext.BaseDirectory, "nexus.exe");
            var options = new ServiceInstallationOptions
            {
                ServiceName = serviceName,
                DisplayName = displayName,
                Description = "Model Context Protocol server for Windows debugging tools",
                ExecutablePath = executablePath,
                StartMode = startMode,
                Account = ServiceAccount.LocalSystem
            };

            var result = await installer.InstallServiceAsync(options);

            if (result.Success)
            {
                Console.WriteLine($"✓ {result.Message}");
                Console.WriteLine($"Service '{serviceName}' installed successfully.");
                Console.WriteLine($"Use 'sc start {serviceName}' to start the service.");
            }
            else
            {
                Console.Error.WriteLine($"✗ {result.Message}");
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    Console.Error.WriteLine($"Details: {result.ErrorDetails}");
                }
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
            getDefaultValue: () => "Nexus");
        updateCommand.AddOption(serviceNameOption);

        updateCommand.SetHandler(async (string serviceName) =>
        {
            Console.WriteLine($"Updating Windows Service '{serviceName}'...");

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddNexusSetupServices();
            var serviceProvider = services.BuildServiceProvider();

            var updater = serviceProvider.GetRequiredService<IServiceUpdater>();
            
            var newExecutablePath = Path.Combine(AppContext.BaseDirectory, "nexus.exe");
            var result = await updater.UpdateServiceAsync(serviceName, newExecutablePath);

            if (result.Success)
            {
                Console.WriteLine($"✓ {result.Message}");
                Console.WriteLine($"Service '{serviceName}' updated successfully.");
            }
            else
            {
                Console.Error.WriteLine($"✗ {result.Message}");
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    Console.Error.WriteLine($"Details: {result.ErrorDetails}");
                }
                Environment.Exit(1);
            }
        }, serviceNameOption);

        return updateCommand;
    }
}

