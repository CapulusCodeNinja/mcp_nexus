using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Configuration for Windows service installation and management
    /// </summary>
    public class ServiceConfiguration
    {
        public string ServiceName { get; set; } = "MCP-Nexus";
        public string DisplayName { get; set; } = "MCP Nexus Service";
        public string Description { get; set; } = "MCP Nexus Debugging Service";
        public string ExecutablePath { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public string ProjectFileName { get; set; } = "mcp_nexus.csproj";
        public string[] Dependencies { get; set; } = Array.Empty<string>();
        public ServiceStartType StartType { get; set; } = ServiceStartType.Automatic;
        public ServiceAccount Account { get; set; } = ServiceAccount.LocalService;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool DelayedStart { get; set; } = false;
        public int RestartDelay { get; set; } = 5000;
        public int MaxRestartAttempts { get; set; } = 3;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public string[] RequiredPermissions { get; set; } = Array.Empty<string>();
        public bool EnableEventLogging { get; set; } = true;
        public string LogLevel { get; set; } = "Information";
        public string[] AllowedUsers { get; set; } = Array.Empty<string>();
        public bool RequireAdministrator { get; set; } = true;
        public string BackupPath { get; set; } = string.Empty;
        public int BackupRetentionDays { get; set; } = 30;
        public bool EnableHealthChecks { get; set; } = true;
        public int HealthCheckInterval { get; set; } = 30000;
        public string[] HealthCheckEndpoints { get; set; } = Array.Empty<string>();
        public bool EnableMetrics { get; set; } = true;
        public string MetricsEndpoint { get; set; } = string.Empty;
        public bool EnableTracing { get; set; } = false;
        public string TracingEndpoint { get; set; } = string.Empty;
        public Dictionary<string, object> CustomSettings { get; set; } = new();

        // Additional properties expected by tests (instance properties)
        public string ServiceDisplayName { get; set; } = "MCP Nexus Service";
        public string ServiceDescription { get; set; } = "MCP Nexus Debugging Service";
        public string InstallFolder { get; set; } = @"C:\Program Files\MCP-Nexus";
        public string ServiceArguments { get; set; } = string.Empty;
        public int ServiceStopDelayMs { get; set; } = 5000;
        public int ServiceStartDelayMs { get; set; } = 2000;
        public int ServiceDeleteDelayMs { get; set; } = 1000;
        public int ServiceCleanupDelayMs { get; set; } = 3000;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
        public string ExecutableName { get; set; } = "mcp_nexus.exe";
        public string BackupsFolderName { get; set; } = "backups";
        public string BuildConfiguration { get; set; } = "Release";
        public int MaxBackupsToKeep { get; set; } = 10;
        public string BackupsBaseFolder { get; set; } = @"C:\ProgramData\MCP-Nexus\Backups";

        // Computed properties
        public string BackupsFolder => Path.Combine(InstallFolder, BackupsFolderName);

        // Static properties for test compatibility (only unique ones)
        // Note: Instance properties are used for most cases, static access via new ServiceConfiguration()

        // Static methods expected by tests
        public static string GetCreateServiceCommand(string serviceName, string displayName, string description, string executablePath)
        {
            return $"sc create \"{serviceName}\" binPath=\"{executablePath}\" DisplayName=\"{displayName}\" start=auto";
        }

        public static string GetDeleteServiceCommand(string serviceName)
        {
            return $"sc delete \"{serviceName}\"";
        }

        public static string GetServiceStartCommand(string serviceName)
        {
            return $"sc start \"{serviceName}\"";
        }

        public static string GetServiceStopCommand(string serviceName)
        {
            return $"sc stop \"{serviceName}\"";
        }

        public static string GetServiceDescriptionCommand(string serviceName, string description)
        {
            return $"sc description \"{serviceName}\" \"{description}\"";
        }

        public static string GetTimestampedBackupFolder(string basePath)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(basePath, $"backup_{timestamp}");
        }
    }

    public enum ServiceStartType
    {
        Automatic,
        Manual,
        Disabled,
        Boot,
        System
    }

    public enum ServiceAccount
    {
        LocalService,
        NetworkService,
        LocalSystem,
        User
    }
}
