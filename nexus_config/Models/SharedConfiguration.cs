namespace Nexus.Config.Models;

/// <summary>
/// Shared configuration settings for all Nexus libraries.
/// </summary>
public class SharedConfiguration
{
    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the MCP Nexus specific settings.
    /// </summary>
    public McpNexusSettings McpNexus { get; set; } = new();

    /// <summary>
    /// Gets or sets the IP rate limiting settings.
    /// </summary>
    public IpRateLimitingSettings IpRateLimiting { get; set; } = new();
}

/// <summary>
/// Logging configuration settings.
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string LogLevel { get; set; } = "Information";
}

/// <summary>
/// MCP Nexus specific configuration settings.
/// </summary>
public class McpNexusSettings
{
    /// <summary>
    /// Gets or sets the server configuration.
    /// </summary>
    public ServerSettings Server { get; set; } = new();

    /// <summary>
    /// Gets or sets the transport configuration.
    /// </summary>
    public TransportSettings Transport { get; set; } = new();

    /// <summary>
    /// Gets or sets the debugging configuration.
    /// </summary>
    public DebuggingSettings Debugging { get; set; } = new();

    /// <summary>
    /// Gets or sets the automated recovery configuration.
    /// </summary>
    public AutomatedRecoverySettings AutomatedRecovery { get; set; } = new();

    /// <summary>
    /// Gets or sets the service configuration.
    /// </summary>
    public ServiceSettings Service { get; set; } = new();

    /// <summary>
    /// Gets or sets the session management configuration.
    /// </summary>
    public SessionManagementSettings SessionManagement { get; set; } = new();

    /// <summary>
    /// Gets or sets the extensions settings.
    /// </summary>
    public ExtensionsSettings Extensions { get; set; } = new();

    /// <summary>
    /// Gets or sets the batching settings.
    /// </summary>
    public BatchingSettings Batching { get; set; } = new();
}

/// <summary>
/// Server configuration settings.
/// </summary>
public class ServerSettings
{
    /// <summary>
    /// Gets or sets the server host.
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// Gets or sets the server port.
    /// </summary>
    public int Port { get; set; } = 5511;
}

/// <summary>
/// Transport configuration settings.
/// </summary>
public class TransportSettings
{
    /// <summary>
    /// Gets or sets the transport mode.
    /// </summary>
    public string Mode { get; set; } = "http";

    /// <summary>
    /// Gets or sets whether service mode is enabled.
    /// </summary>
    public bool ServiceMode { get; set; } = true;
}

/// <summary>
/// Debugging configuration settings.
/// </summary>
public class DebuggingSettings
{
    /// <summary>
    /// Gets or sets the CDB path.
    /// </summary>
    public string? CdbPath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the command timeout in milliseconds.
    /// </summary>
    public int CommandTimeoutMs { get; set; } = 600000;

    /// <summary>
    /// Gets or sets the idle timeout in milliseconds.
    /// </summary>
    public int IdleTimeoutMs { get; set; } = 300000;

    /// <summary>
    /// Gets or sets the symbol server max retries.
    /// </summary>
    public int SymbolServerMaxRetries { get; set; } = 1;

    /// <summary>
    /// Gets or sets the symbol search path.
    /// </summary>
    public string SymbolSearchPath { get; set; } = "srv*T:\\symbols*https://symbols.int.avast.com/symbols;srv*T:\\symbols*https://msdl.microsoft.com/download/symbols";

    /// <summary>
    /// Gets or sets the startup delay in milliseconds.
    /// </summary>
    public int StartupDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the output reading timeout in milliseconds.
    /// </summary>
    public int OutputReadingTimeoutMs { get; set; } = 300000;

    /// <summary>
    /// Gets or sets whether command preprocessing is enabled.
    /// </summary>
    public bool EnableCommandPreprocessing { get; set; } = true;
}

/// <summary>
/// Automated recovery configuration settings.
/// </summary>
public class AutomatedRecoverySettings
{
    /// <summary>
    /// Gets or sets the default command timeout in minutes.
    /// </summary>
    public int DefaultCommandTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Gets the default command timeout as a TimeSpan.
    /// </summary>
    /// <returns>The default command timeout.</returns>
    public TimeSpan GetDefaultCommandTimeout()
    {
        return TimeSpan.FromMinutes(DefaultCommandTimeoutMinutes);
    }

