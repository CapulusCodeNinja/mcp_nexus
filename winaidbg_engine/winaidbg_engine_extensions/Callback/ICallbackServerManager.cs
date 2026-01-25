namespace WinAiDbg.Engine.Extensions.Callback;

/// <summary>
/// Interface for managing the extension callback HTTP server lifecycle.
/// </summary>
internal interface ICallbackServerManager : IAsyncDisposable
{
    /// <summary>
    /// Gets the port number the callback server is listening on.
    /// </summary>
    int Port
    {
        get;
    }

    /// <summary>
    /// Gets the callback URL for extensions to use.
    /// </summary>
    string CallbackUrl
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether the server is currently running.
    /// </summary>
    bool IsRunning
    {
        get;
    }

    /// <summary>
    /// Starts the callback server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the callback server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
