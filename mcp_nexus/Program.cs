using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NLog.Web;
using AspNetCoreRateLimit;
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using mcp_nexus.Constants;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Recovery;
using mcp_nexus.Infrastructure;
using mcp_nexus.Session;
using mcp_nexus.Tools;
using mcp_nexus.Middleware;
using mcp_nexus.Configuration;

namespace mcp_nexus
{
    /// <summary>
    /// Main entry point for the MCP Nexus application.
    /// Handles both HTTP and stdio modes, service installation, and application lifecycle management.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task Main(string[] args)
        {
            // IMMEDIATE startup logging to track how far we get
            try
            {
                Console.Error.WriteLine($" MCP Nexus starting...");

                // Set up global exception handlers FIRST
                Console.Error.WriteLine($" Setting up global exception handlers...");

                SetupGlobalExceptionHandlers();
                Console.Error.WriteLine($" Global exception handlers set up.");
            }
            catch (Exception startupEx)
            {
                Console.Error.WriteLine($" STARTUP EXCEPTION: {startupEx}");
                Environment.Exit(1);
            }

            try
            {
                Console.Error.WriteLine($" Setting environment variables...");

                // Pre-warm ThreadPool to reduce cold-start thread acquisition under bursty load
                mcp_nexus.Infrastructure.ThreadPoolTuning.Apply();

                // Set environment based on configuration if not already set
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
                {
                    // Check if we're running in service mode
                    if (args.Contains("--service") || args.Contains("--install") || args.Contains("--uninstall") || args.Contains("--update"))
                    {
                        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Service");
                        Console.Error.WriteLine($" Environment set to Service mode");
                    }
                    else
                    {
                        // Default to Production for non-development builds
                        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
                        Console.Error.WriteLine($" Environment set to Production mode");
                    }
                }

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

                        var installResult = await WindowsServiceInstaller.InstallServiceAsync(logger);
                        Environment.Exit(installResult ? 0 : 1);
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
                    Console.WriteLine("Uninstall command detected");
                    if (OperatingSystem.IsWindows())
                    {
                        try
                        {
                            Console.WriteLine("Starting service uninstallation...");
                            // Create a logger using NLog configuration for the uninstallation process
                            using var loggerFactory = LoggerFactory.Create(builder =>
                            {
                                builder.ClearProviders();
                                builder.AddNLogWeb();
                                builder.SetMinimumLevel(LogLevel.Information);
                            });
                            var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");

                            var uninstallResult = await WindowsServiceInstaller.UninstallServiceAsync(logger);
                            Console.WriteLine($"Uninstall result: {uninstallResult}");
                            Environment.Exit(uninstallResult ? 0 : 1);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during uninstall: {ex.Message}");
                            Console.WriteLine($"Stack trace: {ex.StackTrace}");
                            Environment.Exit(1);
                        }
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
                    Console.Error.WriteLine($" Update command detected");

                    if (OperatingSystem.IsWindows())
                    {
                        Console.Error.WriteLine($" Creating logger for update process...");

                        // Create a logger using NLog configuration for the update process
                        using var loggerFactory = LoggerFactory.Create(builder =>
                        {
                            builder.ClearProviders();
                            builder.AddNLogWeb();
                            builder.SetMinimumLevel(LogLevel.Information);
                        });
                        var logger = loggerFactory.CreateLogger("MCP.Nexus.ServiceInstaller");

                        Console.Error.WriteLine($" Starting update service call...");

                        var updateResult = await WindowsServiceInstaller.UpdateServiceAsync(logger);

                        Console.Error.WriteLine($" Update service call completed with result: {updateResult}");

                        Environment.Exit(updateResult ? 0 : 1);
                    }
                    else
                    {
                        await Console.Error.WriteLineAsync("ERROR: Service update is only supported on Windows.");
                    }
                    return;
                }

                // Determine transport mode
                bool useHttp = commandLineArgs.UseHttp || commandLineArgs.ServiceMode;

                // Validate service mode is only used on Windows
                if (commandLineArgs.ServiceMode && !OperatingSystem.IsWindows())
                {
                    await Console.Error.WriteLineAsync("ERROR: Service mode is only supported on Windows.");
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
            catch (Exception ex)
            {
                // Comprehensive exception logging to help diagnose crashes
                try
                {
                    await Console.Error.WriteLineAsync("================================================================================");
                    await Console.Error.WriteLineAsync("FATAL UNHANDLED EXCEPTION IN MCP NEXUS");
                    await Console.Error.WriteLineAsync("================================================================================");
                    await Console.Error.WriteLineAsync($"Exception Type: {ex.GetType().FullName}");
                    await Console.Error.WriteLineAsync($"Message: {ex.Message}");
                    await Console.Error.WriteLineAsync($"Source: {ex.Source}");
                    await Console.Error.WriteLineAsync($"TargetSite: {ex.TargetSite}");
                    await Console.Error.WriteLineAsync("Stack Trace:");
                    await Console.Error.WriteLineAsync(ex.StackTrace ?? "No stack trace available");

                    if (ex.InnerException != null)
                    {
                        await Console.Error.WriteLineAsync("Inner Exception:");
                        await Console.Error.WriteLineAsync($"  Type: {ex.InnerException.GetType().FullName}");
                        await Console.Error.WriteLineAsync($"  Message: {ex.InnerException.Message}");
                        await Console.Error.WriteLineAsync($"  Stack Trace: {ex.InnerException.StackTrace}");
                    }

                    await Console.Error.WriteLineAsync("================================================================================");
                    await Console.Error.WriteLineAsync($"Command Line Args: {string.Join(" ", args)}");
                    await Console.Error.WriteLineAsync($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
                    await Console.Error.WriteLineAsync($"OS Version: {Environment.OSVersion}");
                    await Console.Error.WriteLineAsync($".NET Version: {Environment.Version}");
                    await Console.Error.WriteLineAsync($"Working Directory: {Environment.CurrentDirectory}");
                    await Console.Error.WriteLineAsync("================================================================================");
                }
                catch
                {
                    // If console logging fails, try to write to a file
                    try
                    {
                        var logFile = Path.Combine(Environment.CurrentDirectory, "mcp_nexus_crash.log");
                        await File.WriteAllTextAsync(logFile, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} - FATAL ERROR: {ex}");
                    }
                    catch
                    {
                        // Last resort - do nothing, but at least we tried
                    }
                }

                // Exit with error code
                Environment.Exit(1);
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
            Console.WriteLine("  - HTTP mode runs on localhost:5000/ (or custom port if specified)");
            Console.WriteLine();
            Console.WriteLine("For more information, visit: https://github.com/your-repo/mcp_nexus");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Parses command line arguments using System.CommandLine.
        /// </summary>
        /// <param name="args">The command line arguments to parse.</param>
        /// <returns>A CommandLineArguments object containing the parsed values.</returns>
        private static CommandLineArguments ParseCommandLineArguments(string[] args)
        {
            var result = new CommandLineArguments();

            var cdbPathOption = new Option<string?>("--cdb-path", "Custom path to CDB.exe debugger executable");
            var httpOption = new Option<bool>("--http", "Use HTTP transport instead of stdio");
            var serviceOption = new Option<bool>("--service", "Run in Windows service mode (implies --http)");
            var installOption = new Option<bool>("--install", "Install MCP Nexus as Windows service");
            var uninstallOption = new Option<bool>("--uninstall", "Uninstall MCP Nexus Windows service");
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
                result.Update = parseResult.GetValueForOption(updateOption);
                result.Port = parseResult.GetValueForOption(portOption);
                result.Host = parseResult.GetValueForOption(hostOption);

                // Track which values came from command line
                result.PortFromCommandLine = result.Port.HasValue;
                result.HostFromCommandLine = result.Host != null;
            }

            return result;
        }

        /// <summary>
        /// <summary>
        /// Logs the startup banner with application information and configuration details.
        /// </summary>
        /// <param name="args">The parsed command line arguments.</param>
        /// <param name="host">The host address the application will bind to.</param>
        /// <param name="port">The port number the application will listen on.</param>
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

        /// <summary>
        /// Formats a banner line with a label and value, truncating if necessary.
        /// </summary>
        /// <param name="label">The label for the banner line.</param>
        /// <param name="value">The value to display.</param>
        /// <param name="contentWidth">The maximum width for the content.</param>
        /// <returns>A formatted banner line string.</returns>
        private static string FormatBannerLine(string label, string value, int contentWidth)
        {
            var content = $"{label,-12} {value}";
            if (content.Length > contentWidth)
            {
                content = content.Substring(0, contentWidth);
            }
            return $"* {content.PadRight(contentWidth)} *";
        }

        /// <summary>
        /// <summary>
        /// Formats a centered banner line with the specified text.
        /// </summary>
        /// <param name="text">The text to center in the banner line.</param>
        /// <param name="contentWidth">The maximum width for the content.</param>
        /// <returns>A formatted centered banner line string.</returns>
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

        /// <summary>
        /// Logs the current configuration settings for debugging purposes.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="commandLineArgs">The parsed command line arguments.</param>
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
            logger.Info($"│ CDB Path:                {configuration["McpNexus:Debugging:CdbPath"] ?? "Auto-detect during service registration"}");
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
            logger.Info($"│ Log Level:         {configuration["Logging:LogLevel"] ?? "Not configured"}");
            logger.Info($"│ Environment:       {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"}");

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

        /// <summary>
        /// Gets information about the CDB executable path for logging.
        /// </summary>
        /// <returns>A string describing the CDB path configuration.</returns>
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

        /// <summary>
        /// Gets information about a configuration provider for logging.
        /// </summary>
        /// <param name="provider">The configuration provider to get information about.</param>
        /// <returns>A string describing the configuration provider.</returns>
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

        /// <summary>
        /// Represents parsed command line arguments for the MCP Nexus application.
        /// </summary>
        private class CommandLineArguments
        {
            /// <summary>
            /// Gets or sets the custom path to the CDB.exe debugger executable.
            /// </summary>
            public string? CustomCdbPath { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to use HTTP transport instead of stdio.
            /// </summary>
            public bool UseHttp { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to run in Windows service mode (implies HTTP).
            /// </summary>
            public bool ServiceMode { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to install MCP Nexus as a Windows service.
            /// </summary>
            public bool Install { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to uninstall the MCP Nexus Windows service.
            /// </summary>
            public bool Uninstall { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to update the MCP Nexus service (stop, update files, restart).
            /// </summary>
            public bool Update { get; set; }

            /// <summary>
            /// Gets or sets the HTTP server port number.
            /// </summary>
            public int? Port { get; set; }

            /// <summary>
            /// Gets or sets the HTTP server host binding address.
            /// </summary>
            public string? Host { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the host was specified via command line (for source reporting).
            /// </summary>
            public bool HostFromCommandLine { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the port was specified via command line (for source reporting).
            /// </summary>
            public bool PortFromCommandLine { get; set; }
        }

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

            // Configure logging FIRST so startup logs go to the correct location
            LoggingSetup.ConfigureLogging(webBuilder.Logging, commandLineArgs.ServiceMode, webBuilder.Configuration);

            // Add Windows service support if in service mode
            if (commandLineArgs.ServiceMode && OperatingSystem.IsWindows())
            {
                webBuilder.Host.UseWindowsService();
            }

            // Log startup banner AFTER logging is configured
            LogStartupBanner(commandLineArgs, host, port);

            // Log detailed configuration settings AFTER logging is configured
            LogConfigurationSettings(webBuilder.Configuration, commandLineArgs);
            ServiceRegistration.RegisterServices(webBuilder.Services, webBuilder.Configuration, commandLineArgs.CustomCdbPath);
            HttpServerSetup.ConfigureHttpServices(webBuilder.Services, webBuilder.Configuration);

            var app = webBuilder.Build();
            ConfigureHttpPipeline(app);

            // Log the resolved CDB path after service registration
            var cdbOptions = app.Services.GetService<IOptions<mcp_nexus.Session.Models.CdbSessionOptions>>()?.Value;
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
        private static async Task RunStdioServer(string[] args, CommandLineArguments commandLineArgs)
        {
            // Configure UTF-8 encoding for all console streams (stdin, stdout, stderr)
            EncodingConfiguration.ConfigureConsoleEncoding();

            // CRITICAL: In stdio mode, stdout is reserved for MCP protocol
            // All console output must go to stderr
            await Console.Error.WriteLineAsync("Configuring for stdio transport...");
            var builder = Host.CreateApplicationBuilder(args);

            LoggingSetup.ConfigureLogging(builder.Logging, false, builder.Configuration);

            // Log startup banner for stdio mode
            LogStartupBanner(commandLineArgs, "stdio", null);

            // Log detailed configuration settings for stdio mode
            LogConfigurationSettings(builder.Configuration, commandLineArgs);

            ServiceRegistration.RegisterServices(builder.Services, builder.Configuration, commandLineArgs.CustomCdbPath);
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




        /// <summary>
        /// Configures services for stdio mode operation.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
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
            if (ShouldEnableJsonRpcLogging(loggerFactory))
            {
                app.UseMiddleware<JsonRpcLoggingMiddleware>();
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
        /// <summary>
        /// Determines if JSON-RPC debug logging should be enabled based on logging levels.
        /// </summary>
        private static bool ShouldEnableJsonRpcLogging(ILoggerFactory loggerFactory)
        {
            // Check if debug logging is enabled for the application
            var debugLogger = loggerFactory.CreateLogger("MCP.JsonRpc");
            return debugLogger.IsEnabled(LogLevel.Debug);
        }

        /// <summary>
        /// <summary>
        /// Formats Server-Sent Events (SSE) response for better human readability in logs.
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
        /// <summary>
        /// Formats JSON string for better human readability in logs.
        /// </summary>
        private static string FormatJsonForLogging(string json)
        {
            try
            {
                // Try to parse and pretty-print the JSON
                using var document = JsonDocument.Parse(json);
                return System.Text.Json.JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (JsonException ex)
            {
                // Log the parsing error for debugging and return sanitized version
                var sanitizedJson = json.Length > 1000 ? json.Substring(0, 1000) + "..." : json;
                return $"[Invalid JSON - {ex.Message}]: {sanitizedJson}";
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                return $"[JSON formatting error - {ex.Message}]: {json.Substring(0, Math.Min(json.Length, 100))}...";
            }
        }

        /// <summary>
        /// <summary>
        /// Sets up global exception handlers to catch unhandled exceptions from all sources.
        /// </summary>
        private static void SetupGlobalExceptionHandlers()
        {
            // Handle unhandled exceptions in the current AppDomain
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogFatalException(ex, "AppDomain.UnhandledException", e.IsTerminating);
            };

            // Handle unhandled exceptions in tasks
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogFatalException(e.Exception, "TaskScheduler.UnobservedTaskException", false);
                e.SetObserved(); // Prevent the process from terminating
            };

            // Handle unhandled exceptions in the current thread (for console apps)
            if (!Environment.UserInteractive)
            {
                // For service mode, also handle process exit
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Process exiting...");
                };
            }
        }

        /// <summary>
        /// <summary>
        /// Logs fatal exceptions with comprehensive details.
        /// </summary>
        private static void LogFatalException(Exception? ex, string source, bool isTerminating)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

                // IMMEDIATE console output with flushing
                Console.Error.WriteLine("################################################################################");
                Console.Error.WriteLine($"FATAL UNHANDLED EXCEPTION - {source}");
                Console.Error.WriteLine($"Time: {timestamp}");
                Console.Error.WriteLine($"Terminating: {isTerminating}");
                Console.Error.WriteLine("################################################################################");

                if (ex != null)
                {
                    Console.Error.WriteLine($"Exception Type: {ex.GetType().FullName}");
                    Console.Error.WriteLine($"Message: {ex.Message}");
                    Console.Error.WriteLine($"Source: {ex.Source}");
                    Console.Error.WriteLine($"TargetSite: {ex.TargetSite}");
                    Console.Error.WriteLine("Stack Trace:");
                    Console.Error.WriteLine(ex.StackTrace ?? "No stack trace available");

                    // Handle inner exceptions
                    var innerEx = ex.InnerException;
                    int depth = 1;
                    while (innerEx != null && depth <= 5) // Limit depth to prevent infinite loops
                    {
                        Console.Error.WriteLine($"Inner Exception (Level {depth}):");
                        Console.Error.WriteLine($"  Type: {innerEx.GetType().FullName}");
                        Console.Error.WriteLine($"  Message: {innerEx.Message}");
                        Console.Error.WriteLine($"  Stack Trace: {innerEx.StackTrace}");
                        innerEx = innerEx.InnerException;
                        depth++;
                    }

                    // Handle AggregateException specially
                    if (ex is AggregateException aggEx)
                    {
                        Console.Error.WriteLine($"AggregateException contains {aggEx.InnerExceptions.Count} inner exceptions:");
                        for (int i = 0; i < aggEx.InnerExceptions.Count && i < 10; i++) // Limit to first 10
                        {
                            var innerException = aggEx.InnerExceptions[i];
                            Console.Error.WriteLine($"  [{i}] {innerException.GetType().FullName}: {innerException.Message}");
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine("Exception object is null");
                }

                Console.Error.WriteLine("################################################################################");
                Console.Error.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
                Console.Error.WriteLine($"OS Version: {Environment.OSVersion}");
                Console.Error.WriteLine($".NET Version: {Environment.Version}");
                Console.Error.WriteLine($"Working Directory: {Environment.CurrentDirectory}");
                Console.Error.WriteLine($"Process ID: {Environment.ProcessId}");
                Console.Error.WriteLine($"Thread ID: {Environment.CurrentManagedThreadId}");
                Console.Error.WriteLine("################################################################################");

                // AGGRESSIVELY write to multiple crash log locations
                var logContent = $"{timestamp} - {source} - Terminating: {isTerminating}\n{ex}\n\n";
                var logLocations = new[]
                {
                    Path.Combine(Environment.CurrentDirectory, "mcp_nexus_crash.log"),
                    Path.Combine(Path.GetTempPath(), "mcp_nexus_crash.log"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "mcp_nexus_crash.log")
                };

                foreach (var logFile in logLocations)
                {
                    try
                    {
                        File.AppendAllText(logFile, logContent);
                        Console.Error.WriteLine($"Crash details written to: {logFile}");
                        break; // Stop after first successful write
                    }
                    catch
                    {
                        // Try next location
                    }
                }

                // Also try Windows Event Log as last resort
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        using var eventLog = new System.Diagnostics.EventLog("Application");
                        eventLog.Source = "MCP Nexus";
                        eventLog.WriteEntry($"FATAL EXCEPTION - {source}: {ex?.Message}", System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                catch
                {
                    // Ignore event log errors
                }
            }
            catch
            {
                // If even our error logging fails, try one last desperate attempt
                try
                {
                    Console.Error.WriteLine($"CRITICAL: Exception in exception handler! Original: {ex?.Message}");
                }
                catch
                {
                    // Give up
                }
            }
        }
    }
}
