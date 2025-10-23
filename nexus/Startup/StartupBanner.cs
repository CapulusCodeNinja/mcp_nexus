using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Runtime.InteropServices;
using nexus.CommandLine;
using nexus.config.ServiceRegistration;

namespace nexus.Startup;

/// <summary>
/// Displays the startup banner with configuration information.
/// </summary>
internal class StartupBanner
{
    private readonly ILogger<StartupBanner> m_Logger;
    private readonly IConfiguration m_Configuration;
    private readonly bool m_IsServiceMode;

    public StartupBanner(IConfiguration configuration, ILogger<StartupBanner> logger, bool isServiceMode)
    {
        m_Logger = logger;
        m_Configuration = configuration;
        m_IsServiceMode = isServiceMode;
    }

    /// <summary>
    /// Displays the startup banner with system and configuration information.
    /// </summary>
    public void DisplayBanner()
    {
        try
        {
            // Display main startup header
            DisplayStartupHeader();
            
            // Display configuration sections
            DisplayApplicationConfiguration();
            DisplayCommandLineConfiguration();
            DisplayServerConfiguration();
            DisplayTransportConfiguration();
            DisplayDebuggingConfiguration();
            DisplayServiceConfiguration();
            DisplayLoggingConfiguration();
            DisplayEnvironmentVariables();
            DisplaySystemInformation();
        }
        catch (Exception ex)
        {
            m_Logger.LogWarning(ex, "Failed to display startup banner");
        }
    }

    /// <summary>
    /// Displays the main startup header with basic application information.
    /// </summary>
    private void DisplayStartupHeader()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        var processId = Environment.ProcessId;
        var startTime = DateTime.Now;
        var host = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(':').LastOrDefault() ?? "0.0.0.0";
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(':').LastOrDefault() ?? "5511";
        var transportMode = "http";

