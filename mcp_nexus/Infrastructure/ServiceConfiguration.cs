using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Configuration constants and settings for the Windows service
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServiceConfiguration
    {
        public const string ServiceName = "MCP-Nexus";
        public const string ServiceDisplayName = "MCP Nexus Server";
        public const string ServiceDescription = "Model Context Protocol server providing AI tool integration";
        public const string InstallFolder = @"C:\Program Files\MCP-Nexus";
        public const string ServiceArguments = "--service";

        // Timing constants
        public const int ServiceStopDelayMs = 2000;
        public const int ServiceStartDelayMs = 3000;
        public const int ServiceDeleteDelayMs = 3000;
        public const int ServiceCleanupDelayMs = 5000;

        // Retry constants
        public const int MaxRetryAttempts = 3;
        public const int RetryDelayMs = 2000;

        // File operation constants
        public const string ExecutableName = "mcp_nexus.exe";
        public const string BackupsFolderName = "backups";
        public const string ProjectFileName = "mcp_nexus.csproj";
        public const string BuildConfiguration = "Release";
        public const int MaxBackupsToKeep = 5;

        /// <summary>
        /// Gets the full path to the backups base folder (outside installation directory to avoid recursion)
        /// </summary>
        public static string BackupsBaseFolder => Path.Combine(Path.GetTempPath(), "MCP-Nexus-Backups");

        /// <summary>
        /// Gets the full path to the service executable
        /// </summary>
        public static string ExecutablePath => Path.Combine(InstallFolder, ExecutableName);

        /// <summary>
        /// Gets the full path to the backups folder
        /// </summary>
        public static string BackupsFolder => Path.Combine(InstallFolder, BackupsFolderName);

        /// <summary>
        /// Gets the service creation command arguments
        /// </summary>
        public static string GetCreateServiceCommand(string executablePath)
        {
            if (executablePath == null)
                throw new ArgumentNullException(nameof(executablePath));

            return $@"create ""{ServiceName}"" binPath= ""{executablePath} {ServiceArguments}"" " +
                   $@"start= auto DisplayName= ""{ServiceDisplayName}""";
        }

        /// <summary>
        /// Gets the service deletion command arguments
        /// </summary>
        public static string GetDeleteServiceCommand()
        {
            return $@"delete ""{ServiceName}""";
        }

        /// <summary>
        /// Gets the service start command arguments
        /// </summary>
        public static string GetServiceStartCommand()
        {
            return $@"start ""{ServiceName}""";
        }

        /// <summary>
        /// Gets the service stop command arguments
        /// </summary>
        public static string GetServiceStopCommand()
        {
            return $@"stop ""{ServiceName}""";
        }

        /// <summary>
        /// Gets a timestamped backup folder path
        /// </summary>
        public static string GetTimestampedBackupFolder()
        {
            return Path.Combine(BackupsFolder, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        }

        /// <summary>
        /// Gets the service description command arguments
        /// </summary>
        public static string GetServiceDescriptionCommand()
        {
            return $@"description ""{ServiceName}"" ""{ServiceDescription}""";
        }
    }
}
