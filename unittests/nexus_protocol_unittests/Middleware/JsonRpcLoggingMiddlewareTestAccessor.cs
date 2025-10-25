using System.Text.Json;

using Microsoft.AspNetCore.Http;

using Nexus.Protocol.Middleware;

namespace Nexus.Protocol.Unittests.Middleware;

/// <summary>
/// Test accessor class for JsonRpcLoggingMiddleware to access protected methods.
/// </summary>
internal class JsonRpcLoggingMiddlewareTestAccessor : JsonRpcLoggingMiddleware
{
    /// <summary>
    /// Initializes a new instance of the JsonRpcLoggingMiddlewareTestAccessor class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public JsonRpcLoggingMiddlewareTestAccessor(RequestDelegate next) : base(next)
    {
    }

    /// <summary>
    /// Test accessor for IsSseFormat method.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if the content appears to be SSE format; otherwise, false.</returns>
    public static new bool IsSseFormat(string content)
    {
        return JsonRpcLoggingMiddleware.IsSseFormat(content);
    }

    /// <summary>
    /// Test accessor for FormatSseContent method.
    /// </summary>
    /// <param name="sseContent">The SSE content to format.</param>
    /// <returns>The formatted SSE content with formatted JSON data.</returns>
    public static new string FormatSseContent(string sseContent)
    {
        return JsonRpcLoggingMiddleware.FormatSseContent(sseContent);
    }

    /// <summary>
    /// Test accessor for ExtractJsonFromSseLine method.
    /// </summary>
    /// <param name="line">The SSE data line.</param>
    /// <returns>The JSON content, or empty string if not found.</returns>
    public static new string ExtractJsonFromSseLine(string line)
    {
        return JsonRpcLoggingMiddleware.ExtractJsonFromSseLine(line);
    }

    /// <summary>
    /// Test accessor for FormatAndTruncateJson method.
    /// </summary>
    /// <param name="jsonString">The JSON string to format and truncate.</param>
    /// <returns>The formatted and truncated JSON string.</returns>
    public static new string FormatAndTruncateJson(string jsonString)
    {
        return JsonRpcLoggingMiddleware.FormatAndTruncateJson(jsonString);
    }

    /// <summary>
    /// Test accessor for ExtractTextFields method.
    /// </summary>
    /// <param name="jsonString">The JSON string to extract text fields from.</param>
    /// <returns>A list of extracted text content.</returns>
    public static new List<string> ExtractTextFields(string jsonString)
    {
        return JsonRpcLoggingMiddleware.ExtractTextFields(jsonString);
    }

    /// <summary>
    /// Test accessor for ExtractTextFieldsFromSse method.
    /// </summary>
    /// <param name="sseContent">The SSE content to process.</param>
    /// <returns>A list of extracted text content.</returns>
    public static new List<string> ExtractTextFieldsFromSse(string sseContent)
    {
        return JsonRpcLoggingMiddleware.ExtractTextFieldsFromSse(sseContent);
    }

    /// <summary>
    /// Test accessor for ExtractTextFieldsRecursive method.
    /// </summary>
    /// <param name="element">The JSON element to process.</param>
    /// <param name="extractedTexts">The list to add extracted texts to.</param>
    public static new void ExtractTextFieldsRecursive(JsonElement element, List<string> extractedTexts)
    {
        JsonRpcLoggingMiddleware.ExtractTextFieldsRecursive(element, extractedTexts);
    }

    /// <summary>
    /// Test accessor for UnescapeJsonInText method.
    /// </summary>
    /// <param name="text">The text that may contain escaped JSON.</param>
    /// <returns>The unescaped text.</returns>
    public static new string UnescapeJsonInText(string text)
    {
        return JsonRpcLoggingMiddleware.UnescapeJsonInText(text);
    }

    /// <summary>
    /// Test accessor for TruncateJsonElement method.
    /// </summary>
    /// <param name="element">The JSON element to process.</param>
    /// <param name="maxLength">The maximum length for string values.</param>
    /// <returns>A new JSON element with truncated strings.</returns>
    public static new object? TruncateJsonElement(JsonElement element, int maxLength)
    {
        return JsonRpcLoggingMiddleware.TruncateJsonElement(element, maxLength);
    }

    /// <summary>
    /// Test accessor for TruncateString method.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The truncated string with indicator.</returns>
    public static new string TruncateString(string value, int maxLength)
    {
        return JsonRpcLoggingMiddleware.TruncateString(value, maxLength);
    }
}
