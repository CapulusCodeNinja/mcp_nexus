using System.Text.Json;
using System.Text.Json.Serialization;
using mcp_nexus.Session.Models;

namespace mcp_nexus.Models
{
    /// <summary>
    /// Helper class to get version information
    /// </summary>
    internal static class VersionHelper
    {
        internal static string GetFileVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fileVersionInfo.FileVersion ?? "1.0.0.0";
        }
    }
    public class McpRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class McpResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("result")]
        public object? Result { get; set; }
        [JsonPropertyName("error")]
        public McpError? Error { get; set; }
    }

    public class McpSuccessResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("result")]
        public object? Result { get; set; }
    }

    public class McpErrorResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("error")]
        public McpError Error { get; set; } = new();
    }

    public class McpError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    public class McpToolSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("inputSchema")]
        public object InputSchema { get; set; } = new { };
    }

    public class McpToolResult
    {
        [JsonPropertyName("content")]
        public McpContent[] Content { get; set; } = Array.Empty<McpContent>();
    }

    public class McpContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class McpInitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2025-06-18";
        [JsonPropertyName("capabilities")]
        public McpCapabilities Capabilities { get; set; } = new();
        [JsonPropertyName("serverInfo")]
        public McpServerDetails ServerInfo { get; set; } = new();
    }

    public class McpToolsListResult
    {
        [JsonPropertyName("tools")]
        public McpToolSchema[] Tools { get; set; } = Array.Empty<McpToolSchema>();
    }

    public class McpServerInfoResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("result")]
        public McpServerInfoResult Result { get; set; } = new();
    }

    public class McpServerInfoResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2025-06-18";
        [JsonPropertyName("capabilities")]
        public McpCapabilities Capabilities { get; set; } = new();
        [JsonPropertyName("serverInfo")]
        public McpServerDetails ServerInfo { get; set; } = new();
    }

    public class McpCapabilities
    {
        [JsonPropertyName("tools")]
        public object Tools { get; set; } = new { listChanged = true };
        [JsonPropertyName("notifications")]
        public object Notifications { get; set; } = new
        {
            // Standard MCP notifications
            tools = new { listChanged = true },
            // Custom MCP Nexus notifications  
            commandStatus = true,
            sessionRecovery = true,
            serverHealth = true
        };
    }

    public class McpServerDetails
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "mcp-nexus";
        [JsonPropertyName("version")]
        public string Version { get; set; } = VersionHelper.GetFileVersion();
        [JsonPropertyName("description")]
        public string Description { get; set; } = "Windows Debugging Tools MCP Server with real-time command notifications. " +
            "Provides asynchronous debugging commands with live status updates via server-initiated notifications. " +
            "Supports notifications/commandStatus (execution progress), notifications/commandHeartbeat (long-running command updates), " +
            "notifications/sessionRecovery (debugging session recovery), notifications/serverHealth (server status), " +
            "and standard MCP notifications/tools/list_changed. All commands execute asynchronously - use nexus_dump_analyze_session_async_command_status() " +
            "to get results and monitor for real-time notifications about execution progress.";
    }

    // ===== SERVER NOTIFICATION MODELS =====

    /// <summary>
    /// Server-initiated notification (no ID, no response expected)
    /// </summary>
    public class McpNotification
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

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
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("commandId")]
        public string CommandId { get; set; } = string.Empty;

        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("progress")]
        public int? Progress { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("result")]
        public string? Result { get; set; }

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
        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("commandId")]
        public string CommandId { get; set; } = string.Empty;

        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("elapsedSeconds")]
        public double ElapsedSeconds { get; set; }

        [JsonPropertyName("elapsedDisplay")]
        public string ElapsedDisplay { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
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
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("recoveryStep")]
        public string RecoveryStep { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

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
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("cdbSessionActive")]
        public bool CdbSessionActive { get; set; }

        [JsonPropertyName("queueSize")]
        public int QueueSize { get; set; }

        [JsonPropertyName("activeCommands")]
        public int ActiveCommands { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

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
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public SessionContext? Context { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}






