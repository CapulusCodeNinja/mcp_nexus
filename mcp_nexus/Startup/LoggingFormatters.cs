using System.Text.Json;
using Microsoft.Extensions.Logging;
using mcp_nexus.Models;

namespace mcp_nexus.Startup
{
    /// <summary>
    /// Provides formatting utilities for logging JSON and SSE responses.
    /// </summary>
    public static class LoggingFormatters
    {
        /// <summary>
        /// Determines if JSON-RPC debug logging should be enabled based on logging levels.
        /// </summary>
        /// <param name="loggerFactory">The logger factory to check.</param>
        /// <returns>True if debug logging should be enabled, false otherwise.</returns>
        public static bool ShouldEnableJsonRpcLogging(ILoggerFactory loggerFactory)
        {
            // Check if debug logging is enabled for the application
            var debugLogger = loggerFactory.CreateLogger("MCP.JsonRpc");
            return debugLogger.IsEnabled(LogLevel.Debug);
        }

        /// <summary>
        /// Formats Server-Sent Events (SSE) response for better human readability in logs.
        /// </summary>
        /// <param name="sseResponse">The SSE response to format.</param>
        /// <returns>A formatted version of the SSE response.</returns>
        public static string FormatSseResponseForLogging(string sseResponse)
        {
            try
            {
                var lines = sseResponse.Split('\n');
                var formattedLines = new List<string>();

                foreach (var line in lines)
                {
                    if (line.StartsWith("event: "))
                    {
                        formattedLines.Add($"event: {line[7..]}");
                    }
                    else if (line.StartsWith("data: "))
                    {
                        var jsonData = line[6..];
                        var formattedJson = FormatJsonForLogging(jsonData);
                        formattedLines.Add($"data: {formattedJson}");
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        formattedLines.Add(line);
                    }
                }

                return string.Join("\n", formattedLines);
            }
            catch
            {
                // If formatting fails, return as-is
                return sseResponse;
            }
        }

        /// <summary>
        /// Formats JSON for better readability in logs.
        /// </summary>
        /// <param name="json">The JSON string to format.</param>
        /// <returns>A formatted version of the JSON string.</returns>
        public static string FormatJsonForLogging(string json)
        {
            try
            {
                // Try to parse and pretty-print the JSON
                using var document = JsonDocument.Parse(json);
                var options = new JsonSerializerOptions { WriteIndented = true };
                return System.Text.Json.JsonSerializer.Serialize(document.RootElement, options);
            }
            catch (JsonException ex)
            {
                // Log the parsing error for debugging and return sanitized version
                var sanitizedJson = json.Length > 1000 ? json[..1000] + "..." : json;
                return $"[Invalid JSON - {ex.Message}]: {sanitizedJson}";
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                return $"[JSON formatting error - {ex.Message}]: {json[..Math.Min(json.Length, 100)]}...";
            }
        }
    }
}

