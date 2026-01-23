using System.Diagnostics;

using Nexus.External.Apis.Native;

namespace Nexus.External.Apis.ProcessManagement;

/// <summary>
/// Concrete implementation of IProcessManager that uses the real process management.
/// </summary>
public class ProcessManager : IProcessManager
{
    /// <summary>
    /// Starts a new process with the specified start info.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <returns>The started process.</returns>
    public Process StartProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start process");
        ProcessTracker.AddProcess(process);
        return process;
    }

    /// <summary>
    /// Gets a process by its ID.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>The process, or null if not found.</returns>
    public Process? GetProcessById(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all processes with the specified name.
    /// </summary>
    /// <param name="processName">The process name.</param>
    /// <returns>An array of processes with the specified name.</returns>
    public Process[] GetProcessesByName(string processName)
    {
        return Process.GetProcessesByName(processName);
    }

    /// <summary>
    /// Gets all running processes.
    /// </summary>
    /// <returns>An array of all running processes.</returns>
    public Process[] GetProcesses()
    {
        return Process.GetProcesses();
    }

    /// <summary>
    /// Kills a process by its ID.
    /// </summary>
    /// <param name="processId">The process ID to kill.</param>
    public void KillProcess(int processId)
    {
        var process = GetProcessById(processId);
        if (process != null)
        {
            KillProcess(process);
        }
    }

    /// <summary>
    /// Kills a process and its entire process tree.
    /// </summary>
    /// <param name="process">The process to kill.</param>
    public void KillProcess(Process process)
    {
        if (!process.HasExited)
        {
            // Kill entire process tree to ensure child processes (like conhost.exe) are also terminated
            process.Kill(entireProcessTree: true);
        }
    }

    /// <summary>
    /// Checks if a process is running.
    /// </summary>
    /// <param name="processId">The process ID to check.</param>
    /// <returns>True if the process is running, false otherwise.</returns>
    public bool IsProcessRunning(int processId)
    {
        var process = GetProcessById(processId);
        return process != null && !process.HasExited;
    }

    /// <summary>
    /// Waits for a process to exit.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">The timeout in milliseconds. Use -1 for infinite timeout.</param>
    /// <returns>True if the process exited within the timeout, false otherwise.</returns>
    public bool WaitForProcessExit(Process process, int timeout = -1)
    {
        return process.WaitForExit(timeout);
    }

    /// <summary>
    /// Waits for a process to exit asynchronously.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">The timeout in milliseconds. Use -1 for infinite timeout.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is true if the process
    /// exited within the timeout window and false if the operation was cancelled or timed out.
    /// </returns>
    public async Task<bool> WaitForProcessExitAsync(Process process, int timeout = -1, CancellationToken cancellationToken = default)
    {
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (timeout > 0)
        {
            linkedTokenSource.CancelAfter(timeout);
        }

        try
        {
            await process.WaitForExitAsync(linkedTokenSource.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            // Treat both timeout-based and external cancellation uniformly as "did not exit in time".
            return false;
        }
    }
}
