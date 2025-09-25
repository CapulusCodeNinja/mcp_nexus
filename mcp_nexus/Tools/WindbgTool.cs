using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Utilities;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class WindbgTool(ILogger<WindbgTool> logger, ICdbSession cdbSession, ICommandQueueService commandQueueService)
    {
        // IMPROVED: Better polling with exponential backoff and cancellation support
        private async Task<string> WaitForCommandCompletion(string commandId, string commandDescription, CancellationToken cancellationToken = default)
        {
            logger.LogTrace("Waiting for {Description} command {CommandId} to complete...", commandDescription, commandId);

            var maxWaitTime = TimeSpan.FromMinutes(5); // 5 minute timeout
            var startTime = DateTime.UtcNow;
            var pollInterval = TimeSpan.FromMilliseconds(100); // Start with 100ms
            var maxPollInterval = TimeSpan.FromSeconds(2); // Max 2 seconds between polls

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            combinedCts.CancelAfter(maxWaitTime);

            try
            {
                while (!combinedCts.Token.IsCancellationRequested)
                {
                    var result = await commandQueueService.GetCommandResult(commandId);

                    // If command completed successfully, return the result
                    if (!result.StartsWith("Command is still") && !result.StartsWith("Command not found"))
                    {
                        logger.LogTrace("{Description} command {CommandId} completed", commandDescription, commandId);
                        return result;
                    }

                    // Exponential backoff for polling
                    await Task.Delay(pollInterval, combinedCts.Token);
                    pollInterval = TimeSpan.FromMilliseconds(Math.Min(pollInterval.TotalMilliseconds * 1.5, maxPollInterval.TotalMilliseconds));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("{Description} command {CommandId} was cancelled by caller", commandDescription, commandId);
                commandQueueService.CancelCommand(commandId);
                return $"Command was cancelled: {commandDescription}";
            }
            catch (OperationCanceledException)
            {
                // Timeout reached
                logger.LogError("{Description} command {CommandId} timed out after 5 minutes", commandDescription, commandId);
                commandQueueService.CancelCommand(commandId);
                return $"Command timed out after 5 minutes: {commandDescription}";
            }

            // Fallback timeout
            logger.LogError("{Description} command {CommandId} timed out after 5 minutes", commandDescription, commandId);
            commandQueueService.CancelCommand(commandId);
            return $"Command timed out after 5 minutes: {commandDescription}";
        }

        // Crash Dump Analysis Tools

        [McpServerTool, Description("Analyze a Windows crash dump file using common debugger commands")]
        public async Task<string> NexusOpenDump(string dumpPath, string? symbolsPath = null)
        {
            logger.LogDebug("NexusOpenDump called with dumpPath: {DumpPath}, symbolsPath: {SymbolsPath}", dumpPath, symbolsPath);

            try
            {
                // Validate dump path
                if (string.IsNullOrWhiteSpace(dumpPath))
                {
                    logger.LogError("Dump path is null or empty");
                    return "Dump path cannot be null or empty";
                }

                // Convert WSL paths to Windows format for file operations
                var originalDumpPath = dumpPath;
                dumpPath = PathHandler.NormalizeForWindows(dumpPath);
                if (originalDumpPath != dumpPath)
                {
                    logger.LogInformation("Converted WSL path '{OriginalPath}' to Windows path '{WindowsPath}'", originalDumpPath, dumpPath);
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
                    // Convert WSL paths to Windows format for symbols path
                    var originalSymbolsPath = symbolsPath;
                    symbolsPath = PathHandler.NormalizeForWindows(symbolsPath);
                    if (originalSymbolsPath != symbolsPath)
                    {
                        logger.LogInformation("Converted WSL symbols path '{OriginalPath}' to Windows path '{WindowsPath}'", originalSymbolsPath, symbolsPath);
                    }

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

                logger.LogDebug("Starting CDB session for crash dump analysis...");
                var success = await cdbSession.StartSession(target, null);

                if (success)
                {
                    logger.LogInformation("Opened crash dump: {DumpPath}", dumpPath);
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
        public async Task<string> NexusCloseDump()
        {
            logger.LogDebug("NexusCloseDump called");

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
        public async Task<string> NexusStartRemoteDebug(string connectionString, string? symbolsPath = null)
        {
            logger.LogInformation("NexusStartRemoteDebug called with connectionString: {ConnectionString}, symbolsPath: {SymbolsPath}", connectionString, symbolsPath);

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
                    // Convert WSL paths to Windows format for symbols path
                    var originalSymbolsPath = symbolsPath;
                    symbolsPath = PathHandler.NormalizeForWindows(symbolsPath);
                    if (originalSymbolsPath != symbolsPath)
                    {
                        logger.LogInformation("Converted WSL symbols path '{OriginalPath}' to Windows path '{WindowsPath}'", originalSymbolsPath, symbolsPath);
                    }

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

                logger.LogDebug("Starting CDB session for remote debugging...");
                var success = await cdbSession.StartSession(target, null);

                if (success)
                {
                    logger.LogInformation("Connected to remote debugging session: {ConnectionString}", connectionString);
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
        public async Task<string> NexusStopRemoteDebug()
        {
            logger.LogInformation("NexusStopRemoteDebug called");

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


        [McpServerTool, Description("🔄 ASYNC QUEUE: Execute debugger command in background queue. ⚠️ NEVER returns command results directly! Always returns commandId. You MUST call nexus_debugger_command_status(commandId) to get results. NO EXCEPTIONS! Works for ALL commands: version, lsa, !analyze -v, !process, etc.")]
        public Task<string> NexusExecDebuggerCommandAsync(string command)
        {
            logger.LogDebug("NexusExecDebuggerCommandAsync called with command: {Command}", command);

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

                logger.LogDebug("Queued command {CommandId}: {Command}", commandId, command);

                // PURE ASYNC: Always return command ID immediately, never wait
                var response = $@"{{
  ""⚠️ IMPORTANT"": ""THIS RESPONSE CONTAINS NO COMMAND RESULTS!"",
  ""commandId"": ""{commandId}"",
  ""status"": ""queued"",
  ""message"": ""✅ QUEUED: Command added to execution queue"",
  ""command"": ""{command.Replace("\"", "\\\"")}"",
  
  ""🚨 CRITICAL_INSTRUCTIONS"": {{
    ""step_1"": ""The command '{command}' is now queued for execution"",
    ""step_2"": ""You MUST call nexus_debugger_command_status(commandId='{commandId}') to get results"",
    ""step_3"": ""Keep calling nexus_debugger_command_status every 5-10 seconds until status='completed'"",
    ""step_4"": ""Extract 'result' field from completed response for actual debugger output"",
    ""no_exceptions"": ""NEVER expect results in this response - they don't exist here!""
  }},

  ""next_required_call"": {{
    ""function"": ""nexus_debugger_command_status"",
    ""parameters"": {{""commandId"": ""{commandId}""}},
    ""when"": ""Call immediately, then every 5-10 seconds"",
    ""until"": ""status field equals 'completed'""
  }},

  ""example_workflow"": [
    ""1. You just called nexus_exec_debugger_command_async → Got this commandId"",
    ""2. Now call nexus_debugger_command_status('{commandId}') → Check if done"",
    ""3. If status='queued' or 'executing' → Wait 5-10s, call again"",
    ""4. If status='completed' → Extract 'result' field = your answer!""
  ],

  ""🔴 WARNING"": ""Command results are NOT in this JSON! Use nexus_debugger_command_status!""
}}";

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error queueing WinDBG command: {Command}", command);
                return Task.FromResult($"Error queueing command: {ex.Message}");
            }
        }

        // Command Queue Management Tools

        // Async Job Management Tools

        [McpServerTool, Description("Check status of queued command from nexus_exec_debugger_command_async. Returns JSON with status: 'queued' (waiting in line), 'executing' (running now), 'completed' (extract 'result' field for command output), or 'cancelled'. CRITICAL: You must call this to get actual results from commands!")]
        public async Task<string> NexusDebuggerCommandStatus(string commandId)
        {
            logger.LogInformation("NexusDebuggerCommandStatus called with commandId: {CommandId}", commandId);

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
        public Task<string> NexusDebuggerCommandCancel(string commandId)
        {
            logger.LogInformation("NexusDebuggerCommandCancel called with commandId: {CommandId}", commandId);

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
        public Task<string> NexusListDebuggerCommands()
        {
            logger.LogInformation("NexusListDebuggerCommands called");

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
