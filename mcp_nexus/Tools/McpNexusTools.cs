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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public static class McpNexusTools
    {
        [McpServerTool, Description("🔓 OPEN SESSION: Create a new debugging session for a crash dump file. Returns sessionId that MUST be used for all subsequent operations.")]
        public static async Task<object> nexus_open_dump_analyze_session(
            IServiceProvider serviceProvider,
            [Description("Full path to the crash dump file (.dmp)")] string dumpPath,
            [Description("Optional path to symbol files directory")] string? symbolsPath = null)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("🔓 Opening new debugging session for dump: {DumpPath}", dumpPath);

            try
            {
                var sessionId = await sessionManager.CreateSessionAsync(dumpPath, symbolsPath);
                var context = sessionManager.GetSessionContext(sessionId);

                var response = new
                {
                    sessionId = sessionId,
                    dumpFile = Path.GetFileName(dumpPath),
                    commandId = (string?)null,
                    success = true,
                    operation = "nexus_open_dump_analyze_session",
                    message = $"Session created successfully: {sessionId}. Use mcp://nexus/sessions/list to manage sessions."
                };

                logger.LogInformation("Session {SessionId} created successfully", sessionId);
                return response;
            }
            catch (SessionLimitExceededException ex)
            {
                logger.LogWarning("Session limit exceeded: {Message}", ex.Message);

                var errorResponse = new
                {
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
                    message = $"Failed to create debugging session: {ex.Message}"
                };

                return errorResponse;
            }
        }

        [McpServerTool, Description("🔒 CLOSE SESSION: Close an active debugging session and clean up resources. Use this when done with a session.")]
        public static async Task<object> nexus_close_dump_analyze_session(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("🔒 Closing debugging session: {SessionId}", sessionId);

            try
            {
                if (!sessionManager.SessionExists(sessionId))
                {
                    var notFoundResponse = new
                    {
                        sessionId = sessionId,
                        success = false,
                        operation = "nexus_close_dump_analyze_session",
                        message = $"Session {sessionId} not found. Use mcp://nexus/sessions/list to see available sessions."
                    };

                    logger.LogWarning("Attempted to close non-existent session: {SessionId}", sessionId);
                    return notFoundResponse;
                }

                await sessionManager.CloseSessionAsync(sessionId);

                var response = new
                {
                    sessionId = sessionId,
                    success = true,
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Session {sessionId} closed successfully"
                };

                logger.LogInformation("Session {SessionId} closed successfully", sessionId);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close debugging session: {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    success = false,
                    operation = "nexus_close_dump_analyze_session",
                    message = $"Failed to close session: {ex.Message}"
                };

                return errorResponse;
            }
        }

        [McpServerTool, Description("⚡ EXECUTE COMMAND: Queue a WinDBG command for execution in a debugging session. Returns commandId for tracking.")]
        public static async Task<object> nexus_enqueue_async_dump_analyze_command(
            IServiceProvider serviceProvider,
            [Description("Session ID from nexus_open_dump_analyze_session")] string sessionId,
            [Description("WinDBG command to execute (e.g., '!analyze -v', 'k', '!threads')")] string command)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();

            logger.LogInformation("⚡ Queuing command '{Command}' for session: {SessionId}", command, sessionId);

            try
            {
                if (!sessionManager.SessionExists(sessionId))
                {
                    var notFoundResponse = new
                    {
                        sessionId = sessionId,
                        commandId = (string?)null,
                        success = false,
                        operation = "nexus_enqueue_async_dump_analyze_command",
                        message = $"Session {sessionId} not found. Use mcp://nexus/sessions/list to see available sessions."
                    };

                    logger.LogWarning("Attempted to queue command for non-existent session: {SessionId}", sessionId);
                    return notFoundResponse;
                }

                var context = sessionManager.GetSessionContext(sessionId);
                if (context == null)
                {
                    var contextErrorResponse = new
                    {
                        sessionId = sessionId,
                        commandId = (string?)null,
                        success = false,
                        operation = "nexus_enqueue_async_dump_analyze_command",
                        message = $"Session context not available for {sessionId}. Session may be in an invalid state."
                    };

                    logger.LogError("Session context not available for session: {SessionId}", sessionId);
                    return contextErrorResponse;
                }

                var commandQueue = sessionManager.GetCommandQueue(sessionId);
                var commandId = commandQueue.QueueCommand(command);

                var response = new
                {
                    sessionId = sessionId,
                    commandId = commandId,
                    command = command,
                    success = true,
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Command queued successfully. Use mcp://nexus/commands/result?sessionId={sessionId}&commandId={commandId} to get results."
                };

                logger.LogInformation("Command {CommandId} queued successfully for session {SessionId}", commandId, sessionId);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue command for session: {SessionId}", sessionId);

                var errorResponse = new
                {
                    sessionId = sessionId,
                    commandId = (string?)null,
                    command = command,
                    success = false,
                    operation = "nexus_enqueue_async_dump_analyze_command",
                    message = $"Failed to queue command: {ex.Message}"
                };

                return errorResponse;
            }
        }
    }
}
