using System.Runtime.Versioning;

using Nexus.External.Apis.FileSystem;

using NLog;

namespace Nexus.Setup.Utilities
{
    /// <summary>
    /// Utility class for directory copying operations with infinite loop prevention.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class DirectoryCopyUtility
    {
        private readonly Logger m_Logger;
        private readonly IFileSystem m_FileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryCopyUtility"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        public DirectoryCopyUtility(IFileSystem fileSystem)
        {
            m_Logger = LogManager.GetCurrentClassLogger();
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
                m_Logger.Debug("Skipping copy to prevent infinite loop: {SourceDir} -> {DestDir}", sourceDir, destDir);
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
