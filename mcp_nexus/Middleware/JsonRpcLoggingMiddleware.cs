using System.Text.Json;

namespace mcp_nexus.Middleware
{
    /// <summary>
    /// Middleware that logs JSON-RPC requests and responses for debugging purposes
    /// </summary>
    public class JsonRpcLoggingMiddleware
    {
        private readonly RequestDelegate m_next;
        private readonly ILogger<JsonRpcLoggingMiddleware> m_logger;

        public JsonRpcLoggingMiddleware(RequestDelegate next, ILogger<JsonRpcLoggingMiddleware> logger)
        {
            m_next = next;
            m_logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ShouldLogRequest(context))
            {
                await LogRequestAndResponseAsync(context);
            }
            else
            {
                await m_next(context);
            }
        }

        private static bool ShouldLogRequest(HttpContext context)
        {
            return context.Request.Path == "/" && context.Request.Method == "POST";
        }

        private async Task LogRequestAndResponseAsync(HttpContext context)
        {
            // Log the request
            var requestBody = await ReadAndLogRequestAsync(context);

            // Capture the response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await m_next(context);

            // Log the response
            await ReadAndLogResponseAsync(context, responseBody);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }

        private async Task<string> ReadAndLogRequestAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            string requestBody;
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            context.Request.Body.Position = 0;

            var formattedRequest = FormatJsonForLogging(requestBody);
            m_logger.LogInformation("📨 JSON-RPC Request:\n{RequestBody}", formattedRequest);

            return requestBody;
        }

        private async Task ReadAndLogResponseAsync(HttpContext context, MemoryStream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);

            string responseBodyText;
            using (var reader = new StreamReader(responseBody, leaveOpen: true))
            {
                responseBodyText = await reader.ReadToEndAsync();
            }
            responseBody.Seek(0, SeekOrigin.Begin);

            var formattedResponse = FormatSseResponseForLogging(responseBodyText);
            m_logger.LogInformation("📤 JSON-RPC Response:\n{ResponseBody}", formattedResponse);
        }

        private static string FormatJsonForLogging(string json)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                document.WriteTo(writer);
                writer.Flush();
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException ex)
            {
                var sanitizedJson = json.Length > 1000 ? json.Substring(0, 1000) + "..." : json;
                return $"[Invalid JSON - {ex.Message}]: {sanitizedJson}";
            }
            catch (Exception ex)
            {
                return $"[JSON formatting error - {ex.Message}]: {json.Substring(0, Math.Min(json.Length, 100))}...";
            }
        }

        private static string FormatSseResponseForLogging(string sseResponse)
        {
            try
            {
                // Handle Server-Sent Events format - extract only the JSON data part
                var lines = sseResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("data: "))
                    {
                        var jsonPart = line.Substring(6).Trim();
                        return FormatJsonForLogging(jsonPart);
                    }
                }

                // If no data: line found, try to format as JSON directly
                return FormatJsonForLogging(sseResponse);
            }
            catch
            {
                return sseResponse;
            }
        }
    }
}
