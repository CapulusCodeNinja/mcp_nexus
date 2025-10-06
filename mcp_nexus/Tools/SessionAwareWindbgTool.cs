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
    /// <param name="logger">The logger instance for recording tool operations and errors.</param>
    /// <param name="sessionManager">The session manager for managing debugging sessions.</param>
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
                    "TOOLS: Use tools/call method to execute debugging operations (open session, run commands, close session)",
                    "RESOURCES: Use resources/read method to access data (command lists, session lists, documentation, metrics)",
                    "After opening an analyze session, WinDBG commands can be asynchronously executed.",
                    "Command results can be accessed via the 'nexus_read_dump_analyze_command_result' tool or 'List Commands' resource.",
                    "Opening a session without executing commands will not have any effect."
                },
                available_tools = new object[]
                {
                    new
                    {
                        step_title = "Tooling - Open Session",
                        tool_name = "nexus_open_dump_analyze_session",
                        action = "Open the analyze session for the dump file with the tool from Nexus MCP server.",
                        input = new { dumpPath = "string (required)", symbolsPath = "string (optional)" },
                        output = (string?)"sessionid",
                        note = (string?)"This EXACT sessionid IS REQUIRED TO BE USED for all following commands in the session."
                    },
                    new
                    {
                        step_title = "Tooling - Exec Command",
                        tool_name = "nexus_enqueue_async_dump_analyze_command",
                        action = "Use the tool to start asynchronous execution of the WinDBG commands.",
                        input = new { command = "string (required)", sessionId = "string (required)" },
                        output = (string?)"commandId",
                        note = (string?)"This EXACT commandId IS REQUIRED TO BE USED for the 'nexus_read_dump_analyze_command_result' tool to get the asynchronous result."
                    },
                    new
                    {
                        step_title = "Tooling - Close Session",
                        tool_name = "nexus_close_dump_analyze_session",
                        action = "Use the tool to close the analyze session of the dump file after all commands are executed or the session is not needed anymore.",
                        input = new { sessionId = "string (required)" },
                        output = (string?)null,
                        note = (string?)null
                    },
                    new
                    {
                        step_title = "Tooling - Get Command Result",
                        tool_name = "nexus_read_dump_analyze_command_result",
                        action = "Get status and results of a specific async command that was previously queued.",
                        input = new { sessionId = "string (required)", commandId = "string (required)" },
                        output = (string?)"command result and status",
                        note = (string?)"Use this tool to retrieve results from commands executed with nexus_enqueue_async_dump_analyze_command"
                    }
                }
            },
            resources = new
            {
                title = "MCP Resources",
                description = "Access data and results using resources/read method (NOT tools/call)",
                usage_notes = new[]
                {
                    "Use resources/read method to access these resources",
                    "Resources provide data access, tools provide action execution"
                },
                available_resources = new object[]
                {
                    new
                    {
                        uri = "workflows",
                        name = "Crash Analysis Workflows",
                        description = "Comprehensive step-by-step analysis workflows for Windows crash dump investigation",
                        input = (object?)null,
                        note = (string?)"Access via MCP resources/read method"
                    },
                    new
                    {
                        uri = "usage",
                        name = "Usage",
                        description = "Essential tool usage information for MCP Nexus server",
                        input = (object?)null,
                        note = (string?)"Access via MCP resources/read method"
                    },
                    new
                    {
                        uri = "sessions",
                        name = "List Sessions",
                        description = "List all debugging sessions with status and activity information",
                        input = (object?)null,
                        note = (string?)"Use: sessions resource (no parameters - returns all sessions)"
                    },
                    new
                    {
                        uri = "commands",
                        name = "List Commands",
                        description = "List async commands from all sessions with status and timing information",
                        input = (object?)null,
                        note = (string?)"Use: commands resource (no parameters - returns all commands)"
                    },
                    new
                    {
                        uri = "metrics",
                        name = "Performance Metrics",
                        description = "Get comprehensive performance metrics and statistics",
                        input = (object?)null,
                        note = (string?)"Use: metrics resource (no parameters - returns performance data)"
                    },
                    new
                    {
                        uri = "circuits",
                        name = "Circuit Breaker Status",
                        description = "Get circuit breaker status and health information",
                        input = (object?)null,
                        note = (string?)"Use: circuits resource (no parameters - returns circuit status)"
                    },
                    new
                    {
                        uri = "health",
                        name = "System Health",
                        description = "Get comprehensive system health status",
                        input = (object?)null,
                        note = (string?)"Use: health resource (no parameters - returns health status)"
                    },
                    new
                    {
                        uri = "cache",
                        name = "Cache Statistics",
                        description = "Get cache statistics and memory usage information",
                        input = (object?)null,
                        note = (string?)"Use: cache resource (no parameters - returns cache stats)"
                    }
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
                // Validate dump file exists before creating session
                if (!File.Exists(dumpPath))
                {
                    logger.LogError("Dump file does not exist: {DumpPath}", dumpPath);

                    var errorResponse = new
                    {
                        sessionId = (string?)null,
                        dumpFile = Path.GetFileName(dumpPath),
                        commandId = (string?)null,
                        status = "Failed",
                        operation = "nexus_open_dump_analyze_session",
                        message = $"Dump file does not exist: {dumpPath}",
                        usage = USAGE_EXPLANATION
                    };

                    return errorResponse;
                }

                // Create new session
                var sessionId = await sessionManager.CreateSessionAsync(dumpPath, symbolsPath);
                var context = sessionManager.GetSessionContext(sessionId);

                // Return standardized response with required fields
                var response = new
                {
                    sessionId = sessionId,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    status = "Success",
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Session created successfully: {sessionId}. Use 'sessions' resource to manage sessions.",
                    sessionInfo = new
                    {
                        maxCommands = "Unlimited commands per session",
                        status = "Active and ready for commands",
                        important = "⚠️ IMPORTANT: Call 'nexus_close_dump_analyze_session' when done to free resources immediately"
                    },
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };

                logger.LogInformation("Session {SessionId} created successfully", sessionId);
                return response;
            }
            catch (SessionLimitExceededException ex)
            {
                logger.LogWarning("Session limit exceeded: {Message}", ex.Message);

                var errorResponse = new
                {
                    usage = USAGE_EXPLANATION,
                    sessionId = (string?)null,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    status = "Failed",
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Maximum concurrent sessions exceeded: {ex.CurrentSessions}/{ex.MaxSessions}"
                };

                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create debugging session for {DumpPath}", dumpPath);

                var errorResponse = new
                {
                    sessionId = (string?)null,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    status = "Failed",
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Failed to create debugging session: {ex.Message}",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
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
                        sessionId = sessionId,
                        dumpFile = (string?)null,
                        commandId = (string?)null,
                        status = "Failed",
                        operation = "nexus_close_dump_analyze_session",
                        message = $"Session not found or already closed: {sessionId}. Use 'sessions' resource to see available sessions.",
                        usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                    };

                    return notFoundResponse;
                }

                var context = sessionManager.GetSessionContext(sessionId);
                var closed = await sessionManager.CloseSessionAsync(sessionId);

                // Return standardized response with required fields
                var response = new
                {
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    status = closed ? "Success" : "Failed",
                    operation = "nexus_close_dump_analyze_session",
                    message = closed
                        ? $"Session closed successfully: {sessionId}"
                        : $"Session may have already been closed: {sessionId}",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };

                logger.LogInformation("Session {SessionId} closed successfully", sessionId);
                return response;
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning("Invalid session ID (null): {Message}", ex.Message);
                var errorResponse = new
                {
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    status = "Failed",
                    operation = "nexus_close_dump_analyze_session",
                    message = "Session ID cannot be null",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };
                return errorResponse;
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning("Invalid session ID (empty/whitespace): {Message}", ex.Message);
                var errorResponse = new
                {
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    status = "Failed",
                    operation = "nexus_close_dump_analyze_session",
                    message = "Session ID cannot be empty or whitespace",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };
                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing session {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    status = "Failed",
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Error closing session: {ex.Message}",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };

                return errorResponse;
            }
        }

        #endregion

        #region Command Execution Tools

        /// <summary>
        /// Execute a debugger command asynchronously in the specified session
        /// </summary>
        [Description("ASYNC COMMAND: Execute debugger command in background queue. NEVER returns results directly! Always returns commandId. Use the resource 'commands' to observe status and the 'nexus_read_dump_analyze_command_result' tool to get actual results.")]
        public Task<object> nexus_enqueue_async_dump_analyze_command(
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("Debugger command to execute (e.g., '!analyze -v', 'k', '!peb')")] string command)
        {
            logger.LogInformation("Executing async command in session {SessionId}: {Command}", sessionId, command);

            return Task.FromResult(ExecuteCommandSync(sessionId, command));
        }

        /// <summary>
        /// Executes a command synchronously against the specified session.
        /// </summary>
        /// <param name="sessionId">The ID of the session to execute the command against.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>The result of the command execution.</returns>
        private object ExecuteCommandSync(string sessionId, string command)
        {
            try
            {
                // Validate session
                if (!sessionManager.SessionExists(sessionId))
                {
                    var sessionNotFoundResponse = new
                    {
                        sessionId = sessionId,
                        dumpFile = (string?)null,
                        commandId = (string?)null,
                        status = "Failed",
                        operation = "nexus_enqueue_async_dump_analyze_command",
                        message = $"Session not found or expired: {sessionId}. Use 'sessions' resource to see available sessions.",
                        usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
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
                    sessionId = sessionId,
                    dumpFile = context?.DumpPath != null ? Path.GetFileName(context.DumpPath) : null,
                    commandId = commandId,
                    status = "Success",
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Command queued successfully: {commandId}. Use the resource 'commands' to observe the status and the 'nexus_read_dump_analyze_command_result' tool to get results.",
                    commandInfo = new
                    {
                        command = command,
                        queuePosition = 0, // Will be updated by queue service
                        estimatedExecutionTime = GetEstimatedExecutionTime(command),
                        status = "Queued",
                        nextSteps = new[]
                        {
                            "Command will execute automatically",
                            "Use 'nexus_read_dump_analyze_command_result' to get results",
                            "Use 'commands' resource to monitor all commands",
                            "⚠️ IMPORTANT: Call 'nexus_close_dump_analyze_session' when analysis is complete"
                        }
                    },
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };

                logger.LogInformation("Command {CommandId} queued in session {SessionId}", commandId, sessionId);
                return response;
            }
            catch (SessionNotFoundException ex)
            {
                logger.LogWarning("Session not found: {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    status = "Failed",
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Session not found: {ex.Message}",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };

                return errorResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing command in session {SessionId}: {Command}", sessionId, command);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    status = "Failed",
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Error executing command: {ex.Message}",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };

                return errorResponse;
            }
        }


        #endregion

        #region Utility Methods

        /// <summary>
        /// Estimates execution time for a command based on command type
        /// </summary>
        /// <param name="command">The command to estimate</param>
        /// <returns>Estimated execution time description</returns>
        private string GetEstimatedExecutionTime(string command)
        {
            var cmd = command.Trim().ToLowerInvariant();

            return cmd switch
            {
                var c when c.StartsWith("!analyze") => "30-60 seconds (complex analysis)",
                var c when c.StartsWith("lm") => "1-3 seconds (module listing)",
                var c when c.StartsWith("!peb") => "1-2 seconds (process environment)",
                var c when c.StartsWith("k") || c.StartsWith("!k") => "1-2 seconds (stack trace)",
                var c when c.StartsWith("!threads") => "2-5 seconds (thread listing)",
                var c when c.StartsWith("!heap") => "5-15 seconds (heap analysis)",
                var c when c.StartsWith("!gchandles") => "3-8 seconds (GC handles)",
                var c when c.StartsWith("!dump") => "5-30 seconds (memory dumps)",
                _ => "1-10 seconds (varies by command complexity)"
            };
        }

        /// <summary>
        /// Generates AI guidance based on command status and results.
        /// </summary>
        /// <param name="status">The current status of the command.</param>
        /// <param name="commandId">The ID of the command.</param>
        /// <param name="result">The result of the command execution.</param>
        /// <returns>AI guidance information for the user.</returns>
        private AIGuidance GenerateStatusGuidance(string status, string commandId, string result)
        {
            return status switch
            {
                "queued" => new AIGuidance
                {
                    NextSteps = new List<string>
                    {
                        "Wait a moment and check status again",
                        "Use 'commands' resource to monitor execution status",
                        "Commands execute sequentially in session queue"
                    },
                    UsageHints = new List<string>
                    {
                        "Command is waiting in queue",
                        "Check 'commands' resource for status updates",
                        "Execution order is first-in-first-out per session"
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
                        "Command completed successfully",
                        "Full output is available in the result",
                        "Use results to guide next debugging steps"
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