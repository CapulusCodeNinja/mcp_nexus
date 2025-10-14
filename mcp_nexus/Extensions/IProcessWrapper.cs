using System.Diagnostics;

namespace mcp_nexus.Extensions
{
    /// <summary>
    /// Interface for wrapping Process to enable testing.
    /// </summary>
    public interface IProcessWrapper
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
    public interface IProcessHandle : IDisposable
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

    /// <summary>
    /// Default implementation of IProcessWrapper using real System.Diagnostics.Process.
    /// </summary>
    public class ProcessWrapper : IProcessWrapper
    {
        /// <summary>
        /// Creates a process configured for extension execution.
        /// </summary>
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
        private class ProcessHandle(Process process) : IProcessHandle
        {
            private readonly Process m_Process = process ?? throw new ArgumentNullException(nameof(process));

            public event DataReceivedEventHandler? OutputDataReceived
            {
                add => m_Process.OutputDataReceived += value;
                remove => m_Process.OutputDataReceived -= value;
            }

            public event DataReceivedEventHandler? ErrorDataReceived
            {
                add => m_Process.ErrorDataReceived += value;
                remove => m_Process.ErrorDataReceived -= value;
            }

            public void Start() => m_Process.Start();
            public void BeginOutputReadLine() => m_Process.BeginOutputReadLine();
            public void BeginErrorReadLine() => m_Process.BeginErrorReadLine();
            public Task WaitForExitAsync(CancellationToken cancellationToken) => m_Process.WaitForExitAsync(cancellationToken);
            public void WaitForExit() => m_Process.WaitForExit();
            public void Kill(bool entireProcessTree) => m_Process.Kill(entireProcessTree);
            public bool HasExited => m_Process.HasExited;
            public int Id => m_Process.Id;
            public int ExitCode => m_Process.ExitCode;

            public void Dispose()
            {
                m_Process?.Dispose();
            }
        }
    }
}

