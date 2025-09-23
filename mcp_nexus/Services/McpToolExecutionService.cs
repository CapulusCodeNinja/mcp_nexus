using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;

namespace mcp_nexus.Services
{
    public class McpToolExecutionService
    {
        private readonly WindbgTool _windbgTool;
        private readonly TimeTool _timeTool;
        private readonly ILogger<McpToolExecutionService> _logger;

        public McpToolExecutionService(
            WindbgTool windbgTool,
            TimeTool timeTool,
            ILogger<McpToolExecutionService> logger)
        {
            _windbgTool = windbgTool;
            _timeTool = timeTool;
            _logger = logger;
        }

        public async Task<object> ExecuteTool(string toolName, JsonElement arguments)
        {
            _logger.LogDebug("Executing tool: {ToolName}", toolName);

            try
            {
                return toolName switch
                {
                    "open_windbg_dump" => await ExecuteOpenWindbgDump(arguments),
                    "open_windbg_remote" => await ExecuteOpenWindbgRemote(arguments),
                    "close_windbg_dump" => await ExecuteCloseWindbgDump(),
                    "close_windbg_remote" => await ExecuteCloseWindbgRemote(),
                    "run_windbg_cmd" => await ExecuteRunWindbgCmd(arguments),
                    "list_windbg_dumps" => await ExecuteListWindbgDumps(arguments),
                    "get_session_info" => await ExecuteGetSessionInfo(),
                    "analyze_call_stack" => await ExecuteAnalyzeCallStack(),
                    "analyze_memory" => await ExecuteAnalyzeMemory(),
                    "analyze_crash_patterns" => await ExecuteAnalyzeCrashPatterns(),
                    "get_current_time" => ExecuteGetCurrentTime(arguments),
                    _ => CreateErrorResult(-32602, $"Unknown tool: {toolName}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
                return CreateErrorResult(-32603, ex.Message);
            }
        }

        private async Task<object> ExecuteOpenWindbgDump(JsonElement arguments)
        {
            var dumpPath = GetRequiredStringArgument(arguments, "dumpPath");
            if (dumpPath == null)
                return CreateErrorResult(-32602, "Missing or invalid dumpPath argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await _windbgTool.OpenWindbgDump(dumpPath, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteOpenWindbgRemote(JsonElement arguments)
        {
            var connectionString = GetRequiredStringArgument(arguments, "connectionString");
            if (connectionString == null)
                return CreateErrorResult(-32602, "Missing or invalid connectionString argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await _windbgTool.OpenWindbgRemote(connectionString, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgDump()
        {
            var result = await _windbgTool.CloseWindbgDump();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgRemote()
        {
            var result = await _windbgTool.CloseWindbgRemote();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteRunWindbgCmd(JsonElement arguments)
        {
            var command = GetRequiredStringArgument(arguments, "command");
            if (command == null)
                return CreateErrorResult(-32602, "Missing or invalid command argument");

            var result = await _windbgTool.RunWindbgCmd(command);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteListWindbgDumps(JsonElement arguments)
        {
            var directoryPath = GetRequiredStringArgument(arguments, "directoryPath");
            if (directoryPath == null)
                return CreateErrorResult(-32602, "Missing or invalid directoryPath argument");

            var result = await _windbgTool.ListWindbgDumps(directoryPath);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteGetSessionInfo()
        {
            var result = await _windbgTool.GetSessionInfo();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeCallStack()
        {
            var result = await _windbgTool.AnalyzeCallStack();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeMemory()
        {
            var result = await _windbgTool.AnalyzeMemory();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeCrashPatterns()
        {
            var result = await _windbgTool.AnalyzeCrashPatterns();
            return CreateTextResult(result);
        }

        private object ExecuteGetCurrentTime(JsonElement arguments)
        {
            var city = GetRequiredStringArgument(arguments, "city");
            if (city == null)
                return CreateErrorResult(-32602, "Missing or invalid city argument");

            var result = _timeTool.GetCurrentTime(city);
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






