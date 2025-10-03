using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using mcp_nexus.Session.Models;

namespace mcp_nexus.Models
{
    /// <summary>
    /// Represents a Model Context Protocol (MCP) request.
    /// Contains the JSON-RPC request structure with method, parameters, and ID.
    /// </summary>
    public class McpRequest
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version. Must be "2.0".
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        [Required(ErrorMessage = "jsonrpc field is required")]
        [RegularExpression("^2\\.0$", ErrorMessage = "jsonrpc must be '2.0'")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// Gets or sets the JSON-RPC method name.
        /// </summary>
        [JsonPropertyName("method")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "method field is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "method must be between 1 and 100 characters")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON-RPC parameters.
        /// </summary>
        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }

        [JsonPropertyName("id")]
        public object? Id { get; set; }
    }

    /// <summary>
    /// Represents a Model Context Protocol (MCP) response.
    /// Contains the JSON-RPC response structure with result or error information.
    /// </summary>
    public class McpResponse
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        
        /// <summary>
        /// Gets or sets the JSON-RPC response ID.
        /// </summary>
        [JsonPropertyName("id")]
        public object? Id { get; set; }
        
        /// <summary>
        /// Gets or sets the JSON-RPC result data.
        /// </summary>
        [JsonPropertyName("result")]
        public object? Result { get; set; }
        
