using mcp_nexus.Helper;
using mcp_nexus.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class WindbgTool(ILogger<WindbgTool> logger, CdbSession cdbSession, BackgroundJobService backgroundJobService)
    {
        // Crash Dump Analysis Tools

        [McpServerTool, Description("Analyze a Windows crash dump file using common WinDBG commands")]
        public async Task<string> OpenWindbgDump(string dumpPath, string? symbolsPath = null)
        {
            logger.LogDebug("OpenWindbgDump called with dumpPath: {DumpPath}, symbolsPath: {SymbolsPath}", dumpPath, symbolsPath);

            try
            {
                // Validate dump path
                if (string.IsNullOrWhiteSpace(dumpPath))
                {
                    logger.LogError("Dump path is null or empty");
                    return "Dump path cannot be null or empty";
                }

                logger.LogDebug("Checking if dump file exists: {DumpPath}", dumpPath);
                if (!File.Exists(dumpPath))
                {
                    logger.LogError("Dump file not found: {DumpPath}", dumpPath);
                    return $"Dump file not found: {dumpPath}";
                }

                var fileInfo = new FileInfo(dumpPath);
                logger.LogDebug("Dump file found - Size: {Size} bytes, LastModified: {LastModified}", fileInfo.Length, fileInfo.LastWriteTime);

                // Check if symbols path is valid if provided
                if (!string.IsNullOrEmpty(symbolsPath))
                {
                    logger.LogDebug("Validating symbols path: {SymbolsPath}", symbolsPath);
                    if (!Directory.Exists(symbolsPath))
                    {
                        logger.LogWarning("Symbols path does not exist: {SymbolsPath}", symbolsPath);
                    }
                    else
                    {
                        logger.LogInformation("Symbols path validated: {SymbolsPath}", symbolsPath);
                    }
                }

                var target = symbolsPath != null ? $"-y \"{symbolsPath}\" -z \"{dumpPath}\"" : $"-z \"{dumpPath}\"";
                logger.LogDebug("CDB target arguments: {Target}", target);

                logger.LogInformation("Starting CDB session for crash dump analysis...");
                var success = await cdbSession.StartSession(target);

                if (success)
                {
                    logger.LogInformation("Successfully opened crash dump: {DumpPath}", dumpPath);
                    return $"Successfully opened crash dump: {dumpPath}";
                }
                else
                {
                    logger.LogError("Failed to start CDB session for crash dump: {DumpPath}", dumpPath);
                    return $"Failed to open crash dump: {dumpPath}";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error opening crash dump: {DumpPath}", dumpPath);
                return $"Error opening crash dump: {ex.Message}";
            }
        }

        [McpServerTool, Description("Unload a crash dump and release resources")]
        public async Task<string> CloseWindbgDump()
        {
            logger.LogInformation("CloseWindbgDump called");

            try
            {
                logger.LogDebug("Checking if session is active before closing...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active session to close");
                    return "No active session to close";
                }

                logger.LogInformation("Stopping CDB session...");
                var success = await cdbSession.StopSession();

                if (success)
                {
                    logger.LogInformation("Successfully closed crash dump session");
                    return "Successfully closed crash dump session";
                }
                else
                {
                    logger.LogError("Failed to close crash dump session");
                    return "Failed to close crash dump session or no session was active";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing crash dump session");
                return $"Error closing crash dump session: {ex.Message}";
            }
        }

        // Remote Debugging Tools

        [McpServerTool, Description("Connect to a remote debugging session using a connection string (e.g., tcp:Port=5005,Server=192.168.0.100)")]
        public async Task<string> OpenWindbgRemote(string connectionString, string? symbolsPath = null)
        {
            logger.LogInformation("OpenWindbgRemote called with connectionString: {ConnectionString}, symbolsPath: {SymbolsPath}", connectionString, symbolsPath);

            try
            {
                // Validate connection string
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    logger.LogError("Connection string is null or empty");
                    return "Connection string cannot be null or empty";
                }

                // Check if symbols path is valid if provided
                if (!string.IsNullOrEmpty(symbolsPath))
                {
                    logger.LogDebug("Validating symbols path: {SymbolsPath}", symbolsPath);
                    if (!Directory.Exists(symbolsPath))
                    {
                        logger.LogWarning("Symbols path does not exist: {SymbolsPath}", symbolsPath);
                    }
                    else
                    {
                        logger.LogInformation("Symbols path validated: {SymbolsPath}", symbolsPath);
                    }
                }

                var target = symbolsPath != null ? $"-y \"{symbolsPath}\" -remote {connectionString}" : $"-remote {connectionString}";
                logger.LogDebug("CDB target arguments for remote connection: {Target}", target);

                logger.LogInformation("Starting CDB session for remote debugging...");
                var success = await cdbSession.StartSession(target);

                if (success)
                {
                    logger.LogInformation("Successfully connected to remote debugging session: {ConnectionString}", connectionString);
                    return $"Successfully connected to remote debugging session: {connectionString}";
                }
                else
                {
                    logger.LogError("Failed to start CDB session for remote debugging: {ConnectionString}", connectionString);
                    return $"Failed to connect to remote debugging session: {connectionString}";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error connecting to remote debugging session: {ConnectionString}", connectionString);
                return $"Error connecting to remote debugging session: {ex.Message}";
            }
        }

        [McpServerTool, Description("Disconnect from a remote debugging session and release resources")]
        public async Task<string> CloseWindbgRemote()
        {
            logger.LogInformation("CloseWindbgRemote called");

            try
            {
                logger.LogDebug("Checking if session is active before disconnecting...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active session to disconnect from");
                    return "No active session to disconnect from";
                }

                logger.LogInformation("Stopping CDB remote session...");
                var success = await cdbSession.StopSession();

                if (success)
                {
                    logger.LogInformation("Successfully disconnected from remote debugging session");
                    return "Successfully disconnected from remote debugging session";
                }
                else
                {
                    logger.LogError("Failed to disconnect from remote debugging session");
                    return "Failed to disconnect from remote debugging session or no session was active";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disconnecting from remote debugging session");
                return $"Error disconnecting from remote debugging session: {ex.Message}";
            }
        }

        // General Commands


        [McpServerTool, Description("Execute any WinDBG command with smart timeout handling. HYBRID BEHAVIOR: Quick commands (<5s) return results immediately. Long commands (>5s) return job ID for polling. NO TIMEOUTS EVER. For job polling: call get_job_status(jobId) every 10-15 seconds until status='completed', then extract 'result' field. Works for ALL commands: version, lsa, !analyze -v, !process, etc.")]
        public async Task<string> RunWindbgCmdAsync(string command)
        {
            logger.LogInformation("RunWindbgCmdAsync called with command: {Command} [ASYNCHRONOUS MODE]", command);

            try
            {
                // Validate command
                if (string.IsNullOrWhiteSpace(command))
                {
                    logger.LogError("Command is null or empty in async method");
                    return "Command cannot be null or empty";
                }

                logger.LogDebug("Checking if CDB session is active for async command...");
                logger.LogInformation("CdbSession.IsActive: {IsActive}", cdbSession.IsActive);

                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session for async command - cannot execute");
                    return "No active debugging session. Please open a crash dump or connect to a remote session first.";
                }

                // Start async execution with hybrid approach
                var jobId = backgroundJobService.StartJob(command, async (cancellationToken) =>
                {
                    logger.LogInformation("Executing WinDBG command asynchronously: {Command}", command);
                    return await cdbSession.ExecuteCommand(command, cancellationToken);
                });

                logger.LogInformation("Started async job {JobId} for command: {Command}", jobId, command);

                // Hybrid approach: Wait briefly for quick completion
                await Task.Delay(5000); // Wait 5 seconds

                var job = backgroundJobService.GetJob(jobId);
                if (job is { Status: JobStatus.Completed, Result: not null })
                {
                    logger.LogInformation("Job {JobId} completed quickly, returning result directly", jobId);
                    return $@"{{
  ""status"": ""completed"",
  ""result"": ""{job.Result.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}"",
  ""message"": ""✅ COMPLETED: Command finished quickly, results ready!"",
  ""executionTime"": ""{(job.EndTime - job.StartTime)?.TotalSeconds:F1} seconds"",
  ""ai_guidance"": ""SUCCESS: The 'result' field above contains the complete command output. No further polling needed!""
}}";
                }

                // Still running, return job ID for polling
                return $@"{{
  ""jobId"": ""{jobId}"",
  ""status"": ""running"",
  ""message"": ""⏳ RUNNING: Command needs more time, polling required"",
  ""ai_guidance"": ""CRITICAL: This response contains NO RESULTS. You MUST call get_job_status(jobId='{jobId}') to get the actual command output!"",
  ""next_action"": {{
    ""required_call"": ""get_job_status"",
    ""parameters"": {{""jobId"": ""{jobId}""}},
    ""when"": ""Poll every 10-15 seconds until status='completed'"",
    ""then"": ""Extract 'result' field from completed job for the actual WinDBG output""
  }},
  ""warning"": ""The command is running in background. Results are NOT in this response!""
}}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing async WinDBG command: {Command}", command);
                return $"Error executing async command: {ex.Message}";
            }
        }

        [McpServerTool, Description("List Windows crash dump (.dmp) files in the specified directory")]
        public Task<string> ListWindbgDumps(string directoryPath)
        {
            logger.LogInformation("ListWindbgDumps called with directoryPath: {DirectoryPath}", directoryPath);

            try
            {
                // Validate directory path
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    logger.LogError("Directory path is null or empty");
                    return Task.FromResult("Directory path cannot be null or empty");
                }

                logger.LogDebug("Checking if directory exists: {DirectoryPath}", directoryPath);
                if (!Directory.Exists(directoryPath))
                {
                    logger.LogError("Directory not found: {DirectoryPath}", directoryPath);
                    return Task.FromResult($"Directory not found: {directoryPath}");
                }

                logger.LogInformation("Searching for .dmp files in directory: {DirectoryPath}", directoryPath);
                var dumpFiles = Directory.GetFiles(directoryPath, "*.dmp", SearchOption.AllDirectories)
                    .Select(file => new FileInfo(file))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new
                    {
                        f.Name,
                        Path = f.FullName,
                        Size = f.Length,
                        LastModified = f.LastWriteTime
                    })
                    .ToList();

                logger.LogInformation("Found {Count} .dmp files in directory: {DirectoryPath}", dumpFiles.Count, directoryPath);

                if (dumpFiles.Count == 0)
                {
                    logger.LogInformation("No .dmp files found in directory: {DirectoryPath}", directoryPath);
                    return Task.FromResult($"No .dmp files found in directory: {directoryPath}");
                }

                var result = new StringBuilder();
                result.AppendLine($"Found {dumpFiles.Count} dump files in {directoryPath}:");
                result.AppendLine();

                foreach (var dump in dumpFiles)
                {
                    logger.LogDebug("Processing dump file: {Name} ({Size} bytes)", dump.Name, dump.Size);
                    result.AppendLine($"File: {dump.Name}");
                    result.AppendLine($"Path: {dump.Path}");
                    result.AppendLine($"Size: {dump.Size:N0} bytes");
                    result.AppendLine($"Last Modified: {dump.LastModified:yyyy-MM-dd HH:mm:ss}");
                    result.AppendLine();
                }

                logger.LogInformation("Successfully listed {Count} dump files in directory: {DirectoryPath}", dumpFiles.Count, directoryPath);
                return Task.FromResult(result.ToString());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing dump files in directory: {DirectoryPath}", directoryPath);
                return Task.FromResult($"Error listing dump files: {ex.Message}");
            }
        }

        // Additional Analysis Tools

        [McpServerTool, Description("Get basic information about the current debugging session")]
        public async Task<string> GetSessionInfo()
        {
            logger.LogInformation("GetSessionInfo called");

            try
            {
                logger.LogDebug("Checking if session is active...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session - cannot get session info");
                    return "No active debugging session.";
                }

                logger.LogInformation("Gathering session information...");
                var result = new StringBuilder();

                // Get version info
                logger.LogDebug("Getting version information...");
                var version = await cdbSession.ExecuteCommand("version");
                result.AppendLine("=== Version Information ===");
                result.AppendLine(version);
                result.AppendLine();

                // Get process info
                logger.LogDebug("Getting process information...");
                var processInfo = await cdbSession.ExecuteCommand("!process 0 0");
                result.AppendLine("=== Process Information ===");
                result.AppendLine(processInfo);
                result.AppendLine();

                // Get thread info
                logger.LogDebug("Getting thread information...");
                var threadInfo = await cdbSession.ExecuteCommand("~");
                result.AppendLine("=== Thread Information ===");
                result.AppendLine(threadInfo);

                logger.LogInformation("Session information gathered successfully");
                return result.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting session information");
                return $"Error getting session information: {ex.Message}";
            }
        }

        [McpServerTool, Description("Analyze the current call stack with detailed information")]
        public async Task<string> AnalyzeCallStack()
        {
            logger.LogInformation("AnalyzeCallStack called");

            try
            {
                logger.LogDebug("Checking if session is active...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session - cannot analyze call stack");
                    return "No active debugging session.";
                }

                logger.LogInformation("Analyzing call stack...");
                var result = new StringBuilder();

                // Get call stack
                logger.LogDebug("Getting call stack...");
                var callStack = await cdbSession.ExecuteCommand("k");
                result.AppendLine("=== Call Stack ===");
                result.AppendLine(callStack);
                result.AppendLine();

                // Get exception information if available
                logger.LogDebug("Getting exception context...");
                var exceptionInfo = await cdbSession.ExecuteCommand(".ecxr");
                if (!exceptionInfo.Contains("No exception context available"))
                {
                    logger.LogDebug("Exception context available, adding to result");
                    result.AppendLine("=== Exception Context ===");
                    result.AppendLine(exceptionInfo);
                    result.AppendLine();
                }
                else
                {
                    logger.LogDebug("No exception context available");
                }

                // Get registers
                logger.LogDebug("Getting register information...");
                var registers = await cdbSession.ExecuteCommand("r");
                result.AppendLine("=== Registers ===");
                result.AppendLine(registers);

                logger.LogInformation("Call stack analysis completed successfully");
                return result.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing call stack");
                return $"Error analyzing call stack: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get memory information and analyze memory usage")]
        public async Task<string> AnalyzeMemory()
        {
            logger.LogInformation("AnalyzeMemory called");

            try
            {
                logger.LogDebug("Checking if session is active...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session - cannot analyze memory");
                    return "No active debugging session.";
                }

                logger.LogInformation("Analyzing memory...");
                var result = new StringBuilder();

                // Get virtual memory info
                logger.LogDebug("Getting virtual memory information...");
                var vmemInfo = await cdbSession.ExecuteCommand("!vprot");
                result.AppendLine("=== Virtual Memory Information ===");
                result.AppendLine(vmemInfo);
                result.AppendLine();

                // Get heap information
                logger.LogDebug("Getting heap information...");
                var heapInfo = await cdbSession.ExecuteCommand("!heap -s");
                result.AppendLine("=== Heap Information ===");
                result.AppendLine(heapInfo);
                result.AppendLine();

                // Get loaded modules
                logger.LogDebug("Getting loaded modules information...");
                var modules = await cdbSession.ExecuteCommand("lm");
                result.AppendLine("=== Loaded Modules ===");
                result.AppendLine(modules);

                logger.LogInformation("Memory analysis completed successfully");
                return result.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing memory");
                return $"Error analyzing memory: {ex.Message}";
            }
        }

        [McpServerTool, Description("Check for common crash patterns and provide analysis")]
        public async Task<string> AnalyzeCrashPatterns()
        {
            logger.LogInformation("AnalyzeCrashPatterns called");

            try
            {
                logger.LogDebug("Checking if session is active...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session - cannot analyze crash patterns");
                    return "No active debugging session.";
                }

                logger.LogInformation("Analyzing crash patterns...");
                var result = new StringBuilder();
                result.AppendLine("=== Crash Pattern Analysis ===");
                result.AppendLine();

                // Check for access violations
                logger.LogDebug("Checking for access violations...");
                var exceptionInfo = await cdbSession.ExecuteCommand(".ecxr");
                if (exceptionInfo.Contains("Access violation"))
                {
                    logger.LogWarning("Access violation detected in exception context");
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
                    logger.LogDebug("No access violation detected");
                }

                // Check for stack overflow
                logger.LogDebug("Checking for stack overflow...");
                var callStack = await cdbSession.ExecuteCommand("k");
                if (callStack.Contains("Stack overflow") || callStack.Split('\n').Length > 100)
                {
                    logger.LogWarning("Potential stack overflow detected");
                    result.AppendLine("⚠️  POTENTIAL STACK OVERFLOW");
                    result.AppendLine("This may be caused by:");
                    result.AppendLine("- Infinite recursion");
                    result.AppendLine("- Large local variables on stack");
                    result.AppendLine("- Deep call chains");
                    result.AppendLine();
                }
                else
                {
                    logger.LogDebug("No stack overflow detected");
                }

                // Check for heap corruption
                logger.LogDebug("Checking for heap corruption...");
                var heapInfo = await cdbSession.ExecuteCommand("!heap -s");
                if (heapInfo.Contains("corrupted") || heapInfo.Contains("invalid"))
                {
                    logger.LogWarning("Heap corruption detected");
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
                    logger.LogDebug("No heap corruption detected");
                }

                // Check for deadlocks
                logger.LogDebug("Checking for deadlocks...");
                var threadInfo = await cdbSession.ExecuteCommand("~*k");
                if (threadInfo.Contains("WaitForSingleObject") || threadInfo.Contains("WaitForMultipleObjects"))
                {
                    logger.LogWarning("Potential deadlock detected");
                    result.AppendLine("⚠️  POTENTIAL DEADLOCK DETECTED");
                    result.AppendLine("Threads appear to be waiting for synchronization objects");
                    result.AppendLine();
                }
                else
                {
                    logger.LogDebug("No deadlock patterns detected");
                }

                if (!result.ToString().Contains("⚠️"))
                {
                    logger.LogInformation("No obvious crash patterns detected");
                    result.AppendLine("✅ No obvious crash patterns detected");
                    result.AppendLine("Consider analyzing the call stack and exception context manually");
                }

                logger.LogInformation("Crash pattern analysis completed successfully");
                return result.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing crash patterns");
                return $"Error analyzing crash patterns: {ex.Message}";
            }
        }

        // Async Job Management Tools

        [McpServerTool, Description("Check status of async job from run_windbg_cmd. Returns JSON with status: 'running' (keep polling every 10-15s), 'completed' (extract 'result' field for command output), 'failed' (check 'error' field), or 'cancelled'. CRITICAL: You must call this to get actual results from long-running commands!")]
        public Task<string> GetJobStatus(string jobId)
        {
            logger.LogInformation("GetJobStatus called with jobId: {JobId}", jobId);

            try
            {
                var job = backgroundJobService.GetJob(jobId);
                if (job == null)
                {
                    return Task.FromResult($"{{\"error\": \"Job not found: {jobId}\"}}");
                }

                var elapsed = job.EndTime?.Subtract(job.StartTime) ?? DateTime.UtcNow.Subtract(job.StartTime);

                // Add helpful instructions based on job status
                var instructions = job.Status switch
                {
                    JobStatus.Running => $@"""instructions"": ""Job is still running. Check again in 10-30 seconds with get_job_status(jobId='{job.Id}'). Use cancel_job(jobId='{job.Id}') to stop if needed."",",
                    JobStatus.Completed => $@"""instructions"": ""Job completed successfully! The 'result' field contains the full command output."",",
                    JobStatus.Failed => $@"""instructions"": ""Job failed. Check the 'error' field for details. You may retry with a new async command."",",
                    JobStatus.Cancelled => $@"""instructions"": ""Job was cancelled. You may start a new async command if needed."",",
                    _ => ""
                };

                var statusJson = $@"{{
  ""jobId"": ""{job.Id}"",
  ""command"": ""{job.Command.Replace("\"", "\\\"")}"",
  ""status"": ""{job.Status}"",
  ""startTime"": ""{job.StartTime:yyyy-MM-ddTHH:mm:ss.fffZ}"",
  ""endTime"": {(job.EndTime.HasValue ? $"\"{job.EndTime:yyyy-MM-ddTHH:mm:ss.fffZ}\"" : "null")},
  ""elapsedSeconds"": {elapsed.TotalSeconds:F1},
  {instructions}
  ""result"": {(job.Result != null ? $"\"{job.Result.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}\"" : "null")},
  ""error"": {(job.Error != null ? $"\"{job.Error.Replace("\"", "\\\"")}\"" : "null")}
}}";

                return Task.FromResult(statusJson);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting job status: {JobId}", jobId);
                return Task.FromResult($"{{\"error\": \"Error getting job status: {ex.Message}\"}}");
            }
        }

        [McpServerTool, Description("Cancel a running async job. Useful for stopping long-running commands that are taking too long.")]
        public Task<string> CancelJob(string jobId)
        {
            logger.LogInformation("CancelJob called with jobId: {JobId}", jobId);

            try
            {
                var job = backgroundJobService.GetJob(jobId);
                if (job == null)
                {
                    return Task.FromResult($"{{\"error\": \"Job not found: {jobId}\"}}");
                }

                if (job.Status != JobStatus.Running)
                {
                    return Task.FromResult($"{{\"error\": \"Job is not running: {jobId} (status: {job.Status})\"}}");
                }

                backgroundJobService.CancelJob(jobId);
                return Task.FromResult($"{{\"success\": true, \"message\": \"Job {jobId} has been cancelled\"}}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cancelling job: {JobId}", jobId);
                return Task.FromResult($"{{\"error\": \"Error cancelling job: {ex.Message}\"}}");
            }
        }

        [McpServerTool, Description("List recent async jobs and their status. Shows the last 20 jobs with their completion status and timing.")]
        public Task<string> ListJobs()
        {
            logger.LogInformation("ListJobs called");

            try
            {
                var jobs = backgroundJobService.GetAllJobs().OrderByDescending(j => j.StartTime).Take(20);
                var jobsJson = string.Join(",\n  ", jobs.Select(job =>
                {
                    var elapsed = job.EndTime?.Subtract(job.StartTime) ?? DateTime.UtcNow.Subtract(job.StartTime);
                    return $@"{{
    ""jobId"": ""{job.Id}"",
    ""command"": ""{job.Command.Replace("\"", "\\\"")}"",
    ""status"": ""{job.Status}"",
    ""startTime"": ""{job.StartTime:yyyy-MM-ddTHH:mm:ss.fffZ}"",
    ""elapsedSeconds"": {elapsed.TotalSeconds:F1}
  }}";
                }));

                return Task.FromResult($@"{{
  ""jobs"": [
  {jobsJson}
  ],
  ""instructions"": ""Use get_job_status(jobId) to get detailed status and results for any specific job. Jobs are automatically cleaned up after 1 hour.""
}}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing jobs");
                return Task.FromResult($"{{\"error\": \"Error listing jobs: {ex.Message}\"}}");
            }
        }

        // Legacy method for backward compatibility
        [McpServerTool, Description("Gets the current time for a city (legacy method)")]
        public string GetCurrentTime(string city)
        {
            logger.LogInformation("GetCurrentTime called with city: {City}", city);

            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    logger.LogError("City parameter is null or empty");
                    return "City cannot be null or empty";
                }

                var currentTime = DateTime.Now;
                var timeString = $"It is {currentTime.Hour}:{currentTime.Minute} in {city}.";

                logger.LogInformation("Successfully generated time for city: {City} -> {TimeString}", city, timeString);
                return timeString;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting current time for city: {City}", city);
                return $"Error getting current time: {ex.Message}";
            }
        }
    }
}
