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
    }

    public class McpServerDetails
    {
        public string Name { get; set; } = "mcp-nexus";
        public string Version { get; set; } = "1.0.0";
    }
}





