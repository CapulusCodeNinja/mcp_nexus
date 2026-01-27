using System.Text;

using NLog;

using WinAiDbg.Protocol.Models;
using WinAiDbg.Protocol.Notifications;
using WinAiDbg.Protocol.Utilities;

namespace WinAiDbg.Protocol.Services;

/// <summary>
/// Implementation of the MCP tool definition service.
/// Manages the collection of available debugging tools exposed through the MCP protocol.
/// </summary>
internal class McpToolDefinitionService : IMcpToolDefinitionService
{
    private readonly Logger m_Logger;
    private readonly IMcpNotificationService? m_NotificationService;
    private readonly McpToolSchema[] m_Tools;

    /// <summary>
    /// Creates a standardized Markdown description for MCP tools.
    /// </summary>
    /// <param name="toolName">The MCP tool name.</param>
    /// <param name="summary">A short summary of what the tool does.</param>
    /// <returns>Markdown description including the shared usage guide.</returns>
    private static string CreateToolDescriptionMarkdown(string toolName, string summary)
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine($"## {toolName}");
        _ = sb.AppendLine();
        _ = sb.AppendLine(summary);
        _ = sb.AppendLine(MarkdownFormatter.GetUsageGuideMarkdown());
        return sb.ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolDefinitionService"/> class.
    /// </summary>
    /// <param name="notificationService">Optional notification service for tool list changes.</param>
    public McpToolDefinitionService(
        IMcpNotificationService? notificationService = null)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_NotificationService = notificationService;

