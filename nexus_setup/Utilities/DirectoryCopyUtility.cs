using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.external_apis.FileSystem;
using nexus.setup.Core;
using System.Runtime.Versioning;

namespace nexus.setup.Utilities
{
    /// <summary>
    /// Utility class for directory copying operations with infinite loop prevention.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class DirectoryCopyUtility
    {
        private readonly ILogger m_Logger;
        private readonly IFileSystem m_FileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryCopyUtility"/> class.
        /// </summary>
        public DirectoryCopyUtility(IServiceProvider serviceProvider) : this(serviceProvider, new FileSystem())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryCopyUtility"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        internal DirectoryCopyUtility(IServiceProvider serviceProvider, IFileSystem fileSystem)
        {
            m_Logger = serviceProvider.GetRequiredService<ILogger<DirectoryCopyUtility>>();
            m_FileSystem = fileSystem;
        }

        /// <summary>
        /// Copies a directory and all its contents recursively, preventing infinite loops.
        /// </summary>
        /// <param name="sourceDir">The source directory path.</param>
        /// <param name="destDir">The destination directory path.</param>
        /// <returns>Task representing the copy operation.</returns>
        public async Task CopyDirectoryAsync(string sourceDir, string destDir)
        {
            // Prevent infinite loops by checking if destination is inside source
            var normalizedSourceDir = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedDestDir = Path.GetFullPath(destDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (normalizedDestDir.StartsWith(normalizedSourceDir, StringComparison.OrdinalIgnoreCase))
            {
                m_Logger.LogDebug("Skipping copy to prevent infinite loop: {SourceDir} -> {DestDir}", sourceDir, destDir);
                return;
            }

            // Create the destination directory if it doesn't exist
            m_FileSystem.CreateDirectory(destDir);

            // Get all files in the source directory
            var files = m_FileSystem.GetFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var targetFilePath = Path.Combine(destDir, fileName);
                m_FileSystem.CopyFile(file, targetFilePath, true);
            }

            // Recursively copy subdirectories using DirectoryInfo
            var sourceDirInfo = m_FileSystem.GetDirectoryInfo(sourceDir);
            var subDirs = sourceDirInfo.GetDirectories();
            foreach (var subDir in subDirs)
            {
                var targetSubDir = Path.Combine(destDir, subDir.Name);
                await CopyDirectoryAsync(subDir.FullName, targetSubDir);
            }
        }
    }
}
