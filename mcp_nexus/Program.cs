using System.CommandLine;
using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NLog.Web;
using AspNetCoreRateLimit;
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using mcp_nexus.Constants;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
// Protocol services removed - now using SDK
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Tools;

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
                    await Console.Error.WriteLineAsync("ERROR: Service installation is only supported on Windows.");
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
                    await Console.Error.WriteLineAsync("ERROR: Service uninstallation is only supported on Windows.");
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
                    await Console.Error.WriteLineAsync("ERROR: Service uninstallation is only supported on Windows.");
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
                    await Console.Error.WriteLineAsync("ERROR: Service update is only supported on Windows.");
                    Environment.Exit(1);
                }
                return;
            }

            // Determine transport mode
            bool useHttp = commandLineArgs.UseHttp || commandLineArgs.ServiceMode;

            // Validate service mode is only used on Windows
            if (commandLineArgs.ServiceMode && !OperatingSystem.IsWindows())
            {
                await Console.Error.WriteLineAsync("ERROR: Service mode is only supported on Windows.");
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
            var hostOption = new Option<string?>("--host", "HTTP server host binding (default: localhost, use 0.0.0.0 for all interfaces)");

            var rootCommand = new RootCommand("MCP Nexus - Comprehensive MCP Server Platform")
            {
                cdbPathOption,
                httpOption,
                serviceOption,
                installOption,
                uninstallOption,
                forceUninstallOption,
                updateOption,
                portOption,
                hostOption
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
                result.Host = parseResult.GetValueForOption(hostOption);

                // Track which values came from command line
                result.PortFromCommandLine = result.Port.HasValue;
                result.HostFromCommandLine = result.Host != null;
            }

            return result;
        }

        private static void LogStartupBanner(CommandLineArguments args, string host, int? port)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            const int bannerWidth = 69; // Total width including asterisks
            const int contentWidth = bannerWidth - 4; // Width for content (excluding "* " and " *")

            var banner = new System.Text.StringBuilder();
            banner.AppendLine("*********************************************************************");
            banner.AppendLine(FormatCenteredBannerLine("MCP NEXUS", contentWidth));
            banner.AppendLine(FormatCenteredBannerLine("Model Context Protocol Server", contentWidth));
            banner.AppendLine("*********************************************************************");
            banner.AppendLine(FormatBannerLine("Version:", version, contentWidth));
            banner.AppendLine(FormatBannerLine("Environment:", environment, contentWidth));
            banner.AppendLine(FormatBannerLine("Started:", timestamp, contentWidth));
            banner.AppendLine(FormatBannerLine("PID:", Environment.ProcessId.ToString(), contentWidth));

            if (host == "stdio")
            {
                banner.AppendLine(FormatBannerLine("Transport:", "STDIO Mode", contentWidth));
            }
            else
            {
                var transport = args.ServiceMode ? "HTTP (Service Mode)" : "HTTP (Interactive)";
                banner.AppendLine(FormatBannerLine("Transport:", transport, contentWidth));
                banner.AppendLine(FormatBannerLine("Host:", host, contentWidth));
                banner.AppendLine(FormatBannerLine("Port:", port?.ToString() ?? "Default", contentWidth));
            }

            // Show configuration sources
            var configSources = new List<string>();
            if (args.HostFromCommandLine || args.PortFromCommandLine)
                configSources.Add("Command Line");
            configSources.Add("Configuration File");
            banner.AppendLine(FormatBannerLine("Config:", string.Join(", ", configSources), contentWidth));

            // Show custom CDB path if specified
            if (!string.IsNullOrEmpty(args.CustomCdbPath))
            {
                var cdbPath = args.CustomCdbPath.Length > (contentWidth - 12) ?
                    ApplicationConstants.PathTruncationPrefix + args.CustomCdbPath.Substring(args.CustomCdbPath.Length - (contentWidth - 15)) :
                    args.CustomCdbPath;
                banner.AppendLine(FormatBannerLine("CDB Path:", cdbPath, contentWidth));
            }

            banner.AppendLine("*********************************************************************");

            // Log the banner to both console and log file
            var bannerText = banner.ToString();
            Console.WriteLine(bannerText);

            // Log a clean startup message instead of the messy formatted banner

            logger.Info("╔═══════════════════════════════════════════════════════════════════╗");
            logger.Info("                            MCP NEXUS STARTUP");
            logger.Info("");
            logger.Info($"  Version:     {version}");
            logger.Info($"  Environment: {environment}");
            logger.Info($"  Process ID:  {Environment.ProcessId}");
            if (host == "stdio")
            {
                logger.Info("  Transport:   STDIO Mode");
            }
            else
            {
                var transport = args.ServiceMode ? "HTTP (Service Mode)" : "HTTP (Interactive)";
                logger.Info($"  Transport:   {transport}");
                logger.Info($"  Host:        {host}");
                logger.Info($"  Port:        {port?.ToString() ?? "Default"}");
            }
            logger.Info($"  Started:     {timestamp}");
            logger.Info("╚═══════════════════════════════════════════════════════════════════╝");
        }

        private static string FormatBannerLine(string label, string value, int contentWidth)
        {
            var content = $"{label,-12} {value}";
            if (content.Length > contentWidth)
            {
                content = content.Substring(0, contentWidth);
            }
            return $"* {content.PadRight(contentWidth)} *";
        }

        private static string FormatCenteredBannerLine(string text, int contentWidth)
        {
            if (text.Length > contentWidth)
            {
                text = text.Substring(0, contentWidth);
            }
            // Center the text within the content width
            var totalPadding = contentWidth - text.Length;
            var leftPadding = totalPadding / 2;
            var rightPadding = totalPadding - leftPadding;
            var centeredContent = new string(' ', leftPadding) + text + new string(' ', rightPadding);
            return $"* {centeredContent} *";
        }

        private static void LogConfigurationSettings(IConfiguration configuration, CommandLineArguments commandLineArgs)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();


            logger.Info("");
            logger.Info("╔═══════════════════════════════════════════════════════════════════╗");
            logger.Info("                       CONFIGURATION SETTINGS");
            logger.Info("╚═══════════════════════════════════════════════════════════════════╝");

            // Application Settings
            logger.Info("");
            logger.Info("┌─ Application ──────────────────────────────────────────────────────");
            logger.Info($"│ Environment:       {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}");
            logger.Info($"│ Working Directory: {Directory.GetCurrentDirectory()}");
            logger.Info($"│ Assembly Location: {System.Reflection.Assembly.GetExecutingAssembly().Location}");

            // Command Line Arguments
            logger.Info("");
            logger.Info("┌─ Command Line Arguments ───────────────────────────────────────────");
            logger.Info($"│ Custom CDB Path: {commandLineArgs.CustomCdbPath ?? "Not specified"}");
            logger.Info($"│ Use HTTP:        {commandLineArgs.UseHttp}");
            logger.Info($"│ Service Mode:    {commandLineArgs.ServiceMode}");
            logger.Info($"│ Host:            {commandLineArgs.Host ?? "Not specified"} (from CLI: {commandLineArgs.HostFromCommandLine})");
            logger.Info($"│ Port:            {commandLineArgs.Port?.ToString() ?? "Not specified"} (from CLI: {commandLineArgs.PortFromCommandLine})");

            // Server Configuration
            logger.Info("");
            logger.Info("┌─ Server Configuration ─────────────────────────────────────────────");
            logger.Info($"│ Host: {configuration["McpNexus:Server:Host"] ?? "Not configured"}");
            logger.Info($"│ Port: {configuration["McpNexus:Server:Port"] ?? "Not configured"}");

            // Transport Configuration
            logger.Info("");
            logger.Info("┌─ Transport Configuration ──────────────────────────────────────────");
            logger.Info($"│ Mode:         {configuration["McpNexus:Transport:Mode"] ?? "Not configured"}");
            logger.Info($"│ Service Mode: {configuration["McpNexus:Transport:ServiceMode"] ?? "Not configured"}");

            // Debugging Configuration
            logger.Info("");
            logger.Info("┌─ Debugging Configuration ──────────────────────────────────────────");
            logger.Info($"│ CDB Path:                {configuration["McpNexus:Debugging:CdbPath"] ?? "Not configured"}");
            logger.Info($"│ Command Timeout:         {configuration["McpNexus:Debugging:CommandTimeoutMs"] ?? "Not configured"}ms");
            logger.Info($"│ Symbol Server Timeout:   {configuration["McpNexus:Debugging:SymbolServerTimeoutMs"] ?? "Not configured"}ms");
            logger.Info($"│ Symbol Server Retries:   {configuration["McpNexus:Debugging:SymbolServerMaxRetries"] ?? "Not configured"}");

            // Show both configured and effective symbol search paths
            var configuredSymbolPath = configuration["McpNexus:Debugging:SymbolSearchPath"];
            var effectiveSymbolPath = !string.IsNullOrWhiteSpace(configuredSymbolPath)
                ? configuredSymbolPath
                : Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH") ?? "Not set";

            logger.Info($"│ Symbol Search Path:      {configuredSymbolPath ?? "Not configured (using environment)"}");
            logger.Info($"│ Effective Symbol Path:   {effectiveSymbolPath}");
            logger.Info($"│ Startup Delay:           {configuration["McpNexus:Debugging:StartupDelayMs"] ?? "Not configured"}ms");

            // Service Configuration
            logger.Info("");
            logger.Info("┌─ Service Configuration ────────────────────────────────────────────");
            logger.Info($"│ Install Path: {configuration["McpNexus:Service:InstallPath"] ?? "Not configured"}");
            logger.Info($"│ Backup Path:  {configuration["McpNexus:Service:BackupPath"] ?? "Not configured"}");

            // Logging Configuration
            logger.Info("");
            logger.Info("┌─ Logging Configuration ────────────────────────────────────────────");
            logger.Info($"│ Default Level:     {configuration["Logging:LogLevel:Default"] ?? "Not configured"}");
            logger.Info($"│ ASP.NET Core Level: {configuration["Logging:LogLevel:Microsoft.AspNetCore"] ?? "Not configured"}");

            // Environment Variables (relevant ones)
            logger.Info("");
            logger.Info("┌─ Environment Variables ────────────────────────────────────────────");
            logger.Info($"│ ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"}");
            logger.Info($"│ ASPNETCORE_URLS:        {Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "Not set"}");
            logger.Info($"│ CDB Paths in PATH:      {GetCdbPathInfo()}");

            // System Information
            logger.Info("");
            logger.Info("┌─ System Information ───────────────────────────────────────────────");
            logger.Info($"│ OS:              {Environment.OSVersion}");
            logger.Info($"│ .NET Runtime:    {Environment.Version}");
            logger.Info($"│ Machine Name:    {Environment.MachineName}");
            logger.Info($"│ User Account:    {Environment.UserName}");
            logger.Info($"│ Processor Count: {Environment.ProcessorCount}");

            // Configuration Sources
            logger.Info("");
            logger.Info("┌─ Configuration Sources ────────────────────────────────────────────");
            if (configuration is IConfigurationRoot configRoot)
            {
                var providerIndex = 1;
                foreach (var provider in configRoot.Providers)
                {
                    logger.Info($"│ {providerIndex++}. {provider.GetType().Name}: {GetProviderInfo(provider)}");
                }
            }

            logger.Info("└────────────────────────────────────────────────────────────────────");
            logger.Info("");
        }

        private static string GetCdbPathInfo()
        {
            try
            {
                var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
                var pathDirs = pathVar.Split(';');
                var cdbPaths = pathDirs.Where(dir =>
                    dir.Contains("Windows Kits", StringComparison.OrdinalIgnoreCase) &&
                    dir.Contains("Debuggers", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return cdbPaths.Count > 0 ? string.Join("; ", cdbPaths) : "No CDB paths found in PATH";
            }
            catch
            {
                return "Unable to check PATH";
            }
        }

        private static string GetProviderInfo(IConfigurationProvider provider)
        {
            try
            {
                // Try to get useful information about the provider
                var providerType = provider.GetType();

                if (providerType.Name.Contains("Json"))
                {
                    // Try to get the source property for JSON providers
                    var sourceProperty = providerType.GetProperty("Source");
                    if (sourceProperty?.GetValue(provider) is Microsoft.Extensions.Configuration.Json.JsonConfigurationSource jsonSource)
                    {
                        return $"Path: {jsonSource.Path}, Optional: {jsonSource.Optional}";
                    }
                }
                else if (providerType.Name.Contains("Environment"))
                {
                    return "Environment Variables";
                }
                else if (providerType.Name.Contains("CommandLine"))
                {
                    return "Command Line Arguments";
                }

                return providerType.Name;
            }
            catch
            {
                return "Unknown";
            }
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
            public string? Host { get; set; }

            // Track original command line values for source reporting
            public bool HostFromCommandLine { get; set; }
            public bool PortFromCommandLine { get; set; }
        }

        private static async Task RunHttpServer(string[] args, CommandLineArguments commandLineArgs)
        {
            var logMessage = commandLineArgs.ServiceMode ?
                "Configuring for Windows service mode (HTTP)..." :
                "Configuring for HTTP transport...";
            Console.WriteLine(logMessage);

            var webBuilder = WebApplication.CreateBuilder(args);

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

            // Log startup banner
            LogStartupBanner(commandLineArgs, host, port);

            // Log detailed configuration settings
            LogConfigurationSettings(webBuilder.Configuration, commandLineArgs);

            // Add Windows service support if in service mode
            if (commandLineArgs.ServiceMode && OperatingSystem.IsWindows())
            {
                webBuilder.Host.UseWindowsService();
            }

            ConfigureLogging(webBuilder.Logging, commandLineArgs.ServiceMode);
            RegisterServices(webBuilder.Services, webBuilder.Configuration, commandLineArgs.CustomCdbPath);
            ConfigureHttpServices(webBuilder.Services, webBuilder.Configuration);

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
            await Console.Error.WriteLineAsync("Configuring for stdio transport...");
            var builder = Host.CreateApplicationBuilder(args);

            ConfigureLogging(builder.Logging, false);

            // Log startup banner for stdio mode
            LogStartupBanner(commandLineArgs, "stdio", null);

            // Log detailed configuration settings for stdio mode
            LogConfigurationSettings(builder.Configuration, commandLineArgs);

            RegisterServices(builder.Services, builder.Configuration, commandLineArgs.CustomCdbPath);
            ConfigureStdioServices(builder.Services);

            await Console.Error.WriteLineAsync("Building application host...");
            var host = builder.Build();

            // CRITICAL FIX: Initialize the notification bridge after host is built
            try
            {
                var notificationBridge = host.Services.GetRequiredService<IStdioNotificationBridge>();
                await notificationBridge.InitializeAsync();
                await Console.Error.WriteLineAsync("Notification bridge initialized for stdio MCP server");

                // Send standard MCP tools list changed notification on startup
                var notificationService = host.Services.GetRequiredService<IMcpNotificationService>();
                await notificationService.NotifyToolsListChangedAsync();
                await Console.Error.WriteLineAsync("Sent tools list changed notification to MCP clients");
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Warning: Failed to initialize notification bridge: {ex.Message}");
            }

            await Console.Error.WriteLineAsync("Starting MCP Nexus stdio server...");
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

        private static void RegisterServices(IServiceCollection services, IConfiguration configuration, string? customCdbPath)
        {
            Console.Error.WriteLine("Registering services...");

            // Register automated recovery services for unattended operation
            services.AddSingleton<ICommandTimeoutService, CommandTimeoutService>();
            Console.Error.WriteLine("Registered CommandTimeoutService for automated timeouts");

            // A+++++ ADVANCED SERVICES - Register all advanced services for maximum quality
            services.AddSingleton<mcp_nexus.Metrics.AdvancedMetricsService>();
            Console.Error.WriteLine("Registered AdvancedMetricsService for comprehensive performance monitoring");

            services.AddSingleton<mcp_nexus.Resilience.CircuitBreakerService>();
            Console.Error.WriteLine("Registered CircuitBreakerService for advanced fault tolerance");

            services.AddSingleton<mcp_nexus.Caching.IntelligentCacheService<string, object>>();
            Console.Error.WriteLine("Registered IntelligentCacheService for memory optimization");

            services.AddSingleton<mcp_nexus.Security.AdvancedSecurityService>();
            Console.Error.WriteLine("Registered AdvancedSecurityService for input validation and threat detection");

            services.AddSingleton<mcp_nexus.Health.AdvancedHealthService>();
            Console.Error.WriteLine("Registered AdvancedHealthService for comprehensive system monitoring");

            services.AddSingleton<ICdbSessionRecoveryService>(serviceProvider =>
            {
                var cdbSession = serviceProvider.GetRequiredService<ICdbSession>();
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSessionRecoveryService>>();
                var notificationService = serviceProvider.GetService<IMcpNotificationService>();

                // Create a callback that will be resolved when the command queue service is available
                Func<string, int> cancelAllCommandsCallback = reason =>
                {
                    var commandQueueService = serviceProvider.GetRequiredService<ICommandQueueService>();
                    return commandQueueService.CancelAllCommands(reason);
                };

                return new CdbSessionRecoveryService(cdbSession, logger, cancelAllCommandsCallback, notificationService);
            });
            Console.Error.WriteLine("Registered CdbSessionRecoveryService for automated recovery");

            // MIGRATION: Register session management instead of global command queue
            // Bind from configuration section (appsettings.json: McpNexus:SessionManagement). Model defaults act as fallback.
            services.AddOptions<mcp_nexus.Session.Models.SessionConfiguration>()
                .Bind(configuration.GetSection("McpNexus:SessionManagement"))
                .Validate(cfg => cfg.MaxConcurrentSessions > 0, "MaxConcurrentSessions must be > 0")
                .Validate(cfg => cfg.SessionTimeout > TimeSpan.Zero, "SessionTimeout must be positive")
                .Validate(cfg => cfg.CleanupInterval > TimeSpan.Zero, "CleanupInterval must be positive")
                .Validate(cfg => cfg.DisposalTimeout > TimeSpan.Zero, "DisposalTimeout must be positive")
                .Validate(cfg => cfg.DefaultCommandTimeout > TimeSpan.Zero, "DefaultCommandTimeout must be positive")
                .Validate(cfg => cfg.MemoryCleanupThresholdBytes > 0, "MemoryCleanupThresholdBytes must be > 0")
                .ValidateOnStart();
            services.AddSingleton<ISessionManager, ThreadSafeSessionManager>();
            Console.Error.WriteLine("Registered ThreadSafeSessionManager for multi-session support");

            // MIGRATION: CdbSession is now created per-session by SessionManager
            // Bind debugging/CDB options from configuration; override CustomCdbPath from CLI if provided
            services.AddOptions<mcp_nexus.Session.Models.CdbSessionOptions>()
                .Bind(configuration.GetSection("McpNexus:Debugging"))
                .Validate(o => o.CommandTimeoutMs > 0, "CommandTimeoutMs must be > 0")
                .Validate(o => o.SymbolServerTimeoutMs >= 0, "SymbolServerTimeoutMs must be >= 0")
                .Validate(o => o.SymbolServerMaxRetries >= 0, "SymbolServerMaxRetries must be >= 0")
                .ValidateOnStart();
            if (!string.IsNullOrWhiteSpace(customCdbPath))
            {
                services.PostConfigure<mcp_nexus.Session.Models.CdbSessionOptions>(options =>
                {
                    options.CustomCdbPath = customCdbPath;
                });
            }
            Console.Error.WriteLine("Configured CdbSession parameters for per-session creation");

            // MIGRATION: Register session-aware tool instead of legacy WindbgTool
            services.AddSingleton<SessionAwareWindbgTool>();
            Console.Error.WriteLine("Registered SessionAwareWindbgTool with multi-session support");

            // Register MCP notification service for both HTTP and stdio modes
            services.AddSingleton<IMcpNotificationService, McpNotificationService>();
            Console.Error.WriteLine("Registered McpNotificationService for server-initiated notifications");

            // MCP resources are now handled by the SDK via [McpServerResource] attributes
            Console.Error.WriteLine("MCP resources will be handled by SDK via [McpServerResource] attributes");
        }

        private static void ConfigureHttpServices(IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Configuring MCP server for HTTP...");

            // Configure HTTP request timeout to 15 minutes (longer than command timeout)
            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = null; // Remove request body size limit
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.RequestHeadersTimeout = ApplicationConstants.HttpRequestTimeout;
                options.Limits.KeepAliveTimeout = ApplicationConstants.HttpKeepAliveTimeout;
            });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    // For local MCP server, allow any origin since it's localhost-only
                    // MCP clients typically connect from the same machine
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Configure rate limiting
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

            // Use official SDK for HTTP mode with proper HTTP transport
            services.AddMcpServer()
                .WithHttpTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();

            Console.WriteLine("MCP server configured for HTTP with official SDK (HTTP transport)");
        }

        private static void ConfigureStdioServices(IServiceCollection services)
        {
            Console.Error.WriteLine("Configuring MCP server for stdio...");

            // Use official SDK for stdio mode
            services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();

            // CRITICAL FIX: Bridge notification service to stdio MCP server
            services.AddSingleton<IStdioNotificationBridge, StdioNotificationBridge>();

            Console.Error.WriteLine("MCP server configured with stdio transport, tools, and resources from assembly");
        }

        private static void ConfigureHttpPipeline(WebApplication app)
        {
            Console.WriteLine("Configuring HTTP request pipeline...");

            app.UseIpRateLimiting();
            app.UseCors();
            app.UseRouting();

            // Add JSON-RPC request/response logging middleware (only when debug logging is enabled)
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

            // Check if JSON-RPC debug logging should be enabled
            var enableJsonRpcLogging = ShouldEnableJsonRpcLogging(loggerFactory);

            if (enableJsonRpcLogging)
            {
                app.Use(async (context, next) =>
                {
                    if (context.Request.Path == "/" && context.Request.Method == "POST")
                    {
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                        // Log the request
                        context.Request.EnableBuffering();
                        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                        context.Request.Body.Position = 0;

                        // Format JSON for readability
                        var formattedRequest = FormatJsonForLogging(requestBody);
                        logger.LogInformation("📨 JSON-RPC Request:\n{RequestBody}", formattedRequest);

                        // Capture the response
                        var originalBodyStream = context.Response.Body;
                        using var responseBody = new MemoryStream();
                        context.Response.Body = responseBody;

                        await next();

                        // Log the response
                        responseBody.Seek(0, SeekOrigin.Begin);
                        var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                        responseBody.Seek(0, SeekOrigin.Begin);

                        // Format JSON for readability (handle SSE format)
                        var formattedResponse = FormatSseResponseForLogging(responseBodyText);
                        logger.LogInformation("📤 JSON-RPC Response:\n{ResponseBody}", formattedResponse);

                        await responseBody.CopyToAsync(originalBodyStream);
                    }
                    else
                    {
                        await next();
                    }
                });

                Console.WriteLine("JSON-RPC debug logging middleware enabled");
            }
            else
            {
                Console.WriteLine("JSON-RPC debug logging middleware disabled (not in debug mode)");
            }

            // Use the official SDK's HTTP transport with MapMcp
            app.MapMcp();

            Console.WriteLine("HTTP request pipeline configured with official SDK");
        }

        /// <summary>
        /// Determines if JSON-RPC debug logging should be enabled based on logging levels
        /// </summary>
        private static bool ShouldEnableJsonRpcLogging(ILoggerFactory loggerFactory)
        {
            // Check if debug logging is enabled for the application
            var debugLogger = loggerFactory.CreateLogger("MCP.JsonRpc");
            return debugLogger.IsEnabled(LogLevel.Debug);
        }

        /// <summary>
        /// Formats Server-Sent Events (SSE) response for better human readability in logs
        /// </summary>
        private static string FormatSseResponseForLogging(string sseResponse)
        {
            try
            {
                var lines = sseResponse.Split('\n');
                var formattedLines = new List<string>();

                foreach (var line in lines)
                {
                    if (line.StartsWith("event: "))
                    {
                        formattedLines.Add($"event: {line.Substring(7)}");
                    }
                    else if (line.StartsWith("data: "))
                    {
                        var jsonData = line.Substring(6);
                        var formattedJson = FormatJsonForLogging(jsonData);
                        formattedLines.Add($"data: {formattedJson}");
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        formattedLines.Add(line);
                    }
                }

                return string.Join("\n", formattedLines);
            }
            catch
            {
                // If formatting fails, return as-is
                return sseResponse;
            }
        }

        /// <summary>
        /// Formats JSON string for better human readability in logs
        /// </summary>
        private static string FormatJsonForLogging(string json)
        {
            try
            {
                // Try to parse and pretty-print the JSON
                using var document = JsonDocument.Parse(json);
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                document.WriteTo(writer);
                writer.Flush();
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException)
            {
                // If it's not valid JSON, return as-is
                return json;
            }
        }
    }
}
