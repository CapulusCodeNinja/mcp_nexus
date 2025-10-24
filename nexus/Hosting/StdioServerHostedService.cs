using Microsoft.Extensions.Hosting;

using Nexus.Protocol;

using NLog;

namespace Nexus.Hosting;

/// <summary>
/// Hosted service for standard input/output mode.
/// </summary>
internal class StdioServerHostedService : IHostedService
{
    private readonly Logger m_Logger;
    private readonly IProtocolServer m_ProtocolServer;
    private readonly IHostApplicationLifetime m_Lifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="StdioServerHostedService"/> class.
    /// </summary>
    /// <param name="protocolServer">Protocol server instance.</param>
    /// <param name="lifetime">Application lifetime.</param>
    public StdioServerHostedService(
        IProtocolServer protocolServer,
        IHostApplicationLifetime lifetime)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_ProtocolServer = protocolServer ?? throw new ArgumentNullException(nameof(protocolServer));
        m_Lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    /// <summary>
    /// Starts the stdio server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        m_Logger.Info("Starting stdio mode...");

        try
        {
            await m_ProtocolServer.StartAsync(cancellationToken);
            m_Logger.Info("Stdio server started successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to start stdio server");
            m_Lifetime.StopApplication();
            throw;
        }
    }

    /// <summary>
    /// Stops the stdio server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        m_Logger.Info("Stopping stdio mode...");

        try
        {
            await m_ProtocolServer.StopAsync(cancellationToken);
            m_Logger.Info("Stdio server stopped successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Error stopping stdio server");
        }
    }
}

