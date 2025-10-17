using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using mcp_nexus.Configuration;
using NLog;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Manages the CDB debugger process lifecycle including starting, stopping, and monitoring.
    /// Provides thread-safe access to process streams and handles process cleanup.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CdbProcessManager"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance for recording process operations and errors.</param>
    /// <param name="config">The CDB session configuration containing process settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="config"/> is null.</exception>
    public class CdbProcessManager(ILogger<CdbProcessManager> logger, CdbSessionConfiguration config) : IDisposable
    {
        private readonly ILogger<CdbProcessManager> m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly CdbSessionConfiguration m_Config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly object m_LifecycleLock = new();
        private string? m_SessionId;

        private Process? m_DebuggerProcess;
        private StreamWriter? m_DebuggerInput;
        private volatile bool m_IsActive;  // CRITICAL: volatile ensures visibility across threads
        private volatile bool m_Disposed;  // CRITICAL: volatile ensures visibility across threads
        private volatile bool m_InitOutputConsumed;  // CRITICAL: ensures init output is consumed before commands execute
        private volatile bool m_isStopping;  // Track when we're intentionally stopping the process

        /// <summary>
        /// Gets the CDB debugger process instance.
        /// </summary>
        public virtual Process? DebuggerProcess => m_DebuggerProcess;

        /// <summary>
        /// Gets the input stream writer for sending commands to the CDB process.
        /// </summary>
        public virtual StreamWriter? DebuggerInput => m_DebuggerInput;

        /// <summary>
        /// Gets a value indicating whether the CDB process is active and ready for commands.
        /// </summary>
        public virtual bool IsActive => m_IsActive && !m_Disposed && m_InitOutputConsumed;

        /// <summary>
        /// Sets the session ID for this process manager instance.
        /// Used for creating session-specific log files.
        /// </summary>
        /// <param name="sessionId">The unique session identifier.</param>
        public virtual void SetSessionId(string sessionId)
        {
            m_SessionId = sessionId;
        }

        /// <summary>
        /// Starts the CDB debugger process with the specified target and optional explicit CDB path override.
        /// This method is thread-safe and will stop any existing process before starting a new one.
        /// </summary>
        /// <param name="target">The target to debug (dump file path or process ID).</param>
        /// <param name="cdbPathOverride">Optional override for the CDB executable path. If null, uses the configured path.</param>
        /// <returns>
        /// <c>true</c> if the process was started successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public bool StartProcess(string target, string? cdbPathOverride)
        {
            ThrowIfDisposed();

            try
            {
                // Check if we need to stop existing process OUTSIDE the lock
                bool needsStop = false;
                lock (m_LifecycleLock)
                {
                    needsStop = m_IsActive;
                }

                // Stop process WITHOUT holding lock to avoid blocking other threads for 5-7 seconds
                if (needsStop)
                {
                    m_Logger.LogWarning("Process is already active - stopping current process before starting new one");
                    StopProcess(); // This calls StopProcessInternal with its own lock
                }

                lock (m_LifecycleLock)
                {
                    m_Logger.LogDebug("Acquired lifecycle lock for StartProcess");

                    var cdbPath = !string.IsNullOrWhiteSpace(cdbPathOverride) ? cdbPathOverride : FindCdbExecutable();
                    if (!File.Exists(cdbPath))
                    {
                        m_Logger.LogError("❌ Configured CDB path does not exist: {CdbPath}", cdbPath);
                        return false;
                    }
                    var processInfo = CreateProcessStartInfo(cdbPath, target);

                    return StartProcessInternal(processInfo, target);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to start CDB process with target: {Target}", target);
                return false;
            }
        }

        /// <summary>
        /// Starts the CDB debugger process with the specified target using the configured CDB path.
        /// This is a backward-compatible overload that calls <see cref="StartProcess(string, string?)"/> with a null CDB path override.
        /// </summary>
        /// <param name="target">The target to debug (dump file path or process ID).</param>
        /// <returns>
        /// <c>true</c> if the process was started successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public bool StartProcess(string target)
        {
            return StartProcess(target, null);
        }

        /// <summary>
        /// Stops the CDB debugger process gracefully.
        /// This method sends a quit command to the process and waits for it to exit.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the process was stopped successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        public bool StopProcess()
        {
            ThrowIfDisposed();

            try
            {
                lock (m_LifecycleLock)
                {
                    return StopProcessInternal();
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error stopping CDB process");
                return false;
            }
        }

        /// <summary>
        /// Logs diagnostic information about the current CDB process.
        /// This method provides detailed information about the process state for debugging purposes.
        /// </summary>
        /// <param name="context">The context in which the diagnostics are being logged (e.g., "Startup", "Error").</param>
        public void LogProcessDiagnostics(string context)
        {
            try
            {
                var process = m_DebuggerProcess;
                if (process == null)
                {
                    m_Logger.LogDebug("[{Context}] No debugger process", context);
                    return;
                }

                m_Logger.LogDebug("[{Context}] Process diagnostics:", context);
                m_Logger.LogDebug("  - Process ID: {ProcessId}", process.Id);
                m_Logger.LogDebug("  - Process Name: {ProcessName}", process.ProcessName);
                m_Logger.LogDebug("  - Has Exited: {HasExited}", process.HasExited);

                if (!process.HasExited)
                {
                    m_Logger.LogDebug("  - Start Time: {StartTime}", process.StartTime);
                    m_Logger.LogDebug("  - CPU Time: {TotalProcessorTime}", process.TotalProcessorTime);
                    m_Logger.LogDebug("  - Memory Usage: {WorkingSet64} bytes", process.WorkingSet64);

                    var commandLine = GetProcessCommandLine(process.Id);
                    m_Logger.LogDebug("  - Command Line: {CommandLine}", commandLine ?? "[Unable to retrieve]");
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error logging process diagnostics for context: {Context}", context);
            }
        }

        /// <summary>
        /// Finds the CDB executable path using the configured settings.
        /// </summary>
        /// <returns>The path to the CDB executable.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the CDB executable cannot be found or accessed.</exception>
        /// <remarks>
        /// This method uses the CDB path that was resolved during service startup.
        /// It performs validation to ensure the executable actually exists and is accessible.
        /// </remarks>
        private string FindCdbExecutable()
        {
            m_Logger.LogDebug("Searching for CDB executable...");

            try
            {
                // Use the resolved CDB path from configuration (already resolved by DI)
                var cdbPath = m_Config.CustomCdbPath;
                if (string.IsNullOrEmpty(cdbPath))
                {
                    var errorMessage = "❌ CDB path not configured. This should have been resolved during service startup.";
                    m_Logger.LogError(errorMessage);
                    m_Logger.LogError("💡 This indicates a configuration or DI issue - CDB path should be auto-detected during service registration.");
                    throw new FileNotFoundException(errorMessage);
                }

                // Verify the configured path actually exists
                if (!File.Exists(cdbPath))
                {
                    var errorMessage = $"❌ Configured CDB path does not exist: {cdbPath}";
                    m_Logger.LogError(errorMessage);
                    m_Logger.LogError("💡 The CDB path was resolved during startup but the file is no longer accessible.");
                    throw new FileNotFoundException(errorMessage);
                }

                m_Logger.LogInformation("✅ Using CDB at: {CdbPath}", cdbPath);
                return cdbPath;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "❌ Failed to find CDB executable - this will prevent debugging sessions from starting");
                throw;
            }
        }

        /// <summary>
        /// Creates a ProcessStartInfo object for launching CDB with the specified target and configuration.
        /// </summary>
        /// <param name="cdbPath">The path to the CDB executable.</param>
        /// <param name="target">The target for CDB (dump file path or command line arguments).</param>
        /// <returns>A configured ProcessStartInfo object ready for process execution.</returns>
        /// <remarks>
        /// This method handles both dump file targets (prefixed with -z) and direct command line arguments.
        /// It automatically includes symbol search paths from configuration and sets up proper UTF-8 encoding.
        /// The working directory is set to the CDB executable directory to avoid path resolution issues.
        /// </remarks>
        private ProcessStartInfo CreateProcessStartInfo(string cdbPath, string target)
        {
            if (!target.StartsWith("-"))
            {
                throw new ArgumentException("Target must start with -");
            }

            // Run from CDB directory to avoid path issues
            var workingDirectory = Path.GetDirectoryName(cdbPath) ?? Environment.CurrentDirectory;

            m_Logger.LogDebug("CDB call: {CdbPath} {Arguments}", cdbPath, target);

            // Use centralized UTF-8 encoding configuration for all CDB streams
            var startInfo = EncodingConfiguration.CreateUnicodeProcessStartInfo(cdbPath, target);
            startInfo.WorkingDirectory = workingDirectory;

            return startInfo;
        }

        /// <summary>
        /// Internal method to start the CDB process with the specified configuration.
        /// </summary>
        /// <param name="processInfo">The process start information for CDB.</param>
        /// <param name="target">The target description for logging purposes.</param>
        /// <returns><c>true</c> if the process started successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method handles the actual process creation and stream setup.
        /// It configures large buffer sizes for output streams to improve performance.
        /// </remarks>
        private bool StartProcessInternal(ProcessStartInfo processInfo, string target)
        {
            m_Logger.LogDebug("Starting CDB process...");
            m_isStopping = false; // Reset stopping flag when starting new process

            m_DebuggerProcess = Process.Start(processInfo);
            if (m_DebuggerProcess == null)
            {
                m_Logger.LogError("Failed to start CDB process");
                return false;
            }

            // Set up streams
            m_DebuggerInput = m_DebuggerProcess.StandardInput;

            // Configure streams
            m_DebuggerInput.AutoFlush = true;

            // Monitor process exit to detect crashes
            m_DebuggerProcess.EnableRaisingEvents = true;
            m_DebuggerProcess.Exited += (sender, e) =>
            {
                if (m_IsActive && !m_isStopping)
                {
                    m_Logger.LogError("🔥 CDB process exited unexpectedly! Exit code: {ExitCode}, Was active: {WasActive}",
                        m_DebuggerProcess?.ExitCode ?? -1, m_IsActive);
                    CleanupResources();
                }
                else if (m_isStopping)
                {
                    m_Logger.LogInformation("✅ CDB process stopped as expected during session closure. Exit code: {ExitCode}",
                        m_DebuggerProcess?.ExitCode ?? -1);
                }
            };

            m_IsActive = true;
            m_Logger.LogInformation("Successfully started CDB process with target: {Target}", target);
            m_Logger.LogInformation("Process ID: {ProcessId}, Active: {IsActive}", m_DebuggerProcess.Id, m_IsActive);

            // CRITICAL DECISION: DO NOT consume init output!
            // StreamReader buffering makes it impossible to reliably consume partial output
            // Instead, the FIRST command will see the init output + its own output
            // The command executor's completion detection will handle this correctly

            m_Logger.LogDebug("⚡ CDB process started - skipping init consumer (first command will see init output)");
            m_InitOutputConsumed = true; // Mark as ready immediately

            return true;
        }

        /// <summary>
        /// Internal method to stop the CDB process and clean up resources.
        /// </summary>
        /// <returns><c>true</c> if the process was stopped successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method handles graceful shutdown of the CDB process and cleanup of associated resources.
        /// It attempts to close the process gracefully before forcing termination if necessary.
        /// </remarks>
        private bool StopProcessInternal()
        {
            if (!m_IsActive || m_DebuggerProcess == null)
            {
                m_Logger.LogDebug("No active process to stop");
                return false; // Return false when nothing to stop for test compatibility
            }

            m_Logger.LogDebug("Stopping CDB process...");
            m_isStopping = true; // Mark that we're intentionally stopping the process

            try
            {
                // Send quit command if input stream is available
                if (m_DebuggerInput != null && !m_DebuggerInput.BaseStream.CanWrite == false)
                {
                    try
                    {
                        m_DebuggerInput.WriteLine("q");
                        m_DebuggerInput.Flush();
                        m_Logger.LogDebug("Sent quit command to CDB");
                    }
                    catch (Exception ex)
                    {
                        m_Logger.LogWarning(ex, "Failed to send quit command to CDB");
                    }
                }

                // Wait for graceful exit
                if (!m_DebuggerProcess.WaitForExit(5000))
                {
                    m_Logger.LogWarning("CDB process did not exit gracefully, forcing termination");
                    m_DebuggerProcess.Kill();
                    m_DebuggerProcess.WaitForExit(2000);
                }

                m_Logger.LogInformation("CDB process stopped successfully");
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error stopping CDB process");
                return false;
            }
            finally
            {
                CleanupResources();
            }
        }

        /// <summary>
        /// Cleans up all resources associated with the CDB process.
        /// </summary>
        /// <remarks>
        /// This method disposes of all streams and the process object.
        /// It should be called when the process is no longer needed or during disposal.
        /// </remarks>
        private void CleanupResources()
        {
            var wasActive = m_IsActive;
            m_IsActive = false;

            if (wasActive)
            {
                m_Logger.LogWarning("⚠️ CDB session became inactive - CleanupResources called while session was active");
            }

            try
            {
                m_DebuggerInput?.Dispose();
                m_DebuggerProcess?.Dispose();
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error disposing process resources");
            }
            finally
            {
                m_DebuggerInput = null;
                m_DebuggerProcess = null;
            }
        }

        /// <summary>
        /// Gets the command line arguments for a running process by its process ID.
        /// </summary>
        /// <param name="processId">The process ID to get command line arguments for.</param>
        /// <returns>
        /// The command line arguments of the process if successful; otherwise, <c>null</c>.
        /// Returns <c>null</c> on non-Windows platforms or if the process cannot be accessed.
        /// </returns>
        /// <remarks>
        /// This method is Windows-specific and will return <c>null</c> on other platforms.
        /// The method handles exceptions gracefully and returns <c>null</c> if the process
        /// cannot be found or accessed.
        /// </remarks>
        private static string? GetProcessCommandLine(int processId)
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return null;

                using var process = Process.GetProcessById(processId);
                return process.StartInfo.Arguments;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Throws an ObjectDisposedException if this instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CdbProcessManager));
        }

        /// <summary>
        /// Disposes of resources used by the process manager.
        /// This method stops the CDB process and cleans up all associated resources.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            try
            {
                StopProcess();
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error during disposal");
            }
            finally
            {
                m_Disposed = true;
            }
        }
    }
}
