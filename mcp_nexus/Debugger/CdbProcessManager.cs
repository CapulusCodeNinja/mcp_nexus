using System.Diagnostics;
using System.Runtime.InteropServices;
using mcp_nexus.Configuration;

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
        private volatile bool m_isActive;  // CRITICAL: volatile ensures visibility across threads
        private volatile bool m_disposed;  // CRITICAL: volatile ensures visibility across threads
        private volatile bool m_initOutputConsumed;  // CRITICAL: ensures init output is consumed before commands execute

        public Process? DebuggerProcess => m_debuggerProcess;
        public StreamWriter? DebuggerInput => m_debuggerInput;
        public StreamReader? DebuggerOutput => m_debuggerOutput;
        public StreamReader? DebuggerError => m_debuggerError;
        public bool IsActive => m_isActive && !m_disposed && m_initOutputConsumed;

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

            // Use centralized UTF-8 encoding configuration for all CDB streams
            var startInfo = EncodingConfiguration.CreateUtf8ProcessStartInfo(cdbPath, arguments);
            startInfo.WorkingDirectory = workingDirectory;
            
            return startInfo;
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
            
            // Monitor process exit to detect crashes
            m_debuggerProcess.EnableRaisingEvents = true;
            m_debuggerProcess.Exited += (sender, e) =>
            {
                if (m_isActive)
                {
                    m_logger.LogError("🔥 CDB process exited unexpectedly! Exit code: {ExitCode}, Was active: {WasActive}", 
                        m_debuggerProcess?.ExitCode ?? -1, m_isActive);
                    CleanupResources();
                }
            };

            m_isActive = true;
            m_logger.LogInformation("Successfully started CDB process with target: {Target}", target);
            m_logger.LogInformation("Process ID: {ProcessId}, Active: {IsActive}", m_debuggerProcess.Id, m_isActive);

            // CRITICAL: Consume CDB's initialization output SYNCHRONOUSLY to prevent race conditions
            // CDB outputs dump loading info, symbol paths, and initial prompt immediately on startup
            // We MUST finish consuming this before ANY commands execute, or they'll steal each other's output
            try
            {
                var initOutput = new System.Text.StringBuilder();
                var startTime = DateTime.Now;
                var timeout = TimeSpan.FromSeconds(5);
                
                m_logger.LogDebug("🔧 Consuming CDB initialization output...");
                
                bool foundPromptStart = false;
                while ((DateTime.Now - startTime) < timeout && m_debuggerOutput != null)
                {
                    // Try to read a line with a short timeout
                    var readTask = Task.Run(() => m_debuggerOutput.ReadLine());
                    if (readTask.Wait(100))
                    {
                        var line = readTask.Result;
                        if (line != null)
                        {
                            // DIAGNOSTIC: Log every line to understand CDB output format
                            m_logger.LogTrace("Init consumer read line: '{Line}'", line);
                            
                            // CRITICAL FIX: CDB doesn't output a prompt until the first command is sent!
                            // Instead of waiting for a prompt, detect the END of init output:
                            // - Disassembly line (hex address followed by instruction)
                            // - OR the "For analysis" suggestion line
                            var isDisassembly = System.Text.RegularExpressions.Regex.IsMatch(line, @"^[0-9a-f`]+\s+[0-9a-f]+\s+");
                            var isAnalysisSuggestion = line.Contains("For analysis of this file, run !analyze");
                            
                            if (isDisassembly || isAnalysisSuggestion)
                            {
                                m_logger.LogDebug("✅ CDB initialization complete, detected end marker: '{Line}'", line);
                                foundPromptStart = true;
                                initOutput.AppendLine(line); // Include this line in init output
                                break;
                            }
                            
                            initOutput.AppendLine(line);
                        }
                        else
                        {
                            break; // Stream ended
                        }
                    }
                }
                
                if (!foundPromptStart && initOutput.Length > 0)
                {
                    m_logger.LogWarning("CDB init output consumed but no prompt found - may cause issues");
                }
                
                m_logger.LogInformation("✅ Consumed {Bytes} bytes of CDB initialization output", initOutput.Length);
                m_initOutputConsumed = true; // Mark as ready for commands
            }
            catch (Exception ex)
            {
                m_logger.LogWarning(ex, "Failed to consume CDB initialization output - first command may include startup text");
                m_initOutputConsumed = true; // Allow commands anyway to avoid deadlock
            }

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