        m_Tools = InitializeToolSchemas();
        m_Logger.Debug("Initialized {ToolCount} MCP tool definitions", m_Tools.Length);
    }

    /// <summary>
    /// Gets all available MCP tool schemas.
    /// </summary>
    /// <returns>An array of tool schemas.</returns>
    public McpToolSchema[] GetAllTools()
    {
        return m_Tools;
    }

    /// <summary>
    /// Gets a specific tool schema by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to retrieve.</param>
    /// <returns>The tool schema, or null if not found.</returns>
    public McpToolSchema? GetTool(string toolName)
    {
        return m_Tools.FirstOrDefault(t => t.Name == toolName);
    }

    /// <summary>
    /// Notifies clients that the tools list has changed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task NotifyToolsChangedAsync()
    {
        if (m_NotificationService != null)
        {
            await m_NotificationService.NotifyToolsListChangedAsync();
            m_Logger.Info("Notified clients of tools list change");
        }
    }

    /// <summary>
    /// Initializes the collection of tool schemas.
    /// </summary>
    /// <returns>An array of tool schemas.</returns>
    private static McpToolSchema[] InitializeToolSchemas()
    {
        return
        [
            CreateOpenSessionTool(),
            CreateEnqueueCommandTool(),
            CreateEnqueueExtensionCommandTool(),
            CreateReadResultTool(),
            CreateGetCommandsStatusTool(),
            CreateCloseSessionTool(),
            CreateCancelCommandTool(),
            CreateDeprecatedNexusOpenSessionTool(),
            CreateDeprecatedNexusEnqueueCommandTool(),
            CreateDeprecatedNexusEnqueueExtensionCommandTool(),
            CreateDeprecatedNexusReadResultTool(),
            CreateDeprecatedNexusGetCommandsStatusTool(),
            CreateDeprecatedNexusCloseSessionTool(),
            CreateDeprecatedNexusCancelCommandTool(),
        ];
    }

    /// <summary>
    /// Creates the tool schema for opening a debugging session.
    /// </summary>
    /// <returns>The tool schema describing the open-session operation.</returns>
    private static McpToolSchema CreateOpenSessionTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "winaidbg_open_dump_analyze_session",
            "Opens a new debugging session for crash dump analysis. Returns sessionId for use in subsequent operations. MCP call shape: tools/call with params.arguments { dumpPath: string, symbolsPath?: string }.");

        return new McpToolSchema
        {
            Name = "winaidbg_open_dump_analyze_session",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    dumpPath = new
                    {
                        type = "string",
                        description = "Full path to the crash dump file (.dmp)",
                    },
                },
                required = new[] { "dumpPath" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for enqueuing a debugging command.
    /// </summary>
    /// <returns>The tool schema describing the enqueue-command operation.</returns>
    private static McpToolSchema CreateEnqueueCommandTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "winaidbg_enqueue_async_dump_analyze_command",
            "Enqueues a debugging command for asynchronous execution. Returns commandId for tracking. MCP call shape: tools/call with params.arguments { sessionId: string, command: string }.");

        return new McpToolSchema
        {
            Name = "winaidbg_enqueue_async_dump_analyze_command",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from winaidbg_open_dump_analyze_session",
                    },
                    command = new
                    {
                        type = "string",
                        description = "WinDbg/CDB command to execute (e.g., 'k', '!analyze -v', 'lm')",
                    },
                },
                required = new[] { "sessionId", "command" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for enqueuing an extension command.
    /// </summary>
    /// <returns>The tool schema describing the enqueue-extension operation.</returns>
    private static McpToolSchema CreateEnqueueExtensionCommandTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "winaidbg_enqueue_async_extension_command",
            "Enqueues an extension command for asynchronous execution. Returns commandId for tracking. MCP call shape: tools/call with params.arguments { sessionId: string, extensionName: string, parameters?: object }.");

        return new McpToolSchema
        {
            Name = "winaidbg_enqueue_async_extension_command",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from winaidbg_open_dump_analyze_session",
                    },
                    extensionName = new
                    {
                        type = "string",
                        description = "Name of the extension to execute",
                    },
                    parameters = new
                    {
                        type = "object",
                        description = "Optional parameters to pass to the extension (JSON object)",
                    },
                },
                required = new[] { "sessionId", "extensionName" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for reading command results.
    /// </summary>
    /// <returns>The tool schema describing the read-result operation.</returns>
    private static McpToolSchema CreateReadResultTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "winaidbg_read_dump_analyze_command_result",
            "Reads the result of a previously enqueued command. Waits up to maxWaitSeconds for completion; if still running, returns current state without output. MCP call shape: tools/call with params.arguments { sessionId: string, commandId: string, maxWaitSeconds: integer }.");

        return new McpToolSchema
        {
            Name = "winaidbg_read_dump_analyze_command_result",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from winaidbg_open_dump_analyze_session",
                    },
                    commandId = new
                    {
                        type = "string",
                        description = "Command ID from winaidbg_enqueue_async_dump_analyze_command",
                    },
                    maxWaitSeconds = new
                    {
                        type = "integer",
                        minimum = 1,
                        maximum = 30,
                        description = "Maximum seconds to wait for completion (1-30). For polling (0-second wait), use winaidbg_get_dump_analyze_commands_status.",
                    },
                },
                required = new[] { "sessionId", "commandId", "maxWaitSeconds" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for getting bulk command status.
    /// </summary>
    /// <returns>The tool schema describing the get-commands-status operation.</returns>
    private static McpToolSchema CreateGetCommandsStatusTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "winaidbg_get_dump_analyze_commands_status",
            "Gets status of all commands in a session. Efficient for monitoring multiple commands. MCP call shape: tools/call with params.arguments { sessionId: string }.");

        return new McpToolSchema
        {
            Name = "winaidbg_get_dump_analyze_commands_status",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from winaidbg_open_dump_analyze_session",
                    },
                },
                required = new[] { "sessionId" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for closing a debugging session.
    /// </summary>
    /// <returns>The tool schema describing the close-session operation.</returns>
    private static McpToolSchema CreateCloseSessionTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "winaidbg_close_dump_analyze_session",
            "Closes a debugging session and releases resources. MCP call shape: tools/call with params.arguments { sessionId: string }.");

        return new McpToolSchema
        {
            Name = "winaidbg_close_dump_analyze_session",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from winaidbg_open_dump_analyze_session",
                    },
                },
                required = new[] { "sessionId" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for canceling a command.
    /// </summary>
    /// <returns>The tool schema describing the cancel-command operation.</returns>
    private static McpToolSchema CreateCancelCommandTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "winaidbg_cancel_dump_analyze_command",
            "Cancels a queued or executing command. MCP call shape: tools/call with params.arguments { sessionId: string, commandId: string }.");

        return new McpToolSchema
        {
            Name = "winaidbg_cancel_dump_analyze_command",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from winaidbg_open_dump_analyze_session",
                    },
                    commandId = new
                    {
                        type = "string",
                        description = "Command ID from winaidbg_enqueue_async_dump_analyze_command",
                    },
                },
                required = new[] { "sessionId", "commandId" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for opening a debugging session.
    /// </summary>
    /// <returns>The tool schema describing the open-session operation.</returns>
    private static McpToolSchema CreateDeprecatedNexusOpenSessionTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "nexus_open_dump_analyze_session",
            "Deprecated but kept for backward compatibility. Same as Execute. MCP call shape: tools/call with params.arguments { dumpPath: string, symbolsPath?: string }.");

        return new McpToolSchema
        {
            Name = "nexus_open_dump_analyze_session",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    dumpPath = new
                    {
                        type = "string",
                        description = "Full path to the crash dump file (.dmp)",
                    },
                    symbolsPath = new
                    {
                        type = "string",
                        description = "Optional: Path to symbols directory for enhanced analysis",
                    },
                },
                required = new[] { "dumpPath" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for enqueuing a debugging command.
    /// </summary>
    /// <returns>The tool schema describing the enqueue-command operation.</returns>
    private static McpToolSchema CreateDeprecatedNexusEnqueueCommandTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "nexus_enqueue_async_dump_analyze_command",
            "Deprecated but kept for backward compatibility. Same as Execute. MCP call shape: tools/call with params.arguments { sessionId: string, command: string }.");

        return new McpToolSchema
        {
            Name = "nexus_enqueue_async_dump_analyze_command",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session",
                    },
                    command = new
                    {
                        type = "string",
                        description = "WinDbg/CDB command to execute (e.g., 'k', '!analyze -v', 'lm')",
                    },
                },
                required = new[] { "sessionId", "command" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for enqueuing an extension command.
    /// </summary>
    /// <returns>The tool schema describing the enqueue-extension operation.</returns>
    private static McpToolSchema CreateDeprecatedNexusEnqueueExtensionCommandTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "nexus_enqueue_async_extension_command",
            "Deprecated but kept for backward compatibility. Same as Execute. MCP call shape: tools/call with params.arguments { sessionId: string, extensionName: string, parameters?: object }.");

        return new McpToolSchema
        {
            Name = "nexus_enqueue_async_extension_command",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session",
                    },
                    extensionName = new
                    {
                        type = "string",
                        description = "Name of the extension to execute",
                    },
                    parameters = new
                    {
                        type = "object",
                        description = "Optional parameters to pass to the extension (JSON object)",
                    },
                },
                required = new[] { "sessionId", "extensionName" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for reading command results.
    /// </summary>
    /// <returns>The tool schema describing the read-result operation.</returns>
    private static McpToolSchema CreateDeprecatedNexusReadResultTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "nexus_read_dump_analyze_command_result",
            "Deprecated but kept for backward compatibility. Same behavior as winaidbg_read_dump_analyze_command_result. MCP call shape: tools/call with params.arguments { sessionId: string, commandId: string, maxWaitSeconds: integer }.");

        return new McpToolSchema
        {
            Name = "nexus_read_dump_analyze_command_result",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session",
                    },
                    commandId = new
                    {
                        type = "string",
                        description = "Command ID from nexus_enqueue_async_dump_analyze_command",
                    },
                    maxWaitSeconds = new
                    {
                        type = "integer",
                        minimum = 1,
                        maximum = 30,
                        description = "Maximum seconds to wait for completion (1-30). For polling (0-second wait), use nexus_get_dump_analyze_commands_status.",
                    },
                },
                required = new[] { "sessionId", "commandId", "maxWaitSeconds" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for getting bulk command status.
    /// </summary>
    /// <returns>The tool schema describing the get-commands-status operation.</returns>
    private static McpToolSchema CreateDeprecatedNexusGetCommandsStatusTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "nexus_get_dump_analyze_commands_status",
            "Deprecated but kept for backward compatibility. Same as Execute. MCP call shape: tools/call with params.arguments { sessionId: string }.");

        return new McpToolSchema
        {
            Name = "nexus_get_dump_analyze_commands_status",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session",
                    },
                },
                required = new[] { "sessionId" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for closing a debugging session.
    /// </summary>
    /// <returns>The tool schema describing the close-session operation.</returns>
    private static McpToolSchema CreateDeprecatedNexusCloseSessionTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "nexus_close_dump_analyze_session",
            "Deprecated but kept for backward compatibility. Same as Execute. MCP call shape: tools/call with params.arguments { sessionId: string }.");

        return new McpToolSchema
        {
            Name = "nexus_close_dump_analyze_session",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session",
                    },
                },
                required = new[] { "sessionId" },
            },
        };
    }

    /// <summary>
    /// Creates the tool schema for canceling a command.
    /// </summary>
    /// <returns>The tool schema describing the cancel-command operation.</returns>
    private static McpToolSchema CreateDeprecatedNexusCancelCommandTool()
    {
        var description = CreateToolDescriptionMarkdown(
            "nexus_cancel_dump_analyze_command",
            "Deprecated but kept for backward compatibility. Same as Execute. MCP call shape: tools/call with params.arguments { sessionId: string, commandId: string }.");

        return new McpToolSchema
        {
            Name = "nexus_cancel_dump_analyze_command",
            Description = description,
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session",
                    },
                    commandId = new
                    {
                        type = "string",
                        description = "Command ID from nexus_enqueue_async_dump_analyze_command",
                    },
                },
                required = new[] { "sessionId", "commandId" },
            },
        };
    }
}
