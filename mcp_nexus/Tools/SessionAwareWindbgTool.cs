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
        public async Task<string> nexus_open_dump(
            [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
            [Description("Optional path to symbol files directory")] string? symbolsPath = null)
        {
            logger.LogInformation("🔓 Opening new debugging session for dump: {DumpPath}", dumpPath);

            try
            {
                // Create new session
                var sessionId = await sessionManager.CreateSessionAsync(dumpPath, symbolsPath);
                var context = sessionManager.GetSessionContext(sessionId);

                var response = new SessionAwareResponse
                {
                    SessionId = sessionId,
                    Result = $"✅ Debugging session created successfully!\n\n" +
                             $"📊 Session ID: {sessionId}\n" +
                             $"📁 Dump File: {Path.GetFileName(dumpPath)}\n" +
                             $"🔍 Symbols: {symbolsPath ?? "Default symbol paths"}\n" +
                             $"⏰ Created: {context.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                             $"🎯 IMPORTANT: Use sessionId='{sessionId}' for ALL subsequent commands!",
                    SessionContext = context,
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            $"🔄 ASYNC WORKFLOW: Use nexus_exec_debugger_command_async with sessionId='{sessionId}' to queue commands",
                            "📡 CRITICAL: nexus_exec_debugger_command_async only returns commandId, NOT results!",
                            "🎯 MANDATORY: Call nexus_debugger_command_status(commandId) to get actual debugger output",
                            "💡 Start with basic commands like '!analyze -v' or 'k' (stack trace)",
                            "📊 Monitor notifications for real-time command progress"
                        },
                        UsageHints = new List<string>
                        {
                            "🔄 All commands execute asynchronously - always check status",
                            "📡 Listen for notifications to get real-time updates",
                            "⏰ Session will auto-expire after 30 minutes of inactivity",
                            "🎯 Always include the sessionId in your requests"
                        },
                        CommonErrors = new List<string>
                        {
                            "❌ Missing sessionId parameter in subsequent calls",
                            "❌ Using expired or invalid sessionId",
                            "❌ CRITICAL: Expecting immediate results from nexus_exec_debugger_command_async",
                            "❌ CRITICAL: Not calling nexus_debugger_command_status to get actual results",
                            "❌ Not understanding that commands execute asynchronously"
                        }
                    },
                    WorkflowContext = new WorkflowContext
                    {
                        CurrentStep = "Session Created",
                        SuggestedNextCommands = new List<string>
                        {
                            "!analyze -v  // Comprehensive crash analysis",
                            "k           // Call stack",
                            "!peb        // Process environment block",
                            "lm          // Loaded modules"
                        },
                        SessionState = "Ready for commands"
                    }
                };

                logger.LogInformation("✅ Session {SessionId} created successfully", sessionId);
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
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
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
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
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        /// <summary>
        /// Close a debugging session and clean up resources
        /// </summary>
        [Description("🔒 CLOSE SESSION: Close a debugging session and free resources. Use when done with analysis.")]
        public async Task<string> nexus_close_dump(
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
                    
                    return JsonSerializer.Serialize(notFoundResponse, new JsonSerializerOptions { WriteIndented = true });
                }

                var context = sessionManager.GetSessionContext(sessionId);
                var closed = await sessionManager.CloseSessionAsync(sessionId);

                var response = new SessionAwareResponse
                {
                    SessionId = sessionId,
                    Result = closed 
                        ? $"✅ Session '{sessionId}' closed successfully!\n\n" +
                          $"📊 Session Statistics:\n" +
                          $"• Commands Processed: {context.CommandsProcessed}\n" +
                          $"• Session Duration: {DateTime.UtcNow - context.CreatedAt:hh\\:mm\\:ss}\n" +
                          $"• Last Activity: {context.LastActivity:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
                          "🧹 All resources have been cleaned up."
                        : $"⚠️ Session '{sessionId}' may have already been closed or expired.",
                    SessionContext = context,
                    AIGuidance = new AIGuidance
                    {
                        NextSteps = new List<string>
                        {
                            "Session is now closed and resources freed",
                            "Create new session with nexus_open_dump if needed",
                            "All commands for this session are now invalid"
                        },
                        UsageHints = new List<string>
                        {
                            "🧹 Always close sessions when done to free resources",
                            "📊 Session statistics show the debugging activity",
                            "🔄 Create new sessions for different dump files"
                        }
                    },
                    WorkflowContext = new WorkflowContext
                    {
                        CurrentStep = "Session Closed",
                        SessionState = "Terminated"
                    }
                };

                logger.LogInformation("✅ Session {SessionId} closed successfully", sessionId);
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
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
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        #endregion

        #region Command Execution Tools

        /// <summary>
        /// Execute a debugger command asynchronously in the specified session
        /// </summary>
        [Description("🔄 ASYNC COMMAND: Execute debugger command in background queue. NEVER returns results directly! Always returns commandId. MUST use nexus_debugger_command_status to get actual results.")]
        public Task<string> nexus_exec_debugger_command_async(
            [Description("Session ID from nexus_open_dump")] string sessionId,
            [Description("Debugger command to execute (e.g., '!analyze -v', 'k', '!peb')")] string command)
        {
            logger.LogInformation("🔄 Executing async command in session {SessionId}: {Command}", sessionId, command);

            return Task.FromResult(ExecuteCommandSync(sessionId, command));
        }

        private string ExecuteCommandSync(string sessionId, string command)
        {
            try
            {
                // Validate session
                if (!sessionManager.SessionExists(sessionId))
                {
                    var sessionNotFoundResponse = new SessionAwareResponse
                    {
                        SessionId = sessionId,
                        Result = $"❌ Session '{sessionId}' not found or expired!\n\n" +
                                "🔧 Create a new session with nexus_open_dump first.",
                        AIGuidance = new AIGuidance
                        {
                            NextSteps = new List<string>
                            {
                                "Create new session with nexus_open_dump",
                                "Verify sessionId is correct",
                                "Check for session expiry notifications"
                            },
                            CommonErrors = new List<string>
                            {
                                "Using expired sessionId",
                                "Session was closed or never created",
                                "Typo in sessionId parameter"
                            }
                        }
                    };
                    
                    return JsonSerializer.Serialize(sessionNotFoundResponse, new JsonSerializerOptions { WriteIndented = true });
                }

                // Get command queue for session
                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                var commandId = commandQueue.QueueCommand(command);
                var context = sessionManager.GetSessionContext(sessionId);

                // Return simple response with command ID for MCP protocol
                var response = new
                {
                    commandId = commandId,
                    sessionId = sessionId,
                    message = "✅ Command queued successfully! Use nexus_debugger_command_status(commandId) to get results.",
                    nextStep = $"nexus_debugger_command_status('{commandId}')"
                };

                logger.LogInformation("✅ Command {CommandId} queued in session {SessionId}", commandId, sessionId);
                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
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
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
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
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
            }
        }

        /// <summary>
        /// Get the status and result of a previously queued command
        /// </summary>
        [Description("📊 COMMAND STATUS: Get the result/status of a queued command. This is how you get actual command results from async operations.")]
        public async Task<string> nexus_debugger_command_status(
            [Description("Command ID returned by nexus_exec_debugger_command_async")] string commandId,
            [Description("Optional: Session ID for context (auto-detected from commandId if not provided)")] string? sessionId = null)
        {
            logger.LogInformation("📊 Checking command status: {CommandId}", commandId);

            return await GetCommandStatusAsync(commandId, sessionId);
        }

        private async Task<string> GetCommandStatusAsync(string commandId, string? sessionId)
        {
            try
            {
                // Extract session ID from command ID if not provided
                if (string.IsNullOrEmpty(sessionId))
                {
                    var parts = commandId.Split('-');
                    if (parts.Length >= 5) // cmd-sess-XXXXXX-YYYYYYYY-ZZZZ format
                    {
                        sessionId = $"{parts[1]}-{parts[2]}-{parts[3]}"; // Extract "sess-XXXXXX-YYYYYYYY"
                    }
                    else
                    {
                        var errorResponse = new SessionAwareResponse
                        {
                            Result = $"❌ Invalid command ID format: {commandId}\n\n" +
                                     "Command IDs should be in format: cmd-sess-XXXXXX-YYYYYYYY-ZZZZ",
                            AIGuidance = new AIGuidance
                            {
                                NextSteps = new List<string>
                                {
                                    "Verify commandId is from nexus_exec_debugger_command_async",
                                    "Check for typos in commandId",
                                    "Provide sessionId parameter if commandId format is unclear"
                                }
                            }
                        };
                        
                        return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
                    }
                }

                // Validate session
                if (!sessionManager.SessionExists(sessionId))
                {
                    var sessionNotFoundResponse = new SessionAwareResponse
                    {
                        SessionId = sessionId,
                        Result = $"❌ Session '{sessionId}' not found or expired!\n\n" +
                                "The command may have been lost due to session cleanup.",
                        AIGuidance = new AIGuidance
                        {
                            NextSteps = new List<string>
                            {
                                "Create new session with nexus_open_dump",
                                "Re-execute the command in the new session",
                                "Monitor for session expiry notifications"
                            }
                        }
                    };
                    
                    return JsonSerializer.Serialize(sessionNotFoundResponse, new JsonSerializerOptions { WriteIndented = true });
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

                var response = new SessionAwareResponse
                {
                    SessionId = sessionId,
                    Result = $"📊 Command Status: {status.ToUpper()}\n\n" +
                             $"Command ID: {commandId}\n" +
                             $"Session: {sessionId}\n" +
                             $"Status: {status}\n\n" +
                             $"🔍 Result:\n{result}",
                    SessionContext = context,
                    AIGuidance = GenerateStatusGuidance(status, commandId, result),
                    WorkflowContext = new WorkflowContext
                    {
                        CurrentStep = $"Command {status}",
                        SessionState = $"Last checked: {DateTime.UtcNow:HH:mm:ss}"
                    }
                };

                return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
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
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
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

        /// <summary>
        /// Get all active sessions for auto-detection when sessionId is missing
        /// </summary>
        public Task<IEnumerable<SessionContext>> GetActiveSessionsAsync()
        {
            logger.LogDebug("Getting active sessions for auto-detection");
            var activeSessions = sessionManager.GetActiveSessions();
            return Task.FromResult(activeSessions);
        }

        #endregion
    }
}