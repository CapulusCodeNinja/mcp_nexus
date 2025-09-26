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
                // ✅ NEXUS DEBUGGER COMMANDS - Core functionality for crash dump analysis
                CreateNexusOpenDumpTool(),
                CreateNexusExecDebuggerCommandAsyncTool(),
                CreateNexusDebuggerCommandStatusTool(),
                CreateNexusCloseDumpTool()
                // NOTE: Remote debugging and command cancellation will be added in future releases
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
                "⏰ POLLING REQUIRED: Check nexus_debugger_command_status EVERY 3-5 SECONDS until status is 'completed' " +
                "🔄 EXACT WORKFLOW: nexus_exec_debugger_command_async → GET commandId → nexus_debugger_command_status(commandId) → REPEAT until complete " +
                "🎯 BEST PRACTICE: Always include 'sessionId' parameter for proper API usage " +
                "🚨 FALLBACK ONLY: If sessionId is missing, service will auto-detect most recent session (NOT RECOMMENDED) " +
                "⚠️ AUTO-DETECTION WARNING: This fallback generates warnings and should not be relied upon " +
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
                    "⏳ If status='executing' or 'queued', wait 3-5 seconds and call this again! " +
                    "🔄 KEEP POLLING: Call this repeatedly every 3-5 seconds until status='completed' " +
                    "❌ NEVER GIVE UP: If status is not 'completed', you MUST try again later! " +
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


    }
}