        /// <summary>
        /// Gets or sets the JSON-RPC error information.
        /// </summary>
        [JsonPropertyName("error")]
        public McpError? Error { get; set; }
    }

    /// <summary>
    /// Represents a successful Model Context Protocol (MCP) response.
    /// Contains the JSON-RPC response structure with result data.
    /// </summary>
    public class McpSuccessResponse
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        
        /// <summary>
        /// Gets or sets the JSON-RPC response ID.
        /// </summary>
        [JsonPropertyName("id")]
        public object? Id { get; set; }
        
        /// <summary>
        /// Gets or sets the JSON-RPC result data.
        /// </summary>
        [JsonPropertyName("result")]
        public object? Result { get; set; }
    }

    /// <summary>
    /// Represents an error Model Context Protocol (MCP) response.
    /// Contains the JSON-RPC response structure with error information.
    /// </summary>
    public class McpErrorResponse
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        
        /// <summary>
        /// Gets or sets the JSON-RPC response ID.
        /// </summary>
        [JsonPropertyName("id")]
        public object? Id { get; set; }
        
        /// <summary>
        /// Gets or sets the JSON-RPC error information.
        /// </summary>
        [JsonPropertyName("error")]
        public McpError Error { get; set; } = new();
    }

    /// <summary>
    /// Represents an error in a Model Context Protocol (MCP) response.
    /// Contains error code, message, and optional data.
    /// </summary>
    public class McpError
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        [JsonPropertyName("code")]
        [Required(ErrorMessage = "error code is required")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonPropertyName("message")]
        [Required(ErrorMessage = "error message is required")]
        [StringLength(1000, ErrorMessage = "error message cannot exceed 1000 characters")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional error data.
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    /// <summary>
    /// Represents the schema for a Model Context Protocol (MCP) tool.
    /// Contains tool name, description, and input schema definition.
    /// </summary>
    public class McpToolSchema
    {
        /// <summary>
        /// Gets or sets the tool name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the tool description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the input schema definition.
        /// </summary>
        [JsonPropertyName("inputSchema")]
        public object InputSchema { get; set; } = new { };
    }

    /// <summary>
    /// Represents the result of a Model Context Protocol (MCP) tool execution.
    /// Contains an array of content items returned by the tool.
    /// </summary>
    public class McpToolResult
    {
        /// <summary>
        /// Gets or sets the array of content items returned by the tool.
        /// </summary>
        [JsonPropertyName("content")]
        public McpContent[] Content { get; set; } = Array.Empty<McpContent>();
    }

    /// <summary>
    /// Represents content returned by a Model Context Protocol (MCP) tool.
    /// Contains the content type and text data.
    /// </summary>
    public class McpContent
    {
        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";
        
        /// <summary>
        /// Gets or sets the content text.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of a Model Context Protocol (MCP) initialization.
    /// Contains protocol version, capabilities, and server information.
    /// </summary>
    public class McpInitializeResult
    {
        /// <summary>
        /// Gets or sets the protocol version.
        /// </summary>
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2025-06-18";
        
        /// <summary>
        /// Gets or sets the server capabilities.
        /// </summary>
        [JsonPropertyName("capabilities")]
        public McpCapabilities Capabilities { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the server information.
        /// </summary>
        [JsonPropertyName("serverInfo")]
        public McpServerDetails ServerInfo { get; set; } = new();
    }

    /// <summary>
    /// Represents the result of listing Model Context Protocol (MCP) tools.
    /// Contains an array of available tool schemas.
    /// </summary>
    public class McpToolsListResult
    {
        /// <summary>
        /// Gets or sets the array of available tool schemas.
        /// </summary>
        [JsonPropertyName("tools")]
        public McpToolSchema[] Tools { get; set; } = Array.Empty<McpToolSchema>();
    }

    // ===== MCP RESOURCE MODELS =====

    /// <summary>
    /// Represents a Model Context Protocol (MCP) resource.
    /// Contains resource URI, name, description, and MIME type.
    /// </summary>
    public class McpResource
    {
        /// <summary>
        /// Gets or sets the resource URI.
        /// </summary>
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MIME type of the resource.
        /// </summary>
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        /// <summary>
        /// Gets or sets the expiration time of the resource.
        /// </summary>
        [JsonPropertyName("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Represents the result of listing Model Context Protocol (MCP) resources.
    /// Contains an array of available resources.
    /// </summary>
    public class McpResourcesListResult
    {
        /// <summary>
        /// Gets or sets the array of available resources.
        /// </summary>
        [JsonPropertyName("resources")]
        public McpResource[] Resources { get; set; } = Array.Empty<McpResource>();
    }

    /// <summary>
    /// Represents the content of a Model Context Protocol (MCP) resource.
    /// Contains resource URI, MIME type, and content data (text or blob).
    /// </summary>
    public class McpResourceContent
    {
        /// <summary>
        /// Gets or sets the resource URI.
        /// </summary>
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MIME type of the content.
        /// </summary>
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = "text/plain";

        /// <summary>
        /// Gets or sets the text content.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the binary content as base64 string.
        /// </summary>
        [JsonPropertyName("blob")]
        public string? Blob { get; set; }
    }

    /// <summary>
    /// Represents the result of reading a Model Context Protocol (MCP) resource.
    /// Contains an array of resource content items.
    /// </summary>
    public class McpResourceReadResult
    {
        /// <summary>
        /// Gets or sets the array of resource content items.
        /// </summary>
        [JsonPropertyName("contents")]
        public McpResourceContent[] Contents { get; set; } = Array.Empty<McpResourceContent>();
    }

    /// <summary>
    /// Represents a Model Context Protocol (MCP) server info response.
    /// Contains JSON-RPC response structure with server information.
    /// </summary>
    public class McpServerInfoResponse
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        
        /// <summary>
        /// Gets or sets the server info result.
        /// </summary>
        [JsonPropertyName("result")]
        public McpServerInfoResult Result { get; set; } = new();
    }

    /// <summary>
    /// Represents the result of a Model Context Protocol (MCP) server info request.
    /// Contains protocol version, capabilities, and server details.
    /// </summary>
    public class McpServerInfoResult
    {
        /// <summary>
        /// Gets or sets the protocol version.
        /// </summary>
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2025-06-18";
        
        /// <summary>
        /// Gets or sets the server capabilities.
        /// </summary>
        [JsonPropertyName("capabilities")]
        public McpCapabilities Capabilities { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the server details.
        /// </summary>
        [JsonPropertyName("serverInfo")]
        public McpServerDetails ServerInfo { get; set; } = new();
    }

    /// <summary>
    /// Represents the capabilities of a Model Context Protocol (MCP) server.
    /// Contains tool, resource, and notification capabilities.
    /// </summary>
    public class McpCapabilities
    {
        /// <summary>
        /// Gets or sets the tools capabilities.
        /// </summary>
        [JsonPropertyName("tools")]
        public object Tools { get; set; } = new { listChanged = true };

        /// <summary>
        /// Gets or sets the resources capabilities.
        /// </summary>
        [JsonPropertyName("resources")]
        public object Resources { get; set; } = new
        {
            subscribe = true,
            listChanged = true
        };

        /// <summary>
        /// Gets or sets the notifications capabilities.
        /// </summary>
        [JsonPropertyName("notifications")]
        public object Notifications { get; set; } = new
        {
            // Standard MCP notifications
            tools = new { listChanged = true },
            resources = new { listChanged = true },
            // Custom MCP Nexus notifications  
            commandStatus = true,
            sessionRecovery = true,
            serverHealth = true
        };
    }

    /// <summary>
    /// Represents details about a Model Context Protocol (MCP) server.
    /// Contains server name, version, and description information.
    /// </summary>
    public class McpServerDetails
    {
        /// <summary>
        /// Gets or sets the server name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "mcp-nexus";
        
        /// <summary>
        /// Gets or sets the server version.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = VersionHelper.GetFileVersion();
        
        /// <summary>
        /// Gets or sets the server description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "Windows Debugging Tools MCP Server for crash dump analysis. " +
            "Provides asynchronous debugging commands with comprehensive session management. " +
            "All commands execute asynchronously - use 'nexus_read_dump_analyze_command_result' tool " +
            "to get results and the 'commands' resource to monitor execution status.";
    }

    // ===== SERVER NOTIFICATION MODELS =====

    /// <summary>
    /// Server-initiated notification (no ID, no response expected)
    /// </summary>
    public class McpNotification
    {
        /// <summary>
        /// Gets or sets the JSON-RPC version.
        /// </summary>
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// Gets or sets the notification method.
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification parameters.
        /// </summary>
        [JsonPropertyName("params")]
        public object? Params { get; set; }
    }

    /// <summary>
    /// Command status notification parameters
    /// 
    /// Sent via notifications/commandStatus when debugger commands change state.
    /// Status progression: "queued" → "executing" → "completed" (or "cancelled"/"failed")
    /// 
    /// Example notification:
    /// {
    ///   "jsonrpc": "2.0",
    ///   "method": "notifications/commandStatus", 
    ///   "params": {
    ///     "commandId": "cmd-123",
    ///     "command": "!analyze -v",
    ///     "status": "executing", 
    ///     "progress": 45,
    ///     "message": "Analyzing crash dump modules...",
    ///     "timestamp": "2025-09-25T18:30:15.123Z"
    ///   }
    /// }
    /// </summary>
    public class McpCommandStatusNotification
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the command ID.
        /// </summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command text.
        /// </summary>
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the progress percentage.
        /// </summary>
        [JsonPropertyName("progress")]
        public int? Progress { get; set; }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the notification timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets or sets the command result.
        /// </summary>
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Command heartbeat notification parameters
    /// 
    /// Sent via notifications/commandHeartbeat for long-running commands (>30 seconds).
    /// Shows the command is still active and provides elapsed time information.
    /// 
    /// Example notification:
    /// {
    ///   "jsonrpc": "2.0",
    ///   "method": "notifications/commandHeartbeat",
    ///   "params": {
    ///     "commandId": "cmd-123",
    ///     "command": "!analyze -v", 
    ///     "elapsedSeconds": 125.3,
    ///     "elapsedDisplay": "2m 5s",
    ///     "details": "Still analyzing heap corruption...",
    ///     "timestamp": "2025-09-25T18:32:15.123Z"
    ///   }
    /// }
    /// </summary>
    public class McpCommandHeartbeatNotification
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the command ID.
        /// </summary>
        [JsonPropertyName("commandId")]
        public string CommandId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command text.
        /// </summary>
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the elapsed time in seconds.
        /// </summary>
        [JsonPropertyName("elapsedSeconds")]
        public double ElapsedSeconds { get; set; }

        /// <summary>
        /// Gets or sets the human-readable elapsed time display.
        /// </summary>
        [JsonPropertyName("elapsedDisplay")]
        public string ElapsedDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the additional details.
        /// </summary>
        [JsonPropertyName("details")]
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the notification timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    }

    /// <summary>
    /// Session recovery notification parameters
    /// 
    /// Sent via notifications/sessionRecovery when the debugging session needs recovery.
    /// Indicates automatic recovery attempts and their success/failure status.
    /// 
    /// Example notification:
    /// {
    ///   "jsonrpc": "2.0",
    ///   "method": "notifications/sessionRecovery",
    ///   "params": {
    ///     "reason": "Command timeout: !analyze -v",
    ///     "recoveryStep": "Restarting debugger session",
    ///     "success": true,
    ///     "message": "Session successfully recovered after timeout",
    ///     "affectedCommands": ["cmd-123", "cmd-124"],
    ///     "timestamp": "2025-09-25T18:35:15.123Z"
    ///   }
    /// }
    /// </summary>
    public class McpSessionRecoveryNotification
    {
        /// <summary>
        /// Gets or sets the recovery reason.
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recovery step description.
        /// </summary>
        [JsonPropertyName("recoveryStep")]
        public string RecoveryStep { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the recovery was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the recovery message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the notification timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets or sets the affected command IDs.
        /// </summary>
        [JsonPropertyName("affectedCommands")]
        public string[]? AffectedCommands { get; set; }
    }

    /// <summary>
    /// Server health notification parameters
    /// 
    /// Sent via notifications/serverHealth to report overall server status and resource usage.
    /// Provides insights into debugging session health, command queue status, and server uptime.
    /// 
    /// Example notification:
    /// {
    ///   "jsonrpc": "2.0", 
    ///   "method": "notifications/serverHealth",
    ///   "params": {
    ///     "status": "healthy",
    ///     "cdbSessionActive": true,
    ///     "queueSize": 2,
    ///     "activeCommands": 1,
    ///     "uptime": "00:45:32.123",
    ///     "timestamp": "2025-09-25T18:40:15.123Z"
    ///   }
    /// }
    /// </summary>
    public class McpServerHealthNotification
    {
        /// <summary>
        /// Gets or sets the server status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the CDB session is active.
        /// </summary>
        [JsonPropertyName("cdbSessionActive")]
        public bool CdbSessionActive { get; set; }

        /// <summary>
        /// Gets or sets the command queue size.
        /// </summary>
        [JsonPropertyName("queueSize")]
        public int QueueSize { get; set; }

        /// <summary>
        /// Gets or sets the number of active commands.
        /// </summary>
        [JsonPropertyName("activeCommands")]
        public int ActiveCommands { get; set; }

        /// <summary>
        /// Gets or sets the notification timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets or sets the server uptime.
        /// </summary>
        [JsonPropertyName("uptime")]
        public TimeSpan? Uptime { get; set; }
    }

    /// <summary>
    /// Session event notification parameters
    /// 
    /// Sent via notifications/sessionEvent when important session lifecycle events occur.
    /// AI clients should monitor these to track session state and handle session expiry.
    /// 
    /// Example notification:
    /// {
    ///   "jsonrpc": "2.0",
    ///   "method": "notifications/sessionEvent",
    ///   "params": {
    ///     "sessionId": "sess-000001-abc12345",
    ///     "eventType": "SESSION_CREATED",
    ///     "message": "Session created for mydump.dmp",
    ///     "context": {
    ///       "sessionId": "sess-000001-abc12345",
    ///       "description": "Debugging session for mydump.dmp",
    ///       "createdAt": "2025-09-25T18:30:00.000Z",
    ///       "status": "Active",
    ///       "commandsProcessed": 0,
    ///       "activeCommands": 0
    ///     },
    ///     "timestamp": "2025-09-25T18:30:00.123Z"
    ///   }
    /// }
    /// </summary>
    public class McpSessionEventNotification
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session context.
        /// </summary>
        [JsonPropertyName("context")]
        public SessionContext? Context { get; set; }

        /// <summary>
        /// Gets or sets the notification timestamp.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    }
}






