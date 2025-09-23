using mcp_nexus.Models;

namespace mcp_nexus.Services
{
    public class McpToolDefinitionService
    {
        public McpToolSchema[] GetAllTools()
        {
            return new[]
            {
                CreateOpenWindbgDumpTool(),
                CreateOpenWindbgRemoteTool(),
                CreateCloseWindbgDumpTool(),
                CreateCloseWindbgRemoteTool(),
                CreateRunWindbgCmdTool(),
                CreateListWindbgDumpsTool(),
                CreateGetSessionInfoTool(),
                CreateAnalyzeCallStackTool(),
                CreateAnalyzeMemoryTool(),
                CreateAnalyzeCrashPatternsTool(),
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

        private static McpToolSchema CreateRunWindbgCmdTool()
        {
            return new McpToolSchema
            {
                Name = "run_windbg_cmd",
                Description = "Execute any WinDBG command with smart timeout handling. HYBRID BEHAVIOR: Quick commands (<5s) return results immediately in 'result' field. Long commands (>5s) return jobId for polling with get_job_status(jobId)",
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

