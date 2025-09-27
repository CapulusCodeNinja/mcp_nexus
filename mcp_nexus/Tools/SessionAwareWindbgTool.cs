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
                    "TOOLS: Use tools/call method to execute debugging operations (open session, run commands, close session)",
                    "RESOURCES: Use resources/read method to access data (command results, session lists, documentation)",
                    "After opening an analyze session, WinDBG commands can be asynchronously executed.",
                    "Command results can be accessed via the 'Command Result' resource or 'List Commands' resource.",
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
                        note = (string?)"This EXACT commandId IS REQUIRED TO BE USED for the 'Command Result' resource to get the asynchronous result."
                    },
                    new
                    {
                        step_title = "Tooling - Close Session",
                        tool_name = "nexus_close_dump_analyze_session",
                        action = "Use the tool to close the analyze session of the dump file after all commands are executed or the session is not needed anymore.",
                        input = new { sessionId = "string (required)" },
                        output = (string?)null,
                        note = (string?)null
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
                        uri = "mcp://nexus/docs/workflows",
                        name = "Crash Analysis Workflows",
                        description = "Comprehensive step-by-step analysis workflows for Windows crash dump investigation",
                        input = (object?)null,
                        note = (string?)"Access via MCP resources/read method"
                    },
                    new
                    {
                        uri = "mcp://nexus/docs/usage",
                        name = "Usage",
                        description = "Essential tool usage information for MCP Nexus server",
                        input = (object?)null,
                        note = (string?)"Access via MCP resources/read method"
                    },
                    new
                    {
                        uri = "mcp://nexus/sessions/list",
                        name = "List Sessions",
                        description = "List all debugging sessions with advanced filtering options",
                        input = new {
                            sessionId = "string (optional) - Filter by session ID (partial match)",
                            dumpPath = "string (optional) - Filter by dump file path (partial match)",
                            status = "string (optional) - Filter by session status (Initializing, Active, Disposing, Disposed, Error)",
                            isActive = "bool (optional) - Filter by active status (true/false)",
                            createdFrom = "DateTime (optional) - Filter sessions created from this time",
                            createdTo = "DateTime (optional) - Filter sessions created until this time",
                            limit = "int (optional) - Limit number of results",
                            offset = "int (optional) - Skip number of results (pagination)",
                            sortBy = "string (optional) - Sort by field (sessionId, dumpPath, status, createdAt)",
                            order = "string (optional) - Sort order (asc, desc)"
                        },
                        note = (string?)"Use: mcp://nexus/sessions/list?status=Active&isActive=true&limit=10&sortBy=createdAt&order=desc"
                    },
                    new
                    {
                        uri = "mcp://nexus/commands/list",
                        name = "List Commands",
                        description = "List async commands from all sessions with advanced filtering options",
                        input = new {
                            sessionId = "string (optional) - Filter by specific session",
                            command = "string (optional) - Filter by command text (case-insensitive)",
                            from = "DateTime (optional) - Filter commands from this time",
                            to = "DateTime (optional) - Filter commands until this time",
                            limit = "int (optional) - Limit number of results",
                            offset = "int (optional) - Skip number of results (pagination)",
                            sortBy = "string (optional) - Sort by field (command, status, createdAt)",
                            order = "string (optional) - Sort order (asc, desc)"
                        },
                        note = (string?)"Use: mcp://nexus/commands/list?sessionId=abc123&command=!analyze&limit=10&sortBy=createdAt&order=desc"
                    },
                    new
                    {
                        uri = "mcp://nexus/commands/result",
                        name = "Command Result",
                        description = "Get status and results of a specific async command",
                        input = new { sessionId = "string (required)", commandId = "string (required)" },
                        example = "mcp://nexus/commands/result?sessionId=sess-000001-abc123&commandId=cmd-sess-000001-abc123-0001",
                        usage = "Use resources/read method with this URI to get command results",
                        note = (string?)"This is a RESOURCE, not a tool. Use resources/read method to access it."
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
                // Create new session
                var sessionId = await sessionManager.CreateSessionAsync(dumpPath, symbolsPath);
                var context = sessionManager.GetSessionContext(sessionId);

                // Return standardized response with required fields
                var response = new
                {
                    sessionId = sessionId,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    success = true,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Session created successfully: {sessionId}. Use mcp://nexus/sessions/list to manage sessions.",
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
                    success = false,
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
                    success = false,
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
                        success = false,
                        operation = "nexus_close_dump_analyze_session",
                        message = $"Session not found or already closed: {sessionId}. Use mcp://nexus/sessions/list to see available sessions.",
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
                    success = closed,
                    operation = "nexus_close_dump_analyze_session",
                    message = closed
                        ? $"Session closed successfully: {sessionId}"
                        : $"Session may have already been closed: {sessionId}",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
                };

                logger.LogInformation("Session {SessionId} closed successfully", sessionId);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error closing session {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    dumpFile = (string?)null,
                    commandId = (string?)null,
                    success = false,
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
        [Description("ASYNC COMMAND: Execute debugger command in background queue. NEVER returns results directly! Always returns commandId. MUST use mcp://nexus/commands/result resource to get actual results.")]
        public Task<object> nexus_enqueue_async_dump_analyze_command(
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("Debugger command to execute (e.g., '!analyze -v', 'k', '!peb')")] string command)
        {
            logger.LogInformation("Executing async command in session {SessionId}: {Command}", sessionId, command);

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
                        sessionId = sessionId,
                        dumpFile = (string?)null,
                        commandId = (string?)null,
                        success = false,
                        operation = "nexus_enqueue_async_dump_analyze_command",
                        message = $"Session not found or expired: {sessionId}. Use mcp://nexus/sessions/list to see available sessions.",
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
                    success = true,
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Command queued successfully: {commandId}. Use mcp://nexus/commands/result to get results.",
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
                    success = false,
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
                    success = false,
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Error executing command: {ex.Message}",
                    usage = USAGE_EXPLANATION // IMPORTANT: usage field must always be the last entry in responses
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
                        "Command is waiting in queue",
                        "Watch for notifications/commandStatus updates",
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