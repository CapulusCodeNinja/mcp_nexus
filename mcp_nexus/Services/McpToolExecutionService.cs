using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;

namespace mcp_nexus.Services
{
    public class McpToolExecutionService(
        WindbgTool windbgTool,
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
                    "nexus_list_debugger_commands" => await ExecuteListCommands(),
                    // Deprecated commands - maintain backward compatibility
                    "open_windbg_dump" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_open_dump instead"),
                    "open_windbg_remote" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_start_remote_debug instead"),
                    "close_windbg_dump" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_close_dump instead"),
                    "close_windbg_remote" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_stop_remote_debug instead"),
                    "run_windbg_cmd_async" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_exec_debugger_command_async instead"),
                    "get_command_status" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_debugger_command_status instead"),
                    "cancel_command" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_debugger_command_cancel instead"),
                    "list_commands" => CreateErrorResult(-32602, "DEPRECATED: Use nexus_list_debugger_commands instead"),
                    "run_windbg_cmd" => CreateErrorResult(-32602, "COMMAND REMOVED: Use nexus_exec_debugger_command_async instead"),
                    "run_windbg_cmd_sync" => CreateErrorResult(-32602, "COMMAND REMOVED: Use nexus_exec_debugger_command_async instead"),
                    "list_windbg_dumps" => CreateErrorResult(-32602, "COMMAND OBSOLETE: This functionality has been removed"),
                    "get_session_info" => CreateErrorResult(-32602, "COMMAND OBSOLETE: This functionality has been removed"),
                    "get_current_time" => CreateErrorResult(-32602, "COMMAND OBSOLETE: This functionality has been removed"),
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
            var dumpPath = GetRequiredStringArgument(arguments, "dumpPath");
            if (dumpPath == null)
                return CreateErrorResult(-32602, "Missing or invalid dumpPath argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await windbgTool.NexusOpenDump(dumpPath, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteOpenWindbgRemote(JsonElement arguments)
        {
            var connectionString = GetRequiredStringArgument(arguments, "connectionString");
            if (connectionString == null)
                return CreateErrorResult(-32602, "Missing or invalid connectionString argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await windbgTool.NexusStartRemoteDebug(connectionString, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgDump()
        {
            var result = await windbgTool.NexusCloseDump();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgRemote()
        {
            var result = await windbgTool.NexusStopRemoteDebug();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteRunWindbgCmdAsync(JsonElement arguments)
        {
            var command = GetRequiredStringArgument(arguments, "command");
            if (command == null)
                return CreateErrorResult(-32602, "Missing or invalid command argument");

            var result = await windbgTool.NexusExecDebuggerCommandAsync(command);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteGetCommandStatus(JsonElement arguments)
        {
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                return CreateErrorResult(-32602, "Missing or invalid commandId argument");

            var result = await windbgTool.NexusDebuggerCommandStatus(commandId);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCancelCommand(JsonElement arguments)
        {
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                return CreateErrorResult(-32602, "Missing or invalid commandId argument");

            var result = await windbgTool.NexusDebuggerCommandCancel(commandId);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteListCommands()
        {
            var result = await windbgTool.NexusListDebuggerCommands();
            return CreateTextResult(result);
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






