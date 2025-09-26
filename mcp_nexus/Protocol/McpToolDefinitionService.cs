using mcp_nexus.Models;
using mcp_nexus.Notifications;

namespace mcp_nexus.Protocol
{
    public class McpToolDefinitionService
    {
        private readonly IMcpNotificationService? m_notificationService;

        public McpToolDefinitionService(IMcpNotificationService? notificationService = null)
        {
            m_notificationService = notificationService;
        }

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
                CreateNexusDebuggerCommandCancelTool()
                // REMOVED: CreateNexusListDebuggerCommandsTool() - deprecated in session-aware architecture
            ];
        }

        /// <summary>
        /// Notify clients that the tools list has changed (for future use if tools become dynamic)
        /// </summary>
        public async Task NotifyToolsChanged()
        {
            if (m_notificationService != null)
            {
                await m_notificationService.NotifyToolsListChangedAsync();
            }
        }

        private static McpToolSchema CreateNexusOpenDumpTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_open_dump",
                Description = "🚀 START HERE: Open and analyze a Windows crash dump file (.dmp). " +
                    "This is typically your FIRST step when debugging crashes. " +
                    "Creates a new debugging session that replaces any existing session. " +
                    "After opening, use nexus_exec_debugger_command_async to run commands like '!analyze -v', 'k', 'lm', etc. " +
                    "💡 WORKFLOW: nexus_open_dump → nexus_exec_debugger_command_async → nexus_debugger_command_status (to get results)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        dumpPath = new { type = "string", description = "Full path to the crash dump file (.dmp)" },
                        symbolsPath = new { type = "string", description = "Optional: Path to symbols directory for better analysis" }
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
                Description = "🔗 REMOTE DEBUGGING: Connect to a live process or system for real-time debugging. " +
                    "Use this for debugging running applications, not crash dumps. " +
                    "Creates a new debugging session that replaces any existing session. " +
                    "Connection examples: 'tcp:Port=5005,Server=192.168.0.100' or 'npipe:Pipe=MyApp,Server=.' " +
                    "💡 WORKFLOW: nexus_start_remote_debug → nexus_exec_debugger_command_async → nexus_debugger_command_status",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        connectionString = new { type = "string", description = "Connection string: tcp:Port=XXXX,Server=IP or npipe:Pipe=NAME,Server=." },
                        symbolsPath = new { type = "string", description = "Optional: Path to symbols directory for better analysis" }
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
                Description = "🔚 CLEANUP: Close the current crash dump session and release resources. " +
                    "Use this when you're done analyzing a dump file. " +
                    "After closing, you'll need nexus_open_dump again to analyze another dump.",
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
                Description = "🔌 DISCONNECT: Stop the current remote debugging session and release resources. " +
                    "Use this when you're done debugging a remote process. " +
                    "After stopping, you'll need nexus_start_remote_debug again to connect to another target.",
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
                Description = "⚡ EXECUTE COMMANDS: Run debugger commands like '!analyze -v', 'k', 'lm', 'dt', etc. " +
                    "⚠️ CRITICAL: This only QUEUES the command and returns a commandId - it does NOT return results! " +
                    "🔄 REQUIRED NEXT STEP: You MUST call nexus_debugger_command_status(commandId) to get the actual output. " +
                    "💡 COMMON COMMANDS: " +
                    "• '!analyze -v' - Detailed crash analysis " +
                    "• 'k' - Call stack " +
                    "• 'lm' - List loaded modules " +
                    "• 'dt ModuleName!StructName' - Display type " +
                    "📡 TIP: Listen for notifications/commandStatus to know when commands complete!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new { type = "string", description = "WinDbg/CDB command like '!analyze -v', 'k', 'lm', etc." }
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
                Description = "📋 GET RESULTS: Retrieve the output from a previously queued command. " +
                    "This is the ONLY way to get actual debugger command results! " +
                    "📊 STATUS FLOW: queued → executing → completed " +
                    "✅ When status='completed', the 'result' field contains the debugger output. " +
                    "⏳ If status='executing' or 'queued', wait and try again. " +
                    "💡 SMART TIP: Listen for notifications/commandStatus to know when to check instead of polling constantly.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        commandId = new { type = "string", description = "The commandId returned by nexus_exec_debugger_command_async" }
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
                Description = "❌ CANCEL COMMAND: Stop a queued or running command. " +
                    "Useful for canceling long-running commands that are taking too long. " +
                    "Once canceled, the command status will change to 'canceled'.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        commandId = new { type = "string", description = "The commandId to cancel (from nexus_exec_debugger_command_async)" }
                    },
                    required = new[] { "commandId" }
                }
            };
        }

    }
}


