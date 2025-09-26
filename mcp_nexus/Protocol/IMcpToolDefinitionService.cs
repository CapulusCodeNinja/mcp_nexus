using mcp_nexus.Models;

namespace mcp_nexus.Protocol
{
    /// <summary>
    /// Interface for MCP tool definition service to enable better testability
    /// </summary>
    public interface IMcpToolDefinitionService
    {
        /// <summary>
        /// Get all available MCP tools
        /// </summary>
        /// <returns>Array of tool definitions</returns>
        McpToolSchema[] GetAllTools();
    }
}
