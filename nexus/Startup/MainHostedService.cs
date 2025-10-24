using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using nexus.CommandLine;
using nexus.protocol;
using nexus.protocol.Configuration;
using nexus.setup;

namespace nexus.Startup;

/// <summary>
/// Main hosted service that orchestrates the entire application startup sequence.
/// </summary>
public class MainHostedService : IHostedService
{
    private readonly ILogger<MainHostedService> m_Logger;
    private readonly IConfiguration m_Configuration;
    private readonly CommandLineContext m_CommandLineContext;
    private readonly IServiceProvider m_ServiceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainHostedService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="commandLineContext">Command line context.</param>
    /// <param name="serviceProvider">Service provider.</param>
    public MainHostedService(
        ILogger<MainHostedService> logger,
        IConfiguration configuration,
        CommandLineContext commandLineContext,
        IServiceProvider serviceProvider)
    {
        m_Logger = logger;
        m_Configuration = configuration;
        m_CommandLineContext = commandLineContext;
        m_ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// Starts the hosted service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the start operation.</returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 1. Display startup banner FIRST (guaranteed first log output)
        var startupBannerLogger = m_ServiceProvider.GetRequiredService<ILogger<StartupBanner>>();
        var startupBanner = new StartupBanner(m_Configuration, startupBannerLogger, m_CommandLineContext.IsServiceMode, m_CommandLineContext);
        startupBanner.DisplayBanner();

        // 2. Handle the appropriate command based on command line context
        if (m_CommandLineContext.IsHttpMode)
        {
            await StartHttpServer(cancellationToken);
        }
        else if (m_CommandLineContext.IsStdioMode)
        {
            await StartStdioServer(cancellationToken);
        }
        else if (m_CommandLineContext.IsServiceMode)
        {
            await StartServiceServer(cancellationToken);
        }
        else if (m_CommandLineContext.IsInstallMode)
        {
            await HandleInstallCommand();
        }
        else if (m_CommandLineContext.IsUpdateMode)
        {
            await HandleUpdateCommand();
        }
        else if (m_CommandLineContext.IsUninstallMode)
        {
            await HandleUninstallCommand();
        }
        else
        {
            // Default to HTTP mode
            await StartHttpServer(cancellationToken);
        }
    }

    private async Task StartHttpServer(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Starting HTTP server mode...");

        try
        {
            // Create and configure WebApplication using protocol library (all logic encapsulated)
            var app = HttpServerSetup.CreateConfiguredWebApplication(
                m_Configuration,
                config.LoggingConfiguration.GetInstance(),
                m_CommandLineContext.IsServiceMode);

            ProtocolServer.GetInstance(m_ServiceProvider).SetWebApplication(app);

            // Start the protocol server (which starts the WebApplication)
            await ProtocolServer.GetInstance(m_ServiceProvider).StartAsync(cancellationToken);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown - don't log as error
            m_Logger.LogInformation("HTTP server shutdown requested");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to start HTTP server");
            throw;
        }
    }

    private async Task StartStdioServer(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Starting Stdio server mode...");

        try
        {
            // Create and configure Host using protocol library (all logic encapsulated)
            var host = HttpServerSetup.CreateConfiguredHost(
                m_Configuration,
                config.LoggingConfiguration.GetInstance(),
                m_CommandLineContext.IsServiceMode);

            // Get the protocol server from DI and configure it
            ProtocolServer.GetInstance(m_ServiceProvider).SetHost(host);

            // Start the protocol server (which starts the Host)
            await ProtocolServer.GetInstance(m_ServiceProvider).StartAsync(cancellationToken);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown - don't log as error
            m_Logger.LogInformation("Stdio server shutdown requested");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to start Stdio server");
            throw;
        }
    }

    private async Task StartServiceServer(CancellationToken cancellationToken)
    {
        // Service mode uses HTTP transport
        await StartHttpServer(cancellationToken);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private async Task HandleInstallCommand()
    {
        m_Logger.LogInformation("Handling install command...");

        // Get the installation handler from DI
        var installationHandler = setup.ProductInstallation.Instance;
        var success = await installationHandler.InstallServiceAsync();

        if (!success)
        {
            Environment.Exit(1);
        }

        // Exit successfully after completing install command
        Environment.Exit(0);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private async Task HandleUpdateCommand()
    {
        m_Logger.LogInformation("Handling update command...");

        // Get the installation handler from DI
        var installationHandler = CreateProductInstallation();
        var success = await installationHandler.UpdateServiceAsync();

        if (!success)
        {
            Environment.Exit(1);
        }

        // Exit successfully after completing install command
        Environment.Exit(0);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private async Task HandleUninstallCommand()
    {
        m_Logger.LogInformation("Handling uninstall command...");

        // Get the installation handler from DI
        var installationHandler = CreateProductInstallation();
        var success = await installationHandler.UninstallServiceAsync();

        if (!success)
        {
            Environment.Exit(1);
        }

        // Exit successfully after completing install command
        Environment.Exit(0);
    }

    /// <summary>
    /// Stops the hosted service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Stopping main hosted service...");

        try
        {
            // Stop the protocol server if it's running
            var protocolServer = m_ServiceProvider.GetService<IProtocolServer>();
            if (protocolServer is { IsRunning: true })
            {
                await protocolServer.StopAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error stopping protocol server");
        }

        m_Logger.LogInformation("Main hosted service stopped");
    }
}
