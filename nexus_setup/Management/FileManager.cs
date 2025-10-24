using Microsoft.Extensions.Logging;
using nexus.external_apis.FileSystem;
using nexus.setup.Utilities;
using System.Runtime.Versioning;

namespace nexus.setup.Management
{
    using Microsoft.Extensions.DependencyInjection;
    using NLog;

    /// <summary>
    /// Manages file operations for service installation using utility interfaces.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class FileManager
    {
        private readonly ILogger<FileManager> m_Logger;
        private readonly IFileSystem m_FileSystem;
        private readonly DirectoryCopyUtility m_DirectoryCopyUtility;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileManager"/> class.
        /// </summary>
        public FileManager(IServiceProvider serviceProvider) : this(serviceProvider, new FileSystem())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileManager"/> class.
        /// </summary>
        internal FileManager(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            m_Logger = serviceProvider.GetRequiredService<ILogger<FileManager>>();

            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            m_DirectoryCopyUtility = new DirectoryCopyUtility(
                serviceProvider,
                fileSystem);
        }

        /// <summary>
        /// Copies application files from source to destination directory.
        /// </summary>
        /// <param name="sourceDirectory">Source directory path.</param>
        /// <param name="destinationDirectory">Destination directory path.</param>
        /// <returns>True if copy was successful, false otherwise.</returns>
        public async Task<bool> CopyApplicationFilesAsync(string sourceDirectory, string destinationDirectory)
        {
            try
            {
                m_Logger.LogInformation("Copying application files from {SourceDirectory} to {DestinationDirectory}",
                    sourceDirectory, destinationDirectory);

                // Create destination directory if it doesn't exist
                m_FileSystem.CreateDirectory(destinationDirectory);

                // Copy all files and subdirectories
                await m_DirectoryCopyUtility.CopyDirectoryAsync(sourceDirectory, destinationDirectory);

                m_Logger.LogInformation("Application files copied successfully");
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to copy application files");
                return false;
            }
        }

        /// <summary>
        /// Removes application files from the specified directory.
        /// </summary>
        /// <param name="directoryPath">Directory path to remove.</param>
        /// <returns>True if removal was successful, false otherwise.</returns>
        public bool RemoveApplicationFiles(string directoryPath)
        {
            try
            {
                if (!m_FileSystem.DirectoryExists(directoryPath))
                {
                    m_Logger.LogInformation("Directory does not exist: {DirectoryPath}", directoryPath);
                    return true; // Nothing to remove
                }

                m_Logger.LogInformation("Removing application files from: {DirectoryPath}", directoryPath);
                m_FileSystem.DeleteDirectory(directoryPath, true);
                m_Logger.LogInformation("Application files removed successfully");
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to remove application files from {DirectoryPath}", directoryPath);
                return false;
            }
        }

    }
}
