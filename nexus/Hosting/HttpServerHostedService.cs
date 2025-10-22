using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nexus.protocol;

namespace nexus.Hosting;

/// <summary>
/// Hosted service for HTTP server mode.
/// </summary>
internal class HttpServerHostedService : IHostedService
{
    private readonly ILogger<HttpServerHostedService> m_Logger;
    private readonly IProtocolServer m_ProtocolServer;
    private readonly IHostApplicationLifetime m_Lifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServerHostedService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="protocolServer">Protocol server instance.</param>
    /// <param name="lifetime">Application lifetime.</param>
    public HttpServerHostedService(
        ILogger<HttpServerHostedService> logger,
        IProtocolServer protocolServer,
        IHostApplicationLifetime lifetime)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        m_ProtocolServer = protocolServer ?? throw new ArgumentNullException(nameof(protocolServer));
        m_Lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    /// <summary>
    /// Starts the HTTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Starting HTTP server mode...");
        
        try
        {
            await m_ProtocolServer.StartAsync(cancellationToken);
            m_Logger.LogInformation("HTTP server started successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to start HTTP server");
            m_Lifetime.StopApplication();
            throw;
        }
    }

    /// <summary>
    /// Stops the HTTP server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Stopping HTTP server mode...");
        
        try
        {
            await m_ProtocolServer.StopAsync(cancellationToken);
            m_Logger.LogInformation("HTTP server stopped successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error stopping HTTP server");
        }
    }
}

