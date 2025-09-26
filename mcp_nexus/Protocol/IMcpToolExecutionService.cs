using System.Text.Json;

namespace mcp_nexus.Protocol
{
    /// <summary>
    /// Interface for MCP tool execution service to enable better testability
    /// </summary>
    public interface IMcpToolExecutionService
    {
        /// <summary>
        /// Execute a specific MCP tool
        /// </summary>
        /// <param name="toolName">Name of the tool to execute</param>
        /// <param name="arguments">Tool arguments</param>
        /// <returns>Tool execution result</returns>
        Task<object> ExecuteTool(string toolName, JsonElement arguments);
    }
}