        m_Logger.LogInformation("╔═══════════════════════════════════════════════════════════════════╗");
        m_Logger.LogInformation("                            MCP NEXUS STARTUP");
        m_Logger.LogInformation("");
        m_Logger.LogInformation("  Version:     {Version}", version);
        m_Logger.LogInformation("  Environment: {Environment}", m_IsServiceMode ? "Service" : "Development");
        m_Logger.LogInformation("  Process ID:  {ProcessId}", processId);
        m_Logger.LogInformation("  Transport:   {TransportMode} ({ServiceMode})", 
            transportMode.ToUpper(), m_IsServiceMode ? "Service Mode" : "Development Mode");
        m_Logger.LogInformation("  Host:        {Host}", host);
        m_Logger.LogInformation("  Port:        {Port}", port);
        m_Logger.LogInformation("  Started:     {StartTime:yyyy-MM-dd HH:mm:ss}", startTime);
        m_Logger.LogInformation("╚═══════════════════════════════════════════════════════════════════╝");
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays application configuration information.
    /// </summary>
    private void DisplayApplicationConfiguration()
    {
        var workingDirectory = Environment.CurrentDirectory;
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;

        m_Logger.LogInformation("╔═══════════════════════════════════════════════════════════════════╗");
        m_Logger.LogInformation("                        CONFIGURATION SETTINGS");
        m_Logger.LogInformation("╚═══════════════════════════════════════════════════════════════════╝");
        m_Logger.LogInformation("");

        m_Logger.LogInformation("┌─ Application ──────────────────────────────────────────────────────");
        m_Logger.LogInformation("│ Environment:       {Environment}", m_IsServiceMode ? "Service" : "Development");
        m_Logger.LogInformation("│ Working Directory: {WorkingDirectory}", workingDirectory);
        m_Logger.LogInformation("│ Assembly Location: {AssemblyLocation}", assemblyLocation);
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays command line configuration information.
    /// </summary>
    private void DisplayCommandLineConfiguration()
    {
        var cdbPath = m_Configuration["McpNexus:Debugging:CdbPath"] ?? "";
        var transportMode = m_Configuration["McpNexus:Transport:Mode"] ?? "http";
        var host = m_Configuration["McpNexus:Server:Host"] ?? "0.0.0.0";
        var port = m_Configuration["McpNexus:Server:Port"] ?? "5511";

        m_Logger.LogInformation("┌─ Command Line Arguments ───────────────────────────────────────────");
        m_Logger.LogInformation("│ Custom CDB Path: {CdbPath}", string.IsNullOrEmpty(cdbPath) ? "Not specified" : cdbPath);
        m_Logger.LogInformation("│ Use HTTP:        {UseHttp}", transportMode == "http");
        m_Logger.LogInformation("│ Service Mode:    {ServiceMode}", m_IsServiceMode);
        m_Logger.LogInformation("│ Host:            {Host} (from config: {FromConfig})", host, "True");
        m_Logger.LogInformation("│ Port:            {Port} (from config: {FromConfig})", port, "True");
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays server configuration information.
    /// </summary>
    private void DisplayServerConfiguration()
    {
        var host = m_Configuration["McpNexus:Server:Host"] ?? "0.0.0.0";
        var port = m_Configuration["McpNexus:Server:Port"] ?? "5511";

        m_Logger.LogInformation("┌─ Server Configuration ─────────────────────────────────────────────");
        m_Logger.LogInformation("│ Host: {Host}", host);
        m_Logger.LogInformation("│ Port: {Port}", port);
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays transport m_Configuration information.
    /// </summary>
    private void DisplayTransportConfiguration()
    {
        var transportMode = m_Configuration["McpNexus:Transport:Mode"] ?? "http";

        m_Logger.LogInformation("┌─ Transport Configuration ──────────────────────────────────────────");
        m_Logger.LogInformation("│ Mode:         {TransportMode}", transportMode);
        m_Logger.LogInformation("│ Service Mode: {ServiceMode}", m_IsServiceMode);
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays debugging configuration information.
    /// </summary>
    private void DisplayDebuggingConfiguration()
    {
        var cdbPath = m_Configuration["McpNexus:Debugging:CdbPath"] ?? "";
        var commandTimeout = m_Configuration["McpNexus:Debugging:CommandTimeoutMs"] ?? "600000";
        var symbolRetries = m_Configuration["McpNexus:Debugging:SymbolServerMaxRetries"] ?? "1";
        var symbolPath = m_Configuration["McpNexus:Debugging:SymbolSearchPath"] ?? "";
        var startupDelay = m_Configuration["McpNexus:Debugging:StartupDelayMs"] ?? "500";

        m_Logger.LogInformation("┌─ Debugging Configuration ──────────────────────────────────────────");
        m_Logger.LogInformation("│ CDB Path:                 {CdbPath}", string.IsNullOrEmpty(cdbPath) ? "Not specified" : cdbPath);
        m_Logger.LogInformation("│ Command Timeout:          {CommandTimeout}ms", commandTimeout);
        m_Logger.LogInformation("│ Symbol Server Retries:    {SymbolRetries}", symbolRetries);
        m_Logger.LogInformation("│ Symbol Search Path:       {SymbolPath}", string.IsNullOrEmpty(symbolPath) ? "Not configured" : symbolPath);
        m_Logger.LogInformation("│ Effective Symbol Path:    {SymbolPath}", string.IsNullOrEmpty(symbolPath) ? "Not configured" : symbolPath);
        m_Logger.LogInformation("│ Startup Delay:            {StartupDelay}ms", startupDelay);
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays service configuration information.
    /// </summary>
    private void DisplayServiceConfiguration()
    {
        var installPath = m_Configuration["McpNexus:Service:InstallPath"] ?? "";
        var backupPath = m_Configuration["McpNexus:Service:BackupPath"] ?? "";

        if (!string.IsNullOrEmpty(installPath))
        {
            m_Logger.LogInformation("┌─ Service Configuration ────────────────────────────────────────────");
            m_Logger.LogInformation("│ Install Path: {InstallPath}", installPath);
            m_Logger.LogInformation("│ Backup Path:  {BackupPath}", backupPath);
            m_Logger.LogInformation("");
        }
    }

    /// <summary>
    /// Displays logging configuration information.
    /// </summary>
    private void DisplayLoggingConfiguration()
    {
        var logLevel = m_Configuration["Logging:LogLevel"] ?? "Information";

        m_Logger.LogInformation("┌─ Logging Configuration ────────────────────────────────────────────");
        m_Logger.LogInformation("│ Log Level:         {LogLevel}", logLevel);
        m_Logger.LogInformation("│ Environment:       {Environment}", m_IsServiceMode ? "Service" : "Development");
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays environment variables information.
    /// </summary>
    private void DisplayEnvironmentVariables()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var hasCdbInPath = pathEnv.Contains("cdb.exe", StringComparison.OrdinalIgnoreCase) || 
                          pathEnv.Contains("windbg", StringComparison.OrdinalIgnoreCase);

        m_Logger.LogInformation("┌─ Environment Variables ────────────────────────────────────────────");
        m_Logger.LogInformation("│ ASPNETCORE_ENVIRONMENT: {AspnetcoreEnvironment}", environment);
        m_Logger.LogInformation("│ ASPNETCORE_URLS:        {AspnetcoreUrls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "Not set");
        m_Logger.LogInformation("│ PRIVATE_TOKEN:          {PrivateToken}", Environment.GetEnvironmentVariable("PRIVATE_TOKEN") ?? "Not set");
        m_Logger.LogInformation("│ CDB Paths in PATH:      {CdbInPath}", hasCdbInPath ? "CDB paths found in PATH" : "No CDB paths found in PATH");
        m_Logger.LogInformation("");
    }

    /// <summary>
    /// Displays system information.
    /// </summary>
    private void DisplaySystemInformation()
    {
        var osDescription = RuntimeInformation.OSDescription;
        var dotnetVersion = Environment.Version.ToString();
        var machineName = Environment.MachineName;
        var userAccount = Environment.UserName;
        var processorCount = Environment.ProcessorCount;

        m_Logger.LogInformation("┌─ System Information ───────────────────────────────────────────────");
        m_Logger.LogInformation("│ OS:              {OSDescription}", osDescription);
        m_Logger.LogInformation("│ .NET Runtime:    {DotnetVersion}", dotnetVersion);
        m_Logger.LogInformation("│ Machine Name:    {MachineName}", machineName);
        m_Logger.LogInformation("│ User Account:    {UserAccount}", userAccount);
        m_Logger.LogInformation("│ Processor Count: {ProcessorCount}", processorCount);
    }
}
