using mcp_nexus.Models;

namespace mcp_nexus.Notifications
{
    /// <summary>
    /// Interface for bridging notification service to stdio MCP server transport
    /// </summary>
    public interface IStdioNotificationBridge
    {
        /// <summary>
        /// Initialize the bridge and connect to MCP server and notification service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Cleanup resources and disconnect handlers
        /// </summary>
        void Dispose();
    }
}

