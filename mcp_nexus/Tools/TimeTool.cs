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
            m_Logger.LogInformation("GetCurrentTime called with city: {City}", city);
            
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(city))
                {
                    m_Logger.LogError("City parameter is null or empty");
                    return "City cannot be null or empty";
                }

                var currentTime = DateTime.Now;
                var timeString = $"It is {currentTime.Hour}:{currentTime.Minute} in {city}.";
                
                m_Logger.LogInformation("Successfully generated time for city: {City} -> {TimeString}", city, timeString);
                m_Logger.LogDebug("Current time details - Hour: {Hour}, Minute: {Minute}, Full DateTime: {DateTime}", 
                    currentTime.Hour, currentTime.Minute, currentTime);
                
                return timeString;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting current time for city: {City}", city);
                return $"Error getting current time: {ex.Message}";
            }
        }
    }
}
