using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;
using mcp_nexus.Exceptions;

namespace mcp_nexus.Protocol
{
    public class McpToolExecutionService(
        SessionAwareWindbgTool sessionAwareWindbgTool,
        ILogger<McpToolExecutionService> logger)
    {
        public async Task<object> ExecuteTool(string toolName, JsonElement arguments)
        {
            logger.LogDebug("Executing tool: {ToolName}", toolName);

            try
            {
                return toolName switch
                {
                    "nexus_open_dump" => await ExecuteOpenWindbgDump(arguments),
                    "nexus_start_remote_debug" => await ExecuteOpenWindbgRemote(arguments),
                    "nexus_close_dump" => await ExecuteCloseWindbgDump(),
                    "nexus_stop_remote_debug" => await ExecuteCloseWindbgRemote(),
                    "nexus_exec_debugger_command_async" => await ExecuteRunWindbgCmdAsync(arguments),
                    "nexus_debugger_command_status" => await ExecuteGetCommandStatus(arguments),
                    "nexus_debugger_command_cancel" => await ExecuteCancelCommand(arguments),
                    _ => throw new McpToolException(-32602, $"Unknown tool: {toolName}")
                };
            }
            catch (McpToolException)
            {
                // Re-throw MCP tool exceptions to be handled by the protocol service
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
                throw new McpToolException(-32603, ex.Message, ex);
            }
        }

        private async Task<object> ExecuteOpenWindbgDump(JsonElement arguments)
        {
            // MIGRATION: Legacy HTTP endpoint - redirect to session-aware tool
            logger.LogWarning("Legacy HTTP endpoint called: nexus_open_dump. Consider using the session-aware API directly.");
            
            var dumpPath = GetRequiredStringArgument(arguments, "dumpPath");
            if (dumpPath == null)
                throw new McpToolException(-32602, "❌ MISSING DUMP PATH: You must provide a 'dumpPath' parameter! " +
                    "🔧 RECOVERY: Add 'dumpPath' parameter with full path to a .dmp file " +
                    "💡 EXAMPLE: {\"dumpPath\": \"C:\\\\path\\\\to\\\\crash.dmp\"}");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await sessionAwareWindbgTool.nexus_open_dump(dumpPath, symbolsPath);

            return CreateTextResult(result);
        }

        private Task<object> ExecuteOpenWindbgRemote(JsonElement arguments)
        {
            // MIGRATION: Remote debugging not implemented in session-aware architecture
            logger.LogWarning("Remote debugging feature not available in session-aware architecture");
            throw new McpToolException(-32601, "❌ REMOTE DEBUGGING NOT AVAILABLE: This feature is not implemented in the current architecture. " +
                "🔧 RECOVERY: Use nexus_open_dump instead with a crash dump file (.dmp) " +
                "💡 ALTERNATIVE: Analyze existing dump files rather than live debugging");
        }

        private Task<object> ExecuteCloseWindbgDump()
        {
            // MIGRATION: Session closure is automatic in session-aware architecture
            logger.LogWarning("Manual session closure not needed in session-aware architecture - sessions auto-expire");
            throw new McpToolException(-32601, "❌ MANUAL CLOSE NOT NEEDED: Sessions automatically expire after 30 minutes of inactivity. " +
                "🔧 RECOVERY: No action needed - sessions clean up automatically " +
                "💡 INFO: Focus on analyzing your dump with nexus_exec_debugger_command_async instead");
        }

        private Task<object> ExecuteCloseWindbgRemote()
        {
            // MIGRATION: Remote debugging not implemented in session-aware architecture
            logger.LogWarning("Remote debugging feature not available in session-aware architecture");
            throw new McpToolException(-32601, "❌ REMOTE DEBUGGING NOT AVAILABLE: This feature is not implemented in the current architecture. " +
                "🔧 RECOVERY: Use nexus_open_dump instead with a crash dump file (.dmp) " +
                "💡 ALTERNATIVE: Analyze existing dump files rather than live debugging");
        }

        private async Task<object> ExecuteRunWindbgCmdAsync(JsonElement arguments)
        {
            // Extract command and sessionId from arguments
            var command = GetRequiredStringArgument(arguments, "command");
            var sessionId = GetRequiredStringArgument(arguments, "sessionId");
            
            if (command == null)
                throw new McpToolException(-32602, "❌ MISSING COMMAND: You must provide a 'command' parameter. " +
                    "🔧 RECOVERY: Add 'command' parameter with a WinDbg command like '!analyze -v', 'k', 'lm', etc.");
                
            if (sessionId == null)
                throw new McpToolException(-32602, "❌ MISSING SESSION ID: You must provide a 'sessionId' parameter! " +
                    "🔧 RECOVERY STEPS: " +
                    "1️⃣ First call nexus_open_dump with a .dmp file path " +
                    "2️⃣ Extract the 'sessionId' from the response JSON " +
                    "3️⃣ Retry this command with both 'command' AND 'sessionId' parameters " +
                    "💡 EXAMPLE: {\"command\": \"!analyze -v\", \"sessionId\": \"sess-000001-abc12345\"}");

            logger.LogDebug("Executing command '{Command}' for session '{SessionId}'", command, sessionId);
            
            try
            {
                var result = await sessionAwareWindbgTool.nexus_exec_debugger_command_async(command, sessionId);
                return CreateTextResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute command '{Command}' for session '{SessionId}'", command, sessionId);
                throw new McpToolException(-32603, $"Command execution failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ExecuteGetCommandStatus(JsonElement arguments)
        {
            // MIGRATION: Try to use session-aware command status if available
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                throw new McpToolException(-32602, "❌ MISSING COMMAND ID: You must provide a 'commandId' parameter! " +
                    "🔧 RECOVERY: Add 'commandId' parameter from nexus_exec_debugger_command_async response " +
                    "💡 EXAMPLE: {\"commandId\": \"cmd-12345-abc\"}");

            logger.LogWarning("Legacy command status check called for commandId: {CommandId}", commandId);
            
            try
            {
                var result = await sessionAwareWindbgTool.nexus_debugger_command_status(commandId);
                return CreateTextResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get command status for legacy call");
                throw new McpToolException(-32603, $"Command status check failed: {ex.Message}", ex);
            }
        }

        private Task<object> ExecuteCancelCommand(JsonElement arguments)
        {
            // MIGRATION: Command cancellation not implemented in session-aware architecture
            logger.LogWarning("Legacy command cancellation called");
            throw new McpToolException(-32601, "❌ COMMAND CANCELLATION NOT AVAILABLE: This feature is not implemented in the current architecture. " +
                "🔧 RECOVERY: Wait for the command to complete or start a new session " +
                "💡 INFO: Commands typically complete quickly - use nexus_debugger_command_status to check progress");
        }


        private static string? GetRequiredStringArgument(JsonElement arguments, string name)
        {
            if (!arguments.TryGetProperty(name, out var property))
                return null;

            var value = property.GetString();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static string? GetOptionalStringArgument(JsonElement arguments, string name)
        {
            return arguments.TryGetProperty(name, out var property) ? property.GetString() : null;
        }

        private static object CreateTextResult(string text)
        {
            return new McpToolResult
            {
                Content =
                [
                    new McpContent
                    {
                        Type = "text",
                        Text = text
                    }
                ]
            };
        }

    }
}







