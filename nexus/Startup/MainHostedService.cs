using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using nexus.CommandLine;
using nexus.Hosting;
using nexus.protocol;
using nexus.protocol.Configuration;
using nexus.setup.Interfaces;

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
            await HandleInstallCommand(cancellationToken);
        }
        else if (m_CommandLineContext.IsUpdateMode)
        {
            await HandleUpdateCommand(cancellationToken);
        }
        else if (m_CommandLineContext.IsUninstallMode)
        {
            await HandleUninstallCommand(cancellationToken);
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
            // Get logging configurator from DI
            var loggingConfigurator = m_ServiceProvider.GetRequiredService<nexus.config.ILoggingConfigurator>();
            
            // Create and configure WebApplication using protocol library (all logic encapsulated)
            var app = HttpServerSetup.CreateConfiguredWebApplication(
                m_Configuration,
                loggingConfigurator,
                m_CommandLineContext.IsServiceMode);
            
            // Get the protocol server from DI and configure it
            var protocolServer = m_ServiceProvider.GetRequiredService<IProtocolServer>();
            protocolServer.SetWebApplication(app);
            
            // Start the protocol server (which starts the WebApplication)
            await protocolServer.StartAsync(cancellationToken);
            
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
            // Get logging configurator from DI
            var loggingConfigurator = m_ServiceProvider.GetRequiredService<nexus.config.ILoggingConfigurator>();
            
            // Create and configure Host using protocol library (all logic encapsulated)
            var host = HttpServerSetup.CreateConfiguredHost(
                m_Configuration,
                loggingConfigurator,
                m_CommandLineContext.IsServiceMode);
            
            // Get the protocol server from DI and configure it
            var protocolServer = m_ServiceProvider.GetRequiredService<IProtocolServer>();
            var typedProtocolServer = protocolServer as ProtocolServer;
            if (typedProtocolServer != null)
            {
                typedProtocolServer.SetHost(host);
            }
            else
            {
                throw new InvalidOperationException("ProtocolServer must be of type ProtocolServer to set Host.");
            }
            
            // Start the protocol server (which starts the Host)
            await protocolServer.StartAsync(cancellationToken);
            
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

    private async Task HandleInstallCommand(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Handling install command...");
        
        // Get the installation handler from DI
        var installationHandler = m_ServiceProvider.GetRequiredService<IProductInstallation>();
        var success = await installationHandler.InstallServiceAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }

        // Exit successfully after completing install command
        Environment.Exit(0);
    }

    private async Task HandleUpdateCommand(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Handling update command...");
        
        // Get the installation handler from DI
        var installationHandler = m_ServiceProvider.GetRequiredService<IProductInstallation>();
        var success = await installationHandler.UpdateServiceAsync();
        
        if (!success)
        {
            Environment.Exit(1);
        }

        // Exit successfully after completing install command
        Environment.Exit(0);
    }

    private async Task HandleUninstallCommand(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Handling uninstall command...");
        
        // Get the installation handler from DI
        var installationHandler = m_ServiceProvider.GetRequiredService<IProductInstallation>();
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
            if (protocolServer != null && protocolServer.IsRunning)
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
