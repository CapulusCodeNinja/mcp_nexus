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
        public static readonly object USAGE_EXPLANATION = new
        {
            title = "Usage Guide",
            description = "Complete guide for using the Nexus MCP server tools and resources.",
            tools = new
            {
                title = "MCP Tools",
                description = "Core debugging tools for crash dump analysis",
                general_notes = new[]
                {
                    "Please get further details from the response and the tool listings of the MCP server.",
                    "After opening an analyze session, WinDBG commands can be asynchronously executed.",
                    "The result can be queried (regular polling) by the status API.",
                    "Opening a session without executing commands will not have any effect."
                },
                available_tools = new[]
                {
                    new
                    {
                        step_title = "Tooling - Open Session",
                        tool_name = "nexus_open_dump_analyze_session",
                        action = "Open the analyze session for the dump file with the tool from Nexus MCP server.",
                        output = (string?)"sessionid",
                        note = (string?)"This EXACT sessionid IS REQUIRED TO BE USED for all following commands in the session."
                    },
                    new
                    {
                        step_title = "Tooling - Exec Command",
                        tool_name = "nexus_dump_analyze_session_async_command",
                        action = "Use the tool to start asynchronous execution of the WinDBG commands.",
                        output = (string?)"commandId",
                        note = (string?)"This EXACT commandId IS REQUIRED TO BE USED for the nexus_dump_analyze_session_async_command_status commands to get the asynchronous result."
                    },
                    new
                    {
                        step_title = "Tooling - Get Command Status",
                        tool_name = "nexus_dump_analyze_session_async_command_status",
                        action = "Use the tool to poll for the status or result of the specific asynchronous execution of the WinDBG commands.",
                        output = (string?)"result",
                        note = (string?)null
                    },
                    new
                    {
                        step_title = "Tooling - Close Session",
                        tool_name = "nexus_close_dump_analyze_session",
                        action = "Use the tool to close the analyze session of the dump file after all commands are executed or the session is not needed anymore.",
                        output = (string?)null,
                        note = (string?)null
                    }
                }
            },
            resources = new
            {
                title = "MCP Resources",
                description = "Additional resources and documentation available through MCP",
                available_resources = new[]
                {
                    new
                    {
                        uri = "debugging://docs/crash-analysis-workflows",
                        name = "Crash Analysis Workflows",
                        description = "Comprehensive step-by-step analysis workflows for Windows crash dump investigation"
                    },
                    new
                    {
                        uri = "debugging://docs/usage",
                        name = "Usage",
                        description = "Essential tool usage information for MCP Nexus server"
                    },
                    new
                    {
                        uri = "debugging://tools/sessions",
                        name = "List Sessions",
                        description = "List all active debugging sessions"
                    },
                    new
                    {
                        uri = "debugging://tools/commands",
                        name = "List Commands",
                        description = "List async commands from all sessions or filter by specific session"
                    },
                    new
                    {
                        uri = "debugging://tools/command-result",
                        name = "Command Result",
                        description = "Get status and results of a specific async command"
                    }
                },
                usage_notes = new[]
                {
                    "Access resources using the MCP resources/list and resources/read methods",
                    "Resources provide additional context and documentation beyond core tools",
                    "Session management is now handled through resources for better integration"
                }
            }
        };

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

                // Return standardized response with required fields
                var response = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = sessionId,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    success = true,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Session created successfully: {sessionId}"
                };

                logger.LogInformation("✅ Session {SessionId} created successfully", sessionId);
                return response;
            }
            catch (SessionLimitExceededException ex)
            {
                logger.LogWarning("❌ Session limit exceeded: {Message}", ex.Message);

                var errorResponse = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = (string?)null,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    success = false,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Maximum concurrent sessions exceeded: {ex.CurrentSessions}/{ex.MaxSessions}"
                };

                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to create debugging session for {DumpPath}", dumpPath);

                var errorResponse = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = (string?)null,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    success = false,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Failed to create debugging session: {ex.Message}"
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
                    var notFoundResponse = new
                    {
                        usage = USAGE_EXPLANATION,
                        sessionId = sessionId,
                        dumpFile = (string?)null,
                        commandId = (string?)null,
                        success = false,
                        operation = "nexus_close_dump_analyze_session",
                        message = $"Session not found or already closed: {sessionId}"
                    };

                    return notFoundResponse;
                }

                var context = sessionManager.GetSessionContext(sessionId);
                var closed = await sessionManager.CloseSessionAsync(sessionId);

                // Return standardized response with required fields
                var response = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    success = closed,
                    operation = "nexus_close_dump_analyze_session",
                    message = closed
                        ? $"Session closed successfully: {sessionId}"
                        : $"Session may have already been closed: {sessionId}"
                };

                logger.LogInformation("✅ Session {SessionId} closed successfully", sessionId);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error closing session {SessionId}", sessionId);

                var errorResponse = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    success = false,
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Error closing session: {ex.Message}"
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
                    var sessionNotFoundResponse = new
                    {
                        usage = USAGE_EXPLANATION,
                        sessionId = sessionId,
                        dumpFile = (string?)null,
                        commandId = (string?)null,
                        success = false,
                        operation = "nexus_dump_analyze_session_async_command",
                        message = $"Session not found or expired: {sessionId}"
                    };

                    return sessionNotFoundResponse;
                }

                // Get command queue for session
                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                var commandId = commandQueue.QueueCommand(command);
                var context = sessionManager.GetSessionContext(sessionId);

                // Return standardized response with required fields
                var response = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = sessionId,
                    dumpFile = context?.DumpPath != null ? Path.GetFileName(context.DumpPath) : null,
                    commandId = commandId,
                    success = true,
                    operation = "nexus_dump_analyze_session_async_command",
                    message = $"Command queued successfully: {commandId}"
                };

                logger.LogInformation("✅ Command {CommandId} queued in session {SessionId}", commandId, sessionId);
                return response;
            }
            catch (SessionNotFoundException ex)
            {
                logger.LogWarning("❌ Session not found: {SessionId}", sessionId);

                var errorResponse = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    success = false,
                    operation = "nexus_dump_analyze_session_async_command",
                    message = $"Session not found: {ex.Message}"
                };

                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error executing command in session {SessionId}: {Command}", sessionId, command);

                var errorResponse = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    success = false,
                    operation = "nexus_dump_analyze_session_async_command",
                    message = $"Error executing command: {ex.Message}"
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
                    var errorResponse = new
                    {
                        usage = USAGE_EXPLANATION,
                        sessionId = (string?)null,
                        dumpFile = (string?)null,
                        commandId = commandId,
                        success = false,
                        operation = "nexus_dump_analyze_session_async_command_status",
                        message = $"Invalid command ID format: {commandId}"
                    };

                    return errorResponse;
                }

                // Extract sessionId from commandId (by design, not a fallback)
                var sessionId = $"{parts[1]}-{parts[2]}-{parts[3]}"; // Extract "sess-XXXXXX-YYYYYYYY"

                // Validate session
                if (!sessionManager.SessionExists(sessionId))
                {
                    var sessionNotFoundResponse = new
                    {
                        usage = USAGE_EXPLANATION,
                        sessionId = sessionId,
                        dumpFile = (string?)null,
                        commandId = commandId,
                        success = false,
                        operation = "nexus_dump_analyze_session_async_command_status",
                        message = $"Session not found or expired for command: {commandId}"
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

                // Return standardized response with required fields
                var response = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = sessionId,
                    dumpFile = context?.DumpPath != null ? Path.GetFileName(context.DumpPath) : null,
                    commandId = commandId,
                    success = true,
                    operation = "nexus_dump_analyze_session_async_command_status",
                    message = $"Command status: {status}",
                    result = result // Keep the debugger output
                };

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error checking command status: {CommandId}", commandId);

                var errorResponse = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = (string?)null,
                    dumpFile = (string?)null,
                    commandId = commandId,
                    success = false,
                    operation = "nexus_dump_analyze_session_async_command_status",
                    message = $"Error checking command status: {ex.Message}"
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