using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using NLog;

namespace mcp_nexus.Startup
{
    /// <summary>
    /// Handles logging of application configuration settings for debugging.
    /// </summary>
    public static class ConfigurationLogger
    {
        /// <summary>
        /// Logs the current configuration settings for debugging purposes.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="commandLineArgs">The parsed command line arguments.</param>
        public static void LogConfigurationSettings(IConfiguration configuration, CommandLineArguments commandLineArgs)
        {
            var logger = LogManager.GetCurrentClassLogger();


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
            logger.Info($"│ PRIVATE_TOKEN:          {MaskSecret(Environment.GetEnvironmentVariable("PRIVATE_TOKEN"))}");
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
        /// Masks a secret by keeping the first 5 characters and replacing the rest with '*'.
        /// Returns "Not set" when the input is null or empty.
        /// </summary>
        /// <param name="secret">The secret string to mask.</param>
        /// <returns>The masked secret.</returns>
        public static string MaskSecret(string? secret)
        {
            if (string.IsNullOrEmpty(secret))
            {
                return "Not set";
            }

            const int visiblePrefixLength = 5;
            if (secret.Length <= visiblePrefixLength)
            {
                return secret;
            }

            var prefix = secret.Substring(0, visiblePrefixLength);
            return prefix + new string('*', secret.Length - visiblePrefixLength);
        }

        /// <summary>
        /// Gets information about the CDB executable path for logging.
        /// </summary>
        /// <returns>A string describing the CDB path configuration.</returns>
        public static string GetCdbPathInfo()
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
        public static string GetProviderInfo(IConfigurationProvider provider)
        {
            try
            {
                // Try to get useful information about the provider
                var providerType = provider.GetType();

                if (providerType.Name.Contains("Json"))
                {
                    // Try to get the source property for JSON providers
                    var sourceProperty = providerType.GetProperty("Source");
                    if (sourceProperty?.GetValue(provider) is JsonConfigurationSource jsonSource)
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
    }
}

