using System.Text.Json;
using System.Text.Json.Serialization;

namespace mcp_nexus.Models
{
    public class McpRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
        public JsonElement? Params { get; set; }
        public int Id { get; set; }
    }

    public class McpResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        public int Id { get; set; }
        public object? Result { get; set; }
        public McpError? Error { get; set; }
    }

    public class McpSuccessResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        public int Id { get; set; }
        public object? Result { get; set; }
    }

    public class McpErrorResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        public int Id { get; set; }
        public McpError Error { get; set; } = new();
    }

    public class McpError
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class McpToolSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object InputSchema { get; set; } = new { };
    }

    public class McpToolResult
    {
        public McpContent[] Content { get; set; } = Array.Empty<McpContent>();
    }

    public class McpContent
    {
        public string Type { get; set; } = "text";
        public string Text { get; set; } = string.Empty;
    }

    public class McpInitializeResult
    {
        public string ProtocolVersion { get; set; } = "2025-06-18";
        public object Capabilities { get; set; } = new { tools = new { listChanged = true } };
        public object ServerInfo { get; set; } = new { name = "mcp-nexus", version = "1.0.0" };
    }

    public class McpToolsListResult
    {
        public McpToolSchema[] Tools { get; set; } = Array.Empty<McpToolSchema>();
    }

    public class McpServerInfoResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        public McpServerInfoResult Result { get; set; } = new();
    }

    public class McpServerInfoResult
    {
        public string ProtocolVersion { get; set; } = "2025-06-18";
        public McpCapabilities Capabilities { get; set; } = new();
        public McpServerDetails ServerInfo { get; set; } = new();
    }

    public class McpCapabilities
    {
        public object Tools { get; set; } = new { listChanged = true };
        public object Notifications { get; set; } = new { 
            commandStatus = true, 
            sessionRecovery = true, 
            serverHealth = true 
        };
    }

    public class McpServerDetails
    {
        public string Name { get; set; } = "mcp-nexus";
        public string Version { get; set; } = "1.0.0";
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
    /// </summary>
    public class McpCommandStatusNotification
    {
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
    /// </summary>
    public class McpCommandHeartbeatNotification
    {
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
}





