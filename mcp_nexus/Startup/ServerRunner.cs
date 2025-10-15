using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using NLog;
using mcp_nexus.Configuration;
using mcp_nexus.Constants;
using mcp_nexus.Middleware;
using mcp_nexus.Notifications;

namespace mcp_nexus.Startup
{
    /// <summary>
    /// Handles server startup and configuration for both HTTP and stdio modes.
    /// </summary>
    public static class ServerRunner
    {
        /// <summary>
        /// Runs the MCP server in HTTP mode for web-based integration.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <param name="commandLineArgs">The parsed command line arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// In HTTP mode, the server runs as a web application accessible via HTTP endpoints.
        /// This mode supports both regular HTTP operation and Windows service mode.
        /// The server configuration is determined by appsettings files and command line overrides.
        /// </remarks>
        public static async Task RunHttpServerAsync(string[] args, CommandLineArguments commandLineArgs)
        {
            var webBuilder = WebApplication.CreateBuilder(args);

            // Initialize file logger as early as possible using central configuration
            LoggingSetup.ConfigureLogging(webBuilder.Logging, commandLineArgs.ServiceMode, webBuilder.Configuration);

            var logMessage = commandLineArgs.ServiceMode ?
                "Configuring for Windows service mode (HTTP)..." :
                "Configuring for HTTP transport...";
            Console.WriteLine(logMessage);

            // Read configuration from appsettings files first, then apply command line overrides
            var configHost = webBuilder.Configuration["McpNexus:Server:Host"];
            var configPortStr = webBuilder.Configuration["McpNexus:Server:Port"];
            int.TryParse(configPortStr, out var configPort);

            // Apply configuration hierarchy: config file -> command line
            var host = commandLineArgs.Host ?? configHost ?? "localhost";
            var port = commandLineArgs.Port ?? (configPort > 0 ? configPort : (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? ApplicationConstants.DefaultDevPort : ApplicationConstants.DefaultHttpPort));

            var customUrl = $"http://{host}:{port}";
            webBuilder.WebHost.UseUrls(customUrl);

            // Show the actual configuration being used with source information
            var hostSource = commandLineArgs.HostFromCommandLine ? "command line" :
                            (!string.IsNullOrEmpty(configHost) ? "configuration file" : "default");
            var portSource = commandLineArgs.PortFromCommandLine ? "command line" :
                            (configPort > 0 ? "configuration file" : "default");

            Console.WriteLine(hostSource == portSource
                ? $"Using host: {host}, port: {port} (from {hostSource})"
                : $"Using host: {host} (from {hostSource}), port: {port} (from {portSource})");

            // Add Windows service support if in service mode
            if (commandLineArgs.ServiceMode && OperatingSystem.IsWindows())
            {
                webBuilder.Host.UseWindowsService();
            }

            // Log startup banner AFTER logging is configured
            StartupBanner.LogStartupBanner(commandLineArgs, host, port);

            // Log detailed configuration settings AFTER logging is configured
            ConfigurationLogger.LogConfigurationSettings(webBuilder.Configuration, commandLineArgs);
            ServiceRegistration.RegisterServices(webBuilder.Services, webBuilder.Configuration, commandLineArgs.CustomCdbPath, commandLineArgs.ServiceMode);
            HttpServerSetup.ConfigureHttpServices(webBuilder.Services, webBuilder.Configuration);

            var app = webBuilder.Build();
            ConfigureHttpPipeline(app);

            // Log the resolved CDB path after service registration
            var cdbOptions = app.Services.GetService<IOptions<mcp_nexus.Session.Core.Models.CdbSessionOptions>>()?.Value;
            var resolvedCdbPath = cdbOptions?.CustomCdbPath ?? "Auto-detection failed";
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("🔧 CDB Path resolved: {CdbPath}", resolvedCdbPath);

            var startMessage = commandLineArgs.ServiceMode ?
                "Starting MCP Nexus as Windows service..." :
                $"Starting MCP Nexus HTTP server on {string.Join(", ", app.Urls.DefaultIfEmpty("default URLs"))}...";
            Console.WriteLine(startMessage);

            await app.RunAsync();
        }

