using Microsoft.Extensions.Hosting;

namespace Nexus.Protocol;

/// <summary>
/// Main interface for the MCP protocol server lifecycle management.
/// Provides methods to start, stop, and configure the protocol server.
/// </summary>
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
    /// Sets the host instance for the protocol server.
    /// </summary>
    /// <param name="host">The host instance to set.</param>
    void SetHost(IHost host);

    /// <summary>
    /// Sets the WebApplication instance for HTTP mode operation.
    /// </summary>
    /// <param name="app">The configured web application.</param>
    /// <exception cref="ArgumentNullException">Thrown when app is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to set WebApplication while server is running.</exception>
    void SetWebApplication(Microsoft.AspNetCore.Builder.WebApplication app);

    /// <summary>
    /// Gets a value indicating whether the protocol server is currently running.
    /// </summary>
    bool IsRunning
    {
        get;
    }
}
