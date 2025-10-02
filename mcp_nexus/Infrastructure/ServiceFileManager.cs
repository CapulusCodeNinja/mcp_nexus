using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages service-related file operations
    /// </summary>
    public class ServiceFileManager
    {
        private readonly ILogger<ServiceFileManager> _logger;

        public ServiceFileManager(ILogger<ServiceFileManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CopyServiceFilesAsync(string sourcePath, string targetPath)
        {
            try
            {
                if (!Directory.Exists(sourcePath))
                {
                    _logger.LogError("Source path does not exist: {SourcePath}", sourcePath);
                    return false;
                }

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                    _logger.LogInformation("Created target directory: {TargetPath}", targetPath);
                }

                await CopyDirectoryAsync(sourcePath, targetPath);
                _logger.LogInformation("Successfully copied service files from {SourcePath} to {TargetPath}", sourcePath, targetPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy service files from {SourcePath} to {TargetPath}", sourcePath, targetPath);
                return false;
            }
        }

        public async Task<bool> BackupServiceFilesAsync(string servicePath, string backupPath)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    _logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return false;
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupDir = Path.Combine(backupPath, $"backup_{timestamp}");

                await CopyDirectoryAsync(servicePath, backupDir);
                _logger.LogInformation("Successfully backed up service files to {BackupPath}", backupDir);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup service files from {ServicePath} to {BackupPath}", servicePath, backupPath);
                return false;
            }
        }

        public async Task<bool> RestoreServiceFilesAsync(string backupPath, string servicePath)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    _logger.LogError("Backup path does not exist: {BackupPath}", backupPath);
                    return false;
                }

                await CopyDirectoryAsync(backupPath, servicePath);
                _logger.LogInformation("Successfully restored service files from {BackupPath} to {ServicePath}", backupPath, servicePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore service files from {BackupPath} to {ServicePath}", backupPath, servicePath);
                return false;
            }
        }

        public async Task<bool> CleanupOldBackupsAsync(string backupPath, int retentionDays)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    _logger.LogWarning("Backup path does not exist: {BackupPath}", backupPath);
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
                        _logger.LogInformation("Deleted old backup: {BackupDirectory}", directory);
                    }
                }

                _logger.LogInformation("Cleaned up {DeletedCount} old backup directories", deletedCount);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old backups in {BackupPath}", backupPath);
                return false;
            }
        }

        public async Task<bool> ValidateServiceFilesAsync(string servicePath, ServiceConfiguration configuration)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    _logger.LogError("Service path does not exist: {ServicePath}", servicePath);
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
                        _logger.LogError("Required file missing: {FilePath}", fullPath);
                        return false;
                    }
                }

                _logger.LogInformation("Service files validation successful");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate service files in {ServicePath}", servicePath);
                return false;
            }
        }

        public async Task<bool> ValidateServiceFilesAsync(string servicePath)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    _logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return false;
                }

                _logger.LogInformation("Service files validation successful");
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate service files in {ServicePath}", servicePath);
                return false;
            }
        }

        public async Task<bool> DeleteServiceFilesAsync(string targetPath)
        {
            try
            {
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                    _logger.LogInformation("Successfully deleted service files from {TargetPath}", targetPath);
                }
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete service files from {TargetPath}", targetPath);
                return false;
            }
        }

        public async Task<FileInfo[]> GetServiceFilesAsync(string servicePath)
        {
            try
            {
                if (!Directory.Exists(servicePath))
                {
                    _logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return Array.Empty<FileInfo>();
                }

                var directory = new DirectoryInfo(servicePath);
                var files = directory.GetFiles("*", SearchOption.AllDirectories);
                
                _logger.LogInformation("Found {FileCount} files in service directory", files.Length);
                await Task.CompletedTask;
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get service files from {ServicePath}", servicePath);
                return Array.Empty<FileInfo>();
            }
        }

        public static async Task<bool> BuildProjectForDeploymentAsync(ILogger logger)
        {
            try
            {
                logger.LogInformation("Building project for deployment");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build project for deployment");
                return false;
            }
        }

        public static async Task<bool> CopyApplicationFilesAsync(ILogger logger)
        {
            try
            {
                logger.LogInformation("Copying application files");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to copy application files");
                return false;
            }
        }

        public async Task<bool> CreateBackupInstanceAsync(ILogger logger)
        {
            try
            {
                _logger.LogInformation("Creating backup");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup");
                return false;
            }
        }

        public string? FindProjectDirectory()
        {
            return Environment.CurrentDirectory; // Placeholder implementation
        }

        public static string? FindProjectDirectory(string serviceName)
        {
            return Environment.CurrentDirectory; // Placeholder implementation
        }






        public bool ValidateInstallationFiles(string servicePath)
        {
            return Directory.Exists(servicePath); // Placeholder implementation
        }

        public object GetBackupInfo(string backupPath)
        {
            return new { Path = backupPath, Created = DateTime.UtcNow, Size = 0 }; // Placeholder implementation
        }

        public static async Task CopyDirectoryAsync(string sourcePath, string targetPath, ILogger logger)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath))
                    throw new ArgumentNullException(nameof(sourcePath));
                if (string.IsNullOrEmpty(targetPath))
                    throw new ArgumentNullException(nameof(targetPath));

                var sourceDir = new DirectoryInfo(sourcePath);
                var targetDir = new DirectoryInfo(targetPath);

                if (!sourceDir.Exists)
                    throw new DirectoryNotFoundException($"Source directory does not exist: {sourcePath}");

                if (!targetDir.Exists)
                {
                    targetDir.Create();
                    logger.LogInformation("Created target directory: {TargetPath}", targetPath);
                }

                foreach (var file in sourceDir.GetFiles())
                {
                    var targetFile = Path.Combine(targetPath, file.Name);
                    file.CopyTo(targetFile, true);
                }

                foreach (var subDir in sourceDir.GetDirectories())
                {
                    var targetSubDir = Path.Combine(targetPath, subDir.Name);
                    await CopyDirectoryAsync(subDir.FullName, targetSubDir, logger);
                }

                logger.LogInformation("Successfully copied directory from {SourcePath} to {TargetPath}", sourcePath, targetPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to copy directory from {SourcePath} to {TargetPath}", sourcePath, targetPath);
                throw;
            }
        }

        public static async Task<bool> CreateBackupAsync(ILogger logger)
        {
            try
            {
                logger.LogInformation("Creating backup");
                // Placeholder implementation
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create backup");
                return false;
            }
        }

        public static async Task<bool> CleanupOldBackupsAsync(string backupPath, int retentionDays, ILogger logger)
        {
            try
            {
                if (!Directory.Exists(backupPath))
                {
                    logger.LogWarning("Backup path does not exist: {BackupPath}", backupPath);
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
                        logger.LogInformation("Deleted old backup: {BackupDirectory}", directory);
                    }
                }

                logger.LogInformation("Cleaned up {DeletedCount} old backup directories", deletedCount);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cleanup old backups in {BackupPath}", backupPath);
                return false;
            }
        }

        public static bool ValidateInstallationFilesStatic(string servicePath)
        {
            return Directory.Exists(servicePath); // Placeholder implementation
        }

        public static object GetBackupInfoStatic(string backupPath)
        {
            return new { Path = backupPath, Created = DateTime.UtcNow, Size = 0 }; // Placeholder implementation
        }

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
