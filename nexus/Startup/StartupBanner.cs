using System.Reflection;
using System.Runtime.InteropServices;

using Nexus.CommandLine;
using Nexus.Config;

using NLog;

namespace Nexus.Startup;

/// <summary>
/// Displays the startup banner with configuration information.
/// </summary>
internal class StartupBanner
{
    private readonly Logger m_Logger;
    private readonly ISettings m_Settings;
    private readonly bool m_IsServiceMode;
    private readonly CommandLineContext m_CommandLineContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupBanner"/> class.
    /// </summary>
    /// <param name="isServiceMode">Indicates whether the application is running as a Windows Service.</param>
    /// <param name="commandLineContext">The command line context.</param>
    /// <param name="settings">The product settings.</param>
    public StartupBanner(bool isServiceMode, CommandLineContext commandLineContext, ISettings settings)
    {
        m_Logger = LogManager.GetCurrentClassLogger();
        m_IsServiceMode = isServiceMode;
        m_CommandLineContext = commandLineContext;
        m_Settings = settings;
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
            DisplayValidationConfiguration();
            DisplayServiceConfiguration();
            DisplayLoggingConfiguration();
            DisplayEnvironmentVariables();
            DisplaySystemInformation();

            m_Logger.Info(string.Empty);
        }
        catch (Exception ex)
        {
            m_Logger.Warn(ex, "Failed to display startup banner");
        }
    }

    /// <summary>
    /// Displays the main startup header with basic application information.
    /// </summary>
    private void DisplayStartupHeader()
    {
        var isStdioMode = m_CommandLineContext.IsStdioMode;
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        var processId = Environment.ProcessId;
        var startTime = DateTime.Now;
        string host;
        string port;
        string transportMode;

        if (isStdioMode)
        {
            transportMode = "stdio";
            host = "stdio";
            port = "n/a";
        }
        else
        {
            transportMode = "http";

            // Prefer configuration for host/port; fall back to ASPNETCORE_URLS when available.
            var sharedConfiguration = m_Settings.Get();
            var configuredHost = sharedConfiguration.McpNexus.Server.Host;
            var configuredPort = sharedConfiguration.McpNexus.Server.Port;

            host = configuredHost ?? "0.0.0.0";
            port = configuredPort > 0
                ? configuredPort.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : (Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(':').LastOrDefault() ?? "5511");
        }

        m_Logger.Info("╔═══════════════════════════════════════════════════════════════════╗");
        m_Logger.Info("                            MCP NEXUS STARTUP");
        m_Logger.Info(string.Empty);
        m_Logger.Info("  Version:     {Version}", version);
        m_Logger.Info("  Environment: {Environment}", m_IsServiceMode ? "Service" : "Development");
        m_Logger.Info("  Process ID:  {ProcessId}", processId);
        m_Logger.Info("  Transport:   {TransportMode} ({ServiceMode})", transportMode.ToUpper(), m_IsServiceMode ? "Service Mode" : "Development Mode");
        m_Logger.Info("  Host:        {Host}", host);
        m_Logger.Info("  Port:        {Port}", port);
        m_Logger.Info("  Started:     {StartTime:yyyy-MM-dd HH:mm:ss}", startTime);
        m_Logger.Info("╚═══════════════════════════════════════════════════════════════════╝");
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Displays application configuration information.
    /// </summary>
    private void DisplayApplicationConfiguration()
    {
        var workingDirectory = Environment.CurrentDirectory;
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;

        m_Logger.Info("╔═══════════════════════════════════════════════════════════════════╗");
        m_Logger.Info("                        CONFIGURATION SETTINGS");
        m_Logger.Info("╚═══════════════════════════════════════════════════════════════════╝");
        m_Logger.Info(string.Empty);

        m_Logger.Info("┌─ Application ──────────────────────────────────────────────────────");
        m_Logger.Info("│ Environment:       {Environment}", m_IsServiceMode ? "Service" : "Development");
        m_Logger.Info("│ Working Directory: {WorkingDirectory}", workingDirectory);
        m_Logger.Info("│ Assembly Location: {AssemblyLocation}", assemblyLocation);
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Displays command line configuration information.
    /// </summary>
    private void DisplayCommandLineConfiguration()
    {
        var args = m_CommandLineContext.Args;
        var commandLineString = args.Length > 0 ? string.Join(" ", args) : "No arguments";

        m_Logger.Info("┌─ Command Line Arguments ───────────────────────────────────────────");
        m_Logger.Info("│ Command Line:    {CommandLine}", commandLineString);
        m_Logger.Info("│ Mode:            {Mode}", GetDetectedMode());
        m_Logger.Info("│ Service Mode:    {ServiceMode}", m_IsServiceMode);
        m_Logger.Info("└─────────────────────────────────────────────────────────────────────");
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Gets the detected mode from command line context.
    /// </summary>
    /// <returns>String representation of the detected mode.</returns>
    private string GetDetectedMode()
    {
        return m_CommandLineContext.IsHttpMode
            ? "HTTP Server"
            : m_CommandLineContext.IsStdioMode
            ? "Stdio Server"
            : m_CommandLineContext.IsServiceMode
            ? "Windows Service"
            : m_CommandLineContext.IsInstallMode
            ? "Install Command"
            : m_CommandLineContext.IsUpdateMode
            ? "Update Command"
            : m_CommandLineContext.IsUninstallMode ? "Uninstall Command" : "HTTP Server (default)";
    }

    /// <summary>
    /// Displays server configuration information.
    /// </summary>
    private void DisplayServerConfiguration()
    {
        var host = m_Settings.Get().McpNexus.Server.Host ?? "0.0.0.0";
        var port = m_Settings.Get().McpNexus.Server.Port;

        m_Logger.Info("┌─ Server Configuration ─────────────────────────────────────────────");
        m_Logger.Info("│ Host: {Host}", host);
        m_Logger.Info("│ Port: {Port}", port);
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Displays transport m_Configuration information.
    /// </summary>
    private void DisplayTransportConfiguration()
    {
        var transportMode = m_Settings.Get().McpNexus.Transport.Mode ?? "http";

        m_Logger.Info("┌─ Transport Configuration ──────────────────────────────────────────");
        m_Logger.Info("│ Mode:         {TransportMode}", transportMode);
        m_Logger.Info("│ Service Mode: {ServiceMode}", m_IsServiceMode);
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Displays debugging configuration information.
    /// </summary>
    private void DisplayDebuggingConfiguration()
    {
        var cdbPath = m_Settings.Get().McpNexus.Debugging.CdbPath ?? string.Empty;
        var commandTimeout = m_Settings.Get().McpNexus.Debugging.CommandTimeoutMs;
        var symbolRetries = m_Settings.Get().McpNexus.Debugging.SymbolServerMaxRetries;
        var symbolPath = m_Settings.Get().McpNexus.Debugging.SymbolSearchPath ?? string.Empty;
        var startupDelay = m_Settings.Get().McpNexus.Debugging.StartupDelayMs;

        m_Logger.Info("┌─ Debugging Configuration ──────────────────────────────────────────");
        m_Logger.Info("│ CDB Path:                 {CdbPath}", string.IsNullOrEmpty(cdbPath) ? "Not specified" : cdbPath);
        m_Logger.Info("│ Command Timeout:          {CommandTimeout}ms", commandTimeout);
        m_Logger.Info("│ Symbol Server Retries:    {SymbolRetries}", symbolRetries);
        m_Logger.Info("│ Symbol Search Path:       {SymbolPath}", string.IsNullOrEmpty(symbolPath) ? "Not configured" : symbolPath);
        m_Logger.Info("│ Effective Symbol Path:    {SymbolPath}", string.IsNullOrEmpty(symbolPath) ? "Not configured" : symbolPath);
        m_Logger.Info("│ Startup Delay:            {StartupDelay}ms", startupDelay);
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Displays dump check (dumpchk) configuration information.
    /// </summary>
    private void DisplayValidationConfiguration()
    {
        var validationSettings = m_Settings.Get().McpNexus.Validation;
        var dumpChkPath = validationSettings.DumpChkPath ?? string.Empty;

        m_Logger.Info("┌─ Dump Check Configuration ─────────────────────────────────────────");
        m_Logger.Info("│ Dumpchk Enabled:          {DumpChkEnabled}", validationSettings.DumpChkEnabled);
        m_Logger.Info(
            "│ Dumpchk Path:             {DumpChkPath}",
            string.IsNullOrEmpty(dumpChkPath) ? "Auto-detect (Windows Kits debugger installation)" : dumpChkPath);
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Displays service configuration information.
    /// </summary>
    private void DisplayServiceConfiguration()
    {
        var installPath = m_Settings.Get().McpNexus.Service.InstallPath ?? string.Empty;
        var backupPath = m_Settings.Get().McpNexus.Service.BackupPath ?? string.Empty;

        if (!string.IsNullOrEmpty(installPath))
        {
            m_Logger.Info("┌─ Service Configuration ────────────────────────────────────────────");
            m_Logger.Info("│ Install Path: {InstallPath}", installPath);
            m_Logger.Info("│ Backup Path:  {BackupPath}", backupPath);
            m_Logger.Info(string.Empty);
        }
    }

    /// <summary>
    /// Displays logging configuration information.
    /// </summary>
    private void DisplayLoggingConfiguration()
    {
        var logLevel = m_Settings.Get().Logging.LogLevel ?? "Information";

        m_Logger.Info("┌─ Logging Configuration ────────────────────────────────────────────");
        m_Logger.Info("│ Log Level:         {LogLevel}", logLevel);
        m_Logger.Info("│ Environment:       {Environment}", m_IsServiceMode ? "Service" : "Development");
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Displays environment variables information.
    /// </summary>
    private void DisplayEnvironmentVariables()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var hasCdbInPath = pathEnv.Contains("cdb.exe", StringComparison.OrdinalIgnoreCase) ||
                          pathEnv.Contains("windbg", StringComparison.OrdinalIgnoreCase);

        m_Logger.Info("┌─ Environment Variables ────────────────────────────────────────────");
        m_Logger.Info("│ ASPNETCORE_ENVIRONMENT: {AspnetcoreEnvironment}", environment);
        m_Logger.Info("│ ASPNETCORE_URLS:        {AspnetcoreUrls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "Not set");

        var privateToken = ObfuscateSecret(Environment.GetEnvironmentVariable("PRIVATE_TOKEN"));

        m_Logger.Info("│ PRIVATE_TOKEN:          {PrivateToken}", privateToken);
        m_Logger.Info("│ CDB Paths in PATH:      {CdbInPath}", hasCdbInPath ? "CDB paths found in PATH" : "No CDB paths found in PATH");
        m_Logger.Info(string.Empty);
    }

    /// <summary>
    /// Obfuscates a secret value for safe logging by revealing only the first few characters.
    /// </summary>
    /// <param name="value">The secret value to obfuscate.</param>
    /// <returns>
    /// A string where only the first 10 characters are visible and the remaining characters
    /// are replaced with asterisks, or "Not set" when the value is null or empty.
    /// </returns>
    private static string ObfuscateSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Not set";
        }

        const int visibleCharacters = 10;
        if (value.Length <= visibleCharacters)
        {
            return new string('*', value.Length);
        }

        var prefix = value[..visibleCharacters];
        return prefix + new string('*', value.Length - visibleCharacters);
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

        m_Logger.Info("┌─ System Information ───────────────────────────────────────────────");
        m_Logger.Info("│ OS:              {OSDescription}", osDescription);
        m_Logger.Info("│ .NET Runtime:    {DotnetVersion}", dotnetVersion);
        m_Logger.Info("│ Machine Name:    {MachineName}", machineName);
        m_Logger.Info("│ User Account:    {UserAccount}", userAccount);
        m_Logger.Info("│ Processor Count: {ProcessorCount}", processorCount);
    }
}
