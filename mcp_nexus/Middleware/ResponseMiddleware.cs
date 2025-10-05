using mcp_nexus.Configuration;

namespace mcp_nexus.Middleware
{
    /// <summary>
    /// Middleware that ensures all HTTP responses use proper encoding.
    /// Sets the charset parameter in Content-Type headers for proper text encoding.
    /// </summary>
    public class ResponseMiddleware
    {
        private readonly RequestDelegate m_next;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public ResponseMiddleware(RequestDelegate next)
        {
            m_next = next;
        }

        /// <summary>
        /// Invokes the middleware to process the HTTP request and ensure UTF-8 encoding.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // Intercept response to add UTF-8 charset if not present
            context.Response.OnStarting(() =>
            {
                var contentType = context.Response.ContentType;

                if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("charset", StringComparison.OrdinalIgnoreCase))
                {
                    // Add UTF-8 charset to Content-Type
                    if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.ContentType = EncodingConfiguration.JsonContentType;
                    }
                    else if (contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.ContentType = EncodingConfiguration.TextContentType;
                    }
                    else if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.ContentType = EncodingConfiguration.HtmlContentType;
                    }
                    else if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Any other text type gets UTF-8
                        context.Response.ContentType = $"{contentType}; {EncodingConfiguration.HttpCharset}";
                    }
                }

                return Task.CompletedTask;
            });

            await m_next(context);
        }
    }
}

