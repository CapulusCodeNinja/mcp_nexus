using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;

namespace mcp_nexus.Infrastructure.Installation
{
    /// <summary>
    /// Configuration for Windows service installation and management.
    /// Provides comprehensive settings for service deployment, security, monitoring, and maintenance.
    /// </summary>
    public class ServiceConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the Windows service.
        /// </summary>
        public string ServiceName { get; set; } = "MCP-Nexus";

        /// <summary>
        /// Gets or sets the display name shown in the Windows Services console.
        /// </summary>
        public string DisplayName { get; set; } = "MCP Nexus Service";

        /// <summary>
        /// Gets or sets the description of the service shown in the Windows Services console.
        /// </summary>
        public string Description { get; set; } = "MCP Nexus Debugging Service";

        /// <summary>
        /// Gets or sets the working directory for the service executable.
        /// </summary>
        public string WorkingDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the project file for building the service.
        /// </summary>
        public string ProjectFileName { get; set; } = "mcp_nexus.csproj";

        /// <summary>
        /// Gets or sets the list of service dependencies.
        /// </summary>
        public string[] Dependencies { get; set; } = [];

        /// <summary>
        /// Gets or sets the start type of the service (Automatic, Manual, Disabled, etc.).
        /// </summary>
        public ServiceStartType StartType { get; set; } = ServiceStartType.Automatic;

        /// <summary>
        /// Gets or sets the account under which the service runs.
        /// </summary>
        public ServiceAccount Account { get; set; } = ServiceAccount.LocalService;

        /// <summary>
        /// Gets or sets the username for the service account (when Account is User).
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password for the service account (when Account is User).
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the service should start with a delay.
        /// </summary>
        public bool DelayedStart { get; set; } = false;