        /// <summary>
        /// Runs the MCP server in stdio mode for AI client integration.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <param name="commandLineArgs">The parsed command line arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// In stdio mode, the server communicates with AI clients through standard input/output streams.
        /// All console output is redirected to stderr to avoid interfering with the MCP protocol on stdout.
        /// </remarks>
        public static async Task RunStdioServerAsync(string[] args, CommandLineArguments commandLineArgs)
        {
            // Configure UTF-8 encoding for all console streams (stdin, stdout, stderr)
            EncodingConfiguration.ConfigureConsoleEncoding();

            // CRITICAL: In stdio mode, stdout is reserved for MCP protocol
            // All console output must go to stderr
            var stdioLogger = LogManager.GetCurrentClassLogger();
            stdioLogger.Info("Configuring for stdio transport...");
            var builder = Host.CreateApplicationBuilder(args);

            LoggingSetup.ConfigureLogging(builder.Logging, false, builder.Configuration);

            // Log startup banner for stdio mode
            StartupBanner.LogStartupBanner(commandLineArgs, "stdio", null);

            // Log detailed configuration settings for stdio mode
            ConfigurationLogger.LogConfigurationSettings(builder.Configuration, commandLineArgs);

            ServiceRegistration.RegisterServices(builder.Services, builder.Configuration, commandLineArgs.CustomCdbPath);
            ConfigureStdioServices(builder.Services);

            stdioLogger.Info("Building application host...");
            var host = builder.Build();

            // CRITICAL FIX: Initialize the notification bridge after host is built
            try
            {
                var notificationBridge = host.Services.GetRequiredService<IStdioNotificationBridge>();
                await notificationBridge.InitializeAsync();
                stdioLogger.Info("Notification bridge initialized for stdio MCP server");

                // Send standard MCP tools list changed notification on startup
                var notificationService = host.Services.GetRequiredService<IMcpNotificationService>();
                await notificationService.NotifyToolsListChangedAsync();
                stdioLogger.Info("Sent tools list changed notification to MCP clients");
            }
            catch (Exception ex)
            {
                stdioLogger.Warn(ex, "Failed to initialize notification bridge");
            }
            stdioLogger.Info("Starting MCP Nexus stdio server...");
            await host.RunAsync();
        }

        /// <summary>
        /// Configures services for stdio mode operation.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private static void ConfigureStdioServices(IServiceCollection services)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Configuring MCP server for stdio...");

            // Use official SDK for stdio mode
            services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();

            // CRITICAL FIX: Bridge notification service to stdio MCP server
            services.AddSingleton<IStdioNotificationBridge, StdioNotificationBridge>();

            logger.Info("MCP server configured with stdio transport, tools, and resources from assembly");
        }

        /// <summary>
        /// Configures the HTTP request pipeline for web mode operation.
        /// </summary>
        /// <param name="app">The web application to configure.</param>
        private static void ConfigureHttpPipeline(WebApplication app)
        {
            Console.WriteLine("Configuring HTTP request pipeline...");

            // UTF-8 encoding middleware removed - using standard Unicode encoding

            // Add security middleware (GlobalExceptionHandlerMiddleware removed - was causing crashes)
            app.UseMiddleware<ContentTypeValidationMiddleware>();

            // Add core middleware
            app.UseIpRateLimiting();
            app.UseCors();
            app.UseRouting();
            // Add logging middleware if debug logging is enabled
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            if (LoggingFormatters.ShouldEnableJsonRpcLogging(loggerFactory))
            {
                app.UseMiddleware<JsonRpcLoggingMiddleware>();
                Console.WriteLine("JSON-RPC debug logging middleware enabled");
            }
            else
            {
                Console.WriteLine("JSON-RPC debug logging middleware disabled (not in debug mode)");
            }

            // Map MVC controllers (e.g., extension-callback endpoints)
            app.MapControllers();

            // Use the official SDK's HTTP transport with MapMcp
            app.MapMcp();

            Console.WriteLine("HTTP request pipeline configured with official SDK");
        }
    }
}

