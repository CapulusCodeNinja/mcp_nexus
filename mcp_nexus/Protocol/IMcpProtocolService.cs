using System.Text.Json;

namespace mcp_nexus.Protocol
{
    /// <summary>
    /// Interface for MCP protocol service to enable better testability
    /// </summary>
    public interface IMcpProtocolService
    {
        /// <summary>
        /// Process an incoming MCP request
        /// </summary>
        /// <param name="requestElement">The JSON request element</param>
        /// <returns>Response object or null for notifications</returns>
        Task<object?> ProcessRequest(JsonElement requestElement);
    }
}
