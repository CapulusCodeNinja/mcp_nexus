using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nexus.protocol;

namespace nexus.Hosting;

/// <summary>
/// Hosted service for Windows Service mode.
/// </summary>
internal class ServiceHostedService : IHostedService
{
    private readonly ILogger<ServiceHostedService> m_Logger;
    private readonly IProtocolServer m_ProtocolServer;
    private readonly IHostApplicationLifetime m_Lifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceHostedService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="protocolServer">Protocol server instance.</param>
    /// <param name="lifetime">Application lifetime.</param>
    public ServiceHostedService(
        ILogger<ServiceHostedService> logger,
        IProtocolServer protocolServer,
        IHostApplicationLifetime lifetime)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        m_ProtocolServer = protocolServer ?? throw new ArgumentNullException(nameof(protocolServer));
        m_Lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    /// <summary>
    /// Starts the Windows Service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Starting Windows Service mode...");
        
        try
        {
            await m_ProtocolServer.StartAsync(cancellationToken);
            m_Logger.LogInformation("Windows Service started successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to start Windows Service");
            m_Lifetime.StopApplication();
            throw;
        }
    }

    /// <summary>
    /// Stops the Windows Service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        m_Logger.LogInformation("Stopping Windows Service mode...");
        
        try
        {
            await m_ProtocolServer.StopAsync(cancellationToken);
            m_Logger.LogInformation("Windows Service stopped successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Error stopping Windows Service");
        }
    }
}

