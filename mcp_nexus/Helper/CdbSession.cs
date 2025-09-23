using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace mcp_nexus.Helper
{
    public class CdbSession(ILogger<CdbSession> logger, int commandTimeoutMs = 30000, string? customCdbPath = null, int symbolServerTimeoutMs = 30000, int symbolServerMaxRetries = 1, string? symbolSearchPath = null, int startupDelayMs = 2000)
        : IDisposable, ICdbSession
    {
        private Process? m_DebuggerProcess;
        private StreamWriter? m_DebuggerInput;
        private StreamReader? m_DebuggerOutput;
        private StreamReader? m_DebuggerError;
        private bool m_IsActive;
        private readonly object m_LifecycleLock = new();  // Only for start/stop operations, not command execution
        private CancellationTokenSource? m_CurrentOperationCts;
        private readonly object m_CancellationLock = new();

        public bool IsActive
        {
            get
            {
                // LOCK-FREE VERSION: Don't block on m_SessionLock to avoid deadlocks
                // This is safe to read without locks since these are just status checks
                logger.LogTrace("IsActive: Checking status (lock-free)...");

                var isActive = m_IsActive;  // This is a simple bool read, thread-safe
                var process = m_DebuggerProcess;  // Get reference once
                var hasExited = process?.HasExited ?? true;  // Safe even if process is null

                logger.LogTrace("IsActive: m_IsActive={IsActive}, processExists={ProcessExists}, hasExited={HasExited}",
                    isActive, process != null, hasExited);

                var result = isActive && !hasExited;
                logger.LogTrace("IsActive: Returning {Result} (lock-free)", result);
                return result;
            }
        }

        public void CancelCurrentOperation()
        {
            // Synchronous version for backward compatibility
            _ = CancelCurrentOperationAsync();
        }

        private async Task CancelCurrentOperationAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            logger.LogWarning("🚨 [LOCKLESS-CANCEL] CancelCurrentOperationAsync started - ARCHITECTURAL FIX - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            // ARCHITECTURAL FIX: Since ExecuteCommand no longer holds session lock, this is much simpler
            logger.LogInformation("🎯 [LOCKLESS-CANCEL] Cancelling operation token (ExecuteCommand is lockless now) - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            lock (m_CancellationLock)
            {
                logger.LogWarning("🚨 [LOCKLESS-CANCEL] Cancelling current CDB operation due to client request - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                m_CurrentOperationCts?.Cancel();
                logger.LogInformation("✅ [LOCKLESS-CANCEL] Operation token cancelled successfully - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }

            // Get process info for interrupt commands - no session lock conflicts anymore!
            var debuggerProcess = m_DebuggerProcess;  // Thread-safe read
            var debuggerInput = m_DebuggerInput;      // Thread-safe read
            
            if (debuggerProcess is { HasExited: false } && debuggerInput != null)
            {
                var processId = debuggerProcess.Id;
                logger.LogInformation("📋 [LOCKLESS-CANCEL] Process details captured for cancellation (PID: {ProcessId}) - elapsed: {ElapsedMs}ms", processId, stopwatch.ElapsedMilliseconds);
                
                // Send interrupt commands directly - no session lock needed
                try
                {
                    logger.LogWarning("🚨 [LOCKLESS-CANCEL] Attempting to interrupt CDB command for PID: {ProcessId} - elapsed: {ElapsedMs}ms", processId, stopwatch.ElapsedMilliseconds);
                    
                    logger.LogInformation("📡 [LOCKLESS-CANCEL] Sending Ctrl+C to CDB process - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    await debuggerInput.WriteLineAsync("\x03"); // ASCII ETX (Ctrl+C)
                    await debuggerInput.FlushAsync();
                    
                    logger.LogInformation("✅ [LOCKLESS-CANCEL] Ctrl+C sent, starting 1s wait - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    await Task.Delay(1000);
                    
                    logger.LogInformation("📡 [LOCKLESS-CANCEL] Sending '.' command to get back to prompt - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    await debuggerInput.WriteLineAsync(".");  // Current instruction
                    await debuggerInput.FlushAsync();
                    logger.LogInformation("✅ [LOCKLESS-CANCEL] '.' command sent - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    
                    logger.LogInformation("⏳ [LOCKLESS-CANCEL] Starting final 2s wait - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    await Task.Delay(2000);
                    
                    logger.LogWarning("✅ [LOCKLESS-CANCEL] Command cancellation completed for PID: {ProcessId} - elapsed: {ElapsedMs}ms", processId, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "💥 [LOCKLESS-CANCEL] Failed to interrupt CDB command for PID: {ProcessId} - elapsed: {ElapsedMs}ms", processId, stopwatch.ElapsedMilliseconds);
                }
            }
            else
            {
                logger.LogWarning("❌ [LOCKLESS-CANCEL] No active CDB process to cancel - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            
            stopwatch.Stop();
            logger.LogInformation("🎯 [LOCKLESS-CANCEL] CancelCurrentOperationAsync completed - TOTAL TIME: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }

        public async Task<bool> StartSession(string target, string? arguments = null)
        {
            logger.LogDebug("StartSession called with target: {Target}, arguments: {Arguments}", target, arguments);

            try
            {
                // Add overall timeout to prevent hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(commandTimeoutMs));
                return await Task.Run(() => StartSessionInternal(target), cts.Token);
            }
            catch (OperationCanceledException)
            {
                logger.LogError("StartSession timed out after {TimeoutMs}ms for target: {Target}", commandTimeoutMs, target);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start CDB session with target: {Target}", target);
                return false;
            }
        }

        private bool StartSessionInternal(string target)
        {
            try
            {
                // Check if we need to stop current session (outside lock to avoid deadlock)
                bool needsStop = false;
                lock (m_LifecycleLock)
                {
                    needsStop = m_IsActive;
                }

                if (needsStop)
                {
                    logger.LogInformation("Session is already active - stopping current session before starting new one");
                    StopSession().Wait(); // Synchronous wait since we're in a non-async method
                }

                lock (m_LifecycleLock)
                {
                    logger.LogDebug("Acquired lifecycle lock for StartSession");

                    logger.LogInformation("Searching for CDB executable...");
                    var cdbPath = FindCdbPath();
                    if (string.IsNullOrEmpty(cdbPath))
                    {
                        logger.LogError("CDB.exe not found. Please ensure Windows Debugging Tools are installed.");
                        return false;
                    }
                    logger.LogInformation("Found CDB at: {CdbPath}", cdbPath);

                    // Determine if this is a crash dump file (ends with .dmp)
                    var isCrashDump = target.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase) || 
                                     target.Contains(".dmp\"", StringComparison.OrdinalIgnoreCase) ||
                                     target.Contains(".dmp ", StringComparison.OrdinalIgnoreCase);
                    
                    // For crash dumps, ensure we use -z flag. The target may already contain other arguments.
                    string cdbArguments;
                    if (isCrashDump && !target.TrimStart().StartsWith("-z", StringComparison.OrdinalIgnoreCase))
                    {
                        // If target doesn't already start with -z, add it
                        cdbArguments = $"-z {target}";
                    }
                    else
                    {
                        // Target already has proper formatting or is not a crash dump
                        cdbArguments = target;
                    }
                    
                    // Add startup arguments with symbol server timeout controls
                    var enhancedArguments = $"-lines -n {cdbArguments}";
                    logger.LogDebug("CDB arguments: {Arguments} (isCrashDump: {IsCrashDump})", enhancedArguments, isCrashDump);
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cdbPath,
                        Arguments = enhancedArguments,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        EnvironmentVariables =
                        {
                            // Set timeout controls from configuration
                            ["DBGHELP_SYMSRV_TIMEOUT"] = symbolServerTimeoutMs.ToString(),
                            ["DBGHELP_SYMSRV_MAX_RETRIES"] = symbolServerMaxRetries.ToString()
                        }
                    };

                    // Override symbol search path if configured (otherwise preserve existing _NT_SYMBOL_PATH)
                    string actualSymbolPath;
                    if (!string.IsNullOrWhiteSpace(symbolSearchPath))
                    {
                        startInfo.EnvironmentVariables["_NT_SYMBOL_PATH"] = symbolSearchPath;
                        actualSymbolPath = symbolSearchPath;
                        logger.LogInformation("Using configured symbol search path: {SymbolSearchPath}", symbolSearchPath);
                    }
                    else
                    {
                        // Get the current environment variable value
                        actualSymbolPath = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH") ?? "Not set";
                        logger.LogInformation("Using existing _NT_SYMBOL_PATH from environment: {SymbolSearchPath}", actualSymbolPath);
                    }

                    // Log the effective symbol search path that CDB will use
                    logger.LogInformation("Effective symbol search path for CDB session: {EffectiveSymbolPath}", actualSymbolPath);

                    logger.LogDebug("Creating CDB process with arguments: {Arguments}", startInfo.Arguments);
                    m_DebuggerProcess = new Process { StartInfo = startInfo };
                    
                    logger.LogInformation("Starting CDB process...");
                    var processStarted = m_DebuggerProcess.Start();
                    logger.LogInformation("CDB process start result: {Started}, PID: {ProcessId}", processStarted, m_DebuggerProcess.Id);

                    if (!processStarted)
                    {
                        logger.LogError("Failed to start CDB process");
                        return false;
                    }

                    // Give CDB a moment to start up (configurable delay)
                    logger.LogDebug("Allowing CDB process to start up (PID: {ProcessId}), waiting {DelayMs}ms...", m_DebuggerProcess.Id, startupDelayMs);
                    Thread.Sleep(startupDelayMs);

                    logger.LogDebug("Setting up input/output streams...");
                    m_DebuggerInput = m_DebuggerProcess.StandardInput;
                    m_DebuggerOutput = m_DebuggerProcess.StandardOutput;
                    m_DebuggerError = m_DebuggerProcess.StandardError;

                    m_IsActive = true;
                    logger.LogInformation("CDB session marked as active");
                }

                logger.LogInformation("Successfully started CDB session with target: {Target}", target);
                logger.LogInformation("Session active: {IsActive}, Process running: {IsRunning}", m_IsActive, m_DebuggerProcess?.HasExited == false);
                
                // Don't wait for full initialization - CDB will be ready when we send commands
                logger.LogInformation("CDB process started successfully. Session will be ready for commands.");
                
                logger.LogInformation("StartSession completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start CDB session with target: {Target}", target);
                return false;
            }
        }

        public Task<string> ExecuteCommand(string command)
        {
            return ExecuteCommand(command, CancellationToken.None);
        }

        public Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken)
        {
            logger.LogInformation("🎯 [LOCKLESS] CDB ExecuteCommand START: {Command} - QUEUE PROVIDES SERIALIZATION", command);

            try
            {
                // Create cancellation token for this operation - combine external token with timeout
                // ARCHITECTURAL FIX: No session lock needed - CommandQueueService provides serialization
                CancellationTokenSource operationCts;
                lock (m_CancellationLock)
                {
                    operationCts = CancellationTokenSource.CreateLinkedTokenSource(
                        externalCancellationToken,
                        new CancellationTokenSource(TimeSpan.FromMilliseconds(commandTimeoutMs)).Token);
                    m_CurrentOperationCts = operationCts;
                }

                return Task.Run(() =>
                {
                    try
                    {
                        // ARCHITECTURAL FIX: NO SESSION LOCK - Queue serializes everything
                        // Simple validation without locks (thread-safe reads)
                        
                        // Check for cancellation before proceeding
                        operationCts.Token.ThrowIfCancellationRequested();

                        logger.LogDebug("🔓 [LOCKLESS] ExecuteCommand - no session lock needed (queue serializes)");
                        logger.LogInformation("🔓 [LOCKLESS] ExecuteCommand - IsActive: {IsActive}, ProcessExited: {ProcessExited}", 
                            m_IsActive, m_DebuggerProcess?.HasExited);

                        // Simple thread-safe validation
                        if (!m_IsActive)
                        {
                            logger.LogWarning("No active debug session - cannot execute command");
                            return "No active debug session. Please start a session first.";
                        }

                        var debuggerProcess = m_DebuggerProcess;
                        var debuggerInput = m_DebuggerInput;

                        if (debuggerProcess?.HasExited == true)
                        {
                            logger.LogWarning("Debug process has exited - cannot execute command");
                            return "Debug process has exited. Please start a new session.";
                        }

                        if (debuggerInput == null)
                        {
                            logger.LogError("Debug session input stream is not available");
                            return "Debug session input stream is not available.";
                        }

                        // Check for cancellation before sending command
                        operationCts.Token.ThrowIfCancellationRequested();

                        logger.LogInformation("📡 [LOCKLESS] Sending command to CDB: {Command}", command);

                        // Send command to debugger - no locks needed since queue serializes
                        debuggerInput.WriteLine(command);
                        debuggerInput.Flush();
                        logger.LogDebug("📡 [LOCKLESS] Command sent to CDB, waiting for output...");

                        // Read output with cancellation support and process monitoring
                        var commandStartTime = DateTime.Now;
                        logger.LogInformation("⏱️ [LOCKLESS] Command sent at {StartTime}, timeout: {TimeoutMs}ms", commandStartTime, commandTimeoutMs);

                        var output = ReadDebuggerOutputWithCancellation(commandTimeoutMs, operationCts.Token);

                        var commandDuration = (DateTime.Now - commandStartTime).TotalMilliseconds;
                        logger.LogInformation("✅ [LOCKLESS] Command execution completed in {Duration}ms, output length: {Length} characters",
                            commandDuration, output.Length);
                        logger.LogDebug("✅ [LOCKLESS] Command output: {Output}", output);

                        return output;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogWarning("❌ [LOCKLESS] Command execution was cancelled: {Command}", command);

                        // Capture any available output before reporting cancellation
                        CaptureAvailableOutput("Command execution cancelled");
                        LogProcessDiagnostics("Command execution cancelled");

                        return "Command execution was cancelled due to timeout or client request.";
                    }
                    finally
                    {
                        // Cleanup cancellation token - only need cancellation lock
                        lock (m_CancellationLock)
                        {
                            if (m_CurrentOperationCts == operationCts)
                            {
                                m_CurrentOperationCts = null;
                            }
                        }
                        operationCts.Dispose();
                    }
                }, operationCts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "💥 [LOCKLESS] Failed to execute command: {Command}", command);
                return Task.FromResult($"Command execution failed: {ex.Message}");
            }
        }

        public Task<bool> StopSession()
        {
            logger.LogInformation("🔥 [LOCKLESS-STOP] StopSession called - ARCHITECTURAL FIX ACTIVE");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Run the stop process asynchronously to prevent blocking the HTTP request
            return Task.Run(async () =>
            {
                try
                {
                    logger.LogInformation("🔥 [LOCKLESS-STOP] StopSession Task.Run started - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                    // First, cancel any ongoing operations (lockless now!)
                    logger.LogInformation("🛑 [LOCKLESS-STOP] Starting CancelCurrentOperationAsync - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    await CancelCurrentOperationAsync();
                    logger.LogInformation("✅ [LOCKLESS-STOP] CancelCurrentOperationAsync completed - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                    // Give cancellation a moment to take effect
                    logger.LogInformation("⏳ [LOCKLESS-STOP] Starting 200ms delay - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    await Task.Delay(200);
                    logger.LogInformation("✅ [LOCKLESS-STOP] 200ms delay completed - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                    // Get process reference - thread-safe reads, no session lock needed
                    var processToStop = m_DebuggerProcess;
                    var inputToStop = m_DebuggerInput;
                    var wasActive = m_IsActive;
                    var processId = processToStop?.Id ?? 0;

                    logger.LogInformation("📋 [LOCKLESS-STOP] Process details captured (PID: {ProcessId}, Active: {IsActive}) - elapsed: {ElapsedMs}ms", 
                        processId, wasActive, stopwatch.ElapsedMilliseconds);

                    if (!wasActive)
                    {
                        logger.LogWarning("❌ [LOCKLESS-STOP] No active session to stop - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                        return false;
                    }

                    // Send quit command - no locks needed
                    if (processToStop != null && !processToStop.HasExited && inputToStop != null)
                    {
                        logger.LogInformation("📝 [LOCKLESS-STOP] Sending quit command to CDB (PID: {ProcessId}) - elapsed: {ElapsedMs}ms", processId, stopwatch.ElapsedMilliseconds);
                        try
                        {
                            inputToStop.WriteLine("q");
                            inputToStop.Flush();
                            logger.LogInformation("✅ [LOCKLESS-STOP] Quit command sent successfully - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "❌ [LOCKLESS-STOP] Failed to send quit command - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                        }

                        // Grace period for quit
                        logger.LogInformation("⏳ [LOCKLESS-STOP] Starting 500ms grace period - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                        await Task.Delay(500);
                        logger.LogInformation("✅ [LOCKLESS-STOP] Grace period completed - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                        // Force kill if needed
                        if (!processToStop.HasExited)
                        {
                            logger.LogWarning("❌ [LOCKLESS-STOP] CDB did not exit after quit. Force-killing process tree - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                            try
                            {
                                processToStop.Kill(entireProcessTree: true);
                                logger.LogInformation("🔪 [LOCKLESS-STOP] Kill command issued - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                                
                                // Short wait for kill
                                await Task.Delay(1000);
                                logger.LogInformation("💀 [LOCKLESS-STOP] Kill wait completed - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "💥 [LOCKLESS-STOP] Failed to force-kill CDB process - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                            }
                        }
                        else
                        {
                            logger.LogInformation("✅ [LOCKLESS-STOP] CDB process exited gracefully - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                        }
                    }
                    else
                    {
                        logger.LogInformation("❌ [LOCKLESS-STOP] No process to stop or already exited - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    }

                    // Clean up resources - only use lifecycle lock for state changes
                    logger.LogInformation("🔒 [LOCKLESS-STOP] Acquiring lifecycle lock for cleanup - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    lock (m_LifecycleLock)
                    {
                        logger.LogInformation("🧹 [LOCKLESS-STOP] Disposing of CDB resources - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    m_DebuggerProcess?.Dispose();
                    m_DebuggerInput?.Dispose();
                    m_DebuggerOutput?.Dispose();
                    m_DebuggerError?.Dispose();

                    m_DebuggerProcess = null;
                    m_DebuggerInput = null;
                    m_DebuggerOutput = null;
                    m_DebuggerError = null;
                    m_IsActive = false;
                    
                        logger.LogInformation("✅ [LOCKLESS-STOP] CDB session resources cleaned up - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                }
                    logger.LogInformation("🔓 [LOCKLESS-STOP] Lifecycle lock released - elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                    stopwatch.Stop();
                    logger.LogInformation("🎯 [LOCKLESS-STOP] CDB session stopped successfully - TOTAL TIME: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return true;
            }
            catch (Exception ex)
            {
                    stopwatch.Stop();
                    logger.LogError(ex, "💥 [LOCKLESS-STOP] Failed to stop CDB session - TOTAL TIME: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    return false;
            }
            });
        }

        private string ReadDebuggerOutputWithCancellation(int timeoutMs, CancellationToken cancellationToken)
        {
            logger.LogDebug("ReadDebuggerOutputWithCancellation called with timeout: {TimeoutMs}ms", timeoutMs);
            
            if (m_DebuggerOutput == null)
            {
                logger.LogError("No output stream available for reading");
                return "No output stream available";
            }

            var output = new StringBuilder();
            var startTime = DateTime.Now;
            var linesRead = 0;
            var lastOutputTime = startTime;

            try
            {
                logger.LogDebug("Starting to read debugger output with cancellation support...");
                
                while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
                {
                    // Check for cancellation first
                    cancellationToken.ThrowIfCancellationRequested();

                    if (m_DebuggerOutput.Peek() == -1)
                    {
                        // Check if we've been waiting too long without any output
                        if ((DateTime.Now - lastOutputTime).TotalMilliseconds > 5000)
                        {
                            logger.LogWarning("No output received for 5 seconds, continuing to wait...");
                        }
                        Thread.Sleep(50); // Increased sleep time for better performance
                        continue;
                    }

                    var line = m_DebuggerOutput.ReadLine();
                    if (line != null)
                    {
                        linesRead++;
                        lastOutputTime = DateTime.Now;
                        output.AppendLine(line);
                        logger.LogTrace("Read line {LineNumber}: {Line}", linesRead, line);

                        // Check for command completion indicators
                        if (IsCommandComplete(line))
                        {
                            logger.LogDebug("Command completion detected in line: {Line}", line);
                            break;
                        }
                    }
                }
                
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                logger.LogDebug("Finished reading debugger output after {ElapsedMs}ms, read {LineCount} lines", elapsed, linesRead);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("ReadDebuggerOutput was cancelled after {ElapsedMs}ms", (DateTime.Now - startTime).TotalMilliseconds);
                output.AppendLine("Command execution was cancelled.");
                throw; // Re-throw to propagate cancellation
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error reading debugger output after {ElapsedMs}ms", (DateTime.Now - startTime).TotalMilliseconds);
                output.AppendLine($"Error reading output: {ex.Message}");
            }

            var result = output.ToString();
            logger.LogDebug("ReadDebuggerOutputWithCancellation returning {Length} characters", result.Length);
            return result;
        }

        private void CaptureAvailableOutput(string context)
        {
            try
            {
                logger.LogInformation("");

                logger.LogInformation("╔═══════════════════════════════════════════════════════════════════╗");
                logger.LogInformation("                         CDB OUTPUT CAPTURE");
                logger.LogInformation($"  Context: {context}");
                logger.LogInformation("╚═══════════════════════════════════════════════════════════════════╝");

                // Try to read any available stdout
                if (m_DebuggerOutput != null)
                {
                    var stdoutLines = new List<string>();
                    try
                    {
                        while (m_DebuggerOutput.Peek() != -1)
                        {
                            var line = m_DebuggerOutput.ReadLine();
                            if (line != null)
                            {
                                stdoutLines.Add(line);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Could not read stdout lines (stream may be closed): {Context}", context);
                        stdoutLines.Add($"[Error reading stdout: {ex.Message}]");
                    }

                    if (stdoutLines.Count > 0)
                    {
                        logger.LogInformation("");
                        logger.LogInformation("┌─ CDB Standard Output ──────────────────────────────────────────────");
                        logger.LogInformation("│ Captured {LineCount} lines from stdout buffer", stdoutLines.Count);
                        logger.LogInformation("├─────────────────────────────────────────────────────────────────────");
                        foreach (var line in stdoutLines.Take(20)) // Limit to first 20 lines
                        {
                            logger.LogInformation("│ {Line}", line);
                        }
                        if (stdoutLines.Count > 20)
                        {
                            logger.LogInformation("│ ... and {MoreLines} more lines (truncated for brevity)", stdoutLines.Count - 20);
                        }
                        logger.LogInformation("└─────────────────────────────────────────────────────────────────────");
                    }
                    else
                    {
                        logger.LogInformation("");
                        logger.LogInformation("┌─ CDB Standard Output ──────────────────────────────────────────────");
                        logger.LogInformation("│ No output available in stdout buffer");
                        logger.LogInformation("└─────────────────────────────────────────────────────────────────────");
                    }
                }

                // Try to read any available stderr
                if (m_DebuggerError != null)
                {
                    var stderrLines = new List<string>();
                    try
                    {
                        while (m_DebuggerError.Peek() != -1)
                        {
                            var line = m_DebuggerError.ReadLine();
                            if (line != null)
                            {
                                stderrLines.Add(line);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Could not read stderr lines (stream may be closed): {Context}", context);
                        stderrLines.Add($"[Error reading stderr: {ex.Message}]");
                    }

                    if (stderrLines.Count > 0)
                    {
                        logger.LogWarning("");
                        logger.LogWarning("┌─ CDB Standard Error ───────────────────────────────────────────────");
                        logger.LogWarning("│ Captured {LineCount} lines from stderr buffer", stderrLines.Count);
                        logger.LogWarning("├─────────────────────────────────────────────────────────────────────");
                        foreach (var line in stderrLines.Take(20)) // Limit to first 20 lines
                        {
                            logger.LogWarning("│ {Line}", line);
                        }
                        if (stderrLines.Count > 20)
                        {
                            logger.LogWarning("│ ... and {MoreLines} more lines (truncated for brevity)", stderrLines.Count - 20);
                        }
                        logger.LogWarning("└─────────────────────────────────────────────────────────────────────");
                    }
                    else
                    {
                        logger.LogInformation("");
                        logger.LogInformation("┌─ CDB Standard Error ───────────────────────────────────────────────");
                        logger.LogInformation("│ No errors available in stderr buffer");
                        logger.LogInformation("└─────────────────────────────────────────────────────────────────────");
                    }
                }

                logger.LogInformation("");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to capture CDB output for {Context}", context);
            }
        }

        private void LogProcessDiagnostics(string context)
        {
            if (m_DebuggerProcess == null) return;

            try
            {
                // Use debug level for progress messages, info level for errors/crashes
                var isProgressUpdate = context.Contains("in progress") || context.Contains("Symbol download");
                var logLevel = isProgressUpdate ? LogLevel.Debug : LogLevel.Information;

                var process = m_DebuggerProcess;
                logger.Log(logLevel, "");

                logger.Log(logLevel, "╔═══════════════════════════════════════════════════════════════════╗");
                logger.Log(logLevel, "                        PROCESS DIAGNOSTICS");
                logger.Log(logLevel, $"  Context: {context}");
                logger.Log(logLevel, "");
                logger.Log(logLevel, $"  PID:          {process.Id}");
                logger.Log(logLevel, $"  Process Name: {process.ProcessName}");
                logger.Log(logLevel, $"  Has Exited:   {process.HasExited}");

                if (!process.HasExited)
                {
                    logger.Log(logLevel, $"  Start Time:   {process.StartTime}");
                    logger.Log(logLevel, $"  CPU Time:     {process.TotalProcessorTime}");
                    var memoryInfo = $"{process.WorkingSet64:N0} bytes ({process.WorkingSet64 / 1024.0 / 1024.0:F1} MB)";
                    logger.Log(logLevel, $"  Memory Usage: {memoryInfo}");
                    logger.Log(logLevel, $"  Threads:      {process.Threads.Count}");

                    // Log command line if we can get it
                    try
                    {
                        var commandLine = GetProcessCommandLine(process.Id);
                        if (!string.IsNullOrEmpty(commandLine))
                        {
                            logger.Log(logLevel, $"  Command Line: {commandLine}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Could not retrieve command line for process {ProcessId}", process.Id);
                        logger.Log(logLevel, "  Command Line: Unable to retrieve");
                    }
                }
                else
                {
                    logger.Log(logLevel, $"  Exit Code:    {process.ExitCode}");
                    logger.Log(logLevel, $"  Exit Time:    {process.ExitTime}");
                }

                logger.Log(logLevel, "╚═══════════════════════════════════════════════════════════════════╝");
                logger.Log(logLevel, "");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to gather process diagnostics for {Context}", context);
            }
        }

        private static string GetProcessCommandLine(int processId)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = $"process where processid={processId} get commandline /format:value",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null && process.WaitForExit(5000))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var commandLineLine = lines.FirstOrDefault(l => l.StartsWith("CommandLine="));
                    return commandLineLine?.Substring("CommandLine=".Length).Trim() ?? "";
                }
            }
            catch
            {
                // Ignore errors - this is just for diagnostics
            }
            return "";
        }

        private bool IsCommandComplete(string line)
        {
            // CDB typically shows "0:000>" prompt when ready for next command
            var isComplete = line.Contains(">") && Regex.IsMatch(line, @"\d+:\d+>");
            logger.LogTrace("IsCommandComplete checking line: '{Line}' -> {IsComplete}", line, isComplete);
            return isComplete;
        }

        private string GetCurrentArchitecture()
        {
            var architecture = RuntimeInformation.ProcessArchitecture;
            logger.LogDebug("Detected process architecture: {Architecture}", architecture);
            
            return architecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                _ => "x64" // Default to x64 for unknown architectures
            };
        }

        private string FindCdbPath()
        {
            logger.LogDebug("FindCDBPath called - searching for CDB executable");
            
            // 1. Check custom path provided via --cdb-path parameter
            if (!string.IsNullOrEmpty(customCdbPath))
            {
                logger.LogInformation("Using custom CDB path from --cdb-path parameter: {Path}", customCdbPath);
                if (File.Exists(customCdbPath))
                {
                    logger.LogInformation("Custom CDB path verified: {Path}", customCdbPath);
                    return customCdbPath;
                }
                else
                {
                    logger.LogWarning("Custom CDB path does not exist: {Path}", customCdbPath);
                }
            }
            
            // 2. Continue with automatic path detection
            var currentArch = GetCurrentArchitecture();
            logger.LogInformation("Current machine architecture: {Architecture}", currentArch);
            
            // Create prioritized list based on current architecture
            var possiblePaths = new List<string>();
            
            // Add paths for current architecture first
            switch (currentArch)
            {
                case "x64":
                    possiblePaths.AddRange(new[]
                    {
                        @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                        @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                        @"C:\Program Files (x86)\Debugging Tools for Windows (x64)\cdb.exe",
                        @"C:\Program Files\Debugging Tools for Windows (x64)\cdb.exe"
                    });
                    break;
                case "x86":
                    possiblePaths.AddRange(new[]
                    {
                        @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe",
                        @"C:\Program Files\Windows Kits\10\Debuggers\x86\cdb.exe",
                        @"C:\Program Files (x86)\Debugging Tools for Windows (x86)\cdb.exe",
                        @"C:\Program Files\Debugging Tools for Windows (x86)\cdb.exe"
                    });
                    break;
                case "arm64":
                    possiblePaths.AddRange(new[]
                    {
                        @"C:\Program Files (x86)\Windows Kits\10\Debuggers\arm64\cdb.exe",
                        @"C:\Program Files\Windows Kits\10\Debuggers\arm64\cdb.exe"
                    });
                    break;
            }
            
            // Add fallback paths for other architectures
            if (currentArch != "x64")
            {
                possiblePaths.AddRange(new[]
                {
                    @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                    @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                    @"C:\Program Files (x86)\Debugging Tools for Windows (x64)\cdb.exe",
                    @"C:\Program Files\Debugging Tools for Windows (x64)\cdb.exe"
                });
            }
            
            if (currentArch != "x86")
            {
                possiblePaths.AddRange(new[]
                {
                    @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe",
                    @"C:\Program Files\Windows Kits\10\Debuggers\x86\cdb.exe",
                    @"C:\Program Files (x86)\Debugging Tools for Windows (x86)\cdb.exe",
                    @"C:\Program Files\Debugging Tools for Windows (x86)\cdb.exe"
                });
            }
            
            if (currentArch != "arm64")
            {
                possiblePaths.AddRange(new[]
                {
                    @"C:\Program Files (x86)\Windows Kits\10\Debuggers\arm64\cdb.exe",
                    @"C:\Program Files\Windows Kits\10\Debuggers\arm64\cdb.exe"
                });
            }

            logger.LogDebug("Checking {Count} prioritized CDB paths (current arch: {Architecture})", possiblePaths.Count, currentArch);
            foreach (var path in possiblePaths)
            {
                logger.LogTrace("Checking path: {Path}", path);
                if (File.Exists(path))
                {
                    logger.LogInformation("Found CDB at path: {Path} (architecture-aware selection)", path);
                    return path;
                }
            }

            logger.LogDebug("CDB not found in standard paths, searching PATH...");
            // Try to find in PATH with timeout
            try
            {
                using var result = Process.Start(new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "cdb.exe",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (result != null)
                {
                    // Use timeout to prevent hanging
                    const int timeoutMs = 5000; // 5 second timeout
                    logger.LogDebug("Executing 'where cdb.exe' with {TimeoutMs}ms timeout", timeoutMs);

                    if (result.WaitForExit(timeoutMs))
                {
                    var output = result.StandardOutput.ReadToEnd();
                        logger.LogDebug("'where cdb.exe' command exit code: {ExitCode}", result.ExitCode);
                    
                    if (result.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                            logger.LogDebug("Found {Count} CDB paths in PATH", lines.Length);
                        
                        if (lines.Length > 0)
                        {
                            var cdbPath = lines[0].Trim();
                                logger.LogInformation("Found CDB in PATH: {Path}", cdbPath);
                            return cdbPath;
                        }
                    }
                    else
                    {
                            logger.LogDebug("'where cdb.exe' found no results");
                        }
                    }
                    else
                    {
                        logger.LogWarning("'where cdb.exe' command timed out after {TimeoutMs}ms", timeoutMs);
                        try
                        {
                            result.Kill();
                        }
                        catch (Exception killEx)
                        {
                            logger.LogDebug(killEx, "Error killing timed-out 'where' process");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error searching for CDB in PATH");
            }

            logger.LogError("CDB executable not found in any standard location or PATH");
            return string.Empty;
        }

        public void Dispose()
        {
            logger.LogDebug("Dispose called on CdbSession");

            // Cancel any running operations first
            lock (m_CancellationLock)
            {
                if (m_CurrentOperationCts != null)
                {
                    logger.LogWarning("Cancelling running operation during dispose");
                    m_CurrentOperationCts.Cancel();
                }
            }
            
            if (m_IsActive)
            {
                logger.LogInformation("Disposing active CDB session...");
                StopSession().Wait();
            }
            else
            {
                logger.LogDebug("No active session to dispose");
            }
            
            logger.LogDebug("CdbSession disposal completed");
        }
    }
}
