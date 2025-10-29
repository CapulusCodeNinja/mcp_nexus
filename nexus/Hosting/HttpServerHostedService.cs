using Microsoft.Extensions.Hosting;

using Nexus.Protocol;

using NLog;

namespace Nexus.Hosting;

/// <summary>
/// Hosted service for HTTP server mode.
/// </summary>
internal class HttpServerHostedService : IHostedService
{
    private readonly Logger m_Logger;
    private readonly IProtocolServer m_ProtocolServer;
    private readonly IHostApplicationLifetime m_Lifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServerHostedService"/> class.
    /// </summary>
    /// <param name="protocolServer">Protocol server instance.</param>
    /// <param name="lifetime">Application lifetime.</param>
    public HttpServerHostedService(
        IProtocolServer protocolServer,
        IHostApplicationLifetime lifetime)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
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
        m_Logger.Info("Starting HTTP server mode...");

        try
        {
            await m_ProtocolServer.StartAsync(cancellationToken);
            m_Logger.Info("HTTP server started successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to start HTTP server");
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
        m_Logger.Info("Stopping HTTP server mode...");

        try
        {
            await m_ProtocolServer.StopAsync(cancellationToken);
            m_Logger.Info("HTTP server stopped successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error stopping HTTP server");
        }
    }
}
