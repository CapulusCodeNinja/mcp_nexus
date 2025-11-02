using System.Text.Json;
using System.Text.Json.Serialization;

using AspNetCoreRateLimit;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Nexus.Config;

namespace Nexus.Protocol.Configuration;

/// <summary>
/// Static helper class for configuring HTTP server services for MCP.
/// Provides methods to set up server limits, CORS, rate limiting, and MCP protocol.
/// </summary>
internal static class HttpServerSetup
{
    /// <summary>
    /// Configures all HTTP services required for MCP server operation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="serverConfig">Optional server configuration. Uses defaults if not provided.</param>
    public static void ConfigureHttpServices(
        IServiceCollection services,
        HttpServerConfiguration? serverConfig = null)
    {
        serverConfig ??= new HttpServerConfiguration();
        serverConfig.Validate();

        ConfigureServerLimits(services, serverConfig);

        if (serverConfig.EnableCors)
        {
            ConfigureCors(services);
        }

        if (serverConfig.EnableRateLimit)
        {
            ConfigureRateLimit(services);
        }

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
        _ = services.Configure<KestrelServerOptions>(options =>
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
        _ = services.AddCors(options => options.AddDefaultPolicy(builder => builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()));
    }

    /// <summary>
    /// Configures rate limiting to prevent abuse.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureRateLimit(IServiceCollection services)
    {
        _ = services.AddMemoryCache();
        _ = services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        _ = services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        _ = services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        _ = services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    }

    /// <summary>
    /// Configures JSON serialization options for MCP protocol.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureJsonOptions(IServiceCollection services)
    {
        _ = services.Configure<JsonOptions>(options =>
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
        _ = services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly();
    }

    /// <summary>
    /// Configures the HTTP request pipeline for MCP server operation.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    public static void ConfigureMcpPipeline(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Add middleware in correct order
        _ = app.UseMiddleware<Middleware.JsonRpcLoggingMiddleware>();
        _ = app.UseMiddleware<Middleware.ContentTypeValidationMiddleware>();
        _ = app.UseMiddleware<Middleware.ResponseFormattingMiddleware>();
        _ = app.UseCors();
        _ = app.UseRouting();

        // CRITICAL: Map MCP endpoints
        _ = app.MapMcp();
    }

    /// <summary>
    /// Configures all services required for stdio mode operation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureStdioServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Use official SDK for stdio mode
        _ = services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly();
    }

    /// <summary>
    /// Creates and configures a WebApplication for HTTP mode with all required settings.
    /// </summary>
    /// <param name="settings">The product settings.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    /// <returns>A fully configured WebApplication ready to start.</returns>
    public static WebApplication CreateConfiguredWebApplication(
        ISettings settings,
        bool isServiceMode)
    {
        // Read host and port from configuration
        var host = settings.Get().McpNexus.Server.Host ?? "localhost";
        var port = settings.Get().McpNexus.Server.Port;

        var url = $"http://{host}:{port}";

        // Create WebApplication builder
        var webBuilder = WebApplication.CreateBuilder();

        // Configure the URLs
        _ = webBuilder.WebHost.UseUrls(url);

        // Configure logging
        settings.ConfigureLogging(webBuilder.Logging, isServiceMode);

        // Configure services
        ConfigureHttpServices(webBuilder.Services);

        // Build the application
        var app = webBuilder.Build();

        // Configure the HTTP pipeline
        ConfigureMcpPipeline(app);

        return app;
    }

    /// <summary>
    /// Creates and configures a Host for stdio mode with all required settings.
    /// </summary>
    /// <param name="settings">The product settings.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    /// <returns>A fully configured Host ready to start.</returns>
    public static IHost CreateConfiguredHost(
        ISettings settings,
        bool isServiceMode)
    {
        // Create Host builder for stdio mode
        var hostBuilder = Host.CreateApplicationBuilder();

        // Configure logging
        settings.ConfigureLogging(hostBuilder.Logging, isServiceMode);

        // Configure stdio services
        ConfigureStdioServices(hostBuilder.Services);

        // Build the host
        return hostBuilder.Build();
    }
}
