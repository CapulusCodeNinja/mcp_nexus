using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

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

    /// <summary>
    /// Gets the singleton instance of the protocol server.
    /// </summary>
    public static IProtocolServer Instance { get; } = new ProtocolServer();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolServer"/> class.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
    private ProtocolServer()
    {
        IsRunning = false;
        m_Logger = LogManager.GetCurrentClassLogger();
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
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server is already running.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(m_Disposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Protocol server is already running.");
        }

        if (m_WebApplication == null && m_Host == null)
        {
            throw new InvalidOperationException("Server instance must be set before starting. Call SetWebApplication() for HTTP mode or set Host for Stdio mode.");
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
    public void SetWebApplication(WebApplication app)
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
    public void SetHost(IHost host)
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

        if (m_WebApplication != null)
        {
            m_WebApplication.DisposeAsync().GetAwaiter().GetResult();
            m_WebApplication = null;
        }

        if (m_Host != null)
        {
            m_Host.Dispose();
            m_Host = null;
        }

        m_Disposed = true;
        m_Logger.Debug("Protocol server disposed.");
    }
}
