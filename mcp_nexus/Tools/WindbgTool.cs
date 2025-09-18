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
            m_Logger.LogInformation("OpenWindbgDump called with dumpPath: {DumpPath}, symbolsPath: {SymbolsPath}", dumpPath, symbolsPath);
            
            try
            {
                // Validate dump path
                if (string.IsNullOrWhiteSpace(dumpPath))
                {
                    m_Logger.LogError("Dump path is null or empty");
                    return "Dump path cannot be null or empty";
                }

                m_Logger.LogDebug("Checking if dump file exists: {DumpPath}", dumpPath);
                if (!File.Exists(dumpPath))
                {
                    m_Logger.LogError("Dump file not found: {DumpPath}", dumpPath);
                    return $"Dump file not found: {dumpPath}";
                }

                var fileInfo = new FileInfo(dumpPath);
                m_Logger.LogInformation("Dump file found - Size: {Size} bytes, LastModified: {LastModified}", fileInfo.Length, fileInfo.LastWriteTime);

                // Check if symbols path is valid if provided
                if (!string.IsNullOrEmpty(symbolsPath))
                {
                    m_Logger.LogDebug("Validating symbols path: {SymbolsPath}", symbolsPath);
                    if (!Directory.Exists(symbolsPath))
                    {
                        m_Logger.LogWarning("Symbols path does not exist: {SymbolsPath}", symbolsPath);
                    }
                    else
                    {
                        m_Logger.LogInformation("Symbols path validated: {SymbolsPath}", symbolsPath);
                    }
                }

                var target = symbolsPath != null ? $"-y \"{symbolsPath}\" \"{dumpPath}\"" : $"\"{dumpPath}\"";
                m_Logger.LogDebug("CDB target arguments: {Target}", target);
                
                m_Logger.LogInformation("Starting CDB session for crash dump analysis...");
                var success = await m_CdbSession.StartSession(target);
                
                if (success)
                {
                    m_Logger.LogInformation("Successfully opened crash dump: {DumpPath}", dumpPath);
                    return $"Successfully opened crash dump: {dumpPath}";
                }
                else
                {
                    m_Logger.LogError("Failed to start CDB session for crash dump: {DumpPath}", dumpPath);
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
            m_Logger.LogInformation("CloseWindbgDump called");
            
            try
            {
                m_Logger.LogDebug("Checking if session is active before closing...");
                if (!m_CdbSession.IsActive)
                {
                    m_Logger.LogWarning("No active session to close");
                    return "No active session to close";
                }

                m_Logger.LogInformation("Stopping CDB session...");
                var success = await m_CdbSession.StopSession();
                
                if (success)
                {
                    m_Logger.LogInformation("Successfully closed crash dump session");
                    return "Successfully closed crash dump session";
                }
                else
                {
                    m_Logger.LogError("Failed to close crash dump session");
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
            m_Logger.LogInformation("OpenWindbgRemote called with connectionString: {ConnectionString}, symbolsPath: {SymbolsPath}", connectionString, symbolsPath);
            
            try
            {
                // Validate connection string
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    m_Logger.LogError("Connection string is null or empty");
                    return "Connection string cannot be null or empty";
                }

                // Check if symbols path is valid if provided
                if (!string.IsNullOrEmpty(symbolsPath))
                {
                    m_Logger.LogDebug("Validating symbols path: {SymbolsPath}", symbolsPath);
                    if (!Directory.Exists(symbolsPath))
                    {
                        m_Logger.LogWarning("Symbols path does not exist: {SymbolsPath}", symbolsPath);
                    }
                    else
                    {
                        m_Logger.LogInformation("Symbols path validated: {SymbolsPath}", symbolsPath);
                    }
                }

                var target = symbolsPath != null ? $"-y \"{symbolsPath}\" -remote {connectionString}" : $"-remote {connectionString}";
                m_Logger.LogDebug("CDB target arguments for remote connection: {Target}", target);
                
                m_Logger.LogInformation("Starting CDB session for remote debugging...");
                var success = await m_CdbSession.StartSession(target);
                
                if (success)
                {
                    m_Logger.LogInformation("Successfully connected to remote debugging session: {ConnectionString}", connectionString);
                    return $"Successfully connected to remote debugging session: {connectionString}";
                }
                else
                {
                    m_Logger.LogError("Failed to start CDB session for remote debugging: {ConnectionString}", connectionString);
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
            m_Logger.LogInformation("CloseWindbgRemote called");
            
            try
            {
                m_Logger.LogDebug("Checking if session is active before disconnecting...");
                if (!m_CdbSession.IsActive)
                {
                    m_Logger.LogWarning("No active session to disconnect from");
                    return "No active session to disconnect from";
                }

                m_Logger.LogInformation("Stopping CDB remote session...");
                var success = await m_CdbSession.StopSession();
                
                if (success)
                {
                    m_Logger.LogInformation("Successfully disconnected from remote debugging session");
                    return "Successfully disconnected from remote debugging session";
                }
                else
                {
                    m_Logger.LogError("Failed to disconnect from remote debugging session");
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
            m_Logger.LogInformation("RunWindbgCmd called with command: {Command}", command);
            
            try
            {
                // Validate command
                if (string.IsNullOrWhiteSpace(command))
                {
                    m_Logger.LogError("Command is null or empty");
                    return "Command cannot be null or empty";
                }

                m_Logger.LogDebug("Checking if CDB session is active...");
                m_Logger.LogInformation("CdbSession.IsActive: {IsActive}", m_CdbSession.IsActive);
                
                if (!m_CdbSession.IsActive)
                {
                    m_Logger.LogWarning("No active debugging session - cannot execute command");
                    return "No active debugging session. Please open a crash dump or connect to a remote session first.";
                }

                m_Logger.LogInformation("Executing WinDBG command: {Command}", command);
                var result = await m_CdbSession.ExecuteCommand(command);
                
                m_Logger.LogInformation("Command execution completed, result length: {Length} characters", result.Length);
                m_Logger.LogDebug("Command result: {Result}", result);
                
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
            m_Logger.LogInformation("ListWindbgDumps called with directoryPath: {DirectoryPath}", directoryPath);
            
            try
            {
                // Validate directory path
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    m_Logger.LogError("Directory path is null or empty");
                    return Task.FromResult("Directory path cannot be null or empty");
                }

                m_Logger.LogDebug("Checking if directory exists: {DirectoryPath}", directoryPath);
                if (!Directory.Exists(directoryPath))
                {
                    m_Logger.LogError("Directory not found: {DirectoryPath}", directoryPath);
                    return Task.FromResult($"Directory not found: {directoryPath}");
                }

                m_Logger.LogInformation("Searching for .dmp files in directory: {DirectoryPath}", directoryPath);
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

                m_Logger.LogInformation("Found {Count} .dmp files in directory: {DirectoryPath}", dumpFiles.Count, directoryPath);

                if (dumpFiles.Count == 0)
                {
                    m_Logger.LogInformation("No .dmp files found in directory: {DirectoryPath}", directoryPath);
                    return Task.FromResult($"No .dmp files found in directory: {directoryPath}");
                }

                var result = new StringBuilder();
                result.AppendLine($"Found {dumpFiles.Count} dump files in {directoryPath}:");
                result.AppendLine();

                foreach (var dump in dumpFiles)
                {
                    m_Logger.LogDebug("Processing dump file: {Name} ({Size} bytes)", dump.Name, dump.Size);
                    result.AppendLine($"File: {dump.Name}");
                    result.AppendLine($"Path: {dump.Path}");
                    result.AppendLine($"Size: {dump.Size:N0} bytes");
                    result.AppendLine($"Last Modified: {dump.LastModified:yyyy-MM-dd HH:mm:ss}");
                    result.AppendLine();
                }

                m_Logger.LogInformation("Successfully listed {Count} dump files in directory: {DirectoryPath}", dumpFiles.Count, directoryPath);
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
            m_Logger.LogInformation("GetSessionInfo called");
            
            try
            {
                m_Logger.LogDebug("Checking if session is active...");
                if (!m_CdbSession.IsActive)
                {
                    m_Logger.LogWarning("No active debugging session - cannot get session info");
                    return "No active debugging session.";
                }

                m_Logger.LogInformation("Gathering session information...");
                var result = new StringBuilder();
                
                // Get version info
                m_Logger.LogDebug("Getting version information...");
                var version = await m_CdbSession.ExecuteCommand("version");
                result.AppendLine("=== Version Information ===");
                result.AppendLine(version);
                result.AppendLine();

                // Get process info
                m_Logger.LogDebug("Getting process information...");
                var processInfo = await m_CdbSession.ExecuteCommand("!process 0 0");
                result.AppendLine("=== Process Information ===");
                result.AppendLine(processInfo);
                result.AppendLine();

                // Get thread info
                m_Logger.LogDebug("Getting thread information...");
                var threadInfo = await m_CdbSession.ExecuteCommand("~");
                result.AppendLine("=== Thread Information ===");
                result.AppendLine(threadInfo);

                m_Logger.LogInformation("Session information gathered successfully");
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
            m_Logger.LogInformation("AnalyzeCallStack called");
            
            try
            {
                m_Logger.LogDebug("Checking if session is active...");
                if (!m_CdbSession.IsActive)
                {
                    m_Logger.LogWarning("No active debugging session - cannot analyze call stack");
                    return "No active debugging session.";
                }

                m_Logger.LogInformation("Analyzing call stack...");
                var result = new StringBuilder();
                
                // Get call stack
                m_Logger.LogDebug("Getting call stack...");
                var callStack = await m_CdbSession.ExecuteCommand("k");
                result.AppendLine("=== Call Stack ===");
                result.AppendLine(callStack);
                result.AppendLine();

                // Get exception information if available
                m_Logger.LogDebug("Getting exception context...");
                var exceptionInfo = await m_CdbSession.ExecuteCommand(".ecxr");
                if (!exceptionInfo.Contains("No exception context available"))
                {
                    m_Logger.LogDebug("Exception context available, adding to result");
                    result.AppendLine("=== Exception Context ===");
                    result.AppendLine(exceptionInfo);
                    result.AppendLine();
                }
                else
                {
                    m_Logger.LogDebug("No exception context available");
                }

                // Get registers
                m_Logger.LogDebug("Getting register information...");
                var registers = await m_CdbSession.ExecuteCommand("r");
                result.AppendLine("=== Registers ===");
                result.AppendLine(registers);

                m_Logger.LogInformation("Call stack analysis completed successfully");
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
            m_Logger.LogInformation("AnalyzeMemory called");
            
            try
            {
                m_Logger.LogDebug("Checking if session is active...");
                if (!m_CdbSession.IsActive)
                {
                    m_Logger.LogWarning("No active debugging session - cannot analyze memory");
                    return "No active debugging session.";
                }

                m_Logger.LogInformation("Analyzing memory...");
                var result = new StringBuilder();
                
                // Get virtual memory info
                m_Logger.LogDebug("Getting virtual memory information...");
                var vmemInfo = await m_CdbSession.ExecuteCommand("!vprot");
                result.AppendLine("=== Virtual Memory Information ===");
                result.AppendLine(vmemInfo);
                result.AppendLine();

                // Get heap information
                m_Logger.LogDebug("Getting heap information...");
                var heapInfo = await m_CdbSession.ExecuteCommand("!heap -s");
                result.AppendLine("=== Heap Information ===");
                result.AppendLine(heapInfo);
                result.AppendLine();

                // Get loaded modules
                m_Logger.LogDebug("Getting loaded modules information...");
                var modules = await m_CdbSession.ExecuteCommand("lm");
                result.AppendLine("=== Loaded Modules ===");
                result.AppendLine(modules);

                m_Logger.LogInformation("Memory analysis completed successfully");
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
            m_Logger.LogInformation("AnalyzeCrashPatterns called");
            
            try
            {
                m_Logger.LogDebug("Checking if session is active...");
                if (!m_CdbSession.IsActive)
                {
                    m_Logger.LogWarning("No active debugging session - cannot analyze crash patterns");
                    return "No active debugging session.";
                }

                m_Logger.LogInformation("Analyzing crash patterns...");
                var result = new StringBuilder();
                result.AppendLine("=== Crash Pattern Analysis ===");
                result.AppendLine();

                // Check for access violations
                m_Logger.LogDebug("Checking for access violations...");
                var exceptionInfo = await m_CdbSession.ExecuteCommand(".ecxr");
                if (exceptionInfo.Contains("Access violation"))
                {
                    m_Logger.LogWarning("Access violation detected in exception context");
                    result.AppendLine("⚠️  ACCESS VIOLATION DETECTED");
                    result.AppendLine("This is typically caused by:");
                    result.AppendLine("- Dereferencing null or invalid pointers");
                    result.AppendLine("- Buffer overruns or underruns");
                    result.AppendLine("- Use after free");
                    result.AppendLine("- Stack corruption");
                    result.AppendLine();
                }
                else
                {
                    m_Logger.LogDebug("No access violation detected");
                }

                // Check for stack overflow
                m_Logger.LogDebug("Checking for stack overflow...");
                var callStack = await m_CdbSession.ExecuteCommand("k");
                if (callStack.Contains("Stack overflow") || callStack.Split('\n').Length > 100)
                {
                    m_Logger.LogWarning("Potential stack overflow detected");
                    result.AppendLine("⚠️  POTENTIAL STACK OVERFLOW");
                    result.AppendLine("This may be caused by:");
                    result.AppendLine("- Infinite recursion");
                    result.AppendLine("- Large local variables on stack");
                    result.AppendLine("- Deep call chains");
                    result.AppendLine();
                }
                else
                {
                    m_Logger.LogDebug("No stack overflow detected");
                }

                // Check for heap corruption
                m_Logger.LogDebug("Checking for heap corruption...");
                var heapInfo = await m_CdbSession.ExecuteCommand("!heap -s");
                if (heapInfo.Contains("corrupted") || heapInfo.Contains("invalid"))
                {
                    m_Logger.LogWarning("Heap corruption detected");
                    result.AppendLine("⚠️  HEAP CORRUPTION DETECTED");
                    result.AppendLine("This may be caused by:");
                    result.AppendLine("- Buffer overruns");
                    result.AppendLine("- Double free");
                    result.AppendLine("- Use after free");
                    result.AppendLine("- Memory leaks");
                    result.AppendLine();
                }
                else
                {
                    m_Logger.LogDebug("No heap corruption detected");
                }

                // Check for deadlocks
                m_Logger.LogDebug("Checking for deadlocks...");
                var threadInfo = await m_CdbSession.ExecuteCommand("~*k");
                if (threadInfo.Contains("WaitForSingleObject") || threadInfo.Contains("WaitForMultipleObjects"))
                {
                    m_Logger.LogWarning("Potential deadlock detected");
                    result.AppendLine("⚠️  POTENTIAL DEADLOCK DETECTED");
                    result.AppendLine("Threads appear to be waiting for synchronization objects");
                    result.AppendLine();
                }
                else
                {
                    m_Logger.LogDebug("No deadlock patterns detected");
                }

                if (!result.ToString().Contains("⚠️"))
                {
                    m_Logger.LogInformation("No obvious crash patterns detected");
                    result.AppendLine("✅ No obvious crash patterns detected");
                    result.AppendLine("Consider analyzing the call stack and exception context manually");
                }

                m_Logger.LogInformation("Crash pattern analysis completed successfully");
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
            m_Logger.LogInformation("GetCurrentTime called with city: {City}", city);
            
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    m_Logger.LogError("City parameter is null or empty");
                    return "City cannot be null or empty";
                }

                var currentTime = DateTime.Now;
                var timeString = $"It is {currentTime.Hour}:{currentTime.Minute} in {city}.";
                
                m_Logger.LogInformation("Successfully generated time for city: {City} -> {TimeString}", city, timeString);
                return timeString;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting current time for city: {City}", city);
                return $"Error getting current time: {ex.Message}";
            }
        }
    }
}
