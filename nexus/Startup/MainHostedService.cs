using System.Runtime.Versioning;

using Microsoft.Extensions.Hosting;

using Nexus.CommandLine;
using Nexus.Config;
using Nexus.Protocol;
using Nexus.Setup;

using NLog;

namespace Nexus.Startup;

/// <summary>
/// Main hosted service that orchestrates the entire application startup sequence.
/// </summary>
[SupportedOSPlatform("windows")]
internal class MainHostedService : IHostedService
{
    private readonly Logger m_Logger;
    private readonly ISettings m_Settings;
    private readonly CommandLineContext m_CommandLineContext;
    private readonly IProtocolServer m_ProtocolServer;
    private readonly IProductInstallation m_ProductInstallation;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainHostedService"/> class.
    /// </summary>
    /// <param name="commandLineContext">Command line context.</param>
    /// <param name="settings">The product settings.</param>
    public MainHostedService(
        CommandLineContext commandLineContext, ISettings settings)
        : this(commandLineContext, new ProtocolServer(settings), new ProductInstallation(settings), settings)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainHostedService"/> class.
    /// </summary>
    /// <param name="commandLineContext">Command line context.</param>
    /// <param name="protocolServer">The command line server.</param>
    /// <param name="productInstallation">The product setup finctionality.</param>
    /// <param name="settings">The product settings.</param>
    internal MainHostedService(
        CommandLineContext commandLineContext, IProtocolServer protocolServer, IProductInstallation productInstallation, ISettings settings)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_CommandLineContext = commandLineContext;
        m_ProtocolServer = protocolServer;
        m_ProductInstallation = productInstallation;
        m_Settings = settings;
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
        var startupBanner = new StartupBanner(m_CommandLineContext.IsServiceMode, m_CommandLineContext, m_Settings);
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
            await m_ProtocolServer.StartAsync(m_CommandLineContext.IsServiceMode, true, cancellationToken);

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
            await m_ProtocolServer.StartAsync(m_CommandLineContext.IsServiceMode, false, cancellationToken);

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
        var success = await m_ProductInstallation.InstallServiceAsync();

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
        var success = await m_ProductInstallation.UpdateServiceAsync();

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
        var success = await m_ProductInstallation.UninstallServiceAsync();

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
            if (m_ProtocolServer is { IsRunning: true })
            {
                await m_ProtocolServer.StopAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error stopping protocol server");
        }

        m_Logger.Info("Main hosted service stopped");
    }
}
