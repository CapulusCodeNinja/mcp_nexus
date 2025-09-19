using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace mcp_nexus.Helper
{
    public class CdbSession : IDisposable
    {
        private readonly ILogger<CdbSession> m_Logger;
        private Process? m_DebuggerProcess;
        private StreamWriter? m_DebuggerInput;
        private StreamReader? m_DebuggerOutput;
        private StreamReader? m_DebuggerError;
        private bool m_IsActive;
        private readonly object m_SessionLock = new object();
        private readonly int m_CommandTimeoutMs;
        private readonly string? m_CustomCdbPath;

        public bool IsActive
        {
            get
            {
                lock (m_SessionLock)
                {
                    return m_IsActive && m_DebuggerProcess != null && !m_DebuggerProcess.HasExited;
                }
            }
        }

        public CdbSession(ILogger<CdbSession> logger, int commandTimeoutMs = 30000, string? customCdbPath = null)
        {
            m_Logger = logger;
            m_CommandTimeoutMs = commandTimeoutMs;
            m_CustomCdbPath = customCdbPath;
        }

        public Task<bool> StartSession(string target, string? arguments = null)
        {
            m_Logger.LogDebug("StartSession called with target: {Target}, arguments: {Arguments}", target, arguments);
            
            try
            {
                lock (m_SessionLock)
                {
                    m_Logger.LogDebug("Acquired session lock for StartSession");
                    
                    if (m_IsActive)
                    {
                        m_Logger.LogWarning("Session is already active - cannot start new session");
                        return Task.FromResult(false);
                    }

                    m_Logger.LogInformation("Searching for CDB executable...");
                    var cdbPath = FindCDBPath();
                    if (string.IsNullOrEmpty(cdbPath))
                    {
                        m_Logger.LogError("CDB.exe not found. Please ensure Windows Debugging Tools are installed.");
                        return Task.FromResult(false);
                    }
                    m_Logger.LogInformation("Found CDB at: {CdbPath}", cdbPath);

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
                    
                    m_Logger.LogDebug("CDB arguments: {Arguments} (isCrashDump: {IsCrashDump})", cdbArguments, isCrashDump);
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cdbPath,
                        Arguments = cdbArguments,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    m_Logger.LogDebug("Creating CDB process with arguments: {Arguments}", startInfo.Arguments);
                    m_DebuggerProcess = new Process { StartInfo = startInfo };
                    
                    m_Logger.LogInformation("Starting CDB process...");
                    var processStarted = m_DebuggerProcess.Start();
                    m_Logger.LogInformation("CDB process start result: {Started}", processStarted);

                    if (!processStarted)
                    {
                        m_Logger.LogError("Failed to start CDB process");
                        return Task.FromResult(false);
                    }

                    // Wait a moment for the process to initialize
                    m_Logger.LogDebug("Waiting for CDB process to initialize...");
                    Thread.Sleep(1000);

                    // Check if process is still running
                    if (m_DebuggerProcess.HasExited)
                    {
                        m_Logger.LogError("CDB process exited immediately after starting. Exit code: {ExitCode}", m_DebuggerProcess.ExitCode);
                        return Task.FromResult(false);
                    }

                    m_Logger.LogDebug("Setting up input/output streams...");
                    m_DebuggerInput = m_DebuggerProcess.StandardInput;
                    m_DebuggerOutput = m_DebuggerProcess.StandardOutput;
                    m_DebuggerError = m_DebuggerProcess.StandardError;

                    m_IsActive = true;
                    m_Logger.LogInformation("CDB session marked as active");
                }

                m_Logger.LogInformation("Successfully started CDB session with target: {Target}", target);
                m_Logger.LogInformation("Session active: {IsActive}, Process running: {IsRunning}", m_IsActive, m_DebuggerProcess?.HasExited == false);
                
                // Don't wait for full initialization - CDB will be ready when we send commands
                m_Logger.LogInformation("CDB process started successfully. Session will be ready for commands.");
                
                m_Logger.LogInformation("StartSession completed successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to start CDB session with target: {Target}", target);
                return Task.FromResult(false);
            }
        }

        public Task<string> ExecuteCommand(string command)
        {
            m_Logger.LogDebug("ExecuteCommand called with command: {Command}", command);
            
            try
            {
                lock (m_SessionLock)
                {
                    m_Logger.LogDebug("Acquired session lock for ExecuteCommand");
                    m_Logger.LogInformation("ExecuteCommand - IsActive: {IsActive}, ProcessExited: {ProcessExited}", m_IsActive, m_DebuggerProcess?.HasExited);
                    
                    if (!m_IsActive || m_DebuggerProcess?.HasExited == true)
                    {
                        m_Logger.LogWarning("No active debug session - cannot execute command");
                        return Task.FromResult("No active debug session. Please start a session first.");
                    }

                    if (m_DebuggerInput == null)
                    {
                        m_Logger.LogError("Debug session input stream is not available");
                        return Task.FromResult("Debug session input stream is not available.");
                    }

                    m_Logger.LogInformation("Sending command to CDB: {Command}", command);

                    // Send command to debugger
                    m_DebuggerInput.WriteLine(command);
                    m_DebuggerInput.Flush();
                    m_Logger.LogDebug("Command sent to CDB, waiting for output...");

                    // Read output with extended timeout for large dumps
                    var output = ReadDebuggerOutput(15000); // 15 seconds for large dumps
                    m_Logger.LogInformation("Command execution completed, output length: {Length} characters", output.Length);
                    m_Logger.LogDebug("Command output: {Output}", output);
                    
                    return Task.FromResult(output);
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to execute command: {Command}", command);
                return Task.FromResult($"Command execution failed: {ex.Message}");
            }
        }

        public Task<bool> StopSession()
        {
            m_Logger.LogInformation("StopSession called");
            
            try
            {
                lock (m_SessionLock)
                {
                    m_Logger.LogDebug("Acquired session lock for StopSession");
                    
                    if (!m_IsActive)
                    {
                        m_Logger.LogWarning("No active session to stop");
                        return Task.FromResult(false);
                    }

                    m_Logger.LogInformation("Stopping CDB session...");

                    if (m_DebuggerProcess != null && !m_DebuggerProcess.HasExited)
                    {
                        m_Logger.LogDebug("Sending quit command to CDB...");
                        // Send quit command
                        m_DebuggerInput?.WriteLine("q");
                        m_DebuggerInput?.Flush();

                        m_Logger.LogDebug("Waiting for CDB process to exit gracefully...");
                        // Wait for process to exit
                        if (!m_DebuggerProcess.WaitForExit(5000))
                        {
                            m_Logger.LogWarning("CDB process did not exit gracefully, forcing termination");
                            m_DebuggerProcess.Kill();
                        }
                        else
                        {
                            m_Logger.LogInformation("CDB process exited gracefully");
                        }
                    }
                    else
                    {
                        m_Logger.LogInformation("CDB process already exited or is null");
                    }

                    m_Logger.LogDebug("Disposing of CDB resources...");
                    m_DebuggerProcess?.Dispose();
                    m_DebuggerInput?.Dispose();
                    m_DebuggerOutput?.Dispose();
                    m_DebuggerError?.Dispose();

                    m_DebuggerProcess = null;
                    m_DebuggerInput = null;
                    m_DebuggerOutput = null;
                    m_DebuggerError = null;
                    m_IsActive = false;
                    
                    m_Logger.LogInformation("CDB session resources cleaned up");
                }

                m_Logger.LogInformation("CDB session stopped successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to stop CDB session");
                return Task.FromResult(false);
            }
        }

        private string ReadDebuggerOutput(int timeoutMs)
        {
            m_Logger.LogDebug("ReadDebuggerOutput called with timeout: {TimeoutMs}ms", timeoutMs);
            
            if (m_DebuggerOutput == null)
            {
                m_Logger.LogError("No output stream available for reading");
                return "No output stream available";
            }

            var output = new StringBuilder();
            var startTime = DateTime.Now;
            var linesRead = 0;
            var lastOutputTime = startTime;

            try
            {
                m_Logger.LogDebug("Starting to read debugger output...");
                
                while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
                {
                    if (m_DebuggerOutput.Peek() == -1)
                    {
                        // Check if we've been waiting too long without any output
                        if ((DateTime.Now - lastOutputTime).TotalMilliseconds > 5000)
                        {
                            m_Logger.LogWarning("No output received for 5 seconds, continuing to wait...");
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
                        m_Logger.LogTrace("Read line {LineNumber}: {Line}", linesRead, line);

                        // Check for command completion indicators
                        if (IsCommandComplete(line))
                        {
                            m_Logger.LogDebug("Command completion detected in line: {Line}", line);
                            break;
                        }
                    }
                }
                
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                m_Logger.LogDebug("Finished reading debugger output after {ElapsedMs}ms, read {LinesRead} lines", elapsed, linesRead);
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error reading debugger output after {ElapsedMs}ms", (DateTime.Now - startTime).TotalMilliseconds);
                output.AppendLine($"Error reading output: {ex.Message}");
            }

            var result = output.ToString();
            m_Logger.LogDebug("ReadDebuggerOutput returning {Length} characters", result.Length);
            return result;
        }

        private bool IsCommandComplete(string line)
        {
            // CDB typically shows "0:000>" prompt when ready for next command
            var isComplete = line.Contains(">") && Regex.IsMatch(line, @"\d+:\d+>");
            m_Logger.LogTrace("IsCommandComplete checking line: '{Line}' -> {IsComplete}", line, isComplete);
            return isComplete;
        }

        private string GetCurrentArchitecture()
        {
            var architecture = RuntimeInformation.ProcessArchitecture;
            m_Logger.LogDebug("Detected process architecture: {Architecture}", architecture);
            
            return architecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                _ => "x64" // Default to x64 for unknown architectures
            };
        }

        private string FindCDBPath()
        {
            m_Logger.LogDebug("FindCDBPath called - searching for CDB executable");
            
            // 1. Check custom path provided via --cdb-path parameter
            if (!string.IsNullOrEmpty(m_CustomCdbPath))
            {
                m_Logger.LogInformation("Using custom CDB path from --cdb-path parameter: {Path}", m_CustomCdbPath);
                if (File.Exists(m_CustomCdbPath))
                {
                    m_Logger.LogInformation("Custom CDB path verified: {Path}", m_CustomCdbPath);
                    return m_CustomCdbPath;
                }
                else
                {
                    m_Logger.LogWarning("Custom CDB path does not exist: {Path}", m_CustomCdbPath);
                }
            }
            
            // 2. Continue with automatic path detection
            var currentArch = GetCurrentArchitecture();
            m_Logger.LogInformation("Current machine architecture: {Architecture}", currentArch);
            
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

            m_Logger.LogDebug("Checking {Count} prioritized CDB paths (current arch: {Architecture})", possiblePaths.Count, currentArch);
            foreach (var path in possiblePaths)
            {
                m_Logger.LogTrace("Checking path: {Path}", path);
                if (File.Exists(path))
                {
                    m_Logger.LogInformation("Found CDB at path: {Path} (architecture-aware selection)", path);
                    return path;
                }
            }

            m_Logger.LogDebug("CDB not found in standard paths, searching PATH...");
            // Try to find in PATH
            try
            {
                var result = Process.Start(new ProcessStartInfo
                {
                    FileName = "where",
                    Arguments = "cdb.exe",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (result != null)
                {
                    var output = result.StandardOutput.ReadToEnd();
                    result.WaitForExit();

                    m_Logger.LogDebug("'where cdb.exe' command exit code: {ExitCode}", result.ExitCode);
                    
                    if (result.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        m_Logger.LogDebug("Found {Count} CDB paths in PATH", lines.Length);
                        
                        if (lines.Length > 0)
                        {
                            var cdbPath = lines[0].Trim();
                            m_Logger.LogInformation("Found CDB in PATH: {Path}", cdbPath);
                            return cdbPath;
                        }
                    }
                    else
                    {
                        m_Logger.LogDebug("'where cdb.exe' found no results");
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogDebug(ex, "Error searching for CDB in PATH");
            }

            m_Logger.LogError("CDB executable not found in any standard location or PATH");
            return string.Empty;
        }

        public void Dispose()
        {
            m_Logger.LogDebug("Dispose called on CdbSession");
            
            if (m_IsActive)
            {
                m_Logger.LogInformation("Disposing active CDB session...");
                StopSession().Wait();
            }
            else
            {
                m_Logger.LogDebug("No active session to dispose");
            }
            
            m_Logger.LogDebug("CdbSession disposal completed");
        }
    }
}
