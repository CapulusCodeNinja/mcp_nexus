using WinAiDbg.Protocol.Models;

namespace WinAiDbg.Protocol.Services;

/// <summary>
/// Interface for managing Model Context Protocol (MCP) tool definitions.
/// Provides access to tool schemas and handles tool discovery.
/// </summary>
public interface IMcpToolDefinitionService
{
    /// <summary>
    /// Gets all available MCP tool schemas.
    /// </summary>
    /// <returns>An array of tool schemas.</returns>
    McpToolSchema[] GetAllTools();

    /// <summary>
    /// Gets a specific tool schema by name.
    /// </summary>
    /// <param name="toolName">The name of the tool to retrieve.</param>
    /// <returns>The tool schema, or null if not found.</returns>
    McpToolSchema? GetTool(string toolName);

    /// <summary>
    /// Notifies clients that the tools list has changed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyToolsChangedAsync();
}
