using ModelContextProtocol.Server;
using System.ComponentModel;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class WindbgTool
    {
        [McpServerTool, Description("Gets the current time for a city")]
        public static string GetCurrentTime(string city) =>
            $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
    }
}
