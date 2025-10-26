using Nexus.Protocol.Models;
using Nexus.Protocol.Notifications;

using NLog;

namespace Nexus.Protocol.Services;

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
            CreateCancelCommandTool()
        ];
    }

    /// <summary>
    /// Creates the tool schema for opening a debugging session.
    /// </summary>
    private static McpToolSchema CreateOpenSessionTool()
    {
        return new McpToolSchema
        {
            Name = "nexus_open_dump_analyze_session",
            Description = "Opens a new debugging session for crash dump analysis. Returns sessionId for use in subsequent operations.",
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
                        description = "Optional: Path to symbols directory for enhanced analysis"
                    }
                },
                required = new[] { "dumpPath" }
            }
        };
    }

    /// <summary>
    /// Creates the tool schema for enqueuing a debugging command.
    /// </summary>
    private static McpToolSchema CreateEnqueueCommandTool()
    {
        return new McpToolSchema
        {
            Name = "nexus_enqueue_async_dump_analyze_command",
            Description = "Enqueues a debugging command for asynchronous execution. Returns commandId for tracking.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session"
                    },
                    command = new
                    {
                        type = "string",
                        description = "WinDbg/CDB command to execute (e.g., 'k', '!analyze -v', 'lm')"
                    }
                },
                required = new[] { "sessionId", "command" }
            }
        };
    }

    /// <summary>
    /// Creates the tool schema for enqueuing an extension command.
    /// </summary>
    private static McpToolSchema CreateEnqueueExtensionCommandTool()
    {
        return new McpToolSchema
        {
            Name = "nexus_enqueue_async_extension_command",
            Description = "Enqueues an extension command for asynchronous execution. Returns commandId for tracking.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session"
                    },
                    extensionName = new
                    {
                        type = "string",
                        description = "Name of the extension to execute"
                    },
                    parameters = new
                    {
                        type = "object",
                        description = "Optional parameters to pass to the extension (JSON object)"
                    }
                },
                required = new[] { "sessionId", "extensionName" }
            }
        };
    }

    /// <summary>
    /// Creates the tool schema for reading command results.
    /// </summary>
    private static McpToolSchema CreateReadResultTool()
    {
        return new McpToolSchema
        {
            Name = "nexus_read_dump_analyze_command_result",
            Description = "Reads the result of a previously enqueued command. Blocks until command completes.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session"
                    },
                    commandId = new
                    {
                        type = "string",
                        description = "Command ID from nexus_enqueue_async_dump_analyze_command"
                    }
                },
                required = new[] { "sessionId", "commandId" }
            }
        };
    }

    /// <summary>
    /// Creates the tool schema for getting bulk command status.
    /// </summary>
    private static McpToolSchema CreateGetCommandsStatusTool()
    {
        return new McpToolSchema
        {
            Name = "nexus_get_dump_analyze_commands_status",
            Description = "Gets status of all commands in a session. Efficient for monitoring multiple commands.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session"
                    }
                },
                required = new[] { "sessionId" }
            }
        };
    }

    /// <summary>
    /// Creates the tool schema for closing a debugging session.
    /// </summary>
    private static McpToolSchema CreateCloseSessionTool()
    {
        return new McpToolSchema
        {
            Name = "nexus_close_dump_analyze_session",
            Description = "Closes a debugging session and releases resources.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session"
                    }
                },
                required = new[] { "sessionId" }
            }
        };
    }

    /// <summary>
    /// Creates the tool schema for canceling a command.
    /// </summary>
    private static McpToolSchema CreateCancelCommandTool()
    {
        return new McpToolSchema
        {
            Name = "nexus_cancel_command",
            Description = "Cancels a queued or executing command.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    sessionId = new
                    {
                        type = "string",
                        description = "Session ID from nexus_open_dump_analyze_session"
                    },
                    commandId = new
                    {
                        type = "string",
                        description = "Command ID from nexus_enqueue_async_dump_analyze_command"
                    }
                },
                required = new[] { "sessionId", "commandId" }
            }
        };
    }
}

