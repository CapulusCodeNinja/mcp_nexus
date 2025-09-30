using System.Diagnostics;
using System.Runtime.InteropServices;

namespace mcp_nexus.Debugger
{
    /// <summary>
    /// Manages the CDB debugger process lifecycle (start, stop, monitoring)
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
        private bool m_isActive;
        private bool m_disposed;

        public Process? DebuggerProcess => m_debuggerProcess;
        public StreamWriter? DebuggerInput => m_debuggerInput;
        public StreamReader? DebuggerOutput => m_debuggerOutput;
        public StreamReader? DebuggerError => m_debuggerError;
        public bool IsActive => m_isActive && !m_disposed;

        public CdbProcessManager(ILogger<CdbProcessManager> logger, CdbSessionConfiguration config)
        {
            m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Starts the CDB debugger process with the specified target and optional explicit cdbPath override
        /// </summary>
        public bool StartProcess(string target, string? cdbPathOverride)
        {
            ThrowIfDisposed();

            try
            {
                lock (m_lifecycleLock)
                {
                    m_logger.LogDebug("Acquired lifecycle lock for StartProcess");

                    if (m_isActive)
                    {
                        m_logger.LogWarning("Process is already active - stopping current process before starting new one");
                        StopProcessInternal();
                    }

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
        /// Backward-compatible overload
        /// </summary>
        public bool StartProcess(string target)
        {
            return StartProcess(target, null);
        }

        /// <summary>
        /// Stops the CDB debugger process
        /// </summary>
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
        /// Logs diagnostic information about the current process
        /// </summary>
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

            return new ProcessStartInfo
            {
                FileName = cdbPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
        }

        private bool StartProcessInternal(ProcessStartInfo processInfo, string target)
        {
            m_logger.LogDebug("Starting CDB process...");

            m_debuggerProcess = Process.Start(processInfo);
            if (m_debuggerProcess == null)
            {
                m_logger.LogError("Failed to start CDB process");
                return false;
            }

            // Set up streams
            m_debuggerInput = m_debuggerProcess.StandardInput;
            m_debuggerOutput = m_debuggerProcess.StandardOutput;
            m_debuggerError = m_debuggerProcess.StandardError;

            // Configure streams
            m_debuggerInput.AutoFlush = true;

            m_isActive = true;
            m_logger.LogInformation("Successfully started CDB process with target: {Target}", target);
            m_logger.LogInformation("Process ID: {ProcessId}, Active: {IsActive}", m_debuggerProcess.Id, m_isActive);

            return true;
        }

        private bool StopProcessInternal()
        {
            if (!m_isActive || m_debuggerProcess == null)
            {
                m_logger.LogDebug("No active process to stop");
                return false; // Return false when nothing to stop for test compatibility
            }

            m_logger.LogDebug("Stopping CDB process...");

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

        private void CleanupResources()
        {
            m_isActive = false;

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

        private void ThrowIfDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(CdbProcessManager));
        }

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
