using System.Diagnostics;

namespace WinAiDbg.External.Apis.ProcessManagement;

/// <summary>
/// Interface for process management operations to enable mocking in tests.
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Starts a new process with the specified start info.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>The started process.</returns>
    Process StartProcess(ProcessStartInfo startInfo);

    /// <summary>
    /// Gets a process by its ID.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>The process, or null if not found.</returns>
    Process? GetProcessById(int processId);

    /// <summary>
    /// Gets all processes with the specified name.
    /// </summary>
    /// <param name="processName">The process name.</param>
    /// <returns>An array of processes with the specified name.</returns>
    Process[] GetProcessesByName(string processName);

    /// <summary>
    /// Gets all running processes.
    /// </summary>
    /// <returns>An array of all running processes.</returns>
    Process[] GetProcesses();

    /// <summary>
    /// Kills a process by its ID.
    /// </summary>
    /// <param name="processId">The process ID to kill.</param>
    void KillProcess(int processId);

    /// <summary>
    /// Kills a process.
    /// </summary>
    /// <param name="process">The process to kill.</param>
    void KillProcess(Process process);

    /// <summary>
    /// Checks if a process is running.
    /// </summary>
    /// <param name="processId">The process ID to check.</param>
    /// <returns>True if the process is running, false otherwise.</returns>
    bool IsProcessRunning(int processId);

    /// <summary>
    /// Waits for a process to exit.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">The timeout in milliseconds. Use -1 for infinite timeout.</param>
    /// <returns>True if the process exited within the timeout, false otherwise.</returns>
    bool WaitForProcessExit(Process process, int timeout = -1);

    /// <summary>
    /// Waits for a process to exit asynchronously.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">The timeout in milliseconds. Use -1 for infinite timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true if the process exited within the timeout, false otherwise.</returns>
    Task<bool> WaitForProcessExitAsync(Process process, int timeout = -1, CancellationToken cancellationToken = default);
}
