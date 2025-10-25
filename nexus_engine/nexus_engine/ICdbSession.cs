namespace Nexus.Engine;

/// <summary>
/// Interface for CDB session operations to enable mocking in tests.
/// </summary>
public interface ICdbSession : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the CDB session is active.
    /// </summary>
    bool IsActive
    {
        get;
    }

    /// <summary>
    /// Gets the dump file path.
    /// </summary>
    string DumpFilePath
    {
        get;
    }

    /// <summary>
    /// Gets the symbol path.
    /// </summary>
    string? SymbolPath
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether the session is initialized.
    /// </summary>
    bool IsInitialized
    {
        get;
    }

    /// <summary>
    /// Starts the CDB process asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartCdbProcessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command in the CDB session.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the command output.</returns>
    Task<string> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a batch of commands in the CDB session.
    /// </summary>
    /// <param name="commands">The commands to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the combined output.</returns>
    Task<string> ExecuteBatchCommandAsync(IEnumerable<string> commands, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the CDB process.
    /// </summary>
    void StopCdbProcess();

    /// <summary>
    /// Finds the CDB executable path.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the CDB executable path.</returns>
    Task<string> FindCdbExecutableAsync();
}
