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
                CreateNexusOpenDumpAnalyzeSessionTool(),
                CreateNexusDumpAnalyzeSessionAsyncCommandTool(),
                CreateNexusDumpAnalyzeSessionAsyncCommandStatusTool(),
                CreateNexusCloseDumpAnalyzeSessionTool()
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

        private static McpToolSchema CreateNexusOpenDumpAnalyzeSessionTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_open_dump_analyze_session",
                Description = "STEP 1 - START HERE: Open and analyze a Windows crash dump file (.dmp) by creating a dedicated analysis session. " +
                    "CRITICAL RETURN VALUE: This command RETURNS a sessionId in the response JSON that you MUST EXTRACT and SAVE! " +
                    "RESPONSE CONTAINS: {\"sessionId\": \"sess-000001-abc12345\", ...} " +
                    "YOU MUST: Parse the response JSON and extract the 'sessionId' field value! " +
                    "SAVE IT: Store this sessionId string and use it in ALL subsequent commands! " +
                    "MANDATORY WORKFLOW: " +
                    "1. nexus_open_dump_analyze_session → EXTRACT 'sessionId' from response JSON → SAVE IT! " +
                    "2. nexus_dump_analyze_session_async_command + SAVED sessionId → get commandId " +
                    "3. nexus_dump_analyze_session_async_command_status + commandId → get results " +
                    "4. nexus_close_dump_analyze_session + SAVED sessionId → CLOSE session when done (EXPECTED!) " +
                    "DO NOT MAKE UP sessionId VALUES! Use only what this command returns! " +
                    "CLEANUP EXPECTATION: You SHOULD call nexus_close_dump_analyze_session when finished analyzing to properly release resources and close the debugging session. While sessions auto-expire after 30 minutes, explicit closure is the expected and professional approach!",
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


        private static McpToolSchema CreateNexusCloseDumpAnalyzeSessionTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_close_dump_analyze_session",
                Description = "STEP 4 - CLEANUP: Close the current crash dump analysis session and release resources. " +
                    "EXPECTED BEHAVIOR: You SHOULD call this when done analyzing a dump file! " +
                    "PROFESSIONAL PRACTICE: While sessions auto-expire after 30 minutes, explicit closure is the expected and responsible approach. " +
                    "NEXT SESSION: After closing, you'll need nexus_open_dump_analyze_session again to analyze another dump. " +
                    "AI CLIENT TIP: Always close sessions when finished - it's good resource management!",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sessionId = new { type = "string", description = "REQUIRED: Session ID that you EXTRACTED from nexus_open_dump_analyze_session response JSON. Use the EXACT value (e.g., 'sess-000001-abc12345')" }
                    },
                    required = new[] { "sessionId" }
                }
            };
        }


        private static McpToolSchema CreateNexusDumpAnalyzeSessionAsyncCommandTool()
        {
            return new McpToolSchema
            {
            Name = "nexus_dump_analyze_session_async_command",
                Description = "STEP 2 - EXECUTE COMMANDS: Run debugger commands like '!analyze -v', 'k', 'lm', 'dt', etc. " +
                    "ASYNC WORKFLOW - READ CAREFULLY: " +
                    "1. This command ONLY QUEUES the command and returns a commandId " +
                    "2. It does NOT return the actual debugger output! " +
                    "3. You MUST call nexus_dump_analyze_session_async_command_status(commandId) to get results " +
                    "4. Commands execute asynchronously in background queue " +
                    "POLLING REQUIRED: Check nexus_dump_analyze_session_async_command_status EVERY 3-5 SECONDS until status is 'completed' " +
                    "EXACT WORKFLOW: nexus_dump_analyze_session_async_command → GET commandId → nexus_dump_analyze_session_async_command_status(commandId) → REPEAT until complete " +
                    "MANDATORY sessionId: You MUST include the sessionId from nexus_open_dump_analyze_session response! " +
                    "MISSING sessionId = ERROR: This command will FAIL without a valid sessionId parameter! " +
                    "HOW TO GET sessionId: Call nexus_open_dump_analyze_session first, extract 'sessionId' from response JSON, then use it here " +
                    "COMMON COMMANDS: " +
                    "• '!analyze -v' - Detailed crash analysis " +
                    "• 'k' - Call stack " +
                    "• 'lm' - List loaded modules " +
                    "• 'dt ModuleName!StructName' - Display type " +
                    "TIP: Listen for notifications/commandStatus to know when commands complete!",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    command = new { type = "string", description = "WinDbg/CDB command like '!analyze -v', 'k', 'lm', etc." },
                            sessionId = new { type = "string", description = "REQUIRED: Session ID that you EXTRACTED from nexus_open_dump_analyze_session response JSON. This must be the EXACT value from the 'sessionId' field (e.g., 'sess-000001-abc12345'). DO NOT make up your own values!" }
                },
                required = new[] { "command", "sessionId" }
            }
            };
        }



        private static McpToolSchema CreateNexusDumpAnalyzeSessionAsyncCommandStatusTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_dump_analyze_session_async_command_status",
                Description = "STEP 3 - GET RESULTS: Retrieve the output from a previously queued command. " +
                    "This is the ONLY way to get actual debugger command results! " +
                    "WORKFLOW DEPENDENCY: You can ONLY call this AFTER calling nexus_dump_analyze_session_async_command! " +
                    "REQUIRED SEQUENCE: nexus_open_dump_analyze_session → nexus_dump_analyze_session_async_command → nexus_dump_analyze_session_async_command_status " +
                    "STATUS FLOW: queued → executing → completed " +
                    "When status='completed', the 'result' field contains the debugger output. " +
                    "If status='executing' or 'queued', wait 3-5 seconds and call this again! " +
                    "KEEP POLLING: Call this repeatedly every 3-5 seconds until status='completed' " +
                    "NEVER SKIP STEPS: You cannot make up commandId values or skip nexus_dump_analyze_session_async_command! " +
                    "SMART TIP: Listen for notifications/commandStatus to know when to check instead of polling constantly.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        commandId = new { type = "string", description = "REQUIRED: The EXACT commandId that was returned by nexus_dump_analyze_session_async_command. Format: 'cmd-sess-XXXXXX-YYYYYYYY-ZZZZ'. DO NOT make up your own values!" }
                    },
                    required = new[] { "commandId" }
                }
            };
        }


    }
}


