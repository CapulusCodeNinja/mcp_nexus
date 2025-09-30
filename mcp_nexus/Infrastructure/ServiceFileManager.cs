using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Orchestrates file operations for service installation, including building, copying, and backup operations
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServiceFileManager
    {
        /// <summary>
        /// Builds the project in Release mode for deployment
        /// </summary>
        /// <param name="logger">Optional logger for build operations</param>
        /// <returns>True if build was successful, false otherwise</returns>
        public static async Task<bool> BuildProjectForDeploymentAsync(ILogger? logger = null)
        {
            return await ProjectBuilder.BuildProjectForDeploymentAsync(logger);
        }

        /// <summary>
        /// Finds the project directory containing the .csproj file
        /// </summary>
        /// <param name="startPath">The starting path to search from</param>
        /// <returns>The project directory path, or null if not found</returns>
        public static string? FindProjectDirectory(string startPath)
        {
            return ProjectBuilder.FindProjectDirectory(startPath);
        }

        /// <summary>
        /// Copies application files from the build output to the installation directory
        /// </summary>
        /// <param name="logger">Optional logger for copy operations</param>
        /// <returns>Task representing the copy operation</returns>
        public static async Task CopyApplicationFilesAsync(ILogger? logger = null)
        {
            await FileOperationsManager.CopyApplicationFilesAsync(logger);
        }

        /// <summary>
        /// Recursively copies a directory and all its contents
        /// </summary>
        /// <param name="sourceDir">Source directory path</param>
        /// <param name="targetDir">Target directory path</param>
        /// <param name="logger">Optional logger for copy operations</param>
        /// <returns>Task representing the copy operation</returns>
        public static async Task CopyDirectoryAsync(string sourceDir, string targetDir, ILogger? logger = null)
        {
            await FileOperationsManager.CopyDirectoryAsync(sourceDir, targetDir, logger);
        }

        /// <summary>
        /// Creates a backup of the current installation
        /// </summary>
        /// <param name="logger">Optional logger for backup operations</param>
        /// <returns>The backup directory path if successful, null otherwise</returns>
        public static async Task<string?> CreateBackupAsync(ILogger? logger = null)
        {
            return await BackupManager.CreateBackupAsync(logger);
        }

        /// <summary>
        /// Cleans up old backup folders, keeping only the most recent ones
        /// </summary>
        /// <param name="maxBackupsToKeep">Maximum number of backups to retain</param>
        /// <param name="logger">Optional logger for cleanup operations</param>
        /// <returns>Task representing the cleanup operation</returns>
        public static async Task CleanupOldBackupsAsync(int maxBackupsToKeep = 5, ILogger? logger = null)
        {
            await BackupManager.CleanupOldBackupsAsync(maxBackupsToKeep, logger);
        }

        /// <summary>
        /// Validates that all required files exist in the installation directory
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if all required files exist, false otherwise</returns>
        public static bool ValidateInstallationFiles(ILogger? logger = null)
        {
            return FileOperationsManager.ValidateInstallationFiles(logger);
        }

        /// <summary>
        /// Gets information about existing backups
        /// </summary>
        /// <param name="logger">Optional logger for operations</param>
        /// <returns>List of backup information</returns>
        public static List<BackupInfo> GetBackupInfo(ILogger? logger = null)
        {
            return BackupManager.GetBackupInfo(logger);
        }
    }
}