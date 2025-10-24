using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using nexus.config;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace nexus.protocol.Configuration;

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

    /// <summary>
    /// Configures the HTTP request pipeline for MCP server operation.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    public static void ConfigureMcpPipeline(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Add middleware
        app.UseMiddleware<Middleware.ContentTypeValidationMiddleware>();
        app.UseCors();
        app.UseRouting();

        // CRITICAL: Map MCP endpoints
        app.MapMcp();
    }

    /// <summary>
    /// Configures all services required for stdio mode operation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public static void ConfigureStdioServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Use official SDK for stdio mode
        services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly();
    }

    /// <summary>
    /// Creates and configures a WebApplication for HTTP mode with all required settings.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="settingLoader">The settings loader</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    /// <returns>A fully configured WebApplication ready to start.</returns>
    public static WebApplication CreateConfiguredWebApplication(
        IConfiguration configuration,
        ISettingsLoader settingLoader,
        bool isServiceMode)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(settingLoader);

        // Read host and port from configuration
        var host = configuration["McpNexus:Server:Host"] ?? "localhost";
        var portStr = configuration["McpNexus:Server:Port"] ?? "5000";
        if (!int.TryParse(portStr, out var port))
        {
            port = 5000;
        }

        var url = $"http://{host}:{port}";

        // Create WebApplication builder
        var webBuilder = WebApplication.CreateBuilder();

        // Configure the URLs
        webBuilder.WebHost.UseUrls(url);

        // Load configuration settings
        settingLoader.LoadConfiguration();

        // Configure logging
        settingLoader.ConfigureLogging(webBuilder.Logging, configuration, isServiceMode);

        // Configure services
        ConfigureHttpServices(webBuilder.Services, configuration);

        // Build the application
        var app = webBuilder.Build();

        // Configure the HTTP pipeline
        ConfigureMcpPipeline(app);

        return app;
    }

    /// <summary>
    /// Creates and configures a Host for stdio mode with all required settings.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="settingsLoader">The settings loader.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    /// <returns>A fully configured Host ready to start.</returns>
    public static IHost CreateConfiguredHost(
        IConfiguration configuration,
        ISettingsLoader settingsLoader,
        bool isServiceMode)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(settingsLoader);

        // Create Host builder for stdio mode
        var hostBuilder = Host.CreateApplicationBuilder();

        // Load configuration settings
        settingsLoader.LoadConfiguration();

        // Configure logging
        settingsLoader.ConfigureLogging(hostBuilder.Logging, configuration, isServiceMode);

        // Configure stdio services
        ConfigureStdioServices(hostBuilder.Services);

        // Build the host
        return hostBuilder.Build();
    }
}

