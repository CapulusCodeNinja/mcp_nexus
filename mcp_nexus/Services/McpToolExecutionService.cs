using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;

namespace mcp_nexus.Services
{
    public class McpToolExecutionService(
        WindbgTool windbgTool,
        TimeTool timeTool,
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
                    "run_windbg_cmd" => ExecuteRunWindbgCmdDeprecated(arguments),
                    "run_windbg_cmd_sync" => ExecuteRunWindbgCmdSyncDeprecated(arguments),
                    "list_windbg_dumps" => await ExecuteListWindbgDumps(arguments),
                    "get_session_info" => await ExecuteGetSessionInfo(),
                    "analyze_call_stack" => await ExecuteAnalyzeCallStack(),
                    "analyze_memory" => await ExecuteAnalyzeMemory(),
                    "analyze_crash_patterns" => await ExecuteAnalyzeCrashPatterns(),
                    "get_command_status" => await ExecuteGetCommandStatus(arguments),
                    "cancel_command" => await ExecuteCancelCommand(arguments),
                    "list_commands" => await ExecuteListCommands(),
                    "get_current_time" => ExecuteGetCurrentTime(arguments),
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

        private Task<object> ExecuteRunWindbgCmdDeprecated(JsonElement arguments)
        {
            var command = GetRequiredStringArgument(arguments, "command");
            
            // Return an extremely aggressive deprecation message that's impossible to ignore
            var aggressiveMessage = $@"🚨🚨🚨 COMMAND REMOVED! 🚨🚨🚨

❌ 'run_windbg_cmd' HAS BEEN PERMANENTLY REMOVED!

🔄 YOU MUST USE: run_windbg_cmd_async

Your command: '{command}'

✅ CORRECT USAGE:
{{""name"": ""run_windbg_cmd_async"", ""arguments"": {{""command"": ""{command}""}}}}

⚠️ AFTER calling run_windbg_cmd_async:
1. You get a commandId (NOT the result!)
2. Call get_command_status with that commandId
3. Keep polling until status='completed'
4. Extract 'result' field for actual output

🚨 THIS ERROR WILL NEVER GO AWAY UNTIL YOU SWITCH! 🚨

MIGRATION REQUIRED: run_windbg_cmd → run_windbg_cmd_async";

            return Task.FromResult(CreateTextResult(aggressiveMessage));
        }

        private Task<object> ExecuteRunWindbgCmdSyncDeprecated(JsonElement arguments)
        {
            var command = GetRequiredStringArgument(arguments, "command");
            
            var aggressiveMessage = $@"🚨🚨🚨 COMMAND REMOVED! 🚨🚨🚨

❌ 'run_windbg_cmd_sync' HAS BEEN PERMANENTLY REMOVED!

🔄 YOU MUST USE: run_windbg_cmd_async

Your command: '{command}'

✅ CORRECT USAGE:
{{""name"": ""run_windbg_cmd_async"", ""arguments"": {{""command"": ""{command}""}}}}

⚠️ AFTER calling run_windbg_cmd_async:
1. You get a commandId (NOT the result!)
2. Call get_command_status with that commandId
3. Keep polling until status='completed'
4. Extract 'result' field for actual output

🚨 THIS ERROR WILL NEVER GO AWAY UNTIL YOU SWITCH! 🚨

MIGRATION REQUIRED: run_windbg_cmd_sync → run_windbg_cmd_async";

            return Task.FromResult(CreateTextResult(aggressiveMessage));
        }

        private async Task<object> ExecuteListWindbgDumps(JsonElement arguments)
        {
            var directoryPath = GetRequiredStringArgument(arguments, "directoryPath");
            if (directoryPath == null)
                return CreateErrorResult(-32602, "Missing or invalid directoryPath argument");

            var result = await windbgTool.ListWindbgDumps(directoryPath);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteGetSessionInfo()
        {
            var result = await windbgTool.GetSessionInfo();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeCallStack()
        {
            var result = await windbgTool.AnalyzeCallStack();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeMemory()
        {
            var result = await windbgTool.AnalyzeMemory();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeCrashPatterns()
        {
            var result = await windbgTool.AnalyzeCrashPatterns();
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

        private object ExecuteGetCurrentTime(JsonElement arguments)
        {
            var city = GetRequiredStringArgument(arguments, "city");
            if (city == null)
                return CreateErrorResult(-32602, "Missing or invalid city argument");

            var result = timeTool.GetCurrentTime(city);
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
                Content = new[]
                {
                    new McpContent
                    {
                        Type = "text",
                        Text = text
                    }
                }
            };
        }

        private static object CreateErrorResult(int code, string message)
        {
            return new { error = new McpError { Code = code, Message = message } };
        }
    }
}






