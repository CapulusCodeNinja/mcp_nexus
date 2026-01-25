using System.Diagnostics;
using System.Text;

using NLog;

using WinAiDbg.Config;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.External.Apis.ProcessManagement;

namespace WinAiDbg.Engine.DumpCheck.Internal;

/// <summary>
/// Executes the dumpchk process for a given dump file and aggregates its
/// standard output and error streams into a single result string.
/// </summary>
internal sealed class DumpChkProcessRunner
{
    /// <summary>
    /// Logger for dumpchk execution operations.
    /// </summary>
    private readonly Logger m_Logger;

    /// <summary>
    /// Shared application settings.
    /// </summary>
    private readonly ISettings m_Settings;

    /// <summary>
    /// Process manager abstraction for starting and monitoring dumpchk.
    /// </summary>
    private readonly IProcessManager m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DumpChkProcessRunner"/> class.
    /// </summary>
    /// <param name="settings">The shared application settings.</param>
    /// <param name="processManager">The process manager abstraction.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="settings"/> or <paramref name="processManager"/> is <c>null</c>.
    /// </exception>
    public DumpChkProcessRunner(ISettings settings, IProcessManager processManager)
    {
        m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        m_ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Runs dumpchk for the specified dump file path and returns the combined output.
    /// If dumpchk times out, a graceful result is returned with <see cref="DumpCheckResult.TimedOut"/> set to true,
    /// allowing session creation to continue safely.
    /// </summary>
    /// <param name="dumpChkPath">The resolved path to the dumpchk executable.</param>
    /// <param name="dumpFilePath">The full path to the dump file to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the dumpchk result.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dumpChkPath"/> or <paramref name="dumpFilePath"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started.</exception>
    public async Task<DumpCheckResult> RunAsync(string dumpChkPath, string dumpFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dumpChkPath))
        {
            throw new ArgumentException("Dumpchk path cannot be null or empty", nameof(dumpChkPath));
        }

        if (string.IsNullOrWhiteSpace(dumpFilePath))
        {
            throw new ArgumentException("Dump file path cannot be null or empty", nameof(dumpFilePath));
        }

        var symbolPath = m_Settings.Get().WinAiDbg.Debugging.SymbolSearchPath;
        if (!string.IsNullOrWhiteSpace(symbolPath))
        {
            m_Logger.Info("Using configured symbol path for dumpchk: {SymbolPath}", symbolPath);
        }

        var startInfo = CreateStartInfo(dumpChkPath, dumpFilePath, symbolPath);
        using var process = m_ProcessManager.StartProcess(startInfo) ?? throw new InvalidOperationException("Failed to start dumpchk process");

        m_Logger.Info("Starting dumpchk for dump file: {DumpFilePath}", dumpFilePath);

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var registration = cancellationToken.Register(
            () =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        m_Logger.Warn("Cancellation requested, terminating dumpchk process with PID: {ProcessId}", process.Id);
                        m_ProcessManager.KillProcess(process);
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.Warn(ex, "Error while cancelling dumpchk process");
                }
            });

        // Use dumpchk-specific timeout (default 60 seconds) instead of general output reading timeout
        var timeoutMs = m_Settings.Get().WinAiDbg.Validation.DumpChkTimeoutMs;
        var exited = await m_ProcessManager.WaitForProcessExitAsync(process, timeoutMs, cancellationToken).ConfigureAwait(false);

        if (!exited)
        {
            try
            {
                m_Logger.Warn("dumpchk did not exit within timeout ({TimeoutMs} ms), killing process with PID: {ProcessId}", timeoutMs, process.Id);
                m_ProcessManager.KillProcess(process);

                // Wait briefly for the read tasks to complete after process termination
                // This ensures proper cleanup of stream handles
                const int cleanupTimeoutMs = 1000;
                _ = await Task.WhenAll(stdoutTask, stderrTask)
                    .ContinueWith(_ => true, TaskContinuationOptions.OnlyOnRanToCompletion)
                    .WaitAsync(TimeSpan.FromMilliseconds(cleanupTimeoutMs))
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                m_Logger.Debug("Cleanup timeout expired waiting for read tasks to complete");
            }
            catch (Exception ex)
            {
                m_Logger.Warn(ex, "Error while killing dumpchk process after timeout");
            }

            // Return a graceful result instead of throwing - session creation can continue safely
            var timeoutMessage = $"dumpchk validation timed out after {timeoutMs / 1000} seconds. " +
                "This is typically caused by slow symbol server responses or similar issues. " +
                "The dump file appears accessible and session creation will continue. " +
                "It is safe to proceed with analysis.";

            m_Logger.Warn("dumpchk timed out for {DumpFilePath} - continuing with session creation. {Message}", dumpFilePath, timeoutMessage);

            return new DumpCheckResult
            {
                IsEnabled = true,
                WasExecuted = true,
                ExitCode = -1,
                Message = timeoutMessage,
                TimedOut = true,
            };
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        m_Logger.Info("dumpchk finished for dump file: {DumpFilePath} with exit code {ExitCode}", dumpFilePath, process.ExitCode);

        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            _ = builder.AppendLine(stdout.TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            if (builder.Length > 0)
            {
                _ = builder.AppendLine();
            }

            _ = builder.AppendLine(stderr.TrimEnd());
        }

        return new DumpCheckResult
        {
            IsEnabled = true,
            Message = builder.ToString().TrimEnd(),
            WasExecuted = true,
            ExitCode = process.ExitCode,
        };
    }

    /// <summary>
    /// Creates the process start information for invoking dumpchk.
    /// </summary>
    /// <param name="dumpChkPath">The path to the dumpchk executable.</param>
    /// <param name="dumpFilePath">The dump file to analyze.</param>
    /// <param name="symbolPath">The optional symbol path to pass to dumpchk via -y.</param>
    /// <returns>A configured <see cref="ProcessStartInfo"/> instance.</returns>
    private static ProcessStartInfo CreateStartInfo(string dumpChkPath, string dumpFilePath, string? symbolPath)
    {
        var argumentsBuilder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(symbolPath))
        {
            _ = argumentsBuilder.Append("-y ");
            _ = argumentsBuilder.Append('"');
            _ = argumentsBuilder.Append(symbolPath);
            _ = argumentsBuilder.Append("\" ");
        }

        _ = argumentsBuilder.Append('"');
        _ = argumentsBuilder.Append(dumpFilePath);
        _ = argumentsBuilder.Append('"');

        return new ProcessStartInfo
        {
            FileName = dumpChkPath,
            Arguments = argumentsBuilder.ToString(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
    }
}


