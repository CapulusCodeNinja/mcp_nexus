using mcp_nexus.Models;
using mcp_nexus.Notifications;
using mcp_nexus.Tools;

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
                // NEXUS DEBUGGER COMMANDS - Core functionality for crash dump analysis
                CreateNexusOpenDumpAnalyzeSessionTool(),
                CreateNexusDumpAnalyzeSessionAsyncCommandTool(),
                CreateNexusDumpAnalyzeSessionAsyncCommandStatusTool(),
                CreateNexusCloseDumpAnalyzeSessionTool(),
                CreateNexusListDumpAnalyzeSessionsTool(),
                CreateNexusListDumpAnalyzeSessionAsyncCommandsTool()
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
                Description = System.Text.Json.JsonSerializer.Serialize(
                    SessionAwareWindbgTool.TOOL_USAGE_EXPLANATION,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        dumpPath = new
                        {
                            type = "string",
                            description = "Full path to the crash dump file (.dmp)"
                        },
                        symbolsPath = new
                        {
                            type = "string",
                            description = "Optional: Path to symbols directory for better analysis"
                        }
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
                Description = System.Text.Json.JsonSerializer.Serialize(
                    SessionAwareWindbgTool.TOOL_USAGE_EXPLANATION,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sessionId = new
                        {
                            type = "string",
                            description = "REQUIRED: Session ID that you EXTRACTED from nexus_open_dump_analyze_session response JSON. Use the EXACT value (e.g., 'sess-000001-abc12345')"
                        }
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
                Description = System.Text.Json.JsonSerializer.Serialize(
                    SessionAwareWindbgTool.TOOL_USAGE_EXPLANATION,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        command = new
                        {
                            type = "string",
                            description = "WinDbg/CDB command like '!analyze -v', 'k', 'lm', etc."
                        },
                        sessionId = new
                        {
                            type = "string",
                            description = "REQUIRED: Session ID that you EXTRACTED from nexus_open_dump_analyze_session response JSON. This must be the EXACT value from the 'sessionId' field (e.g., 'sess-000001-abc12345'). DO NOT make up your own values!"
                        }
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
                Description = System.Text.Json.JsonSerializer.Serialize(
                    SessionAwareWindbgTool.TOOL_USAGE_EXPLANATION,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        commandId = new
                        {
                            type = "string",
                            description = "REQUIRED: The EXACT commandId that was returned by nexus_dump_analyze_session_async_command. Format: 'cmd-sess-XXXXXX-YYYYYYYY-ZZZZ'. DO NOT make up your own values!"
                        }
                    },
                    required = new[] { "commandId" }
                }
            };
        }

        private static McpToolSchema CreateNexusListDumpAnalyzeSessionsTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_list_dump_analyze_sessions",
                Description = System.Text.Json.JsonSerializer.Serialize(
                    SessionAwareWindbgTool.TOOL_USAGE_EXPLANATION,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                InputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                }
            };
        }

        private static McpToolSchema CreateNexusListDumpAnalyzeSessionAsyncCommandsTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_list_dump_analyze_session_async_commands",
                Description = System.Text.Json.JsonSerializer.Serialize(
                    SessionAwareWindbgTool.TOOL_USAGE_EXPLANATION,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sessionId = new
                        {
                            type = "string",
                            description = "REQUIRED: The EXACT sessionId that was returned by nexus_open_dump_analyze_session. Format: 'sess-XXXXXX-YYYYYYYY-timestamp-processId'. DO NOT make up your own values!"
                        }
                    },
                    required = new[] { "sessionId" }
                }
            };
        }
    }
}