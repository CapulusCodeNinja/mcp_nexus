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
                    "open_windbg_dump" => await ExecuteOpenWindbgDump(arguments),
                    "open_windbg_remote" => await ExecuteOpenWindbgRemote(arguments),
                    "close_windbg_dump" => await ExecuteCloseWindbgDump(),
                    "close_windbg_remote" => await ExecuteCloseWindbgRemote(),
                    "run_windbg_cmd_async" => await ExecuteRunWindbgCmdAsync(arguments),
                    "get_command_status" => await ExecuteGetCommandStatus(arguments),
                    "cancel_command" => await ExecuteCancelCommand(arguments),
                    "list_commands" => await ExecuteListCommands(),
                    // Deprecated commands
                    "run_windbg_cmd" => CreateErrorResult(-32602, "COMMAND REMOVED: run_windbg_cmd has been permanently removed. Use run_windbg_cmd_async instead for all commands."),
                    "run_windbg_cmd_sync" => CreateErrorResult(-32602, "PERMANENTLY REMOVED: run_windbg_cmd_sync has been removed. Use run_windbg_cmd_async for all commands."),
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
            var result = await windbgTool.OpenWindbgDump(dumpPath, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteOpenWindbgRemote(JsonElement arguments)
        {
            var connectionString = GetRequiredStringArgument(arguments, "connectionString");
            if (connectionString == null)
                return CreateErrorResult(-32602, "Missing or invalid connectionString argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await windbgTool.OpenWindbgRemote(connectionString, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgDump()
        {
            var result = await windbgTool.CloseWindbgDump();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgRemote()
        {
            var result = await windbgTool.CloseWindbgRemote();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteRunWindbgCmdAsync(JsonElement arguments)
        {
            var command = GetRequiredStringArgument(arguments, "command");
            if (command == null)
                return CreateErrorResult(-32602, "Missing or invalid command argument");

            var result = await windbgTool.RunWindbgCmdAsync(command);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteGetCommandStatus(JsonElement arguments)
        {
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                return CreateErrorResult(-32602, "Missing or invalid commandId argument");

            var result = await windbgTool.GetCommandStatus(commandId);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCancelCommand(JsonElement arguments)
        {
            var commandId = GetRequiredStringArgument(arguments, "commandId");
            if (commandId == null)
                return CreateErrorResult(-32602, "Missing or invalid commandId argument");

            var result = await windbgTool.CancelCommand(commandId);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteListCommands()
        {
            var result = await windbgTool.ListCommands();
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






