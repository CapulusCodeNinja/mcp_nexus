using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nexus.protocol;

namespace nexus.Hosting;

/// <summary>
/// Hosted service for standard input/output mode.
/// </summary>
internal class StdioServerHostedService : IHostedService
{
    private readonly ILogger<StdioServerHostedService> m_Logger;
    private readonly IProtocolServer m_ProtocolServer;
    private readonly IHostApplicationLifetime m_Lifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="StdioServerHostedService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="protocolServer">Protocol server instance.</param>
    /// <param name="lifetime">Application lifetime.</param>
    public StdioServerHostedService(
        ILogger<StdioServerHostedService> logger,
        IProtocolServer protocolServer,
        IHostApplicationLifetime lifetime)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        m_Logger.LogInformation("Starting stdio mode...");
        
        try
        {
            await m_ProtocolServer.StartAsync(cancellationToken);
            m_Logger.LogInformation("Stdio server started successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to start stdio server");
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
        m_Logger.LogInformation("Stopping stdio mode...");
        
        try
        {
            await m_ProtocolServer.StopAsync(cancellationToken);
            m_Logger.LogInformation("Stdio server stopped successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error stopping stdio server");
        }
    }
}

