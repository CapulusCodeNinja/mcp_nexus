using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Protocol;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Utilities;
using mcp_nexus.Constants;
using mcp_nexus.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace mcp_nexus.Tools
{
    /// <summary>
    /// Session-aware Windows debugging tool with multi-session support
    /// Each debugging session is isolated with its own CDB process and command queue
    /// </summary>
    [McpServerToolType]
    public class SessionAwareWindbgTool(ILogger<SessionAwareWindbgTool> logger, ISessionManager sessionManager)
    {
        /// <summary>
        /// Session-aware response wrapper for AI client guidance
        /// </summary>
        public class SessionAwareResponse
        {
            [JsonPropertyName("sessionId")]
            public string SessionId { get; set; } = string.Empty;
            
            [JsonPropertyName("result")]
            public string Result { get; set; } = string.Empty;
            
            [JsonPropertyName("sessionContext")]
            public SessionContext? SessionContext { get; set; }
            
            [JsonPropertyName("aiGuidance")]
            public AIGuidance AIGuidance { get; set; } = new();
            
            [JsonPropertyName("workflowContext")]
            public WorkflowContext WorkflowContext { get; set; } = new();
        }

        /// <summary>
        /// AI client guidance information
        /// </summary>
        public class AIGuidance
        {
            [JsonPropertyName("nextSteps")]
            public List<string> NextSteps { get; set; } = new();
            
            [JsonPropertyName("usageHints")]
            public List<string> UsageHints { get; set; } = new();
            
            [JsonPropertyName("commonErrors")]
            public List<string> CommonErrors { get; set; } = new();
        }

        /// <summary>
        /// Workflow context for the current debugging session
        /// </summary>
        public class WorkflowContext
        {
            [JsonPropertyName("currentStep")]
            public string CurrentStep { get; set; } = string.Empty;
            
            [JsonPropertyName("suggestedNextCommands")]
            public List<string> SuggestedNextCommands { get; set; } = new();
            
            [JsonPropertyName("sessionState")]
            public string SessionState { get; set; } = string.Empty;
        }

        #region Session Management Tools

        /// <summary>
        /// Open a new debugging session for a crash dump file
        /// Creates an isolated debugging environment with dedicated CDB process
        /// </summary>
        [Description("🔓 OPEN SESSION: Create a new debugging session for a crash dump file. Returns sessionId that MUST be used for all subsequent operations.")]
        public async Task<object> nexus_open_dump_analyze_session(
            [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
            [Description("Optional path to symbol files directory")] string? symbolsPath = null)
        {
            logger.LogInformation("🔓 Opening new debugging session for dump: {DumpPath}", dumpPath);

            try
            {
                // Create new session
                var sessionId = await sessionManager.CreateSessionAsync(dumpPath, symbolsPath);
                var context = sessionManager.GetSessionContext(sessionId);

                // Return comprehensive response with all critical information
                var response = new
                {
                    // CRITICAL IDENTIFIERS - Always include these
                    sessionId = sessionId,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null, // No commandId for this operation
                    
                    // OPERATION STATUS
                    success = true,
                    operation = "nexus_open_dump_analyze_session",
                    status = "completed",
                    message = $"Session created: {sessionId}",
                    
                    // WORKFLOW GUIDANCE
                    nextCommand = "nexus_dump_analyze_session_async_command",
                    nextCommandParameters = new { sessionId = sessionId, command = "!analyze -v" },
                    workflow = "STEP 1 COMPLETE → NEXT: nexus_dump_analyze_session_async_command → THEN: nexus_dump_analyze_session_async_command_status",
                    
                    // CRITICAL REMINDERS
                    extractSessionId = sessionId, // Extra copy for extraction
                    useThisSessionId = sessionId, // Another copy for clarity
                    instructions = $"SAVE this sessionId '{sessionId}' and use it in ALL subsequent commands!"
                };

                logger.LogInformation("✅ Session {SessionId} created successfully", sessionId);
                return response;
            }
            catch (SessionLimitExceededException ex)
            {
                logger.LogWarning("❌ Session limit exceeded: {Message}", ex.Message);
                
                var errorResponse = new SessionAwareResponse
                {
                    Result = $"❌ Maximum concurrent sessions exceeded: {ex.CurrentSessions}/{ex.MaxSessions}\n\n" +
                             "🔧 Please close unused sessions or wait for inactive sessions to expire.\n" +
                             "💡 Use nexus_close_dump to explicitly close sessions.",
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Check active sessions and close unused ones",
                            "Wait for inactive sessions to auto-expire (30 minutes)",
                            "Retry opening the dump file"
                        },
                        CommonErrors = new List<string>
                        {
                            "Too many concurrent debugging sessions open",
                            "Sessions not being explicitly closed"
                        }
                    }
                };
                
                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to create debugging session for {DumpPath}", dumpPath);
                
                var errorResponse = new SessionAwareResponse
                {
                    Result = $"❌ Failed to create debugging session: {ex.Message}\n\n" +
                             "🔧 Check that the dump file exists and is accessible.\n" +
                             "💡 Verify that WinDbg/CDB is properly installed.",
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Verify dump file path is correct and file exists",
                            "Check file permissions",
                            "Ensure debugging tools are installed",
                            "Try again with correct path"
                        },
                        CommonErrors = new List<string>
                        {
                            "File not found or inaccessible",
                            "Insufficient permissions",
                            "WinDbg/CDB not installed or configured"
                        }
                    }
                };
                
                return errorResponse;
            }
        }

        /// <summary>
        /// Close a debugging session and clean up resources
        /// </summary>
        [Description("🔒 CLOSE SESSION: Close a debugging session and free resources. Use when done with analysis.")]
        public async Task<object> nexus_close_dump_analyze_session(
            [Description("Session ID to close")] string sessionId)
        {
            logger.LogInformation("🔒 Closing debugging session: {SessionId}", sessionId);

            try
            {
                var sessionExists = sessionManager.SessionExists(sessionId);
                if (!sessionExists)
                {
                    var notFoundResponse = new SessionAwareResponse
                    {
                        SessionId = sessionId,
                        Result = $"⚠️ Session '{sessionId}' not found or already closed.\n\n" +
                                "This could be normal if the session expired due to inactivity.",
                        AIGuidance = new AIGuidance
                        {
                            NextSteps = new List<string>
                            {
                                "Verify the sessionId is correct",
                                "Create a new session if needed with nexus_open_dump"
                            },
                            CommonErrors = new List<string>
                            {
                                "Using expired or invalid sessionId",
                                "Session was already closed"
                            }
                        }
                    };
                    
                    return notFoundResponse;
                }

                var context = sessionManager.GetSessionContext(sessionId);
                var closed = await sessionManager.CloseSessionAsync(sessionId);

                // Return comprehensive response with all critical information
                var response = new
                {
                    // CRITICAL IDENTIFIERS - Always include these
                    sessionId = sessionId,
                    dumpFile = (string?)null, // No dump file for close operation
                    commandId = (string?)null, // No commandId for this operation
                    
                    // OPERATION STATUS
                    success = closed,
                    operation = "nexus_close_dump_analyze_session",
                    status = closed ? "completed" : "warning",
                    message = closed 
                        ? $"Session closed: {sessionId}"
                        : $"Session may have already been closed: {sessionId}",
                    
                    // WORKFLOW GUIDANCE
                    nextCommand = closed ? "nexus_open_dump_analyze_session" : null,
                    nextCommandParameters = closed ? new { dumpPath = "<new-dump-file-path>" } : null,
                    workflow = closed ? "Session closed successfully. Use nexus_open_dump_analyze_session to start a new session." : "Session cleanup in progress.",
                    
                    // CRITICAL REMINDERS
                    closedSessionId = sessionId, // Extra copy for tracking
                    resourcesFreed = closed,
                    instructions = closed ? "Session successfully closed. Start new session with nexus_open_dump_analyze_session if needed." : "Session cleanup in progress."
                };

                logger.LogInformation("✅ Session {SessionId} closed successfully", sessionId);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error closing session {SessionId}", sessionId);
                
                var errorResponse = new SessionAwareResponse
                {
                    SessionId = sessionId,
                    Result = $"❌ Error closing session: {ex.Message}\n\n" +
                             "The session may still be partially active. Resources will be cleaned up automatically.",
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Session cleanup will happen automatically",
                            "Monitor for session expiry notifications",
                            "Create new session if needed"
                        }
                    }
                };
                
                return errorResponse;
            }
        }

        #endregion

        #region Command Execution Tools

        /// <summary>
        /// Execute a debugger command asynchronously in the specified session
        /// </summary>
        [Description("🔄 ASYNC COMMAND: Execute debugger command in background queue. NEVER returns results directly! Always returns commandId. MUST use nexus_dump_analyze_session_async_command_status to get actual results.")]
        public Task<object> nexus_dump_analyze_session_async_command(
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("Debugger command to execute (e.g., '!analyze -v', 'k', '!peb')")] string command)
        {
            logger.LogInformation("🔄 Executing async command in session {SessionId}: {Command}", sessionId, command);

            return Task.FromResult(ExecuteCommandSync(sessionId, command));
        }

        private object ExecuteCommandSync(string sessionId, string command)
        {
            try
            {
                // Validate session
                if (!sessionManager.SessionExists(sessionId))
                {
                var sessionNotFoundResponse = new SessionAwareResponse
                {
                    SessionId = sessionId,
                    Result = $"❌ INVALID SESSION ID: '{sessionId}' not found or expired!\n\n" +
                             "🚨 CRITICAL AI CLIENT ERROR: You're using a WRONG or MADE-UP sessionId!\n\n" +
                             "🔧 EXACT RECOVERY STEPS:\n" +
                             "1️⃣ CALL nexus_open_dump with a .dmp file path\n" +
                             "2️⃣ EXTRACT the 'sessionId' field from the JSON response\n" +
                             "3️⃣ USE that EXACT sessionId value here (e.g., 'sess-000001-abc12345')\n" +
                             "4️⃣ DO NOT make up your own sessionId values!\n\n" +
                             "💡 EXAMPLE: If nexus_open_dump returns sessionId='sess-000001-feaf4c54', then use EXACTLY that value!\n" +
                             "❌ WRONG: 'session_12345', 'my_session', 'test_session'\n" +
                             "✅ CORRECT: Copy the sessionId from nexus_open_dump response JSON",
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Call nexus_open_dump and extract the returned sessionId from response JSON",
                            "Use the EXACT sessionId value returned (starts with 'sess-')",
                            "Stop making up your own sessionId values"
                        },
                        CommonErrors = new List<string>
                        {
                            "Making up sessionId instead of using the one from nexus_open_dump",
                            "Using expired or incorrect sessionId",
                            "Not extracting sessionId from nexus_open_dump response"
                        }
                    }
                };
                    
                    return sessionNotFoundResponse;
                }

                // Get command queue for session
                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                var commandId = commandQueue.QueueCommand(command);
                var context = sessionManager.GetSessionContext(sessionId);

                // Return comprehensive response with all critical information
                var response = new
                {
                    // CRITICAL IDENTIFIERS - Always include these
                    sessionId = sessionId,
                    dumpFile = context?.DumpPath != null ? Path.GetFileName(context.DumpPath) : null,
                    commandId = commandId,
                    
                    // OPERATION STATUS
                    success = true,
                    operation = "nexus_dump_analyze_session_async_command",
                    status = "queued",
                    message = $"Command queued: {commandId}",
                    
                    // WORKFLOW GUIDANCE
                    nextCommand = "nexus_dump_analyze_session_async_command_status",
                    nextCommandParameters = new { commandId = commandId },
                    workflow = "STEP 2 COMPLETE → NEXT: nexus_dump_analyze_session_async_command_status → POLL until status='completed'",
                    
                    // CRITICAL REMINDERS
                    extractCommandId = commandId, // Extra copy for extraction
                    useThisCommandId = commandId, // Another copy for clarity
                    useThisSessionId = sessionId, // Session reminder
                    instructions = $"SAVE this commandId '{commandId}' and use it in nexus_dump_analyze_session_async_command_status to get results!",
                    polling = "Check nexus_dump_analyze_session_async_command_status every 3-5 seconds until status='completed'"
                };

                logger.LogInformation("✅ Command {CommandId} queued in session {SessionId}", commandId, sessionId);
                return response;
            }
            catch (SessionNotFoundException ex)
            {
                logger.LogWarning("❌ Session not found: {SessionId}", sessionId);
                
                var errorResponse = new SessionAwareResponse
                {
                    SessionId = sessionId,
                    Result = $"❌ Session not found: {ex.Message}\n\n" +
                             "🔧 The session may have expired or was never created.",
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Create new session with nexus_open_dump",
                            "Check session expiry notifications",
                            "Verify sessionId spelling"
                        }
                    }
                };
                
                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error executing command in session {SessionId}: {Command}", sessionId, command);
                
                var errorResponse = new SessionAwareResponse
                {
                    SessionId = sessionId,
                    Result = $"❌ Error executing command: {ex.Message}\n\n" +
                             "🔧 Check session status and command syntax.",
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Verify command syntax is correct",
                            "Check session is still active",
                            "Try again with corrected command"
                        }
                    }
                };
                
                return errorResponse;
            }
        }

        /// <summary>
        /// Get the status and result of a previously queued command
        /// </summary>
        [Description("📊 COMMAND STATUS: Get the result/status of a queued command. This is how you get actual command results from async operations.")]
        public async Task<object> nexus_dump_analyze_session_async_command_status(
            [Description("Command ID returned by nexus_dump_analyze_session_async_command")] string commandId)
        {
            logger.LogInformation("📊 Checking command status: {CommandId}", commandId);

            return await GetCommandStatusAsync(commandId);
        }

        private async Task<object> GetCommandStatusAsync(string commandId)
        {
            try
            {
                // Extract session ID from command ID format (strict validation)
                var parts = commandId.Split('-');
                if (parts.Length < 5) // cmd-sess-XXXXXX-YYYYYYYY-ZZZZ format
                {
                    var errorResponse = new SessionAwareResponse
                    {
                        Result = $"❌ INVALID COMMAND ID FORMAT: {commandId}\n\n" +
                                 "🔧 REQUIRED FORMAT: cmd-sess-XXXXXX-YYYYYYYY-ZZZZ\n" +
                                 "📋 EXAMPLE: cmd-sess-000001-abc12345-0001\n" +
                                 "❓ WHY THIS ERROR: You must use the exact commandId returned by nexus_exec_debugger_command_async\n" +
                                 "🎯 AI DEBUGGING TIP: Copy the commandId exactly from the previous response!",
                        AIGuidance = new AIGuidance
                        {
                            NextSteps = new List<string>
                            {
                                "Use the exact commandId from nexus_exec_debugger_command_async response",
                                "Check for typos or truncation in the commandId",
                                "Ensure you're copying the full commandId string"
                            }
                        }
                    };
                    
                    return errorResponse;
                }
                
                // Extract sessionId from commandId (by design, not a fallback)
                var sessionId = $"{parts[1]}-{parts[2]}-{parts[3]}"; // Extract "sess-XXXXXX-YYYYYYYY"

                // Validate session
                if (!sessionManager.SessionExists(sessionId))
                {
                    var sessionNotFoundResponse = new SessionAwareResponse
                    {
                        SessionId = sessionId,
                        Result = $"❌ INVALID SESSION ID: '{sessionId}' not found or expired for command '{commandId}'!\n\n" +
                                "🚨 CRITICAL AI CLIENT ERROR: You're using a WRONG or MADE-UP sessionId!\n\n" +
                                "🔧 EXACT RECOVERY STEPS:\n" +
                                "1️⃣ CALL nexus_open_dump with a .dmp file path\n" +
                                "2️⃣ EXTRACT the 'sessionId' field from the JSON response\n" +
                                "3️⃣ USE that EXACT sessionId value (e.g., 'sess-000001-abc12345')\n" +
                                "4️⃣ DO NOT make up your own sessionId values!\n\n" +
                                "💡 EXAMPLE: If nexus_open_dump returns sessionId='sess-000001-feaf4c54', then use EXACTLY that value!\n" +
                                "❌ WRONG: 'session_12345', 'my_session', 'test_session'\n" +
                                "✅ CORRECT: Copy the sessionId from nexus_open_dump response JSON",
                        AIGuidance = new AIGuidance
                        {
                            NextSteps = new List<string>
                            {
                                "Call nexus_open_dump and extract the returned sessionId from response JSON",
                                "Use the EXACT sessionId value returned (starts with 'sess-')",
                                "Stop making up your own sessionId values"
                            },
                            CommonErrors = new List<string>
                            {
                                "Making up sessionId instead of using the one from nexus_open_dump",
                                "Using expired or incorrect sessionId", 
                                "Not extracting sessionId from nexus_open_dump response"
                            }
                        }
                    };
                    
                    return sessionNotFoundResponse;
                }

                // Get command result
                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                var result = await commandQueue.GetCommandResult(commandId);
                var context = sessionManager.GetSessionContext(sessionId);

                // Determine status from result
                var status = result switch
                {
                    var r when r.StartsWith("Command is still") => "queued",
                    var r when r.StartsWith("Command not found") => "not_found",
                    var r when r.Contains("cancelled") => "cancelled",
                    var r when r.Contains("failed") => "failed",
                    _ => "completed"
                };

                // Return comprehensive response with all critical information
                var response = new
                {
                    // CRITICAL IDENTIFIERS - Always include these
                    sessionId = sessionId,
                    dumpFile = context?.DumpPath != null ? Path.GetFileName(context.DumpPath) : null,
                    commandId = commandId,
                    
                    // OPERATION STATUS
                    success = true,
                    operation = "nexus_dump_analyze_session_async_command_status",
                    status = status,
                    message = $"Command status: {status}",
                    
                    // COMMAND RESULT
                    result = result,
                    debuggerOutput = result, // Extra copy for clarity
                    
                    // WORKFLOW GUIDANCE
                    isComplete = status is "completed" or "failed" or "cancelled",
                    nextCommand = status is "completed" or "failed" or "cancelled" ? null : "nexus_dump_analyze_session_async_command_status",
                    nextCommandParameters = status is "completed" or "failed" or "cancelled" ? null : new { commandId = commandId },
                    workflow = status switch
                    {
                        "queued" or "executing" => "POLLING: Call nexus_dump_analyze_session_async_command_status again in 3-5 seconds",
                        "completed" => "STEP 3 COMPLETE → Use 'result' field for debugger output → Call nexus_close_dump_analyze_session when done",
                        "failed" => "COMMAND FAILED → Check 'result' for error details → Try new command or nexus_close_dump_analyze_session",
                        _ => "Check status and continue as needed"
                    },
                    
                    // CRITICAL REMINDERS
                    extractResult = status == "completed" ? result : null, // Extra copy for extraction
                    useThisCommandId = commandId, // Command reminder
                    useThisSessionId = sessionId, // Session reminder
                    instructions = status switch
                    {
                        "queued" or "executing" => $"Command '{commandId}' still processing. Check again in 3-5 seconds.",
                        "completed" => $"Command '{commandId}' completed! Use the 'result' field for debugger output.",
                        "failed" => $"Command '{commandId}' failed. Check 'result' field for error details.",
                        _ => $"Command '{commandId}' status: {status}"
                    },
                    polling = status is "queued" or "executing" ? "Check nexus_dump_analyze_session_async_command_status again in 3-5 seconds" : null
                };

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error checking command status: {CommandId}", commandId);
                
                var errorResponse = new SessionAwareResponse
                {
                    Result = $"❌ Error checking command status: {ex.Message}",
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Verify commandId is correct",
                            "Check session is still active",
                            "Try again or re-execute command"
                        }
                    }
                };
                
                return errorResponse;
            }
        }

        #endregion

        #region Utility Methods

        private AIGuidance GenerateStatusGuidance(string status, string commandId, string result)
        {
            return status switch
            {
                "queued" => new AIGuidance
                {
                    NextSteps = new List<string>
                    {
                        "Wait a moment and check status again",
                        "Monitor notifications for execution updates",
                        "Commands execute sequentially in session queue"
                    },
                    UsageHints = new List<string>
                    {
                        "🔄 Command is waiting in queue",
                        "📡 Watch for notifications/commandStatus updates",
                        "⏱️ Execution order is first-in-first-out per session"
                    }
                },
                "completed" => new AIGuidance
                {
                    NextSteps = new List<string>
                    {
                        "Analyze the command output",
                        "Execute follow-up commands based on results",
                        "Use output for next debugging steps"
                    },
                    UsageHints = new List<string>
                    {
                        "✅ Command completed successfully",
                        "📊 Full output is available in the result",
                        "🎯 Use results to guide next debugging steps"
                    }
                },
                "failed" => new AIGuidance
                {
                    NextSteps = new List<string>
                    {
                        "Check command syntax and try again",
                        "Verify debugging context is correct",
                        "Try alternative command approaches"
                    },
                    CommonErrors = new List<string>
                    {
                        "Invalid command syntax",
                        "Command not applicable to current context",
                        "Missing required debugging symbols"
                    }
                },
                _ => new AIGuidance
                {
                    NextSteps = new List<string>
                    {
                        "Check result details for more information",
                        "Consider re-executing if needed"
                    }
                }
            };
        }


        #endregion
    }
}