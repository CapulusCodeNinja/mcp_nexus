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
    /// <param name="isServiceMode">Run the protocol server in service mode.</param>
    /// <param name="isHttpMode">Run the protocol server in http mode.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the server is already running.</exception>
    Task StartAsync(bool isServiceMode, bool isHttpMode, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the protocol server gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a value indicating whether the protocol server is currently running.
    /// </summary>
    bool IsRunning
    {
        get;
    }
}
