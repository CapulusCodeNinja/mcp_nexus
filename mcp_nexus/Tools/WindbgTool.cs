using mcp_nexus.Helper;
using mcp_nexus.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class WindbgTool(ILogger<WindbgTool> logger, ICdbSession cdbSession, ICommandQueueService commandQueueService)
    {
        // Helper method to wait for async command completion and extract result
        private async Task<string> WaitForCommandCompletion(string commandId, string commandDescription)
        {
            logger.LogDebug("⏳ Waiting for {Description} command {CommandId} to complete...", commandDescription, commandId);

            var maxWaitTime = TimeSpan.FromMinutes(5); // 5 minute timeout
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime) < maxWaitTime)
            {
                var result = await commandQueueService.GetCommandResult(commandId);

                // If command completed successfully, return the result
                if (!result.StartsWith("Command is still") && !result.StartsWith("Command not found"))
                {
                    logger.LogDebug("✅ {Description} command {CommandId} completed", commandDescription, commandId);
                    return result;
                }

                // Wait a bit before checking again
                await Task.Delay(1000);
            }

            // Timeout - cancel the command and return error
            logger.LogError("⏰ {Description} command {CommandId} timed out after 5 minutes", commandDescription, commandId);
            commandQueueService.CancelCommand(commandId);
            return $"Command timed out after 5 minutes: {commandDescription}";
        }

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
                var cancelled = commandQueueService.CancelAllCommands("Session stop requested");
                logger.LogInformation("Cancelled {Count} queued/executing command(s) before session stop", cancelled);
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
                var cancelled = commandQueueService.CancelAllCommands("Remote session stop requested");
                logger.LogInformation("Cancelled {Count} queued/executing command(s) before remote session stop", cancelled);
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


        [McpServerTool, Description("🔄 ASYNC QUEUE: Execute WinDBG command in background queue. ⚠️ NEVER returns command results directly! Always returns commandId. You MUST call get_command_status(commandId) to get results. NO EXCEPTIONS! Works for ALL commands: version, lsa, !analyze -v, !process, etc.")]
        public Task<string> RunWindbgCmdAsync(string command)
        {
            logger.LogInformation("RunWindbgCmdAsync called with command: {Command} [PURE ASYNC QUEUE MODE]", command);

            try
            {
                // Validate command
                if (string.IsNullOrWhiteSpace(command))
                {
                    logger.LogError("Command is null or empty");
                    return Task.FromResult("Command cannot be null or empty");
                }

                logger.LogDebug("Checking if CDB session is active...");
                logger.LogInformation("CdbSession.IsActive: {IsActive}", cdbSession.IsActive);

                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session - cannot execute");
                    return Task.FromResult("No active debugging session. Please open a crash dump or connect to a remote session first.");
                }

                // Queue the command for sequential execution
                var commandId = commandQueueService.QueueCommand(command);

                logger.LogInformation("Queued command {CommandId}: {Command}", commandId, command);

                // PURE ASYNC: Always return command ID immediately, never wait
                var response = $@"{{
  ""⚠️ IMPORTANT"": ""THIS RESPONSE CONTAINS NO COMMAND RESULTS!"",
  ""commandId"": ""{commandId}"",
  ""status"": ""queued"",
  ""message"": ""✅ QUEUED: Command added to execution queue"",
  ""command"": ""{command.Replace("\"", "\\\"")}"",
  
  ""🚨 CRITICAL_INSTRUCTIONS"": {{
    ""step_1"": ""The command '{command}' is now queued for execution"",
    ""step_2"": ""You MUST call get_command_status(commandId='{commandId}') to get results"",
    ""step_3"": ""Keep calling get_command_status every 5-10 seconds until status='completed'"",
    ""step_4"": ""Extract 'result' field from completed response for actual WinDBG output"",
    ""no_exceptions"": ""NEVER expect results in this response - they don't exist here!""
  }},

  ""next_required_call"": {{
    ""function"": ""get_command_status"",
    ""parameters"": {{""commandId"": ""{commandId}""}},
    ""when"": ""Call immediately, then every 5-10 seconds"",
    ""until"": ""status field equals 'completed'""
  }},

  ""example_workflow"": [
    ""1. You just called run_windbg_cmd_async → Got this commandId"",
    ""2. Now call get_command_status('{commandId}') → Check if done"",
    ""3. If status='queued' or 'executing' → Wait 5-10s, call again"",
    ""4. If status='completed' → Extract 'result' field = your answer!""
  ],

  ""🔴 WARNING"": ""Command results are NOT in this JSON! Use get_command_status!""
}}";

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error queueing WinDBG command: {Command}", command);
                return Task.FromResult($"Error queueing command: {ex.Message}");
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
            logger.LogInformation("🔥 AnalyzeCallStack called - USING ASYNC QUEUE TO AVOID RACE CONDITION");

            try
            {
                logger.LogDebug("Checking if session is active...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session - cannot analyze call stack");
                    return "No active debugging session.";
                }

                logger.LogInformation("Analyzing call stack using async queue...");
                var result = new StringBuilder();

                // Get call stack using async queue
                logger.LogDebug("🚀 Queueing call stack command...");
                var callStackCommandId = commandQueueService.QueueCommand("k");
                var callStack = await WaitForCommandCompletion(callStackCommandId, "call stack");
                result.AppendLine("=== Call Stack ===");
                result.AppendLine(callStack);
                result.AppendLine();

                // Get exception information if available using async queue
                logger.LogDebug("🚀 Queueing exception context command...");
                var exceptionCommandId = commandQueueService.QueueCommand(".ecxr");
                var exceptionInfo = await WaitForCommandCompletion(exceptionCommandId, "exception context");
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

                // Get registers using async queue
                logger.LogDebug("🚀 Queueing register command...");
                var registersCommandId = commandQueueService.QueueCommand("r");
                var registers = await WaitForCommandCompletion(registersCommandId, "registers");
                result.AppendLine("=== Registers ===");
                result.AppendLine(registers);

                logger.LogInformation("✅ Call stack analysis completed successfully using async queue");
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
            logger.LogInformation("🔥 AnalyzeCrashPatterns called - USING ASYNC QUEUE TO AVOID RACE CONDITION");

            try
            {
                logger.LogDebug("Checking if session is active...");
                if (!cdbSession.IsActive)
                {
                    logger.LogWarning("No active debugging session - cannot analyze crash patterns");
                    return "No active debugging session.";
                }

                logger.LogInformation("Analyzing crash patterns using async queue...");
                var result = new StringBuilder();
                result.AppendLine("=== Crash Pattern Analysis ===");
                result.AppendLine();

                // Check for access violations using async queue
                logger.LogDebug("🚀 Queueing exception context command for access violation check...");
                var exceptionCommandId = commandQueueService.QueueCommand(".ecxr");
                var exceptionInfo = await WaitForCommandCompletion(exceptionCommandId, "exception context");
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

                // Check for stack overflow using async queue
                logger.LogDebug("🚀 Queueing call stack command for stack overflow check...");
                var callStackCommandId = commandQueueService.QueueCommand("k");
                var callStack = await WaitForCommandCompletion(callStackCommandId, "call stack");
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

                // Check for heap corruption using async queue
                logger.LogDebug("🚀 Queueing heap info command for heap corruption check...");
                var heapCommandId = commandQueueService.QueueCommand("!heap -s");
                var heapInfo = await WaitForCommandCompletion(heapCommandId, "heap info");
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

                // Check for deadlocks using async queue
                logger.LogDebug("🚀 Queueing thread info command for deadlock check...");
                var threadCommandId = commandQueueService.QueueCommand("~*k");
                var threadInfo = await WaitForCommandCompletion(threadCommandId, "thread info");
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

                logger.LogInformation("✅ Crash pattern analysis completed successfully using async queue");
                return result.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing crash patterns");
                return $"Error analyzing crash patterns: {ex.Message}";
            }
        }

        // Async Job Management Tools

        [McpServerTool, Description("Check status of queued command from run_windbg_cmd_async. Returns JSON with status: 'queued' (waiting in line), 'executing' (running now), 'completed' (extract 'result' field for command output), or 'cancelled'. CRITICAL: You must call this to get actual results from commands!")]
        public async Task<string> GetCommandStatus(string commandId)
        {
            logger.LogInformation("GetCommandStatus called with commandId: {CommandId}", commandId);

            try
            {
                var commandResult = await commandQueueService.GetCommandResult(commandId);

                if (commandResult.StartsWith("Command not found"))
                {
                    return $@"{{""error"": ""Command not found: {commandId}""}}";
                }

                // Check if command is completed
                if (!commandResult.StartsWith("Command is still"))
                {
                    // Command completed, return the result
                    return $@"{{
  ""commandId"": ""{commandId}"",
  ""status"": ""completed"",
  ""message"": ""✅ COMPLETED: Command finished successfully"",
  ""result"": ""{commandResult.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "")}"",
  ""instructions"": ""Command completed successfully! The 'result' field contains the full command output."",
  ""ai_guidance"": ""SUCCESS: The command has finished. Use the 'result' field above for the actual WinDBG output.""
}}";
                }

                // Command still in progress, check queue status
                var queueStatus = commandQueueService.GetQueueStatus()
                    .FirstOrDefault(cmd => cmd.Id == commandId);

                if (queueStatus == default)
                {
                    return $@"{{""error"": ""Command not found in queue: {commandId}""}}";
                }

                var waitTime = (DateTime.UtcNow - queueStatus.QueueTime).TotalSeconds;
                var status = queueStatus.Status.ToLower();

                var instructions = status switch
                {
                    "queued" => $@"""instructions"": ""Command is waiting in queue. Check again in 5-10 seconds with get_command_status(commandId='{commandId}'). Use cancel_command(commandId='{commandId}') to stop if needed."",",
                    "executing" => $@"""instructions"": ""Command is currently executing. Check again in 10-15 seconds with get_command_status(commandId='{commandId}'). Use cancel_command(commandId='{commandId}') to stop if needed."",",
                    "cancelled" => @"""instructions"": ""Command was cancelled. You may start a new command if needed."",",
                    _ => $@"""instructions"": ""Command status: {status}. Check again in 10 seconds."","
                };

                return $@"{{
  ""commandId"": ""{commandId}"",
  ""command"": ""{queueStatus.Command.Replace("\"", "\\\"")}"",
  ""status"": ""{status}"",
  ""queueTime"": ""{queueStatus.QueueTime:yyyy-MM-ddTHH:mm:ss.fffZ}"",
  ""waitTimeSeconds"": {waitTime:F1},
  {instructions}
  ""ai_guidance"": ""Command is {status}. Keep polling this method every 5-15 seconds until status='completed', then use 'result' field.""
}}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting command status: {CommandId}", commandId);
                return $@"{{""error"": ""Error getting command status: {ex.Message}""}}";
            }
        }

        [McpServerTool, Description("Cancel a queued or running command. Useful for stopping long-running commands that are taking too long.")]
        public Task<string> CancelCommand(string commandId)
        {
            logger.LogInformation("CancelCommand called with commandId: {CommandId}", commandId);

            try
            {
                var cancelled = commandQueueService.CancelCommand(commandId);
                if (!cancelled)
                {
                    return Task.FromResult($@"{{""error"": ""Command not found or already completed: {commandId}""}}");
                }

                return Task.FromResult($@"{{
  ""success"": true, 
  ""message"": ""✅ CANCELLED: Command {commandId} has been cancelled"",
  ""commandId"": ""{commandId}"",
  ""ai_guidance"": ""Command successfully cancelled. You can start a new command now.""
}}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cancelling command: {CommandId}", commandId);
                return Task.FromResult($@"{{""error"": ""Error cancelling command: {ex.Message}""}}");
            }
        }

        [McpServerTool, Description("List current command queue status. Shows queued, executing, and recent commands with their status.")]
        public Task<string> ListCommands()
        {
            logger.LogInformation("ListCommands called");

            try
            {
                var queueStatus = commandQueueService.GetQueueStatus();
                var currentCommand = commandQueueService.GetCurrentCommand();

                var valueTuples = queueStatus as (string Id, string Command, DateTime QueueTime, string Status)[] ?? queueStatus.ToArray();
                var commandsJson = string.Join(",\n  ", valueTuples.Select(cmd =>
                {
                    var waitTime = (DateTime.UtcNow - cmd.QueueTime).TotalSeconds;
                    return $@"{{
    ""commandId"": ""{cmd.Id}"",
    ""command"": ""{cmd.Command.Replace("\"", "\\\"")}"",
    ""status"": ""{cmd.Status.ToLower()}"",
    ""queueTime"": ""{cmd.QueueTime:yyyy-MM-ddTHH:mm:ss.fffZ}"",
    ""waitTimeSeconds"": {waitTime:F1}
  }}";
                }));

                var currentInfo = currentCommand != null
                    ? $@"""currentlyExecuting"": {{
    ""commandId"": ""{currentCommand.Id}"",
    ""command"": ""{currentCommand.Command.Replace("\"", "\\\"")}"",
    ""queueTime"": ""{currentCommand.QueueTime:yyyy-MM-ddTHH:mm:ss.fffZ}""
  }},"
                    : @"""currentlyExecuting"": null,";

                return Task.FromResult($@"{{
  {currentInfo}
  ""queuedCommands"": [
  {commandsJson}
  ],
  ""queueSize"": {valueTuples.Length},
  ""instructions"": ""Use get_command_status(commandId) to get detailed status and results for any specific command. Commands execute sequentially in order.""
}}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing commands");
                return Task.FromResult($@"{{""error"": ""Error listing commands: {ex.Message}""}}");
            }
        }
    }
}
