using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;

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
                    _ => CreateErrorResult(-32602, $"Unknown tool: {toolName}")
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
                return CreateErrorResult(-32603, ex.Message);
            }
        }

        private async Task<object> ExecuteOpenWindbgDump(JsonElement arguments)
        {
            // MIGRATION: Legacy HTTP endpoint - redirect to session-aware tool
            logger.LogWarning("Legacy HTTP endpoint called: nexus_open_dump. Consider using the session-aware API directly.");
            
            var dumpPath = GetRequiredStringArgument(arguments, "dumpPath");
            if (dumpPath == null)
                return CreateErrorResult(-32602, "Missing or invalid dumpPath argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await sessionAwareWindbgTool.nexus_open_dump(dumpPath, symbolsPath);

            return CreateTextResult(result);
        }

        private Task<object> ExecuteOpenWindbgRemote(JsonElement arguments)
        {
            // MIGRATION: Remote debugging not implemented in session-aware architecture
            logger.LogWarning("Remote debugging feature not available in session-aware architecture");
            return Task.FromResult(CreateErrorResult(-32601, "Remote debugging not implemented in session-aware architecture. Use dump analysis instead."));
        }

        private Task<object> ExecuteCloseWindbgDump()
        {
            // MIGRATION: Session closure is automatic in session-aware architecture
            logger.LogWarning("Manual session closure not needed in session-aware architecture - sessions auto-expire");
            return Task.FromResult(CreateErrorResult(-32601, "Manual session closure not needed. Sessions auto-expire based on inactivity timeout."));
        }

        private Task<object> ExecuteCloseWindbgRemote()
        {
            // MIGRATION: Remote debugging not implemented in session-aware architecture
            logger.LogWarning("Remote debugging feature not available in session-aware architecture");
            return Task.FromResult(CreateErrorResult(-32601, "Remote debugging not implemented in session-aware architecture. Use dump analysis instead."));
        }

        private Task<object> ExecuteRunWindbgCmdAsync(JsonElement arguments)
        {
            // MIGRATION: Command execution requires sessionId in session-aware architecture
            logger.LogWarning("Legacy command execution called without sessionId");
            return Task.FromResult(CreateErrorResult(-32602, "Command execution requires sessionId. Use nexus_open_dump first to get a sessionId, then use the session-aware API."));
        }

        private async Task<object> ExecuteGetCommandStatus(JsonElement arguments)
        {
            // MIGRATION: Try to use session-aware command status if available
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                return CreateErrorResult(-32602, "Missing or invalid commandId argument");

            logger.LogWarning("Legacy command status check called for commandId: {CommandId}", commandId);
            
            try
            {
                var result = await sessionAwareWindbgTool.nexus_debugger_command_status(commandId);
                return CreateTextResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get command status for legacy call");
                return CreateErrorResult(-32603, $"Command status check failed: {ex.Message}");
            }
        }

        private Task<object> ExecuteCancelCommand(JsonElement arguments)
        {
            // MIGRATION: Command cancellation not implemented in session-aware architecture
            logger.LogWarning("Legacy command cancellation called");
            return Task.FromResult(CreateErrorResult(-32601, "Command cancellation not implemented in session-aware architecture. Sessions auto-timeout inactive commands."));
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

        private static object CreateErrorResult(int code, string message)
        {
            return new { error = new McpError { Code = code, Message = message } };
        }
    }
}







