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
            // Check if this is a help request first
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help"))
            {
                await ShowHelpAsync();
                return;
            }

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

            if (commandLineArgs.Update)
            {
                if (OperatingSystem.IsWindows())
                {
                    // Create a logger using NLog configuration for the update process
                    using var loggerFactory = LoggerFactory.Create(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddNLogWeb();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                    var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");
                    
                    var success = await WindowsServiceInstaller.UpdateServiceAsync(logger);
                    Environment.Exit(success ? 0 : 1);
                }
                else
                {
                    Console.Error.WriteLine("ERROR: Service update is only supported on Windows.");
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

        private static async Task ShowHelpAsync()
        {
            Console.WriteLine("MCP Nexus - Comprehensive MCP Server Platform");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  mcp_nexus [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("DESCRIPTION:");
            Console.WriteLine("  MCP Nexus is a Model Context Protocol (MCP) server that provides various tools");
            Console.WriteLine("  and utilities for development and debugging. It supports both stdio and HTTP transports.");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  --http                 Use HTTP transport instead of stdio");
            Console.WriteLine("  --port <PORT>          HTTP server port (default: 5117 dev, 5000 production)");
            Console.WriteLine("  --service              Run in Windows service mode (implies --http)");
            Console.WriteLine("  --cdb-path <PATH>      Custom path to CDB.exe debugger executable");
            Console.WriteLine();
            Console.WriteLine("SERVICE MANAGEMENT (Windows only):");
            Console.WriteLine("  --install              Install MCP Nexus as Windows service");
            Console.WriteLine("  --uninstall            Uninstall MCP Nexus Windows service");
            Console.WriteLine("  --update               Update MCP Nexus service (stop, update files, restart)");
            Console.WriteLine("  --force-uninstall      Force uninstall MCP Nexus service (removes registry entries)");
            Console.WriteLine();
            Console.WriteLine("EXAMPLES:");
            Console.WriteLine("  mcp_nexus                          # Run in stdio mode");
            Console.WriteLine("  mcp_nexus --http                   # Run HTTP server on default port");
            Console.WriteLine("  mcp_nexus --http --port 8080       # Run HTTP server on port 8080");
            Console.WriteLine("  mcp_nexus --install                # Install as Windows service");
            Console.WriteLine("  mcp_nexus --install --port 9000    # Install service on port 9000");
            Console.WriteLine("  mcp_nexus --update                 # Update installed service");
            Console.WriteLine("  mcp_nexus --cdb-path \"C:\\WinDbg\"   # Use custom debugger path");
            Console.WriteLine();
            Console.WriteLine("NOTES:");
            Console.WriteLine("  - Service commands require administrator privileges on Windows");
            Console.WriteLine("  - Updates create backups in: C:\\Program Files\\MCP-Nexus\\backups\\[timestamp]");
            Console.WriteLine("  - HTTP mode runs on localhost:5000/mcp (or custom port if specified)");
            Console.WriteLine();
            Console.WriteLine("For more information, visit: https://github.com/your-repo/mcp_nexus");
            await Task.CompletedTask;
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
            var updateOption = new Option<bool>("--update", "Update MCP Nexus service (stop, update files, restart)");
            var portOption = new Option<int?>("--port", "HTTP server port (default: 5117 dev, 5000 production)");
            
            var rootCommand = new RootCommand("MCP Nexus - Comprehensive MCP Server Platform") 
            { 
                cdbPathOption, 
                httpOption,
                serviceOption,
                installOption,
                uninstallOption,
                forceUninstallOption,
                updateOption,
                portOption
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
                result.Update = parseResult.GetValueForOption(updateOption);
                result.Port = parseResult.GetValueForOption(portOption);
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
            public bool Update { get; set; }
            public int? Port { get; set; }
        }

        private static async Task RunHttpServer(string[] args, CommandLineArguments commandLineArgs)
        {
            var logMessage = commandLineArgs.ServiceMode ? 
                "Configuring for Windows service mode (HTTP)..." : 
                "Configuring for HTTP transport...";
            Console.WriteLine(logMessage);

            var webBuilder = WebApplication.CreateBuilder(args);

            // Configure custom port if specified
            if (commandLineArgs.Port.HasValue)
            {
                var customUrl = $"http://localhost:{commandLineArgs.Port.Value}";
                webBuilder.WebHost.UseUrls(customUrl);
                Console.WriteLine($"Using custom port: {commandLineArgs.Port.Value}");
            }

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