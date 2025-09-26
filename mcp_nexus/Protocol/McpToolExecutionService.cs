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
                    "nexus_open_dump_analyze_session" => await ExecuteOpenWindbgDump(arguments),
                    "nexus_close_dump_analyze_session" => await ExecuteCloseWindbgDump(arguments),
                    "nexus_dump_analyze_session_async_command" => await ExecuteRunWindbgCmdAsync(arguments),
                    "nexus_dump_analyze_session_async_command_status" => await ExecuteGetCommandStatus(arguments),
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
            // Open dump file using session-aware implementation
            logger.LogDebug("Opening dump file: {DumpPath}", arguments);
            
            var dumpPath = GetRequiredStringArgument(arguments, "dumpPath");
            if (dumpPath == null)
                throw new McpToolException(-32602, "❌ MISSING DUMP PATH: You must provide a 'dumpPath' parameter! " +
                    "🔧 RECOVERY: Add 'dumpPath' parameter with full path to a .dmp file " +
                    "💡 EXAMPLE: {\"dumpPath\": \"C:\\\\path\\\\to\\\\crash.dmp\"}");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await sessionAwareWindbgTool.nexus_open_dump_analyze_session(dumpPath, symbolsPath);

            // Return the structured response object directly
            return result;
        }

        private async Task<object> ExecuteCloseWindbgDump(JsonElement arguments)
        {
            // Close the specified session
            var sessionId = GetRequiredStringArgument(arguments, "sessionId");
            
            if (sessionId == null)
                throw new McpToolException(-32602, "❌ MISSING SESSION ID: You must provide a 'sessionId' parameter! " +
                    "🔧 RECOVERY: Include the sessionId from your nexus_open_dump response " +
                    "💡 EXAMPLE: {\"sessionId\": \"sess-000001-abc12345\"}");

            logger.LogInformation("Manual session closure requested for session: {SessionId}", sessionId);
            
            var result = await sessionAwareWindbgTool.nexus_close_dump_analyze_session(sessionId);
            return result;
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
                    "🔧 STEP-BY-STEP RECOVERY: " +
                    "1️⃣ FIRST: Call nexus_open_dump with a .dmp file path → this creates a debugging session " +
                    "2️⃣ EXTRACT: Get the 'sessionId' value from the nexus_open_dump response JSON " +
                    "3️⃣ RETRY: Call this command again with BOTH 'command' AND 'sessionId' parameters " +
                    "4️⃣ REMEMBER: This command only returns a commandId, then call nexus_debugger_command_status(commandId) for actual results " +
                    "💡 CORRECT USAGE EXAMPLE: {\"command\": \"!analyze -v\", \"sessionId\": \"sess-000001-abc12345\"} " +
                    "🚨 ASYNC WORKFLOW: nexus_open_dump → nexus_exec_debugger_command_async → nexus_debugger_command_status " +
                    "❓ WHY THIS ERROR: The AI client didn't extract sessionId from nexus_open_dump response or skipped calling nexus_open_dump entirely. " +
                    "🎯 AI DEBUGGING TIP: Check your previous nexus_open_dump response for the sessionId field!");

            logger.LogDebug("Executing command '{Command}' for session '{SessionId}'", command, sessionId);
            
            try
            {
                var result = await sessionAwareWindbgTool.nexus_dump_analyze_session_async_command(sessionId, command);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute command '{Command}' for session '{SessionId}'", command, sessionId);
                throw new McpToolException(-32603, $"Command execution failed: {ex.Message}", ex);
            }
        }

        private async Task<object> ExecuteGetCommandStatus(JsonElement arguments)
        {
            // Get command status using session-aware implementation
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                throw new McpToolException(-32602, "❌ MISSING COMMAND ID: You must provide a 'commandId' parameter! " +
                    "🔧 RECOVERY: Add 'commandId' parameter from nexus_exec_debugger_command_async response " +
                    "🚨 ASYNC WORKFLOW: nexus_exec_debugger_command_async returns commandId → use it here to get results " +
                    "💡 EXAMPLE: {\"commandId\": \"cmd-12345-abc\"} " +
                    "📡 This is how you get actual debugger command output!");

            logger.LogDebug("Getting command status for commandId: {CommandId}", commandId);
            
            try
            {
                var result = await sessionAwareWindbgTool.nexus_dump_analyze_session_async_command_status(commandId);
                // Return the structured response object directly
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get command status for commandId: {CommandId}", commandId);
                throw new McpToolException(-32603, $"Command status check failed: {ex.Message}", ex);
            }
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







