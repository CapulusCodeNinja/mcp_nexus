using ModelContextProtocol.Server;
using System.ComponentModel;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class WindbgTool
    {
        private readonly ILogger<TimeTool> m_Logger;

        public WindbgTool(ILogger<TimeTool> logger)
        {
            m_Logger = logger;
        }
    }
}
