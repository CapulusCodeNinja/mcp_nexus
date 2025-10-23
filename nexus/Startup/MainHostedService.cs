using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using nexus.CommandLine;
using nexus.Hosting;
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
        var startupBanner = new StartupBanner(m_Configuration, startupBannerLogger, m_CommandLineContext.IsServiceMode);
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
        // Move the logic from HttpServerHostedService here
        m_Logger.LogInformation("Starting HTTP server...");
        // Your HTTP server logic
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task StartStdioServer(CancellationToken cancellationToken)
    {
        // Move the logic from StdioServerHostedService here
        m_Logger.LogInformation("Starting Stdio server...");
        // Your Stdio server logic
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task StartServiceServer(CancellationToken cancellationToken)
    {
        // Move the logic from ServiceHostedService here
        m_Logger.LogInformation("Starting Service server...");
        // Your Service server logic
        await Task.Delay(Timeout.Infinite, cancellationToken);
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
    }

    /// <summary>
    /// Stops the hosted service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
