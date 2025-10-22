using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace mcp_nexus.Protocol.Configuration;

/// <summary>
/// Static helper class for configuring HTTP server services for MCP.
/// Provides methods to set up server limits, CORS, rate limiting, and MCP protocol.
/// </summary>
public static class HttpServerSetup
{
    /// <summary>
    /// Configures all HTTP services required for MCP server operation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="serverConfig">Optional server configuration. Uses defaults if not provided.</param>
    public static void ConfigureHttpServices(
        IServiceCollection services,
        IConfiguration configuration,
        HttpServerConfiguration? serverConfig = null)
    {
        serverConfig ??= new HttpServerConfiguration();
        serverConfig.Validate();

        ConfigureServerLimits(services, serverConfig);

        if (serverConfig.EnableCors)
            ConfigureCors(services);

        if (serverConfig.EnableRateLimit)
            ConfigureRateLimit(services, configuration);

        ConfigureJsonOptions(services);
        ConfigureMcpServer(services);
    }

    /// <summary>
    /// Configures HTTP server request and response limits.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The server configuration.</param>
    private static void ConfigureServerLimits(IServiceCollection services, HttpServerConfiguration config)
    {
        services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = config.MaxRequestBodySize;
        });

        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(config.RequestHeadersTimeoutSeconds);
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(config.KeepAliveTimeoutSeconds);
            options.Limits.MaxRequestBodySize = config.MaxRequestBodySize;
            options.Limits.MaxRequestLineSize = config.MaxRequestLineSize;
            options.Limits.MaxRequestHeadersTotalSize = config.MaxRequestHeadersTotalSize;
        });
    }

    /// <summary>
    /// Configures CORS policy for MCP clients.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
    }

    /// <summary>
    /// Configures rate limiting to prevent abuse.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    private static void ConfigureRateLimit(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    }

    /// <summary>
    /// Configures JSON serialization options for MCP protocol.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureJsonOptions(IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.SerializerOptions.PropertyNamingPolicy = null;
            options.SerializerOptions.AllowTrailingCommas = false;
            options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Disallow;
            options.SerializerOptions.MaxDepth = 64;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.SerializerOptions.PropertyNameCaseInsensitive = false;
        });
    }

    /// <summary>
    /// Configures the MCP server with HTTP transport.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureMcpServer(IServiceCollection services)
    {
        services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly();
    }
}

