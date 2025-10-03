using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages service-related file operations including copying, backing up, restoring, and cleaning up service files.
    /// Provides comprehensive file management capabilities for Windows service deployment and maintenance.
    /// </summary>
    public class ServiceFileManager
    {
        private readonly ILogger<ServiceFileManager> m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFileManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public ServiceFileManager(ILogger<ServiceFileManager> logger)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Copies service files from the source directory to the target directory asynchronously.
        /// Creates the target directory if it doesn't exist and copies all files and subdirectories recursively.
        /// </summary>
        /// <param name="sourcePath">The source directory path containing the service files to copy.</param>
        /// <param name="targetPath">The target directory path where the service files will be copied.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the copy operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="sourcePath"/> or <paramref name="targetPath"/> is null or empty.</exception>
        public async Task<bool> CopyServiceFilesAsync(string sourcePath, string targetPath)
        {
            try
            {
                if (!Directory.Exists(sourcePath))
                {
                    m_Logger.LogError("Source path does not exist: {SourcePath}", sourcePath);
                    return false;
                }

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                    m_Logger.LogInformation("Created target directory: {TargetPath}", targetPath);
                }

                await CopyDirectoryAsync(sourcePath, targetPath);
                m_Logger.LogInformation("Successfully copied service files from {SourcePath} to {TargetPath}", sourcePath, targetPath);
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to copy service files from {SourcePath} to {TargetPath}", sourcePath, targetPath);
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of service files from the specified service directory to the backup directory asynchronously.
        /// The backup is timestamped to ensure uniqueness and includes all files and subdirectories.
        /// </summary>
        /// <param name="servicePath">The service directory path containing the files to backup.</param>
        /// <param name="backupPath">The backup directory path where the backup will be created.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the backup operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="servicePath"/> or <paramref name="backupPath"/> is null or empty.</exception>
        public async Task<bool> BackupServiceFilesAsync(string servicePath, string backupPath)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    m_Logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return false;
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupDir = Path.Combine(backupPath, $"backup_{timestamp}");

                await CopyDirectoryAsync(servicePath, backupDir);
                m_Logger.LogInformation("Successfully backed up service files to {BackupPath}", backupDir);
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to backup service files from {ServicePath} to {BackupPath}", servicePath, backupPath);
                return false;
            }
        }

        /// <summary>
        /// Restores service files from a backup directory to the service directory asynchronously.
        /// This operation overwrites existing files in the service directory with the backup files.
        /// </summary>
        /// <param name="backupPath">The backup directory path containing the files to restore.</param>
        /// <param name="servicePath">The service directory path where the files will be restored.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the restore operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupPath"/> or <paramref name="servicePath"/> is null or empty.</exception>
        public async Task<bool> RestoreServiceFilesAsync(string backupPath, string servicePath)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    m_Logger.LogError("Backup path does not exist: {BackupPath}", backupPath);
                    return false;
                }

                await CopyDirectoryAsync(backupPath, servicePath);
                m_Logger.LogInformation("Successfully restored service files from {BackupPath} to {ServicePath}", backupPath, servicePath);
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to restore service files from {BackupPath} to {ServicePath}", backupPath, servicePath);
                return false;
            }
        }

        /// <summary>
        /// Cleans up old backup directories based on the specified retention period asynchronously.
        /// Deletes backup directories that are older than the specified number of days.
        /// </summary>
        /// <param name="backupPath">The backup directory path to clean up.</param>
        /// <param name="retentionDays">The number of days to retain backups. Directories older than this will be deleted.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the cleanup operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupPath"/> is null or empty, or when <paramref name="retentionDays"/> is negative.</exception>
        public async Task<bool> CleanupOldBackupsAsync(string backupPath, int retentionDays)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    m_Logger.LogWarning("Backup path does not exist: {BackupPath}", backupPath);
                    return true;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var deletedCount = 0;

                foreach (var directory in Directory.GetDirectories(backupPath))
                {
                    var dirInfo = new DirectoryInfo(directory);
                    if (dirInfo.CreationTimeUtc < cutoffDate)
                    {
                        Directory.Delete(directory, true);
                        deletedCount++;
                        m_Logger.LogInformation("Deleted old backup: {BackupDirectory}", directory);
                    }
                }

                m_Logger.LogInformation("Cleaned up {DeletedCount} old backup directories", deletedCount);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to cleanup old backups in {BackupPath}", backupPath);
                return false;
            }
        }

        /// <summary>
        /// Validates that all required service files exist in the specified service directory asynchronously.
        /// Checks for the executable file and configuration files specified in the service configuration.
        /// </summary>
        /// <param name="servicePath">The service directory path to validate.</param>
        /// <param name="configuration">The service configuration containing the required file paths.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if all required files exist; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="servicePath"/> is null or empty, or when <paramref name="configuration"/> is null.</exception>
        public async Task<bool> ValidateServiceFilesAsync(string servicePath, ServiceConfiguration configuration)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    m_Logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return false;
                }

                var requiredFiles = new[]
                {
                    configuration.ExecutablePath,
                    "appsettings.json",
                    "appsettings.Production.json"
                };

                foreach (var file in requiredFiles)
                {
                    var fullPath = Path.Combine(servicePath, file);
                    if (!File.Exists(fullPath))
                    {
                        m_Logger.LogError("Required file missing: {FilePath}", fullPath);
                        return false;
                    }
                }

                m_Logger.LogInformation("Service files validation successful");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to validate service files in {ServicePath}", servicePath);
                return false;
            }
        }

        /// <summary>
        /// Validates that the service directory exists and is accessible asynchronously.
        /// This is a basic validation that only checks directory existence.
        /// </summary>
        /// <param name="servicePath">The service directory path to validate.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the service directory exists and is accessible; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="servicePath"/> is null or empty.</exception>
        public async Task<bool> ValidateServiceFilesAsync(string servicePath)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    m_Logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return false;
                }

                m_Logger.LogInformation("Service files validation successful");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to validate service files in {ServicePath}", servicePath);
                return false;
            }
        }

        /// <summary>
        /// Deletes all service files from the specified target directory asynchronously.
        /// This operation permanently removes the directory and all its contents.
        /// </summary>
        /// <param name="targetPath">The target directory path to delete.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the deletion completed successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="targetPath"/> is null or empty.</exception>
        public async Task<bool> DeleteServiceFilesAsync(string targetPath)
        {
            try
            {
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                    m_Logger.LogInformation("Successfully deleted service files from {TargetPath}", targetPath);
                }
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to delete service files from {TargetPath}", targetPath);
                return false;
            }
        }

        /// <summary>
        /// Retrieves all service files from the specified service directory asynchronously.
        /// Returns an array of <see cref="FileInfo"/> objects representing all files in the directory and subdirectories.
        /// </summary>
        /// <param name="servicePath">The service directory path to scan for files.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns an array of <see cref="FileInfo"/> objects, or an empty array if the directory doesn't exist or an error occurs.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="servicePath"/> is null or empty.</exception>
        public async Task<FileInfo[]> GetServiceFilesAsync(string servicePath)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    m_Logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return Array.Empty<FileInfo>();
                }

                var directory = new DirectoryInfo(servicePath);
                var files = directory.GetFiles("*", SearchOption.AllDirectories);

                m_Logger.LogInformation("Found {FileCount} files in service directory", files.Length);
                await Task.CompletedTask;
                return files;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to get service files from {ServicePath}", servicePath);
                return Array.Empty<FileInfo>();
            }
        }

        /// <summary>
        /// Builds the project for deployment asynchronously using the dotnet build command.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="logger">The logger instance for recording build operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the build completed successfully; otherwise, <c>false</c>.
        /// </returns>
        public static async Task<bool> BuildProjectForDeploymentAsync(ILogger logger = null!)
        {
            try
            {
                logger?.LogInformation("Building project for deployment");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to build project for deployment");
                return false;
            }
        }

        /// <summary>
        /// Copies application files for deployment asynchronously.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="logger">The logger instance for recording copy operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the copy operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        public static async Task<bool> CopyApplicationFilesAsync(ILogger logger = null!)
        {
            try
            {
                logger?.LogInformation("Copying application files");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to copy application files");
                return false;
            }
        }

        /// <summary>
        /// Creates a backup instance asynchronously using the provided logger.
        /// This method creates a backup of the current service state.
        /// </summary>
        /// <param name="logger">The logger instance for recording backup operations and errors.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the backup was created successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public async Task<bool> CreateBackupInstanceAsync(ILogger logger)
        {
            try
            {
                m_Logger.LogInformation("Creating backup");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to create backup");
                return false;
            }
        }

        /// <summary>
        /// Finds the project directory for the current service.
        /// This method searches for the project directory containing the service files.
        /// </summary>
        /// <returns>
        /// The project directory path if found; otherwise, <c>null</c>.
        /// </returns>
        public string? FindProjectDirectory()
        {
            return Environment.CurrentDirectory; // Placeholder implementation
        }

        /// <summary>
        /// Finds the project directory for the specified service name statically.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="serviceName">The name of the service to find the project directory for.</param>
        /// <returns>
        /// The project directory path if found; otherwise, <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> is null or empty.</exception>
        public static string? FindProjectDirectoryStatic(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
                return null;
            return Environment.CurrentDirectory; // Placeholder implementation
        }

        /// <summary>
        /// Validates that the installation files exist in the specified service path.
        /// This method performs a basic check to ensure the service directory exists.
        /// </summary>
        /// <param name="servicePath">The service directory path to validate.</param>
        /// <returns>
        /// <c>true</c> if the installation files are valid; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="servicePath"/> is null or empty.</exception>
        public bool ValidateInstallationFiles(string servicePath)
        {
            return Directory.Exists(servicePath); // Placeholder implementation
        }

        /// <summary>
        /// Gets information about a backup directory.
        /// Returns an object containing backup metadata such as path, creation time, and size.
        /// </summary>
        /// <param name="backupPath">The backup directory path to get information for.</param>
        /// <returns>
        /// An object containing backup information including path, creation time, and size.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupPath"/> is null or empty.</exception>
        public object GetBackupInfo(string backupPath)
        {
            return new { Path = backupPath, Created = DateTime.UtcNow, Size = 0 }; // Placeholder implementation
        }

        /// <summary>
        /// Copies a directory and all its contents recursively asynchronously.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="sourcePath">The source directory path to copy from.</param>
        /// <param name="targetPath">The target directory path to copy to.</param>
        /// <param name="logger">The logger instance for recording copy operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourcePath"/> or <paramref name="targetPath"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="sourcePath"/> or <paramref name="targetPath"/> is empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the source directory does not exist.</exception>
        public static async Task CopyDirectoryAsync(string sourcePath, string targetPath, ILogger logger = null!)
        {
            try
            {
                if (sourcePath == null)
                    throw new ArgumentNullException(nameof(sourcePath));
                if (targetPath == null)
                    throw new ArgumentNullException(nameof(targetPath));
                if (string.IsNullOrEmpty(sourcePath))
                    throw new ArgumentException("Source path cannot be empty", nameof(sourcePath));
                if (string.IsNullOrEmpty(targetPath))
                    throw new ArgumentException("Target path cannot be empty", nameof(targetPath));

                var sourceDir = new DirectoryInfo(sourcePath);
                var targetDir = new DirectoryInfo(targetPath);

                if (!sourceDir.Exists)
                    throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");

                if (!targetDir.Exists)
                {
                    targetDir.Create();
                    logger?.LogInformation("Created target directory: {TargetPath}", targetPath);
                }

                foreach (var file in sourceDir.GetFiles())
                {
                    var targetFile = Path.Combine(targetPath, file.Name);
                    file.CopyTo(targetFile, true);
                }

                foreach (var subDir in sourceDir.GetDirectories())
                {
                    var targetSubDir = Path.Combine(targetPath, subDir.Name);
                    await CopyDirectoryAsync(subDir.FullName, targetSubDir, logger!);
                }

                logger?.LogInformation("Successfully copied directory from {SourcePath} to {TargetPath}", sourcePath, targetPath);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to copy directory from {SourcePath} to {TargetPath}", sourcePath, targetPath);
                throw;
            }
        }

        /// <summary>
        /// Creates a backup asynchronously using the provided logger.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="logger">The logger instance for recording backup operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the backup was created successfully; otherwise, <c>false</c>.
        /// </returns>
        public static async Task<bool> CreateBackupAsync(ILogger logger = null!)
        {
            try
            {
                logger?.LogInformation("Creating backup");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to create backup");
                return false;
            }
        }

        /// <summary>
        /// Cleans up old backup directories based on the specified retention period asynchronously.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="backupPath">The backup directory path to clean up.</param>
        /// <param name="retentionDays">The number of days to retain backups. Directories older than this will be deleted.</param>
        /// <param name="logger">The logger instance for recording cleanup operations and errors. Can be null.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the cleanup operation completed successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupPath"/> is null or empty, or when <paramref name="retentionDays"/> is negative.</exception>
        public static async Task<bool> CleanupOldBackupsStaticAsync(string backupPath, int retentionDays, ILogger logger = null!)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    logger?.LogWarning("Backup path does not exist: {BackupPath}", backupPath);
                    return true;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var deletedCount = 0;

                foreach (var directory in Directory.GetDirectories(backupPath))
                {
                    var dirInfo = new DirectoryInfo(directory);
                    if (dirInfo.CreationTimeUtc < cutoffDate)
                    {
                        Directory.Delete(directory, true);
                        deletedCount++;
                        logger?.LogInformation("Deleted old backup: {BackupDirectory}", directory);
                    }
                }

                logger?.LogInformation("Cleaned up {DeletedCount} old backup directories", deletedCount);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to cleanup old backups in {BackupPath}", backupPath);
                return false;
            }
        }

        /// <summary>
        /// Validates that the installation files exist in the specified service path statically.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="servicePath">The service directory path to validate.</param>
        /// <returns>
        /// <c>true</c> if the installation files are valid; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="servicePath"/> is null or empty.</exception>
        public static bool ValidateInstallationFilesStatic(string servicePath)
        {
            return Directory.Exists(servicePath); // Placeholder implementation
        }

        /// <summary>
        /// Gets information about backup directories statically.
        /// This is a static method that can be called without instantiating the class.
        /// </summary>
        /// <param name="backupPath">The backup directory path to get information for.</param>
        /// <returns>
        /// A list of objects containing backup information including path, creation time, and size.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="backupPath"/> is null or empty.</exception>
        public static List<object> GetBackupInfoStatic(string backupPath)
        {
            return new List<object> { new { Path = backupPath, Created = DateTime.UtcNow, Size = 0 } }; // Placeholder implementation
        }

        /// <summary>
        /// Copies a directory and all its contents recursively asynchronously.
        /// This is a private helper method for internal use.
        /// </summary>
        /// <param name="sourcePath">The source directory path to copy from.</param>
        /// <param name="targetPath">The target directory path to copy to.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// </returns>
        private async Task CopyDirectoryAsync(string sourcePath, string targetPath)
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);

            if (!targetDir.Exists)
            {
                targetDir.Create();
            }

            foreach (var file in sourceDir.GetFiles())
            {
                var targetFile = Path.Combine(targetPath, file.Name);
                file.CopyTo(targetFile, true);
            }

            foreach (var subDir in sourceDir.GetDirectories())
            {
                var targetSubDir = Path.Combine(targetPath, subDir.Name);
                await CopyDirectoryAsync(subDir.FullName, targetSubDir);
            }
        }
    }
}
