using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

using WinAiDbg.Config;
using WinAiDbg.Engine.Extensions.Security;
using WinAiDbg.Engine.Share;

using NLog;

namespace WinAiDbg.Engine.Extensions.Callback;

/// <summary>
/// Manages the extension callback HTTP server lifecycle.
/// </summary>
internal class CallbackServerManager : ICallbackServerManager
{
    private readonly Logger m_Logger;
    private readonly ISettings m_Settings;
    private readonly IDebugEngine m_Engine;
    private readonly TokenValidator m_TokenValidator;
    private readonly string m_Host;
    private readonly int m_ConfiguredPort;
    private WebApplication? m_WebApplication;
    private bool m_Disposed;

    /// <summary>
    /// Gets the port number the callback server is listening on.
    /// </summary>
    public int Port
    {
        get; private set;
    }

    /// <summary>
    /// Gets the callback URL for extensions to use.
    /// </summary>
    public string CallbackUrl
    {
        get; private set;
    }

    /// <summary>
    /// Gets a value indicating whether the server is currently running.
    /// </summary>
    public bool IsRunning
    {
        get; private set;
    }

    /// <summary>
    /// Gets the token validator instance for sharing with other components.
    /// </summary>
    /// <returns>The token validator instance used by the callback server.</returns>
    public TokenValidator GetTokenValidator()
    {
        return m_TokenValidator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackServerManager"/> class.
    /// </summary>
    /// <param name="engine">The debug engine for handling extension callbacks.</param>
    /// <param name="tokenValidator">The token validator for validating extension script callbacks.</param>
    /// <param name="settings">The product settings.</param>
    public CallbackServerManager(IDebugEngine engine, TokenValidator tokenValidator, ISettings settings)
    {
        m_Settings = settings;

        m_Logger = LogManager.GetCurrentClassLogger();
        m_Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        m_TokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));

        // Read configuration
        m_ConfiguredPort = m_Settings.Get().WinAiDbg.Extensions.CallbackPort;
        m_Host = "127.0.0.1"; // Localhost only for security

        Port = 0;
        CallbackUrl = string.Empty;
        IsRunning = false;
        m_Disposed = false;
    }

    /// <summary>
    /// Starts the callback server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(m_Disposed, this);

        if (IsRunning)
        {
            m_Logger.Warn("Extension callback server is already running on port {Port}", Port);
            return;
        }

        try
        {
            m_Logger.Info("Starting extension callback server...");

            // Create the web application
            m_WebApplication = CreateWebApplication();

            // Configure the callback routes
            ConfigureRoutes(m_WebApplication);

            // Start the server
            await m_WebApplication.StartAsync(cancellationToken);

            // Extract the actual port
            Port = ExtractPort(m_WebApplication);

            // Build the callback URL
            CallbackUrl = $"http://{m_Host}:{Port}/extension-callback";

            IsRunning = true;

            m_Logger.Info("Extension callback server started successfully on port {Port}", Port);
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to start extension callback server");
            IsRunning = false;
            Port = 0;
            CallbackUrl = string.Empty;

            // Clean up on failure
            if (m_WebApplication != null)
            {
                await m_WebApplication.DisposeAsync();
                m_WebApplication = null;
            }

            throw;
        }
    }

    /// <summary>
    /// Stops the callback server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || m_WebApplication == null)
        {
            m_Logger.Debug("Extension callback server is not running");
            return;
        }

        try
        {
            m_Logger.Info("Stopping extension callback server on port {Port}", Port);
            await m_WebApplication.StopAsync(cancellationToken);
            IsRunning = false;
            m_Logger.Info("Extension callback server stopped successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error stopping extension callback server");
            throw;
        }
    }

    /// <summary>
    /// Creates and configures a WebApplication for the callback server.
    /// </summary>
    /// <returns>A configured WebApplication instance.</returns>
    private WebApplication CreateWebApplication()
    {
        var builder = WebApplication.CreateBuilder();

        // Configure to listen on localhost only
        var url = m_ConfiguredPort == 0
            ? $"http://{m_Host}:0" // Dynamic port
            : $"http://{m_Host}:{m_ConfiguredPort}";  // Configured port

        _ = builder.WebHost.UseUrls(url);

        // Minimal logging for callback server
        _ = builder.Logging.ClearProviders();

        return builder.Build();
    }

    /// <summary>
    /// Configures the callback routes on the web application.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    private void ConfigureRoutes(WebApplication app)
    {
        var callbackServer = new CallbackServer(m_Engine, m_TokenValidator);
        callbackServer.ConfigureRoutes(app);
    }

    /// <summary>
    /// Extracts the actual port number from the running web application.
    /// </summary>
    /// <param name="app">The running web application.</param>
    /// <returns>The port number the application is listening on.</returns>
    private int ExtractPort(WebApplication app)
    {
        var addresses = app.Urls;
        if (addresses.Any())
        {
            var uri = new Uri(addresses.First());
            return uri.Port;
        }

        // Fallback to configured port
        m_Logger.Warn("Could not determine actual callback port from addresses, using configured port {Port}", m_ConfiguredPort);
        return m_ConfiguredPort;
    }

    /// <summary>
    /// Disposes resources asynchronously, including the web application.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // Stop and dispose the web application
        if (m_WebApplication != null)
        {
            try
            {
                if (IsRunning)
                {
                    await StopAsync();
                }

                await m_WebApplication.DisposeAsync();
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Error disposing extension callback server");
            }
            finally
            {
                m_WebApplication = null;
            }
        }

        GC.SuppressFinalize(this);
    }
}
