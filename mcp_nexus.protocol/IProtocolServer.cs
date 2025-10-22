namespace mcp_nexus.Protocol;

/// <summary>
/// Main interface for the MCP protocol server lifecycle management.
/// Provides methods to start, stop, and configure the protocol server.
/// </summary>
/// <remarks>
/// <para><b>Integration with Main Project:</b></para>
/// <para>
/// 1. Register services in dependency injection:
/// <code>
/// services.AddProtocolServices(configuration);
/// </code>
/// </para>
/// <para>
/// 2. Resolve and configure the protocol server:
/// <code>
/// var protocolServer = serviceProvider.GetRequiredService&lt;IProtocolServer&gt;();
/// protocolServer.SetConfiguration(myProtocolConfig);
/// </code>
/// </para>
/// <para>
/// 3. Start the server:
/// <code>
/// await protocolServer.StartAsync(cancellationToken);
/// </code>
/// </para>
/// <para>
/// 4. Stop the server gracefully:
/// <code>
/// await protocolServer.StopAsync(cancellationToken);
/// </code>
/// </para>
/// <para>
/// <b>Important:</b> Configuration must be set before starting the server.
/// Configuration cannot be changed while the server is running.
/// </para>
/// </remarks>
public interface IProtocolServer : IDisposable
{
    /// <summary>
    /// Starts the protocol server with the current configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server is already running.</exception>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the protocol server gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the server configuration.
    /// </summary>
    /// <param name="configuration">The new configuration to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to change configuration while server is running.</exception>
    void SetConfiguration(object configuration);

    /// <summary>
    /// Gets a value indicating whether the protocol server is currently running.
    /// </summary>
    bool IsRunning { get; }
}