    /// <summary>
    /// Gets or sets the complex command timeout in minutes.
    /// </summary>
    public int ComplexCommandTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum command timeout in minutes.
    /// </summary>
    public int MaxCommandTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum recovery attempts.
    /// </summary>
    public int MaxRecoveryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the recovery delay in seconds.
    /// </summary>
    public int RecoveryDelaySeconds { get; set; } = 5;
}

/// <summary>
/// Service configuration settings.
/// </summary>
public class ServiceSettings
{
    /// <summary>
    /// Gets or sets the service install path.
    /// </summary>
    public string InstallPath { get; set; } = "C:\\Program Files\\MCP-Nexus";

    /// <summary>
    /// Gets or sets the service backup path.
    /// </summary>
    public string BackupPath { get; set; } = "C:\\Program Files\\MCP-Nexus\\backups";

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = "MCP-Nexus";

    /// <summary>
    /// Gets or sets the service display name.
    /// </summary>
    public string DisplayName { get; set; } = "MCP-Nexus Debugging Server";
}

/// <summary>
/// Session management configuration settings.
/// </summary>
public class SessionManagementSettings
{
    /// <summary>
    /// Gets or sets the maximum concurrent sessions.
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the session timeout in minutes.
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the cleanup interval in minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Gets the cleanup interval as a TimeSpan.
    /// </summary>
    /// <returns>The cleanup interval.</returns>
    public TimeSpan GetCleanupInterval()
    {
        return TimeSpan.FromMinutes(CleanupIntervalMinutes);
    }

    /// <summary>
    /// Gets or sets the disposal timeout in seconds.
    /// </summary>
    public int DisposalTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the default command timeout in minutes.
    /// </summary>
    public int DefaultCommandTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the memory cleanup threshold in MB.
    /// </summary>
    public int MemoryCleanupThresholdMB { get; set; } = 1024;
}

/// <summary>
/// Extensions configuration settings.
/// </summary>
public class ExtensionsSettings
{
    /// <summary>
    /// Gets or sets whether extensions are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the extensions path.
    /// </summary>
    public string ExtensionsPath { get; set; } = "extensions";

    /// <summary>
    /// Gets or sets the callback port.
    /// </summary>
    public int CallbackPort { get; set; } = 0;

    /// <summary>
    /// Gets or sets the graceful termination timeout in milliseconds.
    /// </summary>
    public int GracefulTerminationTimeoutMs { get; set; } = 2000;
}

/// <summary>
/// IP rate limiting configuration settings.
/// </summary>
public class IpRateLimitingSettings
{
    /// <summary>
    /// Gets or sets whether endpoint rate limiting is enabled.
    /// </summary>
    public bool EnableEndpointRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to stack blocked requests.
    /// </summary>
    public bool StackBlockedRequests { get; set; } = false;

    /// <summary>
    /// Gets or sets the real IP header.
    /// </summary>
    public string RealIpHeader { get; set; } = "X-Real-IP";

    /// <summary>
    /// Gets or sets the client ID header.
    /// </summary>
    public string ClientIdHeader { get; set; } = "X-ClientId";

    /// <summary>
    /// Gets or sets the general rules.
    /// </summary>
    public List<RateLimitRule> GeneralRules { get; set; } = new();
}

/// <summary>
/// Rate limit rule configuration.
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Gets or sets the endpoint pattern.
    /// </summary>
    public string Endpoint { get; set; } = "*:/";

    /// <summary>
    /// Gets or sets the period.
    /// </summary>
    public string Period { get; set; } = "1m";

    /// <summary>
    /// Gets or sets the limit.
    /// </summary>
    public int Limit { get; set; } = 100;
}

/// <summary>
/// Command batching configuration settings.
/// </summary>
public class BatchingSettings
{
    /// <summary>
    /// Gets or sets whether batching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum batch size.
    /// </summary>
    public int MaxBatchSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum batch size.
    /// </summary>
    public int MinBatchSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets the batch wait timeout in milliseconds.
    /// </summary>
    public int BatchWaitTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the batch timeout multiplier.
    /// </summary>
    public double BatchTimeoutMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the maximum batch timeout in minutes.
    /// </summary>
    public int MaxBatchTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the list of excluded commands from batching.
    /// </summary>
    public List<string> ExcludedCommands { get; set; } = new();
}
