using mcp_nexus.Models;
using mcp_nexus.Notifications;
using mcp_nexus.Tools;
using mcp_nexus.Utilities.Json;

namespace mcp_nexus.Protocol
{
    /// <summary>
    /// Service for providing MCP tool definitions.
    /// Manages the collection and retrieval of available MCP tools for the server.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="McpToolDefinitionService"/> class.
    /// </remarks>
    /// <param name="notificationService">Optional notification service for publishing tool events.</param>
    public class McpToolDefinitionService(IMcpNotificationService? notificationService = null) : IMcpToolDefinitionService
    {
        private readonly IMcpNotificationService? m_NotificationService = notificationService;
        /// <summary>
        /// Gets all available MCP tools.
        /// </summary>
        /// <returns>An array of tool definitions.</returns>
        public McpToolSchema[] GetAllTools()
        {
            return
            [
                // NEXUS DEBUGGER COMMANDS - Core functionality for crash dump analysis
                CreateNexusOpenDumpAnalyzeSessionTool(),
                CreateNexusDumpAnalyzeSessionAsyncCommandTool(),
                CreateNexusCloseDumpAnalyzeSessionTool(),
                // NOTE: Remote debugging and command cancellation will be added in future releases
            ];
        }

        /// <summary>
        /// Notify clients that the tools list has changed (for future use if tools become dynamic)
        /// </summary>
        public async Task NotifyToolsChanged()
        {
            if (m_NotificationService != null)
            {
                await m_NotificationService.NotifyToolsListChangedAsync();
            }
        }


        /// <summary>
        /// Creates the MCP tool schema for opening a dump analyze session.
        /// </summary>
        /// <returns>The MCP tool schema for the open dump analyze session tool.</returns>
        private static readonly string m_ToolUsageJson = System.Text.Json.JsonSerializer.Serialize(
            SessionAwareWindbgTool.USAGE_EXPLANATION,
            JsonOptions.JsonIndented);

        private static McpToolSchema CreateNexusOpenDumpAnalyzeSessionTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_open_dump_analyze_session",
                Description = m_ToolUsageJson,
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

        /// <summary>
        /// Creates the MCP tool schema for closing a dump analyze session.
        /// </summary>
        /// <returns>The MCP tool schema for the close dump analyze session tool.</returns>
        private static McpToolSchema CreateNexusCloseDumpAnalyzeSessionTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_close_dump_analyze_session",
                Description = m_ToolUsageJson,
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

        /// <summary>
        /// Creates the MCP tool schema for executing async commands in a dump analyze session.
        /// </summary>
        /// <returns>The MCP tool schema for the dump analyze session async command tool.</returns>
        private static McpToolSchema CreateNexusDumpAnalyzeSessionAsyncCommandTool()
        {
            return new McpToolSchema
            {
                Name = "nexus_enqueue_async_dump_analyze_command",
                Description = m_ToolUsageJson,
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
    }
}