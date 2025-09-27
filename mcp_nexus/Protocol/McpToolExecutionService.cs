using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;
using mcp_nexus.Exceptions;

namespace mcp_nexus.Protocol
{
    public class McpToolExecutionService(
        SessionAwareWindbgTool sessionAwareWindbgTool,
        ILogger<McpToolExecutionService> logger) : IMcpToolExecutionService
    {
        // PERFORMANCE: Cache JsonSerializerOptions to avoid repeated allocation
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null // Don't change property names - MCP protocol requires exact field names
        };
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
                throw new McpToolException(-32602, "Missing required parameter: dumpPath");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await sessionAwareWindbgTool.nexus_open_dump_analyze_session(dumpPath, symbolsPath);

            // Wrap result in MCP tool result format
            return CreateToolResult(result);
        }

        private async Task<object> ExecuteCloseWindbgDump(JsonElement arguments)
        {
            // Close the specified session
            var sessionId = GetRequiredStringArgument(arguments, "sessionId");

            if (sessionId == null)
                throw new McpToolException(-32602, "Missing required parameter: sessionId");

            logger.LogInformation("Manual session closure requested for session: {SessionId}", sessionId);

            var result = await sessionAwareWindbgTool.nexus_close_dump_analyze_session(sessionId);
            return CreateToolResult(result);
        }

        private async Task<object> ExecuteRunWindbgCmdAsync(JsonElement arguments)
        {
            // Extract command and sessionId from arguments
            var command = GetRequiredStringArgument(arguments, "command");
            var sessionId = GetRequiredStringArgument(arguments, "sessionId");

            if (command == null)
                throw new McpToolException(-32602, "Missing required parameter: command");

            if (sessionId == null)
                throw new McpToolException(-32602, "Missing required parameter: sessionId");

            logger.LogDebug("Executing command '{Command}' for session '{SessionId}'", command, sessionId);

            try
            {
                var result = await sessionAwareWindbgTool.nexus_dump_analyze_session_async_command(sessionId, command);
                return CreateToolResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute command '{Command}' for session '{SessionId}'", command, sessionId);
                throw new McpToolException(-32603, $"Command execution failed: {ex.Message}", ex);
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


        private static object CreateToolResult(object result)
        {
            return new McpToolResult
            {
                Content =
                [
                    new McpContent
                    {
                        Type = "text",
                        Text = System.Text.Json.JsonSerializer.Serialize(result, s_jsonOptions)
                    }
                ]
            };
        }

    }
}







