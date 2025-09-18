using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

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

        public CdbSession(ILogger<CdbSession> logger, int commandTimeoutMs = 30000)
        {
            m_Logger = logger;
            m_CommandTimeoutMs = commandTimeoutMs;
        }

        public Task<bool> StartSession(string target, string? arguments = null)
        {
            try
            {
                lock (m_SessionLock)
                {
                    if (m_IsActive)
                    {
                        m_Logger.LogWarning("Session is already active");
                        return Task.FromResult(false);
                    }

                    var cdbPath = FindCDBPath();
                    if (string.IsNullOrEmpty(cdbPath))
                    {
                        m_Logger.LogError("CDB.exe not found. Please ensure Windows Debugging Tools are installed.");
                        return Task.FromResult(false);
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cdbPath,
                        Arguments = $"-c \"g\" {target}",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    m_DebuggerProcess = new Process { StartInfo = startInfo };
                    m_DebuggerProcess.Start();

                    m_DebuggerInput = m_DebuggerProcess.StandardInput;
                    m_DebuggerOutput = m_DebuggerProcess.StandardOutput;
                    m_DebuggerError = m_DebuggerProcess.StandardError;

                    m_IsActive = true;
                }

                m_Logger.LogInformation("Started CDB session with target: {Target}", target);
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
            try
            {
                lock (m_SessionLock)
                {
                    if (!m_IsActive || m_DebuggerProcess?.HasExited == true)
                    {
                        return Task.FromResult("No active debug session. Please start a session first.");
                    }

                    if (m_DebuggerInput == null)
                    {
                        return Task.FromResult("Debug session input stream is not available.");
                    }

                    m_Logger.LogInformation("Executing CDB command: {Command}", command);

                    // Send command to debugger
                    m_DebuggerInput.WriteLine(command);
                    m_DebuggerInput.Flush();

                    // Read output with timeout
                    var output = ReadDebuggerOutput(m_CommandTimeoutMs);
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
            try
            {
                lock (m_SessionLock)
                {
                    if (!m_IsActive)
                    {
                        m_Logger.LogWarning("No active session to stop");
                        return Task.FromResult(false);
                    }

                    if (m_DebuggerProcess != null && !m_DebuggerProcess.HasExited)
                    {
                        // Send quit command
                        m_DebuggerInput?.WriteLine("q");
                        m_DebuggerInput?.Flush();

                        // Wait for process to exit
                        if (!m_DebuggerProcess.WaitForExit(5000))
                        {
                            m_DebuggerProcess.Kill();
                        }
                    }

                    m_DebuggerProcess?.Dispose();
                    m_DebuggerInput?.Dispose();
                    m_DebuggerOutput?.Dispose();
                    m_DebuggerError?.Dispose();

                    m_DebuggerProcess = null;
                    m_DebuggerInput = null;
                    m_DebuggerOutput = null;
                    m_DebuggerError = null;
                    m_IsActive = false;
                }

                m_Logger.LogInformation("CDB session stopped");
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
            if (m_DebuggerOutput == null)
                return "No output stream available";

            var output = new StringBuilder();
            var startTime = DateTime.Now;

            try
            {
                while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
                {
                    if (m_DebuggerOutput.Peek() == -1)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    var line = m_DebuggerOutput.ReadLine();
                    if (line != null)
                    {
                        output.AppendLine(line);

                        // Check for command completion indicators
                        if (IsCommandComplete(line))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogWarning(ex, "Error reading debugger output");
                output.AppendLine($"Error reading output: {ex.Message}");
            }

            return output.ToString();
        }

        private bool IsCommandComplete(string line)
        {
            // CDB typically shows "0:000>" prompt when ready for next command
            return line.Contains(">") && Regex.IsMatch(line, @"\d+:\d+>");
        }

        private string FindCDBPath()
        {
            var possiblePaths = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe",
                @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe",
                @"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe",
                @"C:\Program Files\Windows Kits\10\Debuggers\x86\cdb.exe",
                @"C:\Program Files (x86)\Debugging Tools for Windows (x64)\cdb.exe",
                @"C:\Program Files (x86)\Debugging Tools for Windows (x86)\cdb.exe",
                @"C:\Program Files\Debugging Tools for Windows (x64)\cdb.exe",
                @"C:\Program Files\Debugging Tools for Windows (x86)\cdb.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

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

                    if (result.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            return lines[0].Trim();
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors when searching PATH
            }

            return string.Empty;
        }

        public void Dispose()
        {
            if (m_IsActive)
            {
                StopSession().Wait();
            }
        }
    }
}
