using System.Text.Json;

using Microsoft.AspNetCore.Http;

using NLog;

namespace Nexus.Protocol.Middleware;

/// <summary>
/// Middleware that validates the Content-Type header for JSON-RPC requests.
/// Ensures all POST requests to the MCP endpoint have application/json content type.
/// </summary>
internal class ContentTypeValidationMiddleware
{
    private readonly RequestDelegate m_Next;
    private readonly Logger m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentTypeValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ContentTypeValidationMiddleware(RequestDelegate next)
    {
        m_Next = next ?? throw new ArgumentNullException(nameof(next));
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Invokes the middleware to validate Content-Type headers.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldValidateContentType(context))
        {
            if (!IsValidContentType(context.Request.ContentType))
            {
                await HandleInvalidContentTypeAsync(context);
                return;
            }
        }

        await m_Next(context);
    }

    /// <summary>
    /// Determines whether content type validation should be performed.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if validation should occur; otherwise, false.</returns>
    private static bool ShouldValidateContentType(HttpContext context)
    {
        return context.Request.Path == "/" && context.Request.Method == "POST";
    }

    /// <summary>
    /// Validates whether the content type is acceptable for JSON-RPC.
    /// </summary>
    /// <param name="contentType">The content type to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool IsValidContentType(string? contentType)
    {
        return !string.IsNullOrEmpty(contentType) && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Handles requests with invalid content types by returning a JSON-RPC error.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleInvalidContentTypeAsync(HttpContext context)
    {
        m_Logger.Warn("Invalid Content-Type received: {ContentType}", context.Request.ContentType);

        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json; charset=utf-8";

        var errorResponse = new
        {
            jsonrpc = "2.0",
            id = (object?)null,
            error = new
            {
                code = -32700,
                message = "Parse error",
                data = "Content-Type must be application/json",
            },
        };

        var json = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
    }
}
