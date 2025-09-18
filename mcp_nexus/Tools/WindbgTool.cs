using mcp_nexus.Helper;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class WindbgTool
    {
        private readonly ILogger<WindbgTool> m_Logger;
        private readonly CdbSession m_CdbSession;

        public WindbgTool(ILogger<WindbgTool> logger, CdbSession cdbSession)
        {
            m_Logger = logger;
            m_CdbSession = cdbSession;
        }

        // Crash Dump Analysis Tools

        [McpServerTool, Description("Analyze a Windows crash dump file using common WinDBG commands")]
        public async Task<string> OpenWindbgDump(string dumpPath, string? symbolsPath = null)
        {
            try
            {
                if (!File.Exists(dumpPath))
                {
                    return $"Dump file not found: {dumpPath}";
                }

                var target = symbolsPath != null ? $"-y \"{symbolsPath}\" \"{dumpPath}\"" : $"\"{dumpPath}\"";
                var success = await m_CdbSession.StartSession(target);
                
                if (success)
                {
                    m_Logger.LogInformation("Opened crash dump: {DumpPath}", dumpPath);
                    return $"Successfully opened crash dump: {dumpPath}";
                }
                else
                {
                    return $"Failed to open crash dump: {dumpPath}";
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error opening crash dump: {DumpPath}", dumpPath);
                return $"Error opening crash dump: {ex.Message}";
            }
        }

        [McpServerTool, Description("Unload a crash dump and release resources")]
        public async Task<string> CloseWindbgDump()
        {
            try
            {
                var success = await m_CdbSession.StopSession();
                
                if (success)
                {
                    m_Logger.LogInformation("Closed crash dump session");
                    return "Successfully closed crash dump session";
                }
                else
                {
                    return "Failed to close crash dump session or no session was active";
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error closing crash dump session");
                return $"Error closing crash dump session: {ex.Message}";
            }
        }

        // Remote Debugging Tools

        [McpServerTool, Description("Connect to a remote debugging session using a connection string (e.g., tcp:Port=5005,Server=192.168.0.100)")]
        public async Task<string> OpenWindbgRemote(string connectionString, string? symbolsPath = null)
        {
            try
            {
                var target = symbolsPath != null ? $"-y \"{symbolsPath}\" -remote {connectionString}" : $"-remote {connectionString}";
                var success = await m_CdbSession.StartSession(target);
                
                if (success)
                {
                    m_Logger.LogInformation("Connected to remote debugging session: {ConnectionString}", connectionString);
                    return $"Successfully connected to remote debugging session: {connectionString}";
                }
                else
                {
                    return $"Failed to connect to remote debugging session: {connectionString}";
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error connecting to remote debugging session: {ConnectionString}", connectionString);
                return $"Error connecting to remote debugging session: {ex.Message}";
            }
        }

        [McpServerTool, Description("Disconnect from a remote debugging session and release resources")]
        public async Task<string> CloseWindbgRemote()
        {
            try
            {
                var success = await m_CdbSession.StopSession();
                
                if (success)
                {
                    m_Logger.LogInformation("Disconnected from remote debugging session");
                    return "Successfully disconnected from remote debugging session";
                }
                else
                {
                    return "Failed to disconnect from remote debugging session or no session was active";
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error disconnecting from remote debugging session");
                return $"Error disconnecting from remote debugging session: {ex.Message}";
            }
        }

        // General Commands

        [McpServerTool, Description("Execute a specific WinDBG command on either a loaded crash dump or active remote session")]
        public async Task<string> RunWindbgCmd(string command)
        {
            try
            {
                if (!m_CdbSession.IsActive)
                {
                    return "No active debugging session. Please open a crash dump or connect to a remote session first.";
                }

                m_Logger.LogInformation("Executing WinDBG command: {Command}", command);
                var result = await m_CdbSession.ExecuteCommand(command);
                return result;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error executing WinDBG command: {Command}", command);
                return $"Error executing command: {ex.Message}";
            }
        }

        [McpServerTool, Description("List Windows crash dump (.dmp) files in the specified directory")]
        public Task<string> ListWindbgDumps(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    return Task.FromResult($"Directory not found: {directoryPath}");
                }

                var dumpFiles = Directory.GetFiles(directoryPath, "*.dmp", SearchOption.AllDirectories)
                    .Select(file => new FileInfo(file))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new
                    {
                        Name = f.Name,
                        Path = f.FullName,
                        Size = f.Length,
                        LastModified = f.LastWriteTime
                    })
                    .ToList();

                if (dumpFiles.Count == 0)
                {
                    return Task.FromResult($"No .dmp files found in directory: {directoryPath}");
                }

                var result = new StringBuilder();
                result.AppendLine($"Found {dumpFiles.Count} dump files in {directoryPath}:");
                result.AppendLine();

                foreach (var dump in dumpFiles)
                {
                    result.AppendLine($"File: {dump.Name}");
                    result.AppendLine($"Path: {dump.Path}");
                    result.AppendLine($"Size: {dump.Size:N0} bytes");
                    result.AppendLine($"Last Modified: {dump.LastModified:yyyy-MM-dd HH:mm:ss}");
                    result.AppendLine();
                }

                m_Logger.LogInformation("Listed {Count} dump files in directory: {DirectoryPath}", dumpFiles.Count, directoryPath);
                return Task.FromResult(result.ToString());
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error listing dump files in directory: {DirectoryPath}", directoryPath);
                return Task.FromResult($"Error listing dump files: {ex.Message}");
            }
        }

        // Additional Analysis Tools

        [McpServerTool, Description("Get basic information about the current debugging session")]
        public async Task<string> GetSessionInfo()
        {
            try
            {
                if (!m_CdbSession.IsActive)
                {
                    return "No active debugging session.";
                }

                var result = new StringBuilder();
                
                // Get version info
                var version = await m_CdbSession.ExecuteCommand("version");
                result.AppendLine("=== Version Information ===");
                result.AppendLine(version);
                result.AppendLine();

                // Get process info
                var processInfo = await m_CdbSession.ExecuteCommand("!process 0 0");
                result.AppendLine("=== Process Information ===");
                result.AppendLine(processInfo);
                result.AppendLine();

                // Get thread info
                var threadInfo = await m_CdbSession.ExecuteCommand("~");
                result.AppendLine("=== Thread Information ===");
                result.AppendLine(threadInfo);

                return result.ToString();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting session information");
                return $"Error getting session information: {ex.Message}";
            }
        }

        [McpServerTool, Description("Analyze the current call stack with detailed information")]
        public async Task<string> AnalyzeCallStack()
        {
            try
            {
                if (!m_CdbSession.IsActive)
                {
                    return "No active debugging session.";
                }

                var result = new StringBuilder();
                
                // Get call stack
                var callStack = await m_CdbSession.ExecuteCommand("k");
                result.AppendLine("=== Call Stack ===");
                result.AppendLine(callStack);
                result.AppendLine();

                // Get exception information if available
                var exceptionInfo = await m_CdbSession.ExecuteCommand(".ecxr");
                if (!exceptionInfo.Contains("No exception context available"))
                {
                    result.AppendLine("=== Exception Context ===");
                    result.AppendLine(exceptionInfo);
                    result.AppendLine();
                }

                // Get registers
                var registers = await m_CdbSession.ExecuteCommand("r");
                result.AppendLine("=== Registers ===");
                result.AppendLine(registers);

                return result.ToString();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error analyzing call stack");
                return $"Error analyzing call stack: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get memory information and analyze memory usage")]
        public async Task<string> AnalyzeMemory()
        {
            try
            {
                if (!m_CdbSession.IsActive)
                {
                    return "No active debugging session.";
                }

                var result = new StringBuilder();
                
                // Get virtual memory info
                var vmemInfo = await m_CdbSession.ExecuteCommand("!vprot");
                result.AppendLine("=== Virtual Memory Information ===");
                result.AppendLine(vmemInfo);
                result.AppendLine();

                // Get heap information
                var heapInfo = await m_CdbSession.ExecuteCommand("!heap -s");
                result.AppendLine("=== Heap Information ===");
                result.AppendLine(heapInfo);
                result.AppendLine();

                // Get loaded modules
                var modules = await m_CdbSession.ExecuteCommand("lm");
                result.AppendLine("=== Loaded Modules ===");
                result.AppendLine(modules);

                return result.ToString();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error analyzing memory");
                return $"Error analyzing memory: {ex.Message}";
            }
        }

        [McpServerTool, Description("Check for common crash patterns and provide analysis")]
        public async Task<string> AnalyzeCrashPatterns()
        {
            try
            {
                if (!m_CdbSession.IsActive)
                {
                    return "No active debugging session.";
                }

                var result = new StringBuilder();
                result.AppendLine("=== Crash Pattern Analysis ===");
                result.AppendLine();

                // Check for access violations
                var exceptionInfo = await m_CdbSession.ExecuteCommand(".ecxr");
                if (exceptionInfo.Contains("Access violation"))
                {
                    result.AppendLine("⚠️  ACCESS VIOLATION DETECTED");
                    result.AppendLine("This is typically caused by:");
                    result.AppendLine("- Dereferencing null or invalid pointers");
                    result.AppendLine("- Buffer overruns or underruns");
                    result.AppendLine("- Use after free");
                    result.AppendLine("- Stack corruption");
                    result.AppendLine();
                }

                // Check for stack overflow
                var callStack = await m_CdbSession.ExecuteCommand("k");
                if (callStack.Contains("Stack overflow") || callStack.Split('\n').Length > 100)
                {
                    result.AppendLine("⚠️  POTENTIAL STACK OVERFLOW");
                    result.AppendLine("This may be caused by:");
                    result.AppendLine("- Infinite recursion");
                    result.AppendLine("- Large local variables on stack");
                    result.AppendLine("- Deep call chains");
                    result.AppendLine();
                }

                // Check for heap corruption
                var heapInfo = await m_CdbSession.ExecuteCommand("!heap -s");
                if (heapInfo.Contains("corrupted") || heapInfo.Contains("invalid"))
                {
                    result.AppendLine("⚠️  HEAP CORRUPTION DETECTED");
                    result.AppendLine("This may be caused by:");
                    result.AppendLine("- Buffer overruns");
                    result.AppendLine("- Double free");
                    result.AppendLine("- Use after free");
                    result.AppendLine("- Memory leaks");
                    result.AppendLine();
                }

                // Check for deadlocks
                var threadInfo = await m_CdbSession.ExecuteCommand("~*k");
                if (threadInfo.Contains("WaitForSingleObject") || threadInfo.Contains("WaitForMultipleObjects"))
                {
                    result.AppendLine("⚠️  POTENTIAL DEADLOCK DETECTED");
                    result.AppendLine("Threads appear to be waiting for synchronization objects");
                    result.AppendLine();
                }

                if (!result.ToString().Contains("⚠️"))
                {
                    result.AppendLine("✅ No obvious crash patterns detected");
                    result.AppendLine("Consider analyzing the call stack and exception context manually");
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error analyzing crash patterns");
                return $"Error analyzing crash patterns: {ex.Message}";
            }
        }

        // Legacy method for backward compatibility
        [McpServerTool, Description("Gets the current time for a city (legacy method)")]
        public string GetCurrentTime(string city)
        {
            m_Logger.LogInformation("LLM requested the time for city: {City}", city);
            return $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
        }
    }
}
