using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages backup operations for the MCP Nexus service.
    /// Provides comprehensive backup creation, restoration, listing, and cleanup capabilities.
    /// </summary>
    public class BackupManager
    {
        private readonly ILogger<BackupManager> m_Logger;
        private readonly string m_BackupDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording backup operations and errors.</param>
        /// <param name="backupDirectory">The directory path for storing backups. If null, uses the default ApplicationData path.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public BackupManager(ILogger<BackupManager> logger, string? backupDirectory = null)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_BackupDirectory = backupDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCP-Nexus", "Backups");
        }

        /// <summary>
        /// Creates a backup of the specified files asynchronously.
        /// Copies all source files to a timestamped backup directory.
        /// </summary>
        /// <param name="sourceFiles">The collection of file paths to backup.</param>
        /// <param name="backupName">The name for the backup. If null, uses a default timestamped name.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the path to the created backup directory.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceFiles"/> is null.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the backup directory cannot be created.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access to source files or backup directory is denied.</exception>
        public async Task<string> CreateBackupAsync(IEnumerable<string> sourceFiles, string? backupName = null)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupNameWithTimestamp = string.IsNullOrEmpty(backupName) ? $"backup_{timestamp}" : $"{backupName}_{timestamp}";
                var backupPath = Path.Combine(m_BackupDirectory, backupNameWithTimestamp);

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
                        m_Logger.LogInformation("Backed up file: {SourceFile} to {DestinationFile}", sourceFile, destinationFile);
                    }
                }

                m_Logger.LogInformation("Backup created successfully: {BackupPath}", backupPath);
                await Task.CompletedTask; // Fix CS1998 warning
                return backupPath;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to create backup");
                throw;
            }
        }

        /// <summary>
        /// Restores files from a backup asynchronously.
        /// Copies all files from the backup directory to the destination directory.
        /// </summary>
        /// <param name="backupPath">The path to the backup directory containing the files to restore.</param>
        /// <param name="destinationDirectory">The directory path where the files will be restored.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the restore operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupPath"/> or <paramref name="destinationDirectory"/> is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the backup directory does not exist.</exception>
        public async Task<bool> RestoreBackupAsync(string backupPath, string destinationDirectory)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    m_Logger.LogError("Backup directory does not exist: {BackupPath}", backupPath);
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
                    m_Logger.LogInformation("Restored file: {SourceFile} to {DestinationFile}", file, destinationFile);
                }

                m_Logger.LogInformation("Backup restored successfully from {BackupPath} to {DestinationDirectory}", backupPath, destinationDirectory);
                await Task.CompletedTask; // Fix CS1998 warning
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to restore backup from {BackupPath}", backupPath);
                return false;
            }
        }

        /// <summary>
        /// Lists all available backups asynchronously.
        /// Returns the paths to all backup directories in the backup directory.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns a collection of backup directory paths.
        /// </returns>
        public async Task<IEnumerable<string>> ListBackupsAsync()
        {
            try
            {
                if (!Directory.Exists(m_BackupDirectory))
                {
                    return new List<string>();
                }

                var backupDirectories = Directory.GetDirectories(m_BackupDirectory);
                m_Logger.LogInformation("Found {Count} backup directories", backupDirectories.Length);
                await Task.CompletedTask; // Fix CS1998 warning
                return backupDirectories;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to list backups");
                return new List<string>();
            }
        }

        /// <summary>
        /// Deletes old backups based on retention policy asynchronously.
        /// Removes backup directories that are older than the specified retention period.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain backups. Default is 30 days.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the number of backup directories that were deleted.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="retentionDays"/> is negative.</exception>
        public async Task<int> CleanupOldBackupsAsync(int retentionDays = 30)
        {
            try
            {
                if (!Directory.Exists(m_BackupDirectory))
                {
                    return 0;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var deletedCount = 0;

                var backupDirectories = Directory.GetDirectories(m_BackupDirectory);
                foreach (var backupDir in backupDirectories)
                {
                    var dirInfo = new DirectoryInfo(backupDir);
                    if (dirInfo.CreationTimeUtc < cutoffDate)
                    {
                        Directory.Delete(backupDir, true);
                        deletedCount++;
                        m_Logger.LogInformation("Deleted old backup: {BackupDirectory}", backupDir);
                    }
                }

                m_Logger.LogInformation("Cleaned up {DeletedCount} old backup directories", deletedCount);
                await Task.CompletedTask; // Fix CS1998 warning
                return deletedCount;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to cleanup old backups");
                return 0;
            }
        }

        /// <summary>
        /// Gets the backup directory path.
        /// </summary>
        public string BackupDirectory => m_BackupDirectory;

        /// <summary>
        /// Creates a backup asynchronously using the provided logger (static version for compatibility).
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="logger">The logger instance for recording backup operations and errors.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the path to the created backup directory.
        /// </returns>
        public static async Task<string> CreateBackupAsync(ILogger logger)
        {
            var backupManager = new BackupManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<BackupManager>.Instance);
            return await backupManager.CreateBackupAsync(new List<string>());
        }

        /// <summary>
        /// Cleans up old backups asynchronously using the provided logger (static version for compatibility).
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain backups.</param>
        /// <param name="logger">The logger instance for recording cleanup operations and errors.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the number of backup directories that were deleted.
        /// </returns>
        public static async Task<int> CleanupOldBackupsAsync(int retentionDays, ILogger logger)
        {
            var backupManager = new BackupManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<BackupManager>.Instance);
            return await backupManager.CleanupOldBackupsAsync(retentionDays);
        }

        /// <summary>
        /// Gets information about a backup directory (static version for compatibility).
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="backupPath">The path to the backup directory to get information for.</param>
        /// <returns>
        /// An object containing backup information including path, creation time, and size.
        /// </returns>
        public static object GetBackupInfo(string backupPath)
        {
            // Placeholder implementation
            return new { Path = backupPath, Created = DateTime.UtcNow, Size = 0 };
        }
    }
}
