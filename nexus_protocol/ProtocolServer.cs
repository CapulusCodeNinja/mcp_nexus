using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

using Nexus.Config;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.Protocol.Configuration;
using Nexus.Protocol.Services;

using NLog;

namespace Nexus.Protocol;

/// <summary>
/// Implementation of the MCP protocol server lifecycle management.
/// Manages the protocol server startup, shutdown, and configuration.
/// </summary>
public class ProtocolServer : IProtocolServer
{
    private readonly Logger m_Logger;
    private object? m_Configuration;
    private WebApplication? m_WebApplication;
    private IHost? m_Host;
    private bool m_Disposed;

    private readonly ISettings m_Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolServer"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <param name="settings">The product settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
    public ProtocolServer(IFileSystem fileSystem, IProcessManager processManager, ISettings settings)
    {
        IsRunning = false;
        m_Logger = LogManager.GetCurrentClassLogger();
        m_Settings = settings;
        EngineService.Initialize(fileSystem, processManager, m_Settings);
    }

    /// <summary>
    /// Gets a value indicating whether the protocol server is currently running.
    /// </summary>
    public bool IsRunning
    {
        get;
        private set;
    }

    /// <summary>
    /// Starts the protocol server with the current configuration.
    /// </summary>
    /// <param name="isServiceMode">Run the protocol server in service mode.</param>
    /// <param name="isHttpMode">Run the protocol server in http mode.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server is already running.</exception>
    public async Task StartAsync(bool isServiceMode, bool isHttpMode, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(m_Disposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Protocol server is already running.");
        }

        if (isServiceMode && isHttpMode == false)
        {
            throw new InvalidOperationException("Running the protocal server as service but not in HTTP mode is not supported");
        }

        if (isServiceMode || isHttpMode)
        {
            // Create and configure WebApplication using protocol library (all logic encapsulated)
            SetWebApplication(HttpServerSetup.CreateConfiguredWebApplication(
                m_Settings,
                isServiceMode));
        }
        else
        {
            SetHost(HttpServerSetup.CreateConfiguredHost(
                m_Settings,
                isServiceMode));
        }

        m_Logger.Info("Starting MCP protocol server...");

        if (m_WebApplication != null)
        {
            // HTTP mode
            await m_WebApplication.StartAsync(cancellationToken);
            m_Logger.Info("MCP protocol server (HTTP) started successfully on {Urls}", string.Join(", ", m_WebApplication.Urls));
        }
        else if (m_Host != null)
        {
            // Stdio mode
            await m_Host.StartAsync(cancellationToken);
            m_Logger.Info("MCP protocol server (Stdio) started successfully");
        }

        IsRunning = true;
    }

    /// <summary>
    /// Stops the protocol server gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(m_Disposed, this);

        if (!IsRunning)
        {
            m_Logger.Warn("Protocol server is not running.");
            return;
        }

        m_Logger.Info("Stopping MCP protocol server...");

        if (m_WebApplication != null)
        {
            await m_WebApplication.StopAsync(cancellationToken);
        }
        else if (m_Host != null)
        {
            await m_Host.StopAsync(cancellationToken);
        }

        IsRunning = false;
        m_Logger.Info("MCP protocol server stopped successfully.");
    }

    /// <summary>
    /// Sets the configuration for the protocol server.
    /// </summary>
    /// <param name="configuration">The configuration object to set.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to change configuration while server is running.</exception>
    public void SetConfiguration(object configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ObjectDisposedException.ThrowIf(m_Disposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Cannot change configuration while server is running. Stop the server first.");
        }

        m_Logger.Debug("Updating protocol server configuration.");
        m_Configuration = configuration;
        m_Logger.Info("Protocol server configuration updated.");
    }

    /// <summary>
    /// Sets the WebApplication instance for HTTP mode operation.
    /// </summary>
    /// <param name="app">The configured web application.</param>
    /// <exception cref="ArgumentNullException">Thrown when app is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to set WebApplication while server is running.</exception>
    private void SetWebApplication(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        ObjectDisposedException.ThrowIf(m_Disposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Cannot set WebApplication while server is running. Stop the server first.");
        }

        m_Logger.Debug("Setting WebApplication for protocol server (HTTP mode).");
        m_WebApplication = app;
        m_Host = null; // Ensure only one mode is active
        m_Logger.Info("WebApplication set successfully for HTTP mode.");
    }

    /// <summary>
    /// Sets the Host instance for stdio mode operation.
    /// </summary>
    /// <param name="host">The configured host.</param>
    /// <exception cref="ArgumentNullException">Thrown when host is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to set Host while server is running.</exception>
    private void SetHost(IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        ObjectDisposedException.ThrowIf(m_Disposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Cannot set Host while server is running. Stop the server first.");
        }

        m_Logger.Debug("Setting Host for protocol server (Stdio mode).");
        m_Host = host;
        m_WebApplication = null; // Ensure only one mode is active
        m_Logger.Info("Host set successfully for Stdio mode.");
    }

    /// <summary>
    /// Disposes of the protocol server and releases all resources.
    /// Ensures that the underlying debug engine and all associated CDB sessions are
    /// shut down before the HTTP/Stdio hosts are disposed.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Logger.Debug("Disposing protocol server...");

        if (IsRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        // Ensure the debug engine and all debug sessions are shut down deterministically
        EngineService.Shutdown();

        if (m_WebApplication is { } webApplication)
        {
            webApplication.DisposeAsync().GetAwaiter().GetResult();
            m_WebApplication = null;
        }

        if (m_Host is { } host)
        {
            host.Dispose();
            m_Host = null;
        }

        m_Disposed = true;
        m_Logger.Debug("Protocol server disposed.");
    }
}
