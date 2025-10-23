using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.InteropServices;
using nexus.CommandLine;

namespace nexus.Startup;

/// <summary>
/// Displays the startup banner with configuration information.
/// </summary>
internal static class StartupBanner
{
    /// <summary>
    /// Displays the startup banner with system and configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="mode">Server mode.</param>
    public static void DisplayBanner(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration, ServerMode mode)
    {
        try
        {
            var isServiceMode = mode == ServerMode.Service;
            
            // Display main startup header
            DisplayStartupHeader(logger, isServiceMode);
            
            // Display configuration sections
            DisplayApplicationConfiguration(logger, isServiceMode);
            DisplayCommandLineConfiguration(logger, configuration, isServiceMode);
            DisplayServerConfiguration(logger, configuration);
            DisplayTransportConfiguration(logger, configuration, isServiceMode);
            DisplayDebuggingConfiguration(logger, configuration);
            DisplayServiceConfiguration(logger, configuration);
            DisplayLoggingConfiguration(logger, configuration, isServiceMode);
            DisplayEnvironmentVariables(logger);
            DisplaySystemInformation(logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to display startup banner");
        }
    }

    /// <summary>
    /// Displays the main startup header with basic application information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    private static void DisplayStartupHeader(Microsoft.Extensions.Logging.ILogger logger, bool isServiceMode)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        var processId = Environment.ProcessId;
        var startTime = DateTime.Now;
        var host = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(':').LastOrDefault() ?? "0.0.0.0";
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(':').LastOrDefault() ?? "5511";
        var transportMode = "http";

        logger.LogInformation("╔═══════════════════════════════════════════════════════════════════╗");
        logger.LogInformation("                            MCP NEXUS STARTUP");
        logger.LogInformation("");
        logger.LogInformation("  Version:     {Version}", version);
        logger.LogInformation("  Environment: {Environment}", isServiceMode ? "Service" : "Development");
        logger.LogInformation("  Process ID:  {ProcessId}", processId);
        logger.LogInformation("  Transport:   {TransportMode} ({ServiceMode})", 
            transportMode.ToUpper(), isServiceMode ? "Service Mode" : "Development Mode");
        logger.LogInformation("  Host:        {Host}", host);
        logger.LogInformation("  Port:        {Port}", port);
        logger.LogInformation("  Started:     {StartTime:yyyy-MM-dd HH:mm:ss}", startTime);
        logger.LogInformation("╚═══════════════════════════════════════════════════════════════════╝");
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays application configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    private static void DisplayApplicationConfiguration(Microsoft.Extensions.Logging.ILogger logger, bool isServiceMode)
    {
        var workingDirectory = Environment.CurrentDirectory;
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;

        logger.LogInformation("╔═══════════════════════════════════════════════════════════════════╗");
        logger.LogInformation("                        CONFIGURATION SETTINGS");
        logger.LogInformation("╚═══════════════════════════════════════════════════════════════════╝");
        logger.LogInformation("");

        logger.LogInformation("┌─ Application ──────────────────────────────────────────────────────");
        logger.LogInformation("│ Environment:       {Environment}", isServiceMode ? "Service" : "Development");
        logger.LogInformation("│ Working Directory: {WorkingDirectory}", workingDirectory);
        logger.LogInformation("│ Assembly Location: {AssemblyLocation}", assemblyLocation);
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays command line configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    private static void DisplayCommandLineConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration, bool isServiceMode)
    {
        var cdbPath = configuration["McpNexus:Debugging:CdbPath"] ?? "";
        var transportMode = configuration["McpNexus:Transport:Mode"] ?? "http";
        var host = configuration["McpNexus:Server:Host"] ?? "0.0.0.0";
        var port = configuration["McpNexus:Server:Port"] ?? "5511";

        logger.LogInformation("┌─ Command Line Arguments ───────────────────────────────────────────");
        logger.LogInformation("│ Custom CDB Path: {CdbPath}", string.IsNullOrEmpty(cdbPath) ? "Not specified" : cdbPath);
        logger.LogInformation("│ Use HTTP:        {UseHttp}", transportMode == "http");
        logger.LogInformation("│ Service Mode:    {ServiceMode}", isServiceMode);
        logger.LogInformation("│ Host:            {Host} (from config: {FromConfig})", host, "True");
        logger.LogInformation("│ Port:            {Port} (from config: {FromConfig})", port, "True");
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays server configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    private static void DisplayServerConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration)
    {
        var host = configuration["McpNexus:Server:Host"] ?? "0.0.0.0";
        var port = configuration["McpNexus:Server:Port"] ?? "5511";

        logger.LogInformation("┌─ Server Configuration ─────────────────────────────────────────────");
        logger.LogInformation("│ Host: {Host}", host);
        logger.LogInformation("│ Port: {Port}", port);
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays transport configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    private static void DisplayTransportConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration, bool isServiceMode)
    {
        var transportMode = configuration["McpNexus:Transport:Mode"] ?? "http";

        logger.LogInformation("┌─ Transport Configuration ──────────────────────────────────────────");
        logger.LogInformation("│ Mode:         {TransportMode}", transportMode);
        logger.LogInformation("│ Service Mode: {ServiceMode}", isServiceMode);
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays debugging configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    private static void DisplayDebuggingConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration)
    {
        var cdbPath = configuration["McpNexus:Debugging:CdbPath"] ?? "";
        var commandTimeout = configuration["McpNexus:Debugging:CommandTimeoutMs"] ?? "600000";
        var symbolRetries = configuration["McpNexus:Debugging:SymbolServerMaxRetries"] ?? "1";
        var symbolPath = configuration["McpNexus:Debugging:SymbolSearchPath"] ?? "";
        var startupDelay = configuration["McpNexus:Debugging:StartupDelayMs"] ?? "500";

        logger.LogInformation("┌─ Debugging Configuration ──────────────────────────────────────────");
        logger.LogInformation("│ CDB Path:                 {CdbPath}", string.IsNullOrEmpty(cdbPath) ? "Not specified" : cdbPath);
        logger.LogInformation("│ Command Timeout:          {CommandTimeout}ms", commandTimeout);
        logger.LogInformation("│ Symbol Server Retries:    {SymbolRetries}", symbolRetries);
        logger.LogInformation("│ Symbol Search Path:       {SymbolPath}", string.IsNullOrEmpty(symbolPath) ? "Not configured" : symbolPath);
        logger.LogInformation("│ Effective Symbol Path:    {SymbolPath}", string.IsNullOrEmpty(symbolPath) ? "Not configured" : symbolPath);
        logger.LogInformation("│ Startup Delay:            {StartupDelay}ms", startupDelay);
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays service configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    private static void DisplayServiceConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration)
    {
        var installPath = configuration["McpNexus:Service:InstallPath"] ?? "";
        var backupPath = configuration["McpNexus:Service:BackupPath"] ?? "";

        if (!string.IsNullOrEmpty(installPath))
        {
            logger.LogInformation("┌─ Service Configuration ────────────────────────────────────────────");
            logger.LogInformation("│ Install Path: {InstallPath}", installPath);
            logger.LogInformation("│ Backup Path:  {BackupPath}", backupPath);
            logger.LogInformation("");
        }
    }

    /// <summary>
    /// Displays logging configuration information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="isServiceMode">Whether running in service mode.</param>
    private static void DisplayLoggingConfiguration(Microsoft.Extensions.Logging.ILogger logger, IConfiguration configuration, bool isServiceMode)
    {
        var logLevel = configuration["Logging:LogLevel"] ?? "Information";

        logger.LogInformation("┌─ Logging Configuration ────────────────────────────────────────────");
        logger.LogInformation("│ Log Level:         {LogLevel}", logLevel);
        logger.LogInformation("│ Environment:       {Environment}", isServiceMode ? "Service" : "Development");
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays environment variables information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    private static void DisplayEnvironmentVariables(Microsoft.Extensions.Logging.ILogger logger)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var hasCdbInPath = pathEnv.Contains("cdb.exe", StringComparison.OrdinalIgnoreCase) || 
                          pathEnv.Contains("windbg", StringComparison.OrdinalIgnoreCase);

        logger.LogInformation("┌─ Environment Variables ────────────────────────────────────────────");
        logger.LogInformation("│ ASPNETCORE_ENVIRONMENT: {AspnetcoreEnvironment}", environment);
        logger.LogInformation("│ ASPNETCORE_URLS:        {AspnetcoreUrls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "Not set");
        logger.LogInformation("│ PRIVATE_TOKEN:          {PrivateToken}", Environment.GetEnvironmentVariable("PRIVATE_TOKEN") ?? "Not set");
        logger.LogInformation("│ CDB Paths in PATH:      {CdbInPath}", hasCdbInPath ? "CDB paths found in PATH" : "No CDB paths found in PATH");
        logger.LogInformation("");
    }

    /// <summary>
    /// Displays system information.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    private static void DisplaySystemInformation(Microsoft.Extensions.Logging.ILogger logger)
    {
        var osDescription = RuntimeInformation.OSDescription;
        var dotnetVersion = Environment.Version.ToString();
        var machineName = Environment.MachineName;
        var userAccount = Environment.UserName;
        var processorCount = Environment.ProcessorCount;

        logger.LogInformation("┌─ System Information ───────────────────────────────────────────────");
        logger.LogInformation("│ OS:              {OSDescription}", osDescription);
        logger.LogInformation("│ .NET Runtime:    {DotnetVersion}", dotnetVersion);
        logger.LogInformation("│ Machine Name:    {MachineName}", machineName);
        logger.LogInformation("│ User Account:    {UserAccount}", userAccount);
        logger.LogInformation("│ Processor Count: {ProcessorCount}", processorCount);
    }
}
