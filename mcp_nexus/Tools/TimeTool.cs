using System.ComponentModel;
using ModelContextProtocol.Server;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public static class TimeTool
    {
        [McpServerTool, Description("Gets the current time for a city")]
        public static string GetCurrentTime(string city) =>
            $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
    }
}
