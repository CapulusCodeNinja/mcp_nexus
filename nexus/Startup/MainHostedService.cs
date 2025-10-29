using Microsoft.Extensions.Hosting;

using Nexus.CommandLine;
using Nexus.Protocol;
using Nexus.Protocol.Configuration;
using Nexus.Setup;

using NLog;

namespace Nexus.Startup;

/// <summary>
/// Main hosted service that orchestrates the entire application startup sequence.
/// </summary>
public class MainHostedService : IHostedService
{
    private readonly Logger m_Logger;
    private readonly CommandLineContext m_CommandLineContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainHostedService"/> class.
    /// </summary>
    /// <param name="commandLineContext">Command line context.</param>
    public MainHostedService(
        CommandLineContext commandLineContext)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_CommandLineContext = commandLineContext;
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
        var startupBanner = new StartupBanner(m_CommandLineContext.IsServiceMode, m_CommandLineContext);
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

    /// <summary>
    /// Starts the HTTP server mode with MCP protocol support.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous server run loop.</returns>
    private async Task StartHttpServer(CancellationToken cancellationToken)
    {
        m_Logger.Info("Starting HTTP server mode...");

        try
        {
            // Create and configure WebApplication using protocol library (all logic encapsulated)
            var app = HttpServerSetup.CreateConfiguredWebApplication(
                m_CommandLineContext.IsServiceMode);

            ProtocolServer.Instance.SetWebApplication(app);

            // Start the protocol server (which starts the WebApplication)
            await ProtocolServer.Instance.StartAsync(cancellationToken);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown - don't log as error
            m_Logger.Info("HTTP server shutdown requested");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to start HTTP server");
            throw;
        }
    }

    /// <summary>
    /// Starts the Stdio server mode with MCP protocol support.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous server run loop.</returns>
    private async Task StartStdioServer(CancellationToken cancellationToken)
    {
        m_Logger.Info("Starting Stdio server mode...");

        try
        {
            // Create and configure Host using protocol library (all logic encapsulated)
            var host = HttpServerSetup.CreateConfiguredHost(m_CommandLineContext.IsServiceMode);

            // Get the protocol server from DI and configure it
            ProtocolServer.Instance.SetHost(host);

            // Start the protocol server (which starts the Host)
            await ProtocolServer.Instance.StartAsync(cancellationToken);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown - don't log as error
            m_Logger.Info("Stdio server shutdown requested");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to start Stdio server");
            throw;
        }
    }

    /// <summary>
    /// Starts the Windows Service mode (uses HTTP transport internally).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous server run loop.</returns>
    private async Task StartServiceServer(CancellationToken cancellationToken)
    {
        // Service mode uses HTTP transport
        await StartHttpServer(cancellationToken);
    }

    /// <summary>
    /// Handles the install command to install the application as a Windows Service.
    /// </summary>
    /// <returns>A task representing the asynchronous install operation.</returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private async Task HandleInstallCommand()
    {
        m_Logger.Info("Handling install command...");

        // Get the installation handler from DI
        var success = await ProductInstallation.Instance.InstallServiceAsync();

        if (!success)
        {
            Environment.Exit(1);
        }

        // Exit successfully after completing install command
        Environment.Exit(0);
    }

    /// <summary>
    /// Handles the update command to update an existing Windows Service installation.
    /// </summary>
    /// <returns>A task representing the asynchronous update operation.</returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private async Task HandleUpdateCommand()
    {
        m_Logger.Info("Handling update command...");

        // Get the installation handler from DI
        var success = await ProductInstallation.Instance.UpdateServiceAsync();

        if (!success)
        {
            Environment.Exit(1);
        }

        // Exit successfully after completing install command
        Environment.Exit(0);
    }

    /// <summary>
    /// Handles the uninstall command to remove the Windows Service.
    /// </summary>
    /// <returns>A task representing the asynchronous uninstall operation.</returns>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private async Task HandleUninstallCommand()
    {
        m_Logger.Info("Handling uninstall command...");

        // Get the installation handler from DI
        var success = await ProductInstallation.Instance.UninstallServiceAsync();

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
        m_Logger.Info("Stopping main hosted service...");

        try
        {
            // Stop the protocol server if it's running
            var protocolServer = ProtocolServer.Instance;
            if (protocolServer is { IsRunning: true })
            {
                await protocolServer.StopAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error stopping protocol server");
        }

        m_Logger.Info("Main hosted service stopped");
    }
}
