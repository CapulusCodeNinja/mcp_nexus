using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages backup operations for the MCP Nexus service
    /// </summary>
    public class BackupManager
    {
        private readonly ILogger<BackupManager> _logger;
        private readonly string _backupDirectory;

        public BackupManager(ILogger<BackupManager> logger, string? backupDirectory = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupDirectory = backupDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCP-Nexus", "Backups");
        }

        /// <summary>
        /// Creates a backup of the specified files
        /// </summary>
        /// <param name="sourceFiles">Files to backup</param>
        /// <param name="backupName">Name for the backup</param>
        /// <returns>Path to the created backup</returns>
        public async Task<string> CreateBackupAsync(IEnumerable<string> sourceFiles, string? backupName = null)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupNameWithTimestamp = string.IsNullOrEmpty(backupName) ? $"backup_{timestamp}" : $"{backupName}_{timestamp}";
                var backupPath = Path.Combine(_backupDirectory, backupNameWithTimestamp);

                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                foreach (var sourceFile in sourceFiles)
                {
                    if (File.Exists(sourceFile))
                    {
                        var fileName = Path.GetFileName(sourceFile);
                        var destinationFile = Path.Combine(backupPath, fileName);
                        File.Copy(sourceFile, destinationFile, true);
                        _logger.LogInformation("Backed up file: {SourceFile} to {DestinationFile}", sourceFile, destinationFile);
                    }
                }

                _logger.LogInformation("Backup created successfully: {BackupPath}", backupPath);
                await Task.CompletedTask; // Fix CS1998 warning
                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup");
                throw;
            }
        }

        /// <summary>
        /// Restores files from a backup
        /// </summary>
        /// <param name="backupPath">Path to the backup</param>
        /// <param name="destinationDirectory">Directory to restore files to</param>
        /// <returns>True if restore was successful</returns>
        public async Task<bool> RestoreBackupAsync(string backupPath, string destinationDirectory)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    _logger.LogError("Backup directory does not exist: {BackupPath}", backupPath);
                    return false;
                }

                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                var files = Directory.GetFiles(backupPath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var destinationFile = Path.Combine(destinationDirectory, fileName);
                    File.Copy(file, destinationFile, true);
                    _logger.LogInformation("Restored file: {SourceFile} to {DestinationFile}", file, destinationFile);
                }

                _logger.LogInformation("Backup restored successfully from {BackupPath} to {DestinationDirectory}", backupPath, destinationDirectory);
                await Task.CompletedTask; // Fix CS1998 warning
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore backup from {BackupPath}", backupPath);
                return false;
            }
        }

        /// <summary>
        /// Lists all available backups
        /// </summary>
        /// <returns>List of backup directories</returns>
        public async Task<IEnumerable<string>> ListBackupsAsync()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    return new List<string>();
                }

                var backupDirectories = Directory.GetDirectories(_backupDirectory);
                _logger.LogInformation("Found {Count} backup directories", backupDirectories.Length);
                await Task.CompletedTask; // Fix CS1998 warning
                return backupDirectories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list backups");
                return new List<string>();
            }
        }

        /// <summary>
        /// Deletes old backups based on retention policy
        /// </summary>
        /// <param name="retentionDays">Number of days to retain backups</param>
        /// <returns>Number of backups deleted</returns>
        public async Task<int> CleanupOldBackupsAsync(int retentionDays = 30)
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    return 0;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var deletedCount = 0;

                var backupDirectories = Directory.GetDirectories(_backupDirectory);
                foreach (var backupDir in backupDirectories)
                {
                    var dirInfo = new DirectoryInfo(backupDir);
                    if (dirInfo.CreationTimeUtc < cutoffDate)
                    {
                        Directory.Delete(backupDir, true);
                        deletedCount++;
                        _logger.LogInformation("Deleted old backup: {BackupDirectory}", backupDir);
                    }
                }

                _logger.LogInformation("Cleaned up {DeletedCount} old backup directories", deletedCount);
                await Task.CompletedTask; // Fix CS1998 warning
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old backups");
                return 0;
            }
        }

        /// <summary>
        /// Gets the backup directory path
        /// </summary>
        public string BackupDirectory => _backupDirectory;

        // Static methods for compatibility with existing code
        public static async Task<string> CreateBackupAsync(ILogger logger)
        {
            var backupManager = new BackupManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<BackupManager>.Instance);
            return await backupManager.CreateBackupAsync(new List<string>());
        }

        public static async Task<int> CleanupOldBackupsAsync(int retentionDays, ILogger logger)
        {
            var backupManager = new BackupManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<BackupManager>.Instance);
            return await backupManager.CleanupOldBackupsAsync(retentionDays);
        }

        public static object GetBackupInfo(string backupPath)
        {
            // Placeholder implementation
            return new { Path = backupPath, Created = DateTime.UtcNow, Size = 0 };
        }
    }
}
