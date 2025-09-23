using System.ComponentModel;
using ModelContextProtocol.Server;

namespace mcp_nexus.Tools
{
    [McpServerToolType]
    public class TimeTool(ILogger<TimeTool> logger)
    {
        [McpServerTool, Description("Gets the current time for a city")]
        public string GetCurrentTime(string city)
        {
            logger.LogInformation("GetCurrentTime called with city: {City}", city);

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(city))
                {
                    logger.LogError("City parameter is null or empty");
                    return "City cannot be null or empty";
                }

                var currentTime = DateTime.Now;
                var timeString = $"It is {currentTime.Hour}:{currentTime.Minute} in {city}.";

                logger.LogInformation("Successfully generated time for city: {City} -> {TimeString}", city, timeString);
                logger.LogDebug("Current time details - Hour: {Hour}, Minute: {Minute}, Full DateTime: {DateTime}",
                    currentTime.Hour, currentTime.Minute, currentTime);

                return timeString;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting current time for city: {City}", city);
                return $"Error getting current time: {ex.Message}";
            }
        }
    }
}
