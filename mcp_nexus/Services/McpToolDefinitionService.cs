using mcp_nexus.Models;

namespace mcp_nexus.Services
{
    public class McpToolDefinitionService
    {
        public McpToolSchema[] GetAllTools()
        {
            return
            [
                // ✅ NEXUS DEBUGGER COMMANDS
                CreateNexusExecDebuggerCommandAsyncTool(),
                CreateNexusOpenDumpTool(),
                CreateNexusStartRemoteDebugTool(),
                CreateNexusCloseDumpTool(),
                CreateNexusStopRemoteDebugTool(),
                CreateNexusDebuggerCommandStatusTool(),
                CreateNexusDebuggerCommandCancelTool(),
                CreateNexusListDebuggerCommandsTool()
            ];
        }

        private static McpToolSchema CreateNexusOpenDumpTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_open_dump",
                Description = "Analyze a Windows crash dump file using common debugger commands. Automatically replaces any existing session with the new dump file.",
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

        private static McpToolSchema CreateNexusStartRemoteDebugTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_start_remote_debug",
                Description = "Connect to a remote debugging session using a connection string (e.g., tcp:Port=5005,Server=192.168.0.100). Automatically replaces any existing session with the new remote connection.",
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

        private static McpToolSchema CreateNexusCloseDumpTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_close_dump",
                Description = "Unload a crash dump and release resources",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateNexusStopRemoteDebugTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_stop_remote_debug",
                Description = "Disconnect from a remote debugging session and release resources",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }

        private static McpToolSchema CreateNexusExecDebuggerCommandAsyncTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_exec_debugger_command_async",
                Description = "🔄 ASYNC QUEUE: Execute debugger command in background queue. ⚠️ NEVER returns results directly! Always returns commandId for polling. You MUST call nexus_debugger_command_status(commandId) to get actual results. NO EXCEPTIONS!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "Debugger command to execute" }
                    },
                    required = new[] { "command" }
                }
            };
        }



        private static McpToolSchema CreateNexusDebuggerCommandStatusTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_debugger_command_status",
                Description = "✅ REQUIRED: Get results from nexus_exec_debugger_command_async. Call this repeatedly until status='completed', then extract 'result' field for actual debugger output. This is the ONLY way to get command results!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        commandId = new { type = "string", description = "Command ID from nexus_exec_debugger_command_async" }
                    },
                    required = new[] { "commandId" }
                }
            };
        }

        private static McpToolSchema CreateNexusDebuggerCommandCancelTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_debugger_command_cancel",
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

        private static McpToolSchema CreateNexusListDebuggerCommandsTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_list_debugger_commands",
                Description = "List current command queue status. Shows queued, executing, and recent commands.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            };
        }
    }
}

