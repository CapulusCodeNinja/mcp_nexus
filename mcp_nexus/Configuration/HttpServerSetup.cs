using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using AspNetCoreRateLimit;
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;
using mcp_nexus.Constants;

namespace mcp_nexus.Configuration
{
    /// <summary>
    /// Handles HTTP server configuration including services and options
    /// </summary>
    public static class HttpServerSetup
    {
        /// <summary>
        /// Configures services for HTTP mode.
        /// Sets up server limits, CORS, rate limiting, JSON options, and MCP server configuration.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        public static void ConfigureHttpServices(IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Configuring MCP server for HTTP...");

            ConfigureServerLimits(services);
            ConfigureCors(services);
            ConfigureRateLimit(services, configuration);
            ConfigureJsonOptions(services);
            ConfigureMcpServer(services);

            Console.WriteLine("MCP server configured for HTTP with official SDK (HTTP transport)");
        }

        /// <summary>
        /// <summary>
        /// Configures HTTP request and response limits for security.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private static void ConfigureServerLimits(IServiceCollection services)
        {
            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB limit for crash dumps
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.RequestHeadersTimeout = ApplicationConstants.HttpRequestTimeout;
                options.Limits.KeepAliveTimeout = ApplicationConstants.HttpKeepAliveTimeout;
                options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB limit for crash dumps
                options.Limits.MaxRequestLineSize = 8192; // 8KB limit for request line
                options.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB limit for headers
            });
        }

        /// <summary>
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
        /// <summary>
        /// Configures JSON serialization options for security and consistency.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private static void ConfigureJsonOptions(IServiceCollection services)
        {
            services.Configure<JsonOptions>(options =>
            {
                // Set UTF-8 encoding for JSON responses
                options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                options.SerializerOptions.PropertyNamingPolicy = null; // MCP requires exact field names
                options.SerializerOptions.AllowTrailingCommas = false; // Strict JSON parsing
                options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Disallow; // No comments
                options.SerializerOptions.MaxDepth = 64; // Prevent deeply nested attacks
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never; // Don't ignore properties
                options.SerializerOptions.PropertyNameCaseInsensitive = false; // Case sensitive
            });
        }

        /// <summary>
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
}
