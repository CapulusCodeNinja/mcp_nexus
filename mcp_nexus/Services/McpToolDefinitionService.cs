using mcp_nexus.Models;

namespace mcp_nexus.Services
{
    public class McpToolDefinitionService
    {
        public McpToolSchema[] GetAllTools()
        {
            return new[]
            {
                // ✅ WORKING COMMANDS ONLY - DEPRECATED COMMANDS HIDDEN FROM AI!
                CreateRunWindbgCmdAsyncTool(),
                CreateOpenWindbgDumpTool(),
                CreateOpenWindbgRemoteTool(),
                CreateCloseWindbgDumpTool(),
                CreateCloseWindbgRemoteTool(),
                CreateListWindbgDumpsTool(),
                CreateGetSessionInfoTool(),
                CreateAnalyzeCallStackTool(),
                CreateAnalyzeMemoryTool(),
                CreateAnalyzeCrashPatternsTool(),
                CreateGetCommandStatusTool(),
                CreateCancelCommandTool(),
                CreateListCommandsTool(),
                CreateGetCurrentTimeTool()
            };
        }

        private static McpToolSchema CreateOpenWindbgDumpTool()
        {
            return new McpToolSchema
            {
                Name = "open_windbg_dump",
                Description = "Analyze a Windows crash dump file using common WinDBG commands",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        dumpPath = new { type = "string", description = "Path to the crash dump file" },
                        symbolsPath = new { type = "string", description = "Optional path to symbols directory" }
                    },
                    required = new[] { "dumpPath" }
                }
            };
        }

        private static McpToolSchema CreateOpenWindbgRemoteTool()
        {
            return new McpToolSchema
            {
                Name = "open_windbg_remote",
                Description = "Connect to a remote debugging session using a connection string (e.g., tcp:Port=5005,Server=192.168.0.100)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        connectionString = new { type = "string", description = "Connection string for remote debugging" },
                        symbolsPath = new { type = "string", description = "Optional path to symbols directory" }
                    },
                    required = new[] { "connectionString" }
                }
            };
        }

        private static McpToolSchema CreateCloseWindbgDumpTool()
        {
            return new McpToolSchema
            {
                Name = "close_windbg_dump",
                Description = "Unload a crash dump and release resources",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateCloseWindbgRemoteTool()
        {
            return new McpToolSchema
            {
                Name = "close_windbg_remote",
                Description = "Disconnect from a remote debugging session and release resources",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateRunWindbgCmdAsyncTool()
        {
            return new McpToolSchema
            {
                Name = "run_windbg_cmd_async",
                Description = "🔄 ASYNC QUEUE: Execute WinDBG command in background queue. ⚠️ NEVER returns results directly! Always returns commandId for polling. You MUST call get_command_status(commandId) to get actual results. NO EXCEPTIONS!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "WinDBG command to execute" }
                    },
                    required = new[] { "command" }
                }
            };
        }

        private static McpToolSchema CreateListWindbgDumpsTool()
        {
            return new McpToolSchema
            {
                Name = "list_windbg_dumps",
                Description = "List Windows crash dump (.dmp) files in the specified directory",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        directoryPath = new { type = "string", description = "Directory path to search for dump files" }
                    },
                    required = new[] { "directoryPath" }
                }
            };
        }

        private static McpToolSchema CreateGetSessionInfoTool()
        {
            return new McpToolSchema
            {
                Name = "get_session_info",
                Description = "Get basic information about the current debugging session",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateAnalyzeCallStackTool()
        {
            return new McpToolSchema
            {
                Name = "analyze_call_stack",
                Description = "Analyze the current call stack with detailed information",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateAnalyzeMemoryTool()
        {
            return new McpToolSchema
            {
                Name = "analyze_memory",
                Description = "Get memory information and analyze memory usage",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateAnalyzeCrashPatternsTool()
        {
            return new McpToolSchema
            {
                Name = "analyze_crash_patterns",
                Description = "Check for common crash patterns and provide automated analysis",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateGetCommandStatusTool()
        {
            return new McpToolSchema
            {
                Name = "get_command_status",
                Description = "✅ REQUIRED: Get results from run_windbg_cmd_async. Call this repeatedly until status='completed', then extract 'result' field for actual WinDBG output. This is the ONLY way to get command results!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        commandId = new { type = "string", description = "Command ID from run_windbg_cmd_async" }
                    },
                    required = new[] { "commandId" }
                }
            };
        }

        private static McpToolSchema CreateCancelCommandTool()
        {
            return new McpToolSchema
            {
                Name = "cancel_command",
                Description = "Cancel a queued or running command. Useful for stopping long-running commands.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        commandId = new { type = "string", description = "Command ID to cancel" }
                    },
                    required = new[] { "commandId" }
                }
            };
        }

        private static McpToolSchema CreateListCommandsTool()
        {
            return new McpToolSchema
            {
                Name = "list_commands",
                Description = "List current command queue status. Shows queued, executing, and recent commands.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateRunWindbgCmdDeprecatedTool()
        {
            return new McpToolSchema
            {
                Name = "run_windbg_cmd",
                Description = "🚨 REMOVED! This command has been PERMANENTLY REMOVED! Use 'run_windbg_cmd_async' instead. Will return aggressive error message until you switch!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "WinDBG command to execute" }
                    },
                    required = new[] { "command" }
                }
            };
        }

        private static McpToolSchema CreateRunWindbgCmdSyncDeprecatedTool()
        {
            return new McpToolSchema
            {
                Name = "run_windbg_cmd_sync",
                Description = "🚨 REMOVED! This command has been PERMANENTLY REMOVED! Use 'run_windbg_cmd_async' instead. Will return aggressive error message until you switch!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "WinDBG command to execute" }
                    },
                    required = new[] { "command" }
                }
            };
        }

        private static McpToolSchema CreateGetCurrentTimeTool()
        {
            return new McpToolSchema
            {
                Name = "get_current_time",
                Description = "Get current time for a city",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        city = new { type = "string", description = "City name" }
                    },
                    required = new[] { "city" }
                }
            };
        }
    }
}

