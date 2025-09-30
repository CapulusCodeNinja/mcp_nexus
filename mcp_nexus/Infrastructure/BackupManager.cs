using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages backup operations for service installations
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class BackupManager
    {
        /// <summary>
        /// Creates a backup of the current installation
        /// </summary>
        /// <param name="logger">Optional logger for backup operations</param>
        /// <returns>Path to the created backup directory, or null if backup failed</returns>
        public static async Task<string?> CreateBackupAsync(ILogger? logger = null)
        {
            try
            {
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] CreateBackupAsync: Starting backup creation");
                Console.Error.Flush();
                
                if (!Directory.Exists(ServiceConfiguration.InstallFolder))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] CreateBackupAsync: Installation folder does not exist: {ServiceConfiguration.InstallFolder}");
                    Console.Error.Flush();
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Backup, "Installation folder does not exist, no backup needed");
                    return null;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] CreateBackupAsync: Installation folder exists: {ServiceConfiguration.InstallFolder}");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Backup, "Creating backup of current installation");

                // Create backup directory with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupDir = Path.Combine(ServiceConfiguration.BackupsBaseFolder, $"backup_{timestamp}");

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] CreateBackupAsync: Creating backup directory: {backupDir}");
                Console.Error.Flush();
                
                Directory.CreateDirectory(backupDir);
                OperationLogger.LogDebug(logger, OperationLogger.Operations.Backup, "Created backup directory: {BackupDir}", backupDir);

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] CreateBackupAsync: Starting file copy from {ServiceConfiguration.InstallFolder} to {backupDir}");
                Console.Error.Flush();
                
                // Copy installation files to backup
                await FileOperationsManager.CopyDirectoryAsync(ServiceConfiguration.InstallFolder, backupDir, logger);

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] CreateBackupAsync: File copy completed successfully");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Backup, "Backup created successfully: {BackupDir}", backupDir);
                return backupDir;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Backup, ex, "Exception during backup creation");
                return null;
            }
        }

        /// <summary>
        /// Cleans up old backup folders, keeping only the most recent ones
        /// </summary>
        /// <param name="maxBackupsToKeep">Maximum number of backups to keep</param>
        /// <param name="logger">Optional logger for cleanup operations</param>
        public static async Task CleanupOldBackupsAsync(int maxBackupsToKeep, ILogger? logger = null)
        {
            try
            {
                if (!Directory.Exists(ServiceConfiguration.BackupsBaseFolder))
                {
                    OperationLogger.LogDebug(logger, OperationLogger.Operations.Cleanup, "Backups folder does not exist, no cleanup needed");
                    await Task.CompletedTask;
                    return;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Cleaning up old backups, keeping {MaxBackups} most recent", maxBackupsToKeep);

                var backupDirs = Directory.GetDirectories(ServiceConfiguration.BackupsBaseFolder, "backup_*")
                    .Select(dir => new DirectoryInfo(dir))
                    .OrderByDescending(dir => dir.CreationTime)
                    .ToList();

                OperationLogger.LogDebug(logger, OperationLogger.Operations.Cleanup, "Found {BackupCount} backup directories", backupDirs.Count);

                if (backupDirs.Count <= maxBackupsToKeep)
                {
                    OperationLogger.LogDebug(logger, OperationLogger.Operations.Cleanup, "No cleanup needed, backup count within limit");
                    await Task.CompletedTask;
                    return;
                }

                // Delete old backups
                var backupsToDelete = backupDirs.Skip(maxBackupsToKeep);
                foreach (var backupDir in backupsToDelete)
                {
                    try
                    {
                        backupDir.Delete(recursive: true);
                        OperationLogger.LogDebug(logger, OperationLogger.Operations.Cleanup, "Deleted old backup: {BackupDir}", backupDir.Name);
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Cleanup, "Failed to delete backup {BackupDir}: {Error}", backupDir.Name, ex.Message);
                    }
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Cleanup, "Backup cleanup completed");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Cleanup, ex, "Exception during backup cleanup");
            }
        }

        /// <summary>
        /// Gets information about existing backups
        /// </summary>
        /// <param name="logger">Optional logger for operations</param>
        /// <returns>List of backup information</returns>
        public static List<BackupInfo> GetBackupInfo(ILogger? logger = null)
        {
            try
            {
                if (!Directory.Exists(ServiceConfiguration.BackupsBaseFolder))
                {
                    return new List<BackupInfo>();
                }

                var backupDirs = Directory.GetDirectories(ServiceConfiguration.BackupsBaseFolder, "backup_*")
                    .Select(dir => new DirectoryInfo(dir))
                    .OrderByDescending(dir => dir.CreationTime)
                    .Select(dir => new BackupInfo
                    {
                        Path = dir.FullName,
                        Name = dir.Name,
                        CreationTime = dir.CreationTime,
                        SizeBytes = GetDirectorySize(dir.FullName)
                    })
                    .ToList();

                return backupDirs;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Backup, ex, "Exception getting backup info");
                return new List<BackupInfo>();
            }
        }

        /// <summary>
        /// Calculates the total size of a directory
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <returns>Total size in bytes</returns>
        private static long GetDirectorySize(string directoryPath)
        {
            try
            {
                return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Information about a backup
    /// </summary>
    public class BackupInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreationTime { get; set; }
        public long SizeBytes { get; set; }
    }
}
