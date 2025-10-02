using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages file operations for the service
    /// </summary>
    public class FileOperationsManager
    {
        private readonly ILogger<FileOperationsManager> _logger;

        public FileOperationsManager(ILogger<FileOperationsManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    _logger.LogError("Source file does not exist: {SourcePath}", sourcePath);
                    return false;
                }

                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                File.Copy(sourcePath, destinationPath, true);
                _logger.LogInformation("Successfully copied file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Successfully deleted file: {FilePath}", filePath);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> CreateDirectoryAsync(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogInformation("Successfully created directory: {DirectoryPath}", directoryPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            return await Task.FromResult(File.Exists(filePath));
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    return await Task.FromResult(fileInfo.Length);
                }
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file size for: {FilePath}", filePath);
                return 0;
            }
        }

        public async Task<string[]> GetFilesAsync(string directoryPath, string pattern = "*")
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
                    return await Task.FromResult(files);
                }
                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get files from directory: {DirectoryPath}", directoryPath);
                return Array.Empty<string>();
            }
        }
    }
}
