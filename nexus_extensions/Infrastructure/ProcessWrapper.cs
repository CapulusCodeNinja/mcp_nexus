using System.Diagnostics;

namespace Nexus.Extensions.Infrastructure;

/// <summary>
/// Default implementation of IProcessWrapper using real System.Diagnostics.Process.
/// </summary>
internal class ProcessWrapper : IProcessWrapper
{
    /// <summary>
    /// Creates a process configured for extension execution.
    /// </summary>
    /// <param name="fileName">Executable file name (e.g., "pwsh.exe").</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="environmentVariables">Environment variables to set.</param>
    /// <returns>A configured process ready to start.</returns>
    public IProcessHandle CreateProcess(string fileName, string arguments, Dictionary<string, string> environmentVariables)
    {
        var process = new Process();
        var startInfo = process.StartInfo;

        startInfo.FileName = fileName;
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardInput = true;
        startInfo.CreateNoWindow = true;

        // Set environment variables
        foreach (var kvp in environmentVariables)
        {
            startInfo.Environment[kvp.Key] = kvp.Value;
        }

        return new ProcessHandle(process);
    }

    /// <summary>
    /// Wrapper around System.Diagnostics.Process.
    /// </summary>
    /// <param name="process">The process to wrap.</param>
    private class ProcessHandle(Process process) : IProcessHandle
    {
        private readonly Process m_Process = process ?? throw new ArgumentNullException(nameof(process));

        /// <summary>
        /// Event raised when output data is received.
        /// </summary>
        public event DataReceivedEventHandler? OutputDataReceived
        {
            add => m_Process.OutputDataReceived += value;
            remove => m_Process.OutputDataReceived -= value;
        }

        /// <summary>
        /// Event raised when error data is received.
        /// </summary>
        public event DataReceivedEventHandler? ErrorDataReceived
        {
            add => m_Process.ErrorDataReceived += value;
            remove => m_Process.ErrorDataReceived -= value;
        }

        /// <summary>
        /// Starts the process.
        /// </summary>
        public void Start() => m_Process.Start();

        /// <summary>
        /// Begins asynchronous reading of standard output.
        /// </summary>
        public void BeginOutputReadLine() => m_Process.BeginOutputReadLine();

        /// <summary>
        /// Begins asynchronous reading of standard error.
        /// </summary>
        public void BeginErrorReadLine() => m_Process.BeginErrorReadLine();

        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task that completes when process exits.</returns>
        public Task WaitForExitAsync(CancellationToken cancellationToken) => m_Process.WaitForExitAsync(cancellationToken);

        /// <summary>
        /// Waits synchronously for the process to exit.
        /// </summary>
        public void WaitForExit() => m_Process.WaitForExit();

        /// <summary>
        /// Kills the process.
        /// </summary>
        /// <param name="entireProcessTree">Whether to kill the entire process tree.</param>
        public void Kill(bool entireProcessTree) => m_Process.Kill(entireProcessTree);

        /// <summary>
        /// Gets whether the process has exited.
        /// </summary>
        public bool HasExited => m_Process.HasExited;

        /// <summary>
        /// Gets the process ID.
        /// </summary>
        public int Id => m_Process.Id;

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        public int ExitCode => m_Process.ExitCode;

        /// <summary>
        /// Disposes the process.
        /// </summary>
        public void Dispose()
        {
            m_Process?.Dispose();
        }
    }
}

