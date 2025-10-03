using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages file operations for the service.
    /// Provides methods for copying, deleting, creating directories, and querying file information.
    /// </summary>
    public class FileOperationsManager
    {
        private readonly ILogger<FileOperationsManager> m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileOperationsManager"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording file operations and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public FileOperationsManager(ILogger<FileOperationsManager> logger)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Copies a file from the source path to the destination path asynchronously.
        /// Uses streaming to handle large files efficiently without blocking threads.
        /// </summary>
        /// <param name="sourcePath">The path of the source file to copy.</param>
        /// <param name="destinationPath">The path where the file should be copied.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the copy operation was successful; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(sourcePath))
                {
                    m_Logger.LogError("Source file does not exist: {SourcePath}", sourcePath);
                    return false;
                }

                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                // Use true asynchronous file streaming instead of synchronous File.Copy
                using (var sourceStream = File.OpenRead(sourcePath))
                using (var destinationStream = File.Create(destinationPath))
                {
                    // Stream the file content asynchronously with cancellation support
                    await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                }

                m_Logger.LogInformation("Successfully copied file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                return true;
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("File copy operation was cancelled: {SourcePath} -> {DestinationPath}", sourcePath, destinationPath);
                return false;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to copy file from {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
                return false;
            }
        }

        /// <summary>
        /// Deletes a file asynchronously.
        /// Uses Task.Run to avoid blocking the calling thread for file system operations.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the delete operation was successful; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use Task.Run to make the synchronous File.Delete operation truly asynchronous
                return await Task.Run(() =>
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        m_Logger.LogInformation("Successfully deleted file: {FilePath}", filePath);
                    }
                    return true;
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("File delete operation was cancelled: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Creates a directory asynchronously.
        /// Uses Task.Run to avoid blocking the calling thread for file system operations.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to create.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the directory creation was successful; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> CreateDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use Task.Run to make the synchronous Directory.CreateDirectory operation truly asynchronous
                return await Task.Run(() =>
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                        m_Logger.LogInformation("Successfully created directory: {DirectoryPath}", directoryPath);
                    }
                    return true;
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("Directory creation operation was cancelled: {DirectoryPath}", directoryPath);
                return false;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);
                return false;
            }
        }

        /// <summary>
        /// Checks if a file exists asynchronously.
        /// Uses Task.Run to avoid blocking the calling thread for file system operations.
        /// </summary>
        /// <param name="filePath">The path of the file to check.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns <c>true</c> if the file exists; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Task.Run(() => File.Exists(filePath), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("File existence check was cancelled: {FilePath}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error checking if file exists: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Gets the size of a file in bytes asynchronously.
        /// </summary>
        /// <param name="filePath">The path of the file to get the size for.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns the file size in bytes, or 0 if the file doesn't exist or an error occurs.
        /// </returns>
        public async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        return fileInfo.Length;
                    }
                    return 0L;
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("File size check was cancelled: {FilePath}", filePath);
                return 0;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to get file size for: {FilePath}", filePath);
                return 0;
            }
        }

        /// <summary>
        /// Gets all files in a directory matching the specified pattern asynchronously.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to search.</param>
        /// <param name="pattern">The search pattern to match files. Default is "*" (all files).</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.
        /// Returns an array of file paths, or an empty array if the directory doesn't exist or an error occurs.
        /// </returns>
        public async Task<string[]> GetFilesAsync(string directoryPath, string pattern = "*", CancellationToken cancellationToken = default)
        {
            try
            {
                return await Task.Run(() =>
                {
                    if (Directory.Exists(directoryPath))
                    {
                        return Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
                    }
                    return Array.Empty<string>();
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                m_Logger.LogWarning("File search was cancelled: {DirectoryPath}", directoryPath);
                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to get files from directory: {DirectoryPath}", directoryPath);
                return Array.Empty<string>();
            }
        }
    }
}
