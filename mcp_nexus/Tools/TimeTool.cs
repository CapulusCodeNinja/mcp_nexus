using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class TimeTool
    {
        private readonly ILogger<TimeTool> m_Logger;

        public TimeTool(ILogger<TimeTool> logger)
        {
            m_Logger = logger;
        }

        [McpServerTool, Description("Gets the current time for a city")]
        public string GetCurrentTime(string city)
        {
            m_Logger.LogInformation("LLM requested the time for city: {City}", city);

            return $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
        }
    }
}
