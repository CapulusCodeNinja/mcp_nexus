using NLog.Web;
using mcp_nexus.Tools;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace mcp_nexus
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // Parse command line arguments
            var commandLineArgs = ParseCommandLineArguments(args);

            // Handle special commands first (Windows only)
            if (commandLineArgs.Install)
            {
                if (OperatingSystem.IsWindows())
                {
                    // Create a logger using NLog configuration for the installation process
                    using var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddNLogWeb();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                    var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");
                    
                    var success = await WindowsServiceInstaller.InstallServiceAsync(logger);
                    Environment.Exit(success ? 0 : 1);
                }
                else
                {
                    Console.Error.WriteLine("ERROR: Service installation is only supported on Windows.");
                    Environment.Exit(1);
                }
                return;
            }

            if (commandLineArgs.Uninstall)
            {
                if (OperatingSystem.IsWindows())
                {
                    // Create a logger using NLog configuration for the uninstallation process
                    using var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddNLogWeb();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                    var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");
                    
                    var success = await WindowsServiceInstaller.UninstallServiceAsync(logger);
                    Environment.Exit(success ? 0 : 1);
                }
                else
                {
                    Console.Error.WriteLine("ERROR: Service uninstallation is only supported on Windows.");
                    Environment.Exit(1);
                }
                return;
            }

            if (commandLineArgs.ForceUninstall)
            {
                if (OperatingSystem.IsWindows())
                {
                    // Create a logger using NLog configuration for the force uninstallation process
                    using var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddNLogWeb();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                    var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");
                    
                    var success = await WindowsServiceInstaller.ForceUninstallServiceAsync(logger);
                    Environment.Exit(success ? 0 : 1);
                }
                else
                {
                    Console.Error.WriteLine("ERROR: Service uninstallation is only supported on Windows.");
                    Environment.Exit(1);
                }
                return;
            }

            // Determine transport mode
            bool useHttp = commandLineArgs.UseHttp || commandLineArgs.ServiceMode;

            // Validate service mode is only used on Windows
            if (commandLineArgs.ServiceMode && !OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("ERROR: Service mode is only supported on Windows.");
                Environment.Exit(1);
                return;
            }

            if (useHttp)
            {
                await RunHttpServer(args, commandLineArgs);
            }
            else
            {
                await RunStdioServer(args, commandLineArgs);
            }
        }

        private static CommandLineArguments ParseCommandLineArguments(string[] args)
        {
            var result = new CommandLineArguments();

            var cdbPathOption = new Option<string?>("--cdb-path", "Custom path to CDB.exe debugger executable");
            var httpOption = new Option<bool>("--http", "Use HTTP transport instead of stdio");
            var serviceOption = new Option<bool>("--service", "Run in Windows service mode (implies --http)");
            var installOption = new Option<bool>("--install", "Install MCP Nexus as Windows service");
            var uninstallOption = new Option<bool>("--uninstall", "Uninstall MCP Nexus Windows service");
            var forceUninstallOption = new Option<bool>("--force-uninstall", "Force uninstall MCP Nexus service (removes registry entries)");
            
            var rootCommand = new RootCommand("MCP Nexus - Comprehensive MCP Server Platform") 
            { 
                cdbPathOption, 
                httpOption,
                serviceOption,
                installOption,
                uninstallOption,
                forceUninstallOption
            };

            var parseResult = rootCommand.Parse(args);
            if (parseResult.Errors.Count == 0)
            {
                result.CustomCdbPath = parseResult.GetValueForOption(cdbPathOption);
                result.UseHttp = parseResult.GetValueForOption(httpOption);
                result.ServiceMode = parseResult.GetValueForOption(serviceOption);
                result.Install = parseResult.GetValueForOption(installOption);
                result.Uninstall = parseResult.GetValueForOption(uninstallOption);
                result.ForceUninstall = parseResult.GetValueForOption(forceUninstallOption);
            }

            return result;
        }

        private class CommandLineArguments
        {
            public string? CustomCdbPath { get; set; }
            public bool UseHttp { get; set; }
            public bool ServiceMode { get; set; }
            public bool Install { get; set; }
            public bool Uninstall { get; set; }
            public bool ForceUninstall { get; set; }
        }

        private static async Task RunHttpServer(string[] args, CommandLineArguments commandLineArgs)
        {
            var logMessage = commandLineArgs.ServiceMode ? 
                "Configuring for Windows service mode (HTTP)..." : 
                "Configuring for HTTP transport...";
            Console.WriteLine(logMessage);

            var webBuilder = WebApplication.CreateBuilder(args);

            // Add Windows service support if in service mode
            if (commandLineArgs.ServiceMode && OperatingSystem.IsWindows())
            {
                webBuilder.Host.UseWindowsService();
            }

            ConfigureLogging(webBuilder.Logging, commandLineArgs.ServiceMode);
            RegisterServices(webBuilder.Services, commandLineArgs.CustomCdbPath);
            ConfigureHttpServices(webBuilder.Services);

            var app = webBuilder.Build();
            ConfigureHttpPipeline(app);

            var startMessage = commandLineArgs.ServiceMode ?
                "Starting MCP Nexus as Windows service..." :
                $"Starting MCP Nexus HTTP server on {string.Join(", ", app.Urls.DefaultIfEmpty("default URLs"))}...";
            Console.WriteLine(startMessage);
            
            await app.RunAsync();
        }

        private static async Task RunStdioServer(string[] args, CommandLineArguments commandLineArgs)
        {
            // CRITICAL: In stdio mode, stdout is reserved for MCP protocol
            // All console output must go to stderr
            Console.Error.WriteLine("Configuring for stdio transport...");
            var builder = Host.CreateApplicationBuilder(args);

            ConfigureLogging(builder.Logging, false);
            RegisterServices(builder.Services, commandLineArgs.CustomCdbPath);
            ConfigureStdioServices(builder.Services);

            Console.Error.WriteLine("Building application host...");
            var host = builder.Build();

            Console.Error.WriteLine("Starting MCP Nexus stdio server...");
            await host.RunAsync();
        }

        private static void ConfigureLogging(ILoggingBuilder logging, bool isServiceMode)
        {
            // Note: We use Console.Error for stdio mode compatibility
            var logMessage = "Configuring logging...";
            if (isServiceMode)
            {
                // For service mode, logging will go to Windows Event Log and files
                Console.WriteLine(logMessage);
            }
            else
            {
                Console.Error.WriteLine(logMessage);
            }
            
            logging.ClearProviders();
            logging.AddNLogWeb();
            
            var completeMessage = "Logging configured with NLog";
            if (isServiceMode)
            {
                Console.WriteLine(completeMessage);
            }
            else
            {
                Console.Error.WriteLine(completeMessage);
            }
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