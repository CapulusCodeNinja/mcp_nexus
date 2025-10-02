using System;
using System.Collections.Generic;
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
