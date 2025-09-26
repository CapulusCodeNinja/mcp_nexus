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
                Description = "🚀 STEP 1 - START HERE: Open and analyze a Windows crash dump file (.dmp). " +
                    "⚠️ CRITICAL: This returns a 'sessionId' that you MUST save and use in ALL subsequent commands! " +
                    "📝 EXTRACT the sessionId from the response JSON and store it for later use. " +
                    "🔄 MANDATORY WORKFLOW: " +
                    "1️⃣ nexus_open_dump → SAVE the sessionId from response " +
                    "2️⃣ nexus_exec_debugger_command_async + sessionId → get commandId " +
                    "3️⃣ nexus_debugger_command_status + commandId → get results " +
                    "❌ WITHOUT sessionId, ALL other commands will FAIL!",
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
                Description = "🔗 ALTERNATIVE STEP 1 - REMOTE DEBUGGING: Connect to a live process or system for real-time debugging. " +
                    "Use this for debugging running applications, not crash dumps. " +
                    "⚠️ CRITICAL: This returns a 'sessionId' that you MUST save and use in ALL subsequent commands! " +
                    "📝 EXTRACT the sessionId from the response JSON and store it for later use. " +
                    "Connection examples: 'tcp:Port=5005,Server=192.168.0.100' or 'npipe:Pipe=MyApp,Server=.' " +
                    "🔄 MANDATORY WORKFLOW: " +
                    "1️⃣ nexus_start_remote_debug → SAVE the sessionId from response " +
                    "2️⃣ nexus_exec_debugger_command_async + sessionId → get commandId " +
                    "3️⃣ nexus_debugger_command_status + commandId → get results " +
                    "❌ WITHOUT sessionId, ALL other commands will FAIL!",
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
            Description = "⚡ STEP 2 - EXECUTE COMMANDS: Run debugger commands like '!analyze -v', 'k', 'lm', 'dt', etc. " +
                "🚨 ASYNC WORKFLOW - READ CAREFULLY: " +
                "1️⃣ This command ONLY QUEUES the command and returns a commandId " +
                "2️⃣ It does NOT return the actual debugger output! " +
                "3️⃣ You MUST call nexus_debugger_command_status(commandId) to get results " +
                "4️⃣ Commands execute asynchronously in background queue " +
                "🎯 BEST PRACTICE: Always include 'sessionId' parameter for proper API usage " +
                "🚨 FALLBACK ONLY: If sessionId is missing, service will auto-detect most recent session (NOT RECOMMENDED) " +
                "⚠️ AUTO-DETECTION WARNING: This fallback generates warnings and should not be relied upon " +
                "🔄 MANDATORY WORKFLOW: nexus_exec_debugger_command_async → nexus_debugger_command_status " +
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
                    command = new { type = "string", description = "WinDbg/CDB command like '!analyze -v', 'k', 'lm', etc." },
                    sessionId = new { type = "string", description = "RECOMMENDED: Session ID from nexus_open_dump response. If omitted, service will auto-detect (with warnings)." }
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
                Description = "📋 STEP 3 - GET RESULTS: Retrieve the output from a previously queued command. " +
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