        /// <summary>
        /// Gets or sets the delay in milliseconds before restarting the service after failure.
        /// </summary>
        public int RestartDelay { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the maximum number of restart attempts before giving up.
        /// </summary>
        public int MaxRestartAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets environment variables to be set for the service.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of required permissions for the service.
        /// </summary>
        public string[] RequiredPermissions { get; set; } = [];

        /// <summary>
        /// Gets or sets whether event logging is enabled for the service.
        /// </summary>
        public bool EnableEventLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets the log level for the service (Trace, Debug, Information, Warning, Error, Critical).
        /// </summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets the list of users allowed to control the service.
        /// </summary>
        public string[] AllowedUsers { get; set; } = [];

        /// <summary>
        /// Gets or sets whether administrator privileges are required for service operations.
        /// </summary>
        public bool RequireAdministrator { get; set; } = true;

        /// <summary>
        /// Gets or sets the path where service backups are stored.
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of days to retain service backups.
        /// </summary>
        public int BackupRetentionDays { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether health checks are enabled for the service.
        /// </summary>
        public bool EnableHealthChecks { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval in milliseconds between health checks.
        /// </summary>
        public int HealthCheckInterval { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the list of health check endpoints to monitor.
        /// </summary>
        public string[] HealthCheckEndpoints { get; set; } = [];

        /// <summary>
        /// Gets or sets whether metrics collection is enabled for the service.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets the endpoint URL for metrics collection.
        /// </summary>
        public string MetricsEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether distributed tracing is enabled for the service.
        /// </summary>
        public bool EnableTracing { get; set; } = false;

        /// <summary>
        /// Gets or sets the endpoint URL for distributed tracing.
        /// </summary>
        public string TracingEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets custom settings for the service configuration.
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = [];

        // Additional properties expected by tests (instance properties)
        /// <summary>
        /// Gets or sets the display name for the service (alternative to DisplayName).
        /// </summary>
        public string ServiceDisplayName { get; set; } = "MCP Nexus Server";

        /// <summary>
        /// Gets or sets the description for the service (alternative to Description).
        /// </summary>
        public string ServiceDescription { get; set; } = "Model Context Protocol server providing AI tool integration";

        /// <summary>
        /// Gets or sets the installation folder path for the service.
        /// </summary>
        public string InstallFolder { get; set; } = @"C:\Program Files\MCP-Nexus";

        /// <summary>
        /// Gets or sets the command line arguments passed to the service executable.
        /// </summary>
        public string ServiceArguments { get; set; } = "--service";

        /// <summary>
        /// Gets or sets the delay in milliseconds when stopping the service.
        /// </summary>
        public int ServiceStopDelayMs { get; set; } = 2000;

        /// <summary>
        /// Gets or sets the delay in milliseconds when starting the service.
        /// </summary>
        public int ServiceStartDelayMs { get; set; } = 3000;

        /// <summary>
        /// Gets or sets the delay in milliseconds when deleting the service.
        /// </summary>
        public int ServiceDeleteDelayMs { get; set; } = 3000;

        /// <summary>
        /// Gets or sets the delay in milliseconds for service cleanup operations.
        /// </summary>
        public int ServiceCleanupDelayMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for service operations.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay in milliseconds between retry attempts.
        /// </summary>
        public int RetryDelayMs { get; set; } = 2000;

        /// <summary>
        /// Gets or sets the name of the service executable file.
        /// </summary>
        public string ExecutableName { get; set; } = "mcp_nexus.exe";

        /// <summary>
        /// Gets or sets the name of the backups folder within the installation directory.
        /// </summary>
        public string BackupsFolderName { get; set; } = "backups";

        /// <summary>
        /// Gets or sets the build configuration (Debug, Release, etc.).
        /// </summary>
        public string BuildConfiguration { get; set; } = "Release";

        /// <summary>
        /// Gets or sets the maximum number of backup files to keep.
        /// </summary>
        public int MaxBackupsToKeep { get; set; } = 5;

        /// <summary>
        /// Gets or sets the base folder path for storing service backups.
        /// </summary>
        public string BackupsBaseFolder { get; set; } = Path.Combine(Path.GetTempPath(), "MCP-Nexus-Backups");

        // Computed properties
        /// <summary>
        /// Gets the full path to the service executable file.
        /// </summary>
        public string ExecutablePath => Path.Combine(InstallFolder, ExecutableName);

        /// <summary>
        /// Gets the full path to the service backups folder.
        /// </summary>
        public string BackupsFolder => Path.Combine(InstallFolder, BackupsFolderName);

        // Static properties for test compatibility (only unique ones)
        // Note: Instance properties are used for most cases, static access via new ServiceConfiguration()

        // Static methods expected by tests
        /// <summary>
        /// Generates a Windows Service Control (sc) command to create a service.
        /// </summary>
        /// <param name="serviceName">The name of the service to create.</param>
        /// <param name="displayName">The display name of the service.</param>
        /// <param name="description">The description of the service.</param>
        /// <param name="executablePath">The path to the service executable.</param>
        /// <returns>A formatted sc create command string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="executablePath"/> is null.</exception>
        public static string GetCreateServiceCommand(string serviceName, string displayName, string description, string executablePath)
        {
            ArgumentNullException.ThrowIfNull(executablePath);

            var config = new ServiceConfiguration();
            return $"sc create \"{serviceName}\" binPath=\"{executablePath} {config.ServiceArguments}\" DisplayName=\"{config.ServiceDisplayName}\" start= auto";
        }

        /// <summary>
        /// Generates a Windows Service Control (sc) command to delete a service.
        /// </summary>
        /// <param name="serviceName">The name of the service to delete.</param>
        /// <returns>A formatted sc delete command string.</returns>
        public static string GetDeleteServiceCommand(string serviceName)
        {
            return $"delete \"{serviceName}\"";
        }

        /// <summary>
        /// Generates a Windows Service Control (sc) command to start a service.
        /// </summary>
        /// <param name="serviceName">The name of the service to start.</param>
        /// <returns>A formatted sc start command string.</returns>
        public static string GetServiceStartCommand(string serviceName)
        {
            return $"start \"{serviceName}\"";
        }

        /// <summary>
        /// Generates a Windows Service Control (sc) command to stop a service.
        /// </summary>
        /// <param name="serviceName">The name of the service to stop.</param>
        /// <returns>A formatted sc stop command string.</returns>
        public static string GetServiceStopCommand(string serviceName)
        {
            return $"stop \"{serviceName}\"";
        }

        /// <summary>
        /// Generates a Windows Service Control (sc) command to set a service description.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="description">The description to set for the service.</param>
        /// <returns>A formatted sc description command string.</returns>
        public static string GetServiceDescriptionCommand(string serviceName, string description)
        {
            var config = new ServiceConfiguration();
            return $"sc description \"{serviceName}\" \"{config.ServiceDescription}\"";
        }

        /// <summary>
        /// Generates a timestamped backup folder path.
        /// </summary>
        /// <param name="basePath">The base path for the backup folder.</param>
        /// <returns>A timestamped backup folder path.</returns>
        public static string GetTimestampedBackupFolder(string basePath)
        {
            var config = new ServiceConfiguration();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(config.BackupsFolder, $"{timestamp}");
        }
    }

    /// <summary>
    /// Specifies the start type of a Windows service.
    /// </summary>
    public enum ServiceStartType
    {
        /// <summary>
        /// Service starts automatically when the system starts.
        /// </summary>
        Automatic,

        /// <summary>
        /// Service must be started manually.
        /// </summary>
        Manual,

        /// <summary>
        /// Service is disabled and cannot be started.
        /// </summary>
        Disabled,

        /// <summary>
        /// Service starts during system boot (device drivers).
        /// </summary>
        Boot,

        /// <summary>
        /// Service starts during system startup (system services).
        /// </summary>
        System
    }

    /// <summary>
    /// Specifies the account type under which a Windows service runs.
    /// </summary>
    public enum ServiceAccount
    {
        /// <summary>
        /// Service runs under the Local Service account.
        /// </summary>
        LocalService,

        /// <summary>
        /// Service runs under the Network Service account.
        /// </summary>
        NetworkService,

        /// <summary>
        /// Service runs under the Local System account.
        /// </summary>
        LocalSystem,

        /// <summary>
        /// Service runs under a specific user account.
        /// </summary>
        User
    }
}
