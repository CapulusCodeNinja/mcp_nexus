using mcp_nexus.Configuration;

namespace mcp_nexus.Middleware
{
    /// <summary>
    /// Middleware that ensures all HTTP responses use UTF-8 encoding
    /// Sets the charset parameter in Content-Type headers
    /// </summary>
    public class Utf8ResponseMiddleware
    {
        private readonly RequestDelegate m_next;

        public Utf8ResponseMiddleware(RequestDelegate next)
        {
            m_next = next;
        }

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

