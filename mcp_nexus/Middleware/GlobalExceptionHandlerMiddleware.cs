using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace mcp_nexus.Middleware
{
    /// <summary>
    /// Middleware that handles unhandled exceptions and converts them to proper JSON-RPC error responses
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate m_next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> m_logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            m_next = next;
            m_logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await m_next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            m_logger.LogError(exception, "Unhandled exception occurred during request processing");

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var requestId = await ExtractRequestIdAsync(context);
            var errorResponse = CreateErrorResponse(exception, requestId, context);

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }

        private async Task<object?> ExtractRequestIdAsync(HttpContext context)
        {
            try
            {
                if (context.Request.ContentType?.Contains("application/json") == true)
                {
                    context.Request.EnableBuffering();
                    context.Request.Body.Position = 0;
                    
                    using var reader = new StreamReader(context.Request.Body);
                    var requestBody = await reader.ReadToEndAsync();
                    
                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        using var doc = JsonDocument.Parse(requestBody);
                        if (doc.RootElement.TryGetProperty("id", out var idProp))
                        {
                            return idProp.ValueKind switch
                            {
                                JsonValueKind.String => idProp.GetString(),
                                JsonValueKind.Number => idProp.GetInt32(),
                                _ => null
                            };
                        }
                    }
                }
            }
            catch
            {
                // If we can't extract the ID, continue with null
            }

            return null;
        }

        private static object CreateErrorResponse(Exception exception, object? requestId, HttpContext context)
        {
            var isDevelopment = context.RequestServices
                .GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment();

            return new
            {
                jsonrpc = "2.0",
                id = requestId,
                error = new
                {
                    code = GetErrorCode(exception),
                    message = GetErrorMessage(exception),
                    data = isDevelopment ? exception.Message : null
                }
            };
        }

        private static int GetErrorCode(Exception exception)
        {
            return exception switch
            {
                JsonException => -32700, // Parse error
                ArgumentException => -32602, // Invalid params
                NotSupportedException => -32601, // Method not found
                _ => -32603 // Internal error
            };
        }

        private static string GetErrorMessage(Exception exception)
        {
            return exception switch
            {
                JsonException => "Parse error",
                ArgumentException => "Invalid params",
                NotSupportedException => "Method not found",
                _ => "Internal error"
            };
        }
    }
}
