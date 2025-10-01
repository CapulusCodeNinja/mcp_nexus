using System.Text.Json;
using mcp_nexus.Configuration;

namespace mcp_nexus.Middleware
{
    /// <summary>
    /// Middleware that validates Content-Type header for JSON-RPC requests
    /// </summary>
    public class ContentTypeValidationMiddleware
    {
        private readonly RequestDelegate m_next;
        private readonly ILogger<ContentTypeValidationMiddleware> m_logger;

        public ContentTypeValidationMiddleware(RequestDelegate next, ILogger<ContentTypeValidationMiddleware> logger)
        {
            m_next = next;
            m_logger = logger;
        }

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

            await m_next(context);
        }

        private static bool ShouldValidateContentType(HttpContext context)
        {
            return context.Request.Path == "/" && context.Request.Method == "POST";
        }

        private static bool IsValidContentType(string? contentType)
        {
            return !string.IsNullOrEmpty(contentType) && contentType.Contains("application/json");
        }

        private async Task HandleInvalidContentTypeAsync(HttpContext context)
        {
            m_logger.LogWarning("Invalid Content-Type received: {ContentType}", context.Request.ContentType);

            context.Response.StatusCode = 400;
            context.Response.ContentType = EncodingConfiguration.JsonContentType;

            var errorResponse = new
            {
                jsonrpc = "2.0",
                id = (object?)null,
                error = new
                {
                    code = -32700,
                    message = "Parse error",
                    data = "Content-Type must be application/json"
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}
