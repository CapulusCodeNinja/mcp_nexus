using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Protocol.Middleware;

/// <summary>
/// Middleware that logs JSON-RPC requests and responses for debugging and auditing.
/// Captures request/response details while sanitizing sensitive information.
/// </summary>
internal class JsonRpcLoggingMiddleware
{
    private readonly RequestDelegate m_Next;
    private readonly ILogger<JsonRpcLoggingMiddleware> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording request/response information.</param>
    public JsonRpcLoggingMiddleware(RequestDelegate next, ILogger<JsonRpcLoggingMiddleware> logger)
    {
        m_Next = next ?? throw new ArgumentNullException(nameof(next));
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        m_Logger.LogDebug("JSON-RPC Request: Method={Method}, Path={Path}, ContentType={ContentType}",
            context.Request.Method, context.Request.Path, context.Request.ContentType);
        m_Logger.LogTrace("JSON-RPC Request Body: {RequestBody}", SanitizeForLogging(requestBody));

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await m_Next(context);

            var responseBodyText = await ReadResponseBodyAsync(responseBody);

            m_Logger.LogDebug("JSON-RPC Response: StatusCode={StatusCode}, ContentType={ContentType}",
                context.Response.StatusCode, context.Response.ContentType);
            m_Logger.LogTrace("JSON-RPC Response Body: {ResponseBody}", SanitizeForLogging(responseBodyText));

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
        responseBody.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
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
    /// Sanitizes the body content for safe logging by limiting length.
    /// </summary>
    /// <param name="body">The body content to sanitize.</param>
    /// <returns>The sanitized content.</returns>
    private static string SanitizeForLogging(string body)
    {
        if (string.IsNullOrEmpty(body))
            return string.Empty;

        const int maxLength = 1000;
        if (body.Length > maxLength)
            return body[..maxLength] + "... (truncated)";

        return body;
    }
}

