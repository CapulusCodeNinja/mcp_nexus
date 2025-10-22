using System.Diagnostics;

namespace nexus.extensions.Infrastructure;

/// <summary>
/// Interface for wrapping Process to enable testing.
/// </summary>
internal interface IProcessWrapper
{
    /// <summary>
    /// Creates and configures a new process for extension execution.
    /// </summary>
    /// <param name="fileName">Executable file name (e.g., "pwsh.exe").</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="environmentVariables">Environment variables to set.</param>
    /// <returns>A configured process ready to start.</returns>
    IProcessHandle CreateProcess(string fileName, string arguments, Dictionary<string, string> environmentVariables);
}

/// <summary>
/// Interface representing a process handle for testing.
/// </summary>
internal interface IProcessHandle : IDisposable
{
    /// <summary>
    /// Event raised when output data is received.
    /// </summary>
    event DataReceivedEventHandler? OutputDataReceived;

    /// <summary>
    /// Event raised when error data is received.
    /// </summary>
    event DataReceivedEventHandler? ErrorDataReceived;

    /// <summary>
    /// Starts the process.
    /// </summary>
    void Start();

    /// <summary>
    /// Begins asynchronous reading of standard output.
    /// </summary>
    void BeginOutputReadLine();

    /// <summary>
    /// Begins asynchronous reading of standard error.
    /// </summary>
    void BeginErrorReadLine();

    /// <summary>
    /// Waits asynchronously for the process to exit.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when process exits.</returns>
    Task WaitForExitAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Waits synchronously for the process to exit.
    /// </summary>
    void WaitForExit();

    /// <summary>
    /// Kills the process.
    /// </summary>
    /// <param name="entireProcessTree">Whether to kill the entire process tree.</param>
    void Kill(bool entireProcessTree);

    /// <summary>
    /// Gets whether the process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Gets the process ID.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    int ExitCode { get; }
}

