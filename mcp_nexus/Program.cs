using NLog.Web;
using mcp_nexus.Tools;
using mcp_nexus.Helper;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;

namespace mcp_nexus
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // Parse command line arguments
            var (customCdbPath, useHttp) = ParseCommandLineArguments(args);

            if (useHttp)
            {
                await RunHttpServer(args, customCdbPath);
            }
            else
            {
                await RunStdioServer(args, customCdbPath);
            }
        }

        private static (string? customCdbPath, bool useHttp) ParseCommandLineArguments(string[] args)
        {
            string? customCdbPath = null;
            bool useHttp = false;

            var cdbPathOption = new Option<string?>("--cdb-path", "Custom path to CDB.exe debugger executable");
            var httpOption = new Option<bool>("--http", "Use HTTP transport instead of stdio");
            var rootCommand = new RootCommand("MCP Nexus - Windows Debugging MCP Server") 
            { 
                cdbPathOption, 
                httpOption 
            };

            var parseResult = rootCommand.Parse(args);
            if (parseResult.Errors.Count == 0)
            {
                customCdbPath = parseResult.GetValueForOption(cdbPathOption);
                useHttp = parseResult.GetValueForOption(httpOption);
            }

            return (customCdbPath, useHttp);
        }

        private static async Task RunHttpServer(string[] args, string? customCdbPath)
        {
            Console.WriteLine("Configuring for HTTP transport...");
            var webBuilder = WebApplication.CreateBuilder(args);

            ConfigureLogging(webBuilder.Logging);
            RegisterServices(webBuilder.Services, customCdbPath);
            ConfigureHttpServices(webBuilder.Services);

            var app = webBuilder.Build();
            ConfigureHttpPipeline(app);

            Console.WriteLine("Starting MCP Nexus HTTP server on {0}...", 
                string.Join(", ", app.Urls.DefaultIfEmpty("default URLs")));
            
            await app.RunAsync();
        }

        private static async Task RunStdioServer(string[] args, string? customCdbPath)
        {
            // CRITICAL: In stdio mode, stdout is reserved for MCP protocol
            // All console output must go to stderr
            Console.Error.WriteLine("Configuring for stdio transport...");
            var builder = Host.CreateApplicationBuilder(args);

            ConfigureLogging(builder.Logging);
            RegisterServices(builder.Services, customCdbPath);
            ConfigureStdioServices(builder.Services);

            Console.Error.WriteLine("Building application host...");
            var host = builder.Build();

            Console.Error.WriteLine("Starting MCP Nexus stdio server...");
            await host.RunAsync();
        }

        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            // Note: We use Console.Error for stdio mode compatibility
            Console.Error.WriteLine("Configuring logging...");
            logging.ClearProviders();
            logging.AddNLogWeb();
            Console.Error.WriteLine("Logging configured with NLog");
        }

        private static void RegisterServices(IServiceCollection services, string? customCdbPath)
        {
            Console.Error.WriteLine("Registering services...");

            services.AddSingleton<TimeTool>();
            Console.Error.WriteLine("Registered TimeTool as singleton");

            services.AddSingleton<CdbSession>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSession>>();
                return new CdbSession(logger, customCdbPath: customCdbPath);
            });
            Console.Error.WriteLine("Registered CdbSession as singleton with custom CDB path: {0}", 
                customCdbPath ?? "auto-detect");

            services.AddSingleton<WindbgTool>();
            Console.Error.WriteLine("Registered WindbgTool as singleton");
        }

        private static void ConfigureHttpServices(IServiceCollection services)
        {
            Console.WriteLine("Configuring MCP server for HTTP...");

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.WriteIndented = true;
                });
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Register MCP services
            services.AddSingleton<mcp_nexus.Services.McpToolDefinitionService>();
            services.AddSingleton<mcp_nexus.Services.McpToolExecutionService>();
            services.AddSingleton<mcp_nexus.Services.McpProtocolService>();

            Console.WriteLine("MCP server configured for HTTP with controllers, CORS, and services");
        }

        private static void ConfigureStdioServices(IServiceCollection services)
        {
            Console.Error.WriteLine("Configuring MCP server for stdio...");

            // Add the MCP protocol service for logging comparison
            services.AddSingleton<mcp_nexus.Services.McpProtocolService>();

            services.AddMcpServer()
                    .WithStdioServerTransport()
                    .WithToolsFromAssembly();

            Console.Error.WriteLine("MCP server configured with stdio transport and tools from assembly");
        }

        private static void ConfigureHttpPipeline(WebApplication app)
        {
            Console.WriteLine("Configuring HTTP request pipeline...");

            app.UseCors();
            app.UseRouting();
            app.MapControllers();

            Console.WriteLine("HTTP request pipeline configured");
        }
    }
}