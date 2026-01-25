using System.Runtime.Versioning;

using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.Setup.Utilities;

using NLog;

namespace WinAiDbg.Setup.Management
{
    /// <summary>
    /// Manages backup and rollback operations for service installation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class BackupManager
    {
        private readonly Logger m_Logger;
        private readonly IFileSystem m_FileSystem;
        private readonly DirectoryCopyUtility m_DirectoryCopyUtility;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupManager"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        public BackupManager(IFileSystem fileSystem)
        {
            m_Logger = LogManager.GetCurrentClassLogger();

            m_FileSystem = fileSystem;

            m_DirectoryCopyUtility = new DirectoryCopyUtility(
                fileSystem);
        }

        /// <summary>
        /// Creates a backup of the existing installation directory.
        /// </summary>
        /// <param name="installationDirectory">The installation directory to backup.</param>
        /// <param name="backupDirectory">The backup directory path.</param>
        /// <returns>True if backup was successful, false otherwise.</returns>
        public async Task<bool> CreateBackupAsync(string installationDirectory, string backupDirectory)
        {
            try
            {
                if (!m_FileSystem.DirectoryExists(installationDirectory))
                {
                    return true; // Nothing to backup
                }

                // Create backup directory with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backupPath = Path.Combine(backupDirectory, $"backup-{timestamp}");

                m_Logger.Info("Creating backup at: {BackupPath}", backupPath);

                // Ensure backup directory exists
                m_FileSystem.CreateDirectory(backupDirectory);

                // Copy the entire installation directory to backup
                await m_DirectoryCopyUtility.CopyDirectoryAsync(installationDirectory, backupPath);

                m_Logger.Info("Backup created successfully at: {BackupPath}", backupPath);
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Failed to create backup");
                return false;
            }
        }

        /// <summary>
        /// Rolls back the installation by restoring from backup or cleaning up.
        /// </summary>
        /// <param name="installationDirectory">The installation directory to rollback.</param>
        /// <param name="backupDirectory">The backup directory path.</param>
        /// <param name="backupCreated">Whether a backup was created during installation.</param>
        /// <returns>Task representing the rollback operation.</returns>
        public async Task RollbackInstallationAsync(string installationDirectory, string backupDirectory, bool backupCreated)
        {
            try
            {
                if (backupCreated && m_FileSystem.DirectoryExists(backupDirectory))
                {
                    // Find the most recent backup
                    var backupDirInfo = m_FileSystem.GetDirectoryInfo(backupDirectory);
                    var backupDirs = backupDirInfo.GetDirectories("backup-*")
                        .OrderByDescending(d => d.Name)
                        .ToArray();

                    if (backupDirs.Length > 0)
                    {
                        var latestBackup = backupDirs[0].FullName;
                        m_Logger.Info("Restoring from backup: {BackupPath}", latestBackup);

                        // Remove current installation
                        if (m_FileSystem.DirectoryExists(installationDirectory))
                        {
                            m_FileSystem.DeleteDirectory(installationDirectory, true);
                        }

                        // Restore from backup
                        await m_DirectoryCopyUtility.CopyDirectoryAsync(latestBackup, installationDirectory);
                        m_Logger.Info("Rollback completed successfully");
                        return;
                    }
                }

                // If no backup available, clean up the installation directory
                m_Logger.Warn("No backup available, cleaning up installation directory");
                if (m_FileSystem.DirectoryExists(installationDirectory))
                {
                    m_FileSystem.DeleteDirectory(installationDirectory, true);
                    m_Logger.Info("Installation directory cleaned up");
                }
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Failed to rollback installation");
            }
        }
    }
}
