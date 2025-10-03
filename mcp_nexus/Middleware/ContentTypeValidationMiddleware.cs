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

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypeValidationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance for recording validation operations.</param>
        public ContentTypeValidationMiddleware(RequestDelegate next, ILogger<ContentTypeValidationMiddleware> logger)
        {
            m_next = next;
            m_logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to validate Content-Type headers for JSON-RPC requests.
        /// </summary>
        /// <param name="context">The HTTP context containing the request and response.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Determines whether the content type should be validated for the given context.
        /// </summary>
        /// <param name="context">The HTTP context of the request.</param>
        /// <returns><c>true</c> if content type validation should be performed; otherwise, <c>false</c>.</returns>
        private static bool ShouldValidateContentType(HttpContext context)
        {
            return context.Request.Path == "/" && context.Request.Method == "POST";
        }

        /// <summary>
        /// Validates whether the content type is acceptable.
        /// </summary>
        /// <param name="contentType">The content type to validate.</param>
        /// <returns><c>true</c> if the content type is valid; otherwise, <c>false</c>.</returns>
        private static bool IsValidContentType(string? contentType)
        {
            return !string.IsNullOrEmpty(contentType) && contentType.Contains("application/json");
        }

        /// <summary>
        /// Handles requests with invalid content types by returning an appropriate error response.
        /// </summary>
        /// <param name="context">The HTTP context containing the request and response.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
