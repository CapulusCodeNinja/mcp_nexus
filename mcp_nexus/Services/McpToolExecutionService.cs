using System.Text.Json;
using mcp_nexus.Models;
using mcp_nexus.Tools;

namespace mcp_nexus.Services
{
    public class McpToolExecutionService
    {
        private readonly WindbgTool m_windbgTool;
        private readonly TimeTool m_timeTool;
        private readonly ILogger<McpToolExecutionService> m_logger;

        public McpToolExecutionService(
            WindbgTool windbgTool,
            TimeTool timeTool,
            ILogger<McpToolExecutionService> logger)
        {
            m_windbgTool = windbgTool;
            m_timeTool = timeTool;
            m_logger = logger;
        }

        public async Task<object> ExecuteTool(string toolName, JsonElement arguments)
        {
            m_logger.LogDebug("Executing tool: {ToolName}", toolName);

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
                m_logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
                return CreateErrorResult(-32603, ex.Message);
            }
        }

        private async Task<object> ExecuteOpenWindbgDump(JsonElement arguments)
        {
            var dumpPath = GetRequiredStringArgument(arguments, "dumpPath");
            if (dumpPath == null)
                return CreateErrorResult(-32602, "Missing or invalid dumpPath argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await m_windbgTool.OpenWindbgDump(dumpPath, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteOpenWindbgRemote(JsonElement arguments)
        {
            var connectionString = GetRequiredStringArgument(arguments, "connectionString");
            if (connectionString == null)
                return CreateErrorResult(-32602, "Missing or invalid connectionString argument");

            var symbolsPath = GetOptionalStringArgument(arguments, "symbolsPath");
            var result = await m_windbgTool.OpenWindbgRemote(connectionString, symbolsPath);

            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgDump()
        {
            var result = await m_windbgTool.CloseWindbgDump();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteCloseWindbgRemote()
        {
            var result = await m_windbgTool.CloseWindbgRemote();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteRunWindbgCmd(JsonElement arguments)
        {
            var command = GetRequiredStringArgument(arguments, "command");
            if (command == null)
                return CreateErrorResult(-32602, "Missing or invalid command argument");

            var result = await m_windbgTool.RunWindbgCmd(command);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteListWindbgDumps(JsonElement arguments)
        {
            var directoryPath = GetRequiredStringArgument(arguments, "directoryPath");
            if (directoryPath == null)
                return CreateErrorResult(-32602, "Missing or invalid directoryPath argument");

            var result = await m_windbgTool.ListWindbgDumps(directoryPath);
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteGetSessionInfo()
        {
            var result = await m_windbgTool.GetSessionInfo();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeCallStack()
        {
            var result = await m_windbgTool.AnalyzeCallStack();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeMemory()
        {
            var result = await m_windbgTool.AnalyzeMemory();
            return CreateTextResult(result);
        }

        private async Task<object> ExecuteAnalyzeCrashPatterns()
        {
            var result = await m_windbgTool.AnalyzeCrashPatterns();
            return CreateTextResult(result);
        }

        private object ExecuteGetCurrentTime(JsonElement arguments)
        {
            var city = GetRequiredStringArgument(arguments, "city");
            if (city == null)
                return CreateErrorResult(-32602, "Missing or invalid city argument");

            var result = m_timeTool.GetCurrentTime(city);
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






