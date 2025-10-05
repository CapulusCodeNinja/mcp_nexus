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

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRpcLoggingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance for recording JSON-RPC operations.</param>
        public JsonRpcLoggingMiddleware(RequestDelegate next, ILogger<JsonRpcLoggingMiddleware> logger)
        {
            m_next = next;
            m_logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to log JSON-RPC requests and responses.
        /// </summary>
        /// <param name="context">The HTTP context containing the request and response.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Determines whether a request should be logged based on its context.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        /// <returns><c>true</c> if the request should be logged; otherwise, <c>false</c>.</returns>
        private static bool ShouldLogRequest(HttpContext context)
        {
            return context.Request.Path == "/" && context.Request.Method == "POST";
        }

        /// <summary>
        /// Logs both the request and response for a JSON-RPC operation.
        /// </summary>
        /// <param name="context">The HTTP context containing the request and response.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Reads and logs the request body for a JSON-RPC operation.
        /// </summary>
        /// <param name="context">The HTTP context containing the request.</param>
        /// <returns>A <see cref="Task{string}"/> containing the request body content.</returns>
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
            m_logger.LogDebug("📨 JSON-RPC Request:\n{RequestBody}", formattedRequest);

            return requestBody;
        }

        /// <summary>
        /// Reads and logs the response body for a JSON-RPC operation.
        /// </summary>
        /// <param name="context">The HTTP context containing the response.</param>
        /// <param name="responseBody">The memory stream containing the response body.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

            // Try to decode the text for easier debugging
            // For Info level, don't truncate; for Debug and higher, truncate large fields
            var (decodedText, decodeSuccess) = DecodeJsonText(responseBodyText, shouldTruncate: false); // Info level - no truncation
            if (decodeSuccess)
            {
                // DecodeJsonText succeeded - use Info for main response, Debug for decoded text
                m_logger.LogTrace("📤 JSON-RPC Response:\n{ResponseBody}", formattedResponse);

                // For Debug level, truncate large fields
                var (truncatedDecodedText, _) = DecodeJsonText(responseBodyText, shouldTruncate: true);
                m_logger.LogDebug("📤 JSON-RPC Response Text:\n{DecodedText}", truncatedDecodedText);
            }
            else
            {
                // DecodeJsonText failed - use Info for main response only
                m_logger.LogDebug("📤 JSON-RPC Response:\n{ResponseBody}", formattedResponse);
            }
        }

        /// <summary>
        /// Formats JSON for better readability in logs.
        /// </summary>
        /// <param name="json">The JSON string to format.</param>
        /// <returns>A formatted JSON string.</returns>
        private static string FormatJsonForLogging(string json)
        {
            // Handle empty or whitespace-only responses
            if (string.IsNullOrWhiteSpace(json))
            {
                return "[Empty response body]";
            }

            try
            {
                using var document = JsonDocument.Parse(json);
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                document.WriteTo(writer);
                writer.Flush();
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException)
            {
                // Return the raw content if it's not valid JSON
                return json;
            }
            catch (Exception)
            {
                // Return the raw content if there's any other error
                return json;
            }
        }

        /// <summary>
        /// Formats Server-Sent Events response for better readability in logs.
        /// </summary>
        /// <param name="sseResponse">The SSE response string to format.</param>
        /// <returns>A formatted SSE response string.</returns>
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

        /// <summary>
        /// Truncates large fields in a JSON element to prevent log overflow.
        /// </summary>
        /// <param name="element">The JSON element to process.</param>
        /// <param name="maxFieldLength">The maximum length for field values.</param>
        /// <param name="shouldTruncate">Whether to actually truncate the fields.</param>
        /// <returns>A new JSON element with truncated fields.</returns>
        private static JsonElement TruncateLargeFields(JsonElement element, int maxFieldLength = 1000, bool shouldTruncate = true)
        {
            // If truncation is disabled, return the element as-is
            if (!shouldTruncate)
            {
                return element;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var truncatedObject = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString()?.Length > maxFieldLength)
                        {
                            var originalValue = property.Value.GetString() ?? "";
                            truncatedObject[property.Name] = originalValue.Substring(0, maxFieldLength) + $"...(truncated {originalValue.Length - maxFieldLength} chars)";
                        }
                        else
                        {
                            truncatedObject[property.Name] = TruncateLargeFields(property.Value, maxFieldLength, shouldTruncate);
                        }
                    }
                    return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(truncatedObject)).RootElement;

                case JsonValueKind.Array:
                    var truncatedArray = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        truncatedArray.Add(TruncateLargeFields(item, maxFieldLength, shouldTruncate));
                    }
                    return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(truncatedArray)).RootElement;

                case JsonValueKind.String:
                    var stringValue = element.GetString() ?? "";
                    if (stringValue.Length > maxFieldLength)
                    {
                        return JsonDocument.Parse($"\"{stringValue.Substring(0, maxFieldLength)}...(truncated {stringValue.Length - maxFieldLength} chars)\"").RootElement;
                    }
                    return element;

                default:
                    return element;
            }
        }

        /// <summary>
        /// Decodes and formats JSON text from a response, optionally truncating large fields.
        /// </summary>
        /// <param name="responseText">The response text to decode.</param>
        /// <param name="shouldTruncate">Whether to truncate large fields in the output.</param>
        /// <returns>A tuple containing the decoded result and a success indicator.</returns>
        private static (string result, bool success) DecodeJsonText(string responseText, bool shouldTruncate = true)
        {
            // Handle empty or whitespace-only responses
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return ("[Empty response body]", false);
            }

            try
            {
                // Handle Server-Sent Events format - extract only the JSON data part
                var lines = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                string jsonContent = responseText;

                foreach (var line in lines)
                {
                    if (line.StartsWith("data: "))
                    {
                        jsonContent = line.Substring(6).Trim();
                        break;
                    }
                }

                // Extract and decode the "text" field content
                using var document = JsonDocument.Parse(jsonContent);
                if (document.RootElement.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("content", out var content) &&
                    content.ValueKind == JsonValueKind.Array &&
                    content.GetArrayLength() > 0)
                {
                    var firstContent = content[0];
                    if (firstContent.TryGetProperty("text", out var textField))
                    {
                        // Decode the text field content
                        var encodedText = textField.GetString() ?? "";
                        string decodedText;
                        
                        // Unescape the string value of the "text" field, which is often a JSON string within a JSON string.
                        // Json.TextSerializer.Deserialize<string> is the most reliable way to unescape a JSON string.
                        try
                        {
                            // Try to unescape the string first. This will handle cases like "{\"key\":\"value\"}" -> {"key":"value"}
                            decodedText = System.Text.Json.JsonSerializer.Deserialize<string>(encodedText) ?? throw new InvalidOperationException("Failed to deserialize the string");
                        }
                        catch
                        {
                            // If it fails, it might mean the string was NOT double-encoded, or it was just general-escaped.
                            // Use the original or a generic unescape as a fallback.
                            decodedText = System.Text.RegularExpressions.Regex.Unescape(encodedText) ?? throw new InvalidOperationException("Failed to unescape the string");
                        }

                        // Now parse the potentially unescaped result
                        using var textDocument = JsonDocument.Parse(decodedText);
                        var truncatedJson = TruncateLargeFields(textDocument.RootElement, 1000, shouldTruncate);

                        using var stream = new MemoryStream();
                        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                        truncatedJson.WriteTo(writer);
                        writer.Flush();
                        return (System.Text.Encoding.UTF8.GetString(stream.ToArray()), true);
                    }
                }

                // If no "text" field found, format the entire JSON content directly
                return (FormatJsonForLogging(jsonContent), true);
            }
            catch
            {
                return (responseText, false);
            }
        }


    }
}
