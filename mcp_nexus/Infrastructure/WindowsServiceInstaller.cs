using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Refactored Windows service installer that orchestrates focused service management components
    /// Provides a clean public API while delegating to specialized classes for actual operations
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowsServiceInstaller
    {
        // Backward compatibility constants (delegated to ServiceConfiguration)
        private const string ServiceName = "MCP-Nexus";
        private const string ServiceDisplayName = "MCP Nexus Server";
        private const string ServiceDescription = "Model Context Protocol server providing AI tool integration";
        private const string InstallFolder = @"C:\Program Files\MCP-Nexus";

        // Backward compatibility private methods for tests
        private static async Task<bool> BuildProjectForDeploymentAsync(ILogger? logger = null)
        {
            return await ServiceFileManager.BuildProjectForDeploymentAsync(logger);
        }

        private static string? FindProjectDirectory(string startPath)
        {
            return ServiceFileManager.FindProjectDirectory(startPath);
        }

        private static async Task CopyApplicationFilesAsync(ILogger? logger = null)
        {
            await ServiceFileManager.CopyApplicationFilesAsync(logger);
        }

        private static async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            await ServiceFileManager.CopyDirectoryAsync(sourceDir, targetDir);
        }

        private static async Task<bool> ForceCleanupServiceAsync(ILogger? logger = null)
        {
            return await ServiceRegistryManager.ForceCleanupServiceAsync(logger);
        }

        private static async Task<bool> DirectRegistryCleanupAsync(ILogger? logger = null)
        {
            return await ServiceRegistryManager.DirectRegistryCleanupAsync(logger);
        }

        private static async Task<bool> RunScCommandAsync(string arguments, ILogger? logger = null, bool allowFailure = false)
        {
            return await ServiceRegistryManager.RunScCommandAsync(arguments, logger, allowFailure);
        }

        private static bool IsServiceInstalled()
        {
            return ServiceRegistryManager.IsServiceInstalled();
        }

        private static bool IsRunAsAdministrator()
        {
            return ServicePermissionValidator.IsRunAsAdministrator();
        }
        /// <summary>
        /// Installs the MCP Nexus Windows service
        /// </summary>
        /// <param name="logger">Optional logger for installation operations</param>
        /// <returns>True if installation was successful, false otherwise</returns>
        public static async Task<bool> InstallServiceAsync(ILogger? logger = null)
        {
            return await ServiceInstallationOrchestrator.InstallServiceAsync(logger);
        }

        /// <summary>
        /// Uninstalls the MCP Nexus Windows service
        /// </summary>
        /// <param name="logger">Optional logger for uninstallation operations</param>
        /// <returns>True if uninstallation was successful, false otherwise</returns>
        public static async Task<bool> UninstallServiceAsync(ILogger? logger = null)
        {
            return await ServiceInstallationOrchestrator.UninstallServiceAsync(logger);
        }

        /// <summary>
        /// Force uninstalls the MCP Nexus Windows service with aggressive cleanup
        /// </summary>
        /// <param name="logger">Optional logger for force uninstallation operations</param>
        /// <returns>True if force uninstallation was successful, false otherwise</returns>
        public static async Task<bool> ForceUninstallServiceAsync(ILogger? logger = null)
        {
            return await ServiceInstallationOrchestrator.ForceUninstallServiceAsync(logger);
        }

        /// <summary>
        /// Updates the MCP Nexus Windows service
        /// </summary>
        /// <param name="logger">Optional logger for update operations</param>
        /// <returns>True if update was successful, false otherwise</returns>
        public static async Task<bool> UpdateServiceAsync(ILogger? logger = null)
        {
            return await ServiceInstallationOrchestrator.UpdateServiceAsync(logger);
        }

        /// <summary>
        /// Validates that all required installation files are present
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if all required files exist, false otherwise</returns>
        public static async Task<bool> ValidateInstallationFilesAsync(ILogger? logger = null)
        {
            return await Task.FromResult(ServiceFileManager.ValidateInstallationFiles(logger));
        }

        /// <summary>
        /// Creates a backup of the current installation
        /// </summary>
        /// <param name="logger">Optional logger for backup operations</param>
        /// <returns>True if backup was successful, false otherwise</returns>
        public static async Task<bool> CreateBackupAsync(ILogger? logger = null)
        {
            var result = await ServiceFileManager.CreateBackupAsync(logger);
            return !string.IsNullOrEmpty(result);
        }

        /// <summary>
        /// Cleans up old backup folders, keeping only the most recent ones
        /// </summary>
        /// <param name="logger">Optional logger for cleanup operations</param>
        /// <returns>Task representing the cleanup operation</returns>
        public static async Task<bool> CleanupOldBackupsAsync(ILogger? logger = null)
        {
            await ServiceFileManager.CleanupOldBackupsAsync(5, logger);
            return true; // ServiceFileManager.CleanupOldBackupsAsync doesn't return a value, assume success
        }
    }
}
