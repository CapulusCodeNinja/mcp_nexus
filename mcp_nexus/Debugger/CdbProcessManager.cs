using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using mcp_nexus.Configuration;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Manages the CDB debugger process lifecycle including starting, stopping, and monitoring.
    /// Provides thread-safe access to process streams and handles process cleanup.
    /// </summary>
    public class CdbProcessManager : IDisposable
    {
        private readonly ILogger<CdbProcessManager> m_logger;
        private readonly CdbSessionConfiguration m_config;
        private readonly object m_lifecycleLock = new();

        private Process? m_debuggerProcess;
        private StreamWriter? m_debuggerInput;
        private StreamReader? m_debuggerOutput;
        private StreamReader? m_debuggerError;
        private volatile bool m_isActive;  // CRITICAL: volatile ensures visibility across threads
        private volatile bool m_disposed;  // CRITICAL: volatile ensures visibility across threads
        private volatile bool m_initOutputConsumed;  // CRITICAL: ensures init output is consumed before commands execute
        private volatile bool m_isStopping;  // Track when we're intentionally stopping the process

        /// <summary>
        /// Gets the CDB debugger process instance.
        /// </summary>
        public virtual Process? DebuggerProcess => m_debuggerProcess;

        /// <summary>
        /// Gets the input stream writer for sending commands to the CDB process.
        /// </summary>
        public virtual StreamWriter? DebuggerInput => m_debuggerInput;

        /// <summary>
        /// Gets the output stream reader for receiving command responses from the CDB process.
        /// </summary>
        public virtual StreamReader? DebuggerOutput => m_debuggerOutput;

        /// <summary>
        /// Gets the error stream reader for receiving error messages from the CDB process.
        /// </summary>
        public virtual StreamReader? DebuggerError => m_debuggerError;

        /// <summary>
        /// Gets a value indicating whether the CDB process is active and ready for commands.
        /// </summary>
        public virtual bool IsActive => m_isActive && !m_disposed && m_initOutputConsumed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbProcessManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording process operations and errors.</param>
        /// <param name="config">The CDB session configuration containing process settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> or <paramref name="config"/> is null.</exception>
        public CdbProcessManager(ILogger<CdbProcessManager> logger, CdbSessionConfiguration config)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
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
                lock (m_lifecycleLock)
                {
                    needsStop = m_isActive;
                }

                // Stop process WITHOUT holding lock to avoid blocking other threads for 5-7 seconds
                if (needsStop)
                {
                    m_logger.LogWarning("Process is already active - stopping current process before starting new one");
                    StopProcess(); // This calls StopProcessInternal with its own lock
                }

                lock (m_lifecycleLock)
                {
                    m_logger.LogDebug("Acquired lifecycle lock for StartProcess");

                    var cdbPath = !string.IsNullOrWhiteSpace(cdbPathOverride) ? cdbPathOverride : FindCdbExecutable();
                    if (!File.Exists(cdbPath))
                    {
                        m_logger.LogError("❌ Configured CDB path does not exist: {CdbPath}", cdbPath);
                        return false;
                    }
                    var processInfo = CreateProcessStartInfo(cdbPath, target);

                    return StartProcessInternal(processInfo, target);
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to start CDB process with target: {Target}", target);
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
                lock (m_lifecycleLock)
                {
                    return StopProcessInternal();
                }
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error stopping CDB process");
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
                var process = m_debuggerProcess;
                if (process == null)
                {
                    m_logger.LogDebug("[{Context}] No debugger process", context);
                    return;
                }

                m_logger.LogDebug("[{Context}] Process diagnostics:", context);
                m_logger.LogDebug("  - Process ID: {ProcessId}", process.Id);
                m_logger.LogDebug("  - Process Name: {ProcessName}", process.ProcessName);
                m_logger.LogDebug("  - Has Exited: {HasExited}", process.HasExited);

                if (!process.HasExited)
                {
                    m_logger.LogDebug("  - Start Time: {StartTime}", process.StartTime);
                    m_logger.LogDebug("  - CPU Time: {TotalProcessorTime}", process.TotalProcessorTime);
                    m_logger.LogDebug("  - Memory Usage: {WorkingSet64} bytes", process.WorkingSet64);

                    var commandLine = GetProcessCommandLine(process.Id);
                    m_logger.LogDebug("  - Command Line: {CommandLine}", commandLine ?? "[Unable to retrieve]");
                }
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error logging process diagnostics for context: {Context}", context);
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
            m_logger.LogDebug("Searching for CDB executable...");

            try
            {
                // Use the resolved CDB path from configuration (already resolved by DI)
                var cdbPath = m_config.CustomCdbPath;
                if (string.IsNullOrEmpty(cdbPath))
                {
                    var errorMessage = "❌ CDB path not configured. This should have been resolved during service startup.";
                    m_logger.LogError(errorMessage);
                    m_logger.LogError("💡 This indicates a configuration or DI issue - CDB path should be auto-detected during service registration.");
                    throw new FileNotFoundException(errorMessage);
                }

                // Verify the configured path actually exists
                if (!File.Exists(cdbPath))
                {
                    var errorMessage = $"❌ Configured CDB path does not exist: {cdbPath}";
                    m_logger.LogError(errorMessage);
                    m_logger.LogError("💡 The CDB path was resolved during startup but the file is no longer accessible.");
                    throw new FileNotFoundException(errorMessage);
                }

                m_logger.LogInformation("✅ Using CDB at: {CdbPath}", cdbPath);
                return cdbPath;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "❌ Failed to find CDB executable - this will prevent debugging sessions from starting");
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
            // If target already contains flags, use as-is; otherwise treat as dump path
            var arguments = target.StartsWith("-") ? target : $"-z \"{target}\"";

            if (!string.IsNullOrWhiteSpace(m_config.SymbolSearchPath))
            {
                arguments += $" -y \"{m_config.SymbolSearchPath}\"";
                m_logger.LogDebug("Using symbol search path: {SymbolSearchPath}", m_config.SymbolSearchPath);
            }

            // Run from CDB directory to avoid path issues
            var workingDirectory = Path.GetDirectoryName(cdbPath) ?? Environment.CurrentDirectory;

            m_logger.LogDebug("CDB call: {CdbPath} {Arguments}", cdbPath, arguments);

            // Use centralized UTF-8 encoding configuration for all CDB streams
            var startInfo = EncodingConfiguration.CreateUnicodeProcessStartInfo(cdbPath, arguments);
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
            m_logger.LogDebug("Starting CDB process...");
            m_isStopping = false; // Reset stopping flag when starting new process

            m_debuggerProcess = Process.Start(processInfo);
            if (m_debuggerProcess == null)
            {
                m_logger.LogError("Failed to start CDB process");
                return false;
            }

            // Set up streams
            m_debuggerInput = m_debuggerProcess.StandardInput;

            // Increase read buffer sizes for stdout/stderr to reduce syscalls and allocations
            // Wrap the underlying base streams with larger-buffer StreamReaders (leaveOpen=true)
            var stdoutEncoding = m_debuggerProcess.StandardOutput.CurrentEncoding;
            var stderrEncoding = m_debuggerProcess.StandardError.CurrentEncoding;
            const int largeBufferSize = 64 * 1024; // 64KB

            m_debuggerOutput = new StreamReader(
                m_debuggerProcess.StandardOutput.BaseStream,
                stdoutEncoding,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: largeBufferSize,
                leaveOpen: true);

            m_debuggerError = new StreamReader(
                m_debuggerProcess.StandardError.BaseStream,
                stderrEncoding,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: largeBufferSize,
                leaveOpen: true);

            // Configure streams
            m_debuggerInput.AutoFlush = true;

            // Monitor process exit to detect crashes
            m_debuggerProcess.EnableRaisingEvents = true;
            m_debuggerProcess.Exited += (sender, e) =>
            {
                if (m_isActive && !m_isStopping)
                {
                    m_logger.LogError("🔥 CDB process exited unexpectedly! Exit code: {ExitCode}, Was active: {WasActive}",
                        m_debuggerProcess?.ExitCode ?? -1, m_isActive);
                    CleanupResources();
                }
                else if (m_isStopping)
                {
                    m_logger.LogInformation("✅ CDB process stopped as expected during session closure. Exit code: {ExitCode}",
                        m_debuggerProcess?.ExitCode ?? -1);
                }
            };

            m_isActive = true;
            m_logger.LogInformation("Successfully started CDB process with target: {Target}", target);
            m_logger.LogInformation("Process ID: {ProcessId}, Active: {IsActive}", m_debuggerProcess.Id, m_isActive);

            // CRITICAL DECISION: DO NOT consume init output!
            // StreamReader buffering makes it impossible to reliably consume partial output
            // Instead, the FIRST command will see the init output + its own output
            // The command executor's completion detection will handle this correctly

            m_logger.LogDebug("⚡ CDB process started - skipping init consumer (first command will see init output)");
            m_initOutputConsumed = true; // Mark as ready immediately

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
            if (!m_isActive || m_debuggerProcess == null)
            {
                m_logger.LogDebug("No active process to stop");
                return false; // Return false when nothing to stop for test compatibility
            }

            m_logger.LogDebug("Stopping CDB process...");
            m_isStopping = true; // Mark that we're intentionally stopping the process

            try
            {
                // Send quit command if input stream is available
                if (m_debuggerInput != null && !m_debuggerInput.BaseStream.CanWrite == false)
                {
                    try
                    {
                        m_debuggerInput.WriteLine("q");
                        m_debuggerInput.Flush();
                        m_logger.LogDebug("Sent quit command to CDB");
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogWarning(ex, "Failed to send quit command to CDB");
                    }
                }

                // Wait for graceful exit
                if (!m_debuggerProcess.WaitForExit(5000))
                {
                    m_logger.LogWarning("CDB process did not exit gracefully, forcing termination");
                    m_debuggerProcess.Kill();
                    m_debuggerProcess.WaitForExit(2000);
                }

                m_logger.LogInformation("CDB process stopped successfully");
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Error stopping CDB process");
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
            var wasActive = m_isActive;
            m_isActive = false;

            if (wasActive)
            {
                m_logger.LogWarning("⚠️ CDB session became inactive - CleanupResources called while session was active");
            }

            try
            {
                m_debuggerInput?.Dispose();
                m_debuggerOutput?.Dispose();
                m_debuggerError?.Dispose();
                m_debuggerProcess?.Dispose();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error disposing process resources");
            }
            finally
            {
                m_debuggerInput = null;
                m_debuggerOutput = null;
                m_debuggerError = null;
                m_debuggerProcess = null;
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
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbProcessManager));
        }

        /// <summary>
        /// Disposes of resources used by the process manager.
        /// This method stops the CDB process and cleans up all associated resources.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
                return;

            try
            {
                StopProcess();
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Error during disposal");
            }
            finally
            {
                m_disposed = true;
            }
        }
    }
}
