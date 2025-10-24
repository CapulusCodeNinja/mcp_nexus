using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using nexus.protocol.Models;

using NLog;

namespace nexus.protocol.Middleware;

/// <summary>
/// Middleware that ensures proper JSON-RPC response formatting.
/// Handles exceptions and converts them to standardized JSON-RPC error responses.
/// </summary>
internal class ResponseFormattingMiddleware
{
    private readonly RequestDelegate m_Next;
    private readonly Logger m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseFormattingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ResponseFormattingMiddleware(RequestDelegate next)
    {
        m_Next = next ?? throw new ArgumentNullException(nameof(next));
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    /// <summary>
    /// Invokes the middleware to format responses and handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await m_Next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exceptions by converting them to JSON-RPC error responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        m_Logger.Error(exception, "Unhandled exception in JSON-RPC pipeline: {Message}", exception.Message);

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = 500;

        var errorResponse = new McpResponse
        {
            JsonRpc = "2.0",
            Id = null,
            Error = new McpError
            {
                Code = -32603,
                Message = "Internal error",
                Data = exception.Message
            }
        };

        var json = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
    }
}

