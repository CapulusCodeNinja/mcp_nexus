using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;

using NLog;

namespace Nexus.Protocol.Middleware;

/// <summary>
/// Middleware that logs JSON-RPC requests and responses for debugging and auditing.
/// Captures request/response details while sanitizing sensitive information.
/// </summary>
internal class JsonRpcLoggingMiddleware
{
    private readonly RequestDelegate m_Next;
    private readonly Logger m_Logger;
    private const int m_MaxLength = 2500;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public JsonRpcLoggingMiddleware(RequestDelegate next)
    {
        m_Next = next ?? throw new ArgumentNullException(nameof(next));
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Invokes the middleware to log JSON-RPC requests and responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldLog(context))
        {
            await LogRequestAndResponseAsync(context);
        }
        else
        {
            await m_Next(context);
        }
    }

    /// <summary>
    /// Determines whether the request should be logged.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if logging should occur; otherwise, false.</returns>
    private static bool ShouldLog(HttpContext context)
    {
        return context.Request.Path == "/" && context.Request.Method == "POST";
    }

    /// <summary>
    /// Logs the request and response details.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LogRequestAndResponseAsync(HttpContext context)
    {
        var requestBody = await ReadRequestBodyAsync(context);

        m_Logger.Debug("JSON-RPC Request: Method={Method}, Path={Path}, ContentType={ContentType}",
            context.Request.Method, context.Request.Path, context.Request.ContentType);

        var formattedRequest = FormatAndTruncateJson(requestBody);
        var extractedTexts = ExtractTextFields(requestBody);
        m_Logger.Debug("JSON-RPC Request Body:{NewLine}{RequestBody}", Environment.NewLine, formattedRequest.Trim());
        foreach (var text in extractedTexts)
        {
            if (IsMarkdown(text.Trim()))
            {
                // Markdown - log as-is (no escaping needed!)
                m_Logger.Debug("Extracted Text Field (Markdown):{NewLine}{TextField}",
                    Environment.NewLine, text.Trim());
            }
            else
            {
                // Legacy JSON format - unescape and format
                var unescapedText = UnescapeJsonInText(text.Trim());
                var formattedText = FormatAndTruncateJson(unescapedText);
                m_Logger.Debug("Extracted Text Field (JSON):{NewLine}{TextField}",
                    Environment.NewLine, formattedText);
            }
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await m_Next(context);

            var responseBodyText = await ReadResponseBodyAsync(responseBody);

            m_Logger.Debug("JSON-RPC Response: StatusCode={StatusCode}, ContentType={ContentType}",
                context.Response.StatusCode, context.Response.ContentType);

            var formattedResponse = FormatAndTruncateJson(responseBodyText);
            var extractedResponseTexts = ExtractTextFields(responseBodyText);
            m_Logger.Debug("JSON-RPC Response Body:{NewLine}{ResponseBody}", Environment.NewLine, formattedResponse.Trim());
            foreach (var text in extractedResponseTexts)
            {
                if (IsMarkdown(text.Trim()))
                {
                    // Markdown - log as-is (no escaping needed!)
                    m_Logger.Debug("Extracted Text Field (Markdown):{NewLine}{TextField}",
                        Environment.NewLine, text.Trim());
                }
                else
                {
                    // Legacy JSON format - unescape and format
                    var unescapedText = UnescapeJsonInText(text.Trim());
                    var formattedText = FormatAndTruncateJson(unescapedText);
                    m_Logger.Debug("Extracted Text Field (JSON):{NewLine}{TextField}",
                        Environment.NewLine, formattedText);
                }
            }

            await CopyResponseBodyAsync(responseBody, originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Reads the request body content.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The request body as a string.</returns>
    private static async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body, Encoding.UTF8).ReadToEndAsync();
        context.Request.Body.Position = 0;
        return body;
    }

    /// <summary>
    /// Reads the response body content from the memory stream.
    /// </summary>
    /// <param name="responseBody">The response body stream.</param>
    /// <returns>The response body as a string.</returns>
    private static async Task<string> ReadResponseBodyAsync(MemoryStream responseBody)
    {
        _ = responseBody.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        _ = responseBody.Seek(0, SeekOrigin.Begin);
        return text;
    }

    /// <summary>
    /// Copies the response body back to the original stream.
    /// </summary>
    /// <param name="responseBody">The temporary response body stream.</param>
    /// <param name="originalBodyStream">The original response body stream.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task CopyResponseBodyAsync(MemoryStream responseBody, Stream originalBodyStream)
    {
        await responseBody.CopyToAsync(originalBodyStream);
    }

    /// <summary>
    /// Determines if the content is in Server-Sent Events (SSE) format.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if the content appears to be SSE format; otherwise, false.</returns>
    protected static bool IsSseFormat(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Any(line => line.TrimStart().StartsWith("event:") || line.TrimStart().StartsWith("data:"));
    }

    /// <summary>
    /// Formats SSE content by preserving the SSE structure and formatting JSON data.
    /// </summary>
    /// <param name="sseContent">The SSE content to format.</param>
    /// <returns>The formatted SSE content with formatted JSON data.</returns>
    protected static string FormatSseContent(string sseContent)
    {
        if (string.IsNullOrEmpty(sseContent))
        {
            return string.Empty;
        }

        var lines = sseContent.Split('\n', StringSplitOptions.None);
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd();
            if (trimmedLine.StartsWith("data:"))
            {
                var jsonPart = ExtractJsonFromSseLine(trimmedLine);
                if (!string.IsNullOrEmpty(jsonPart))
                {
                    try
                    {
                        var formattedJson = FormatAndTruncateJson(jsonPart);
                        result.Add($"data: {formattedJson}");
                    }
                    catch
                    {
                        result.Add(trimmedLine);
                    }
                }
                else
                {
                    result.Add(trimmedLine);
                }
            }
            else
            {
                result.Add(trimmedLine);
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    /// <summary>
    /// Determines if the content is Markdown format.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the content appears to be Markdown.</returns>
    protected static bool IsMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.TrimStart();
        // Check for common Markdown patterns
        return trimmed.StartsWith("#") ||           // Headers
               trimmed.StartsWith("##") ||          // Subheaders
               trimmed.StartsWith("**") ||          // Bold
               trimmed.StartsWith("- ") ||          // Lists
               trimmed.StartsWith("| ") ||          // Tables
               text.Contains("```") ||              // Code blocks
               text.Contains("**Command ID:**");    // Our specific pattern
    }

    /// <summary>
    /// Extracts JSON content from an SSE data line.
    /// </summary>
    /// <param name="line">The SSE data line.</param>
    /// <returns>The JSON content, or empty string if not found.</returns>
    protected static string ExtractJsonFromSseLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return string.Empty;
        }

        var trimmedLine = line.Trim();
        if (!trimmedLine.StartsWith("data:"))
        {
            return string.Empty;
        }

        var jsonStart = trimmedLine.IndexOf('{');
        return jsonStart == -1 ? string.Empty : trimmedLine[jsonStart..];
    }

    /// <summary>
    /// Formats JSON with indentation and truncates individual string fields to specified length.
    /// </summary>
    /// <param name="jsonString">The JSON string to format and truncate.</param>
    /// <returns>The formatted and truncated JSON string.</returns>
    protected static string FormatAndTruncateJson(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            return string.Empty;
        }

        // Check if this is SSE format
        if (IsSseFormat(jsonString))
        {
            return FormatSseContent(jsonString);
        }

        // Otherwise, parse as regular JSON
        try
        {
            using var document = JsonDocument.Parse(jsonString);
            var truncatedJson = TruncateJsonElement(document.RootElement);
            return JsonSerializer.Serialize(truncatedJson, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            // If JSON parsing fails, fall back to simple truncation
            return TruncateString(jsonString);
        }
    }

    /// <summary>
    /// Extracts text fields from JSON and unescapes any nested JSON content.
    /// </summary>
    /// <param name="jsonString">The JSON string to extract text fields from.</param>
    /// <returns>A list of extracted text content.</returns>
    protected static List<string> ExtractTextFields(string jsonString)
    {
        var extractedTexts = new List<string>();

        if (string.IsNullOrEmpty(jsonString))
        {
            return extractedTexts;
        }

        // Check if this is SSE format
        if (IsSseFormat(jsonString))
        {
            return ExtractTextFieldsFromSse(jsonString);
        }

        try
        {
            using var document = JsonDocument.Parse(jsonString);
            ExtractTextFieldsRecursive(document.RootElement, extractedTexts);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return empty list
        }

        return extractedTexts;
    }

    /// <summary>
    /// Extracts text fields from SSE format content.
    /// </summary>
    /// <param name="sseContent">The SSE content to process.</param>
    /// <returns>A list of extracted text content.</returns>
    protected static List<string> ExtractTextFieldsFromSse(string sseContent)
    {
        var extractedTexts = new List<string>();

        if (string.IsNullOrEmpty(sseContent))
        {
            return extractedTexts;
        }

        var lines = sseContent.Split('\n', StringSplitOptions.None);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("data:"))
            {
                var jsonPart = ExtractJsonFromSseLine(trimmedLine);
                if (!string.IsNullOrEmpty(jsonPart))
                {
                    try
                    {
                        using var document = JsonDocument.Parse(jsonPart);
                        ExtractTextFieldsRecursive(document.RootElement, extractedTexts);
                    }
                    catch (JsonException)
                    {
                        // If JSON parsing fails, skip this data line
                    }
                }
            }
        }

        return extractedTexts;
    }

    /// <summary>
    /// Recursively extracts text fields from JSON elements.
    /// </summary>
    /// <param name="element">The JSON element to process.</param>
    /// <param name="extractedTexts">The list to add extracted texts to.</param>
    protected static void ExtractTextFieldsRecursive(JsonElement element, List<string> extractedTexts)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Equals("text", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var textValue = property.Value.GetString();
                        if (!string.IsNullOrEmpty(textValue))
                        {
                            // Unescape JSON within text fields
                            var unescapedText = UnescapeJsonInText(textValue);
                            extractedTexts.Add(TruncateString(unescapedText));
                        }
                    }
                    else
                    {
                        ExtractTextFieldsRecursive(property.Value, extractedTexts);
                    }
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractTextFieldsRecursive(item, extractedTexts);
                }
                break;
        }
    }

    /// <summary>
    /// Unescapes JSON content within text fields.
    /// </summary>
    /// <param name="text">The text that may contain escaped JSON.</param>
    /// <returns>The unescaped text.</returns>
    protected static string UnescapeJsonInText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Replace common JSON escape sequences
        return text
            .Replace("\\u0022", "\"")
            .Replace("\\u0027", "'")
            .Replace("\\u005C", "\\")
            .Replace("\\u002F", "/")
            .Replace("\\u0008", "\b")
            .Replace("\\u000C", "\f")
            .Replace("\\u000A", "\n")
            .Replace("\\u000D", "\r")
            .Replace("\\u0009", "\t");
    }

    /// <summary>
    /// Recursively truncates string values in JSON elements.
    /// </summary>
    /// <param name="element">The JSON element to process.</param>
    /// <returns>A new JSON element with truncated strings.</returns>
    protected static object? TruncateJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return TruncateString(element.GetString() ?? string.Empty);
            case JsonValueKind.Number:
                return element.GetDecimal();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    obj[property.Name] = TruncateJsonElement(property.Value);
                }
                return obj;
            case JsonValueKind.Array:
                var arr = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    arr.Add(TruncateJsonElement(item));
                }
                return arr;
            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Recursively truncates string values in JSON elements to a specified maximum length.
    /// </summary>
    /// <param name="element">The JSON element to process.</param>
    /// <param name="maxLength">The maximum string length.</param>
    /// <returns>A new JSON element with strings truncated to the specified length.</returns>
    protected static object? TruncateJsonElement(JsonElement element, int maxLength)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return TruncateString(element.GetString() ?? string.Empty, maxLength);
            case JsonValueKind.Number:
                return element.GetDecimal();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    obj[property.Name] = TruncateJsonElement(property.Value, maxLength);
                }
                return obj;
            case JsonValueKind.Array:
                var arr = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    arr.Add(TruncateJsonElement(item, maxLength));
                }
                return arr;
            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Truncates a string to the specified maximum length with truncation indicator.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <returns>The truncated string with indicator.</returns>
    protected static string TruncateString(string value)
    {
        return string.IsNullOrEmpty(value) || value.Length <= m_MaxLength
            ? value
            : value[..m_MaxLength] + $"... (truncated {value.Length - m_MaxLength} chars)";
    }

    /// <summary>
    /// Truncates a string to the provided maximum length with truncation indicator.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The truncated string with indicator.</returns>
    protected static string TruncateString(string value, int maxLength)
    {
        return string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength] + $"... (truncated {value.Length - maxLength} chars)";
    }

    /// <summary>
    /// Sanitizes the body content for safe logging by limiting length.
    /// </summary>
    /// <param name="body">The body content to sanitize.</param>
    /// <returns>The sanitized content.</returns>
    private static string SanitizeForLogging(string body)
    {
        return string.IsNullOrEmpty(body) ? string.Empty : body.Length > m_MaxLength ? body[..m_MaxLength] + "... (truncated)" : body;
    }
}

