namespace Nexus.Protocol.Configuration;

/// <summary>
/// Configuration options for the HTTP-based MCP server.
/// Defines limits, timeouts, and security settings.
/// </summary>
internal class HttpServerConfiguration
{
    /// <summary>
    /// Gets or sets the maximum request body size in bytes.
    /// Default is 50MB to accommodate large crash dump uploads.
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the request headers timeout in seconds.
    /// Default is 60 seconds.
    /// </summary>
    public int RequestHeadersTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the keep-alive timeout in seconds.
    /// Default is 120 seconds.
    /// </summary>
    public int KeepAliveTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the maximum request line size in bytes.
    /// Default is 8KB.
    /// </summary>
    public int MaxRequestLineSize { get; set; } = 8192;

    /// <summary>
    /// Gets or sets the maximum total size of request headers in bytes.
    /// Default is 32KB.
    /// </summary>
    public int MaxRequestHeadersTotalSize { get; set; } = 32768;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether CORS is enabled.
    /// Default is true for MCP clients.
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether rate limiting is enabled.
    /// Default is true to prevent abuse.
    /// </summary>
    public bool EnableRateLimit { get; set; } = true;

    /// <summary>
    /// Validates the configuration values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration values are invalid.</exception>
    public void Validate()
    {
        if (MaxRequestBodySize <= 0)
        {
            throw new ArgumentException("MaxRequestBodySize must be positive", nameof(MaxRequestBodySize));
        }

        if (RequestHeadersTimeoutSeconds <= 0)
        {
            throw new ArgumentException("RequestHeadersTimeoutSeconds must be positive", nameof(RequestHeadersTimeoutSeconds));
        }

        if (KeepAliveTimeoutSeconds <= 0)
        {
            throw new ArgumentException("KeepAliveTimeoutSeconds must be positive", nameof(KeepAliveTimeoutSeconds));
        }

        if (MaxRequestLineSize <= 0)
        {
            throw new ArgumentException("MaxRequestLineSize must be positive", nameof(MaxRequestLineSize));
        }

        if (MaxRequestHeadersTotalSize <= 0)
        {
            throw new ArgumentException("MaxRequestHeadersTotalSize must be positive", nameof(MaxRequestHeadersTotalSize));
        }
    }
}
