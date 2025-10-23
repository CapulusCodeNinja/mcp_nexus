using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.setup.Models;
using nexus.setup.Interfaces;
using nexus.utilities.FileSystem;
using nexus.utilities.ProcessManagement;
using nexus.utilities.ServiceManagement;
using nexus.config.Models;
using System.Runtime.Versioning;
using System.Linq;

namespace nexus.setup.Core
{
    /// <summary>
    /// Handles product installation operations including file copying and service installation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ProductInstallation : IProductInstallation
    {
        private readonly ILogger<ProductInstallation> m_Logger;
        private readonly ServiceInstaller m_Installer;
        private readonly ServiceUpdater m_Updater;
        private readonly IServiceController m_ServiceController;
        private readonly SharedConfiguration m_Configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductInstallation"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger factory instance.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="processManager">Process manager abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        /// <param name="configuration">Shared configuration.</param>
        public ProductInstallation(
            ILoggerFactory loggerFactory,
            IFileSystem fileSystem,
            IProcessManager processManager,
            IServiceController serviceController,
            SharedConfiguration configuration)
        {
            m_Logger = loggerFactory.CreateLogger<ProductInstallation>();
            m_ServiceController = serviceController ?? throw new ArgumentNullException(nameof(serviceController));
            m_Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Create internal dependencies
            m_Installer = new ServiceInstaller(
                loggerFactory.CreateLogger<ServiceInstaller>(),
                fileSystem,
                processManager,
                serviceController);
            
            m_Updater = new ServiceUpdater(
                loggerFactory.CreateLogger<ServiceUpdater>(),
                m_Installer,
                fileSystem,
                serviceController);
        }

        /// <summary>
        /// Installs a Windows service using configuration settings.
        /// </summary>
        /// <returns>True if installation succeeded, false otherwise.</returns>
        public async Task<bool> InstallServiceAsync()
        {
            var serviceName = m_Configuration.McpNexus.Service.ServiceName;
            var displayName = m_Configuration.McpNexus.Service.DisplayName;
            var startMode = ServiceStartMode.Automatic; // Fixed value, not configurable
            
            m_Logger.LogInformation("Installing {ServiceName} as Windows Service...", serviceName);

            // Use configuration for installation directory
            var installationDirectory = m_Configuration.McpNexus.Service.InstallPath;
            var backupDirectory = m_Configuration.McpNexus.Service.BackupPath;
            var installedExecutablePath = Path.Combine(installationDirectory, "nexus.exe");
            var sourceDirectory = AppContext.BaseDirectory;

            // PHASE 1: Pre-installation validation (no file system changes)
            m_Logger.LogInformation("Performing pre-installation validation...");
            
            // Check 1: Administrator privileges
            var isAdmin = IsRunningAsAdministrator();
            if (!isAdmin)
            {
                m_Logger.LogError("Administrator privileges required to install to Program Files");
                m_Logger.LogError("Please run this command as Administrator (Run as Administrator)");
                return false;
            }

            // Check 2: Service already installed
            var isServiceInstalled = m_ServiceController.IsServiceInstalled(serviceName);
            if (isServiceInstalled)
            {
                m_Logger.LogWarning("Service {ServiceName} is already installed", serviceName);
                m_Logger.LogInformation("Use --update command to update an existing installation");
                return false;
            }

            // Check 3: Source directory exists and contains required files
            if (!Directory.Exists(sourceDirectory))
            {
                m_Logger.LogError("Source directory does not exist: {SourceDirectory}", sourceDirectory);
                return false;
            }

            var sourceExecutablePath = Path.Combine(sourceDirectory, "nexus.exe");
            if (!File.Exists(sourceExecutablePath))
            {
                m_Logger.LogError("Source executable not found: {SourceExecutablePath}", sourceExecutablePath);
                m_Logger.LogError("Please ensure the application is properly built before installation");
                return false;
            }

            // Check 4: Target directory permissions (without creating it yet)
            try
            {
                var parentDir = Path.GetDirectoryName(installationDirectory);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                {
                    m_Logger.LogError("Parent directory does not exist: {ParentDirectory}", parentDir);
                    return false;
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to validate target directory path");
                return false;
            }

            // Check 5: Backup directory permissions (without creating it yet)
            try
            {
                var backupParentDir = Path.GetDirectoryName(backupDirectory);
                if (!string.IsNullOrEmpty(backupParentDir) && !Directory.Exists(backupParentDir))
                {
                    m_Logger.LogError("Backup parent directory does not exist: {BackupParentDirectory}", backupParentDir);
                    return false;
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to validate backup directory path");
                return false;
            }

            m_Logger.LogInformation("Pre-installation validation completed successfully");

            // PHASE 2: Installation execution (with rollback protection)
            bool backupCreated = false;
            bool filesCopied = false;
            bool serviceInstalled = false;

            try
            {
                // Step 1: Create backup of existing installation (if any)
                if (Directory.Exists(installationDirectory))
                {
                    m_Logger.LogInformation("Creating backup of existing installation...");
                    backupCreated = await CreateBackupAsync(installationDirectory, backupDirectory);
                    if (!backupCreated)
                    {
                        m_Logger.LogWarning("Failed to create backup, but continuing with installation");
                    }
                }

                // Step 2: Copy application files
                m_Logger.LogInformation("Copying application files to: {InstallationDirectory}", installationDirectory);
                var copyResult = await m_Installer.CopyApplicationFilesAsync(sourceDirectory, installationDirectory);
                
                if (!copyResult)
                {
                    m_Logger.LogError("Failed to copy application files to Program Files");
                    m_Logger.LogError("Please ensure you have administrator privileges and the target directory is accessible");
                    return false;
                }
                
                filesCopied = true;
                m_Logger.LogInformation("Application files copied successfully");
                
                // Step 3: Install service
                var options = new ServiceInstallationOptions
                {
                    ServiceName = serviceName,
                    DisplayName = displayName,
                    Description = "Model Context Protocol server for Windows debugging tools",
                    ExecutablePath = installedExecutablePath,
                    StartMode = startMode,
                    Account = ServiceAccount.LocalSystem
                };

                var result = await m_Installer.InstallServiceAsync(options);

                if (result.Success)
                {
                    serviceInstalled = true;
                    m_Logger.LogInformation("{Message}", result.Message);
                    m_Logger.LogInformation("Service '{ServiceName}' installed successfully.", serviceName);
                    
                    // Start the service
                    m_Logger.LogInformation("Starting service '{ServiceName}'...", serviceName);
                    m_ServiceController.StartService(serviceName);
                    m_Logger.LogInformation("Service '{ServiceName}' started successfully.", serviceName);
                    
                    return true;
                }
                else
                {
                    m_Logger.LogError("{Message}", result.Message);
                    if (!string.IsNullOrEmpty(result.ErrorDetails))
                    {
                        m_Logger.LogError("Details: {ErrorDetails}", result.ErrorDetails);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Unexpected error during installation");
                return false;
            }
            finally
            {
                // Rollback on failure
                if (!serviceInstalled && filesCopied)
                {
                    m_Logger.LogWarning("Installation failed, attempting rollback...");
                    await RollbackInstallationAsync(installationDirectory, backupDirectory, backupCreated);
                }
            }
        }

        /// <summary>
        /// Updates an existing Windows service using configuration settings.
        /// </summary>
        /// <returns>True if update succeeded, false otherwise.</returns>
        public async Task<bool> UpdateServiceAsync()
        {
            var serviceName = m_Configuration.McpNexus.Service.ServiceName;
            
            m_Logger.LogInformation("Updating Windows Service '{ServiceName}'...", serviceName);

            var newExecutablePath = Path.Combine(AppContext.BaseDirectory, "nexus.exe");
            var result = await m_Updater.UpdateServiceAsync(serviceName, newExecutablePath);

            if (result.Success)
            {
                m_Logger.LogInformation("{Message}", result.Message);
                m_Logger.LogInformation("Service '{ServiceName}' updated successfully.", serviceName);
                return true;
            }
            else
            {
                m_Logger.LogError("{Message}", result.Message);
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    m_Logger.LogError("Details: {ErrorDetails}", result.ErrorDetails);
                }
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of the existing installation directory.
        /// </summary>
        /// <param name="installationDirectory">The installation directory to backup.</param>
        /// <param name="backupDirectory">The backup directory path.</param>
        /// <returns>True if backup was successful, false otherwise.</returns>
        private async Task<bool> CreateBackupAsync(string installationDirectory, string backupDirectory)
        {
            try
            {
                if (!Directory.Exists(installationDirectory))
                {
                    return true; // Nothing to backup
                }

                // Create backup directory with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var backupPath = Path.Combine(backupDirectory, $"backup-{timestamp}");
                
                m_Logger.LogInformation("Creating backup at: {BackupPath}", backupPath);
                
                // Ensure backup directory exists
                Directory.CreateDirectory(backupDirectory);
                
                // Copy the entire installation directory to backup
                await CopyDirectoryAsync(installationDirectory, backupPath);
                
                m_Logger.LogInformation("Backup created successfully at: {BackupPath}", backupPath);
                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to create backup");
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
        private async Task RollbackInstallationAsync(string installationDirectory, string backupDirectory, bool backupCreated)
        {
            try
            {
                if (backupCreated && Directory.Exists(backupDirectory))
                {
                    // Find the most recent backup
                    var backupDirs = Directory.GetDirectories(backupDirectory, "backup-*")
                        .OrderByDescending(d => d)
                        .ToArray();
                    
                    if (backupDirs.Length > 0)
                    {
                        var latestBackup = backupDirs[0];
                        m_Logger.LogInformation("Restoring from backup: {BackupPath}", latestBackup);
                        
                        // Remove current installation
                        if (Directory.Exists(installationDirectory))
                        {
                            Directory.Delete(installationDirectory, true);
                        }
                        
                        // Restore from backup
                        await CopyDirectoryAsync(latestBackup, installationDirectory);
                        m_Logger.LogInformation("Rollback completed successfully");
                        return;
                    }
                }
                
                // If no backup available, clean up the installation directory
                m_Logger.LogWarning("No backup available, cleaning up installation directory");
                if (Directory.Exists(installationDirectory))
                {
                    Directory.Delete(installationDirectory, true);
                    m_Logger.LogInformation("Installation directory cleaned up");
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to rollback installation");
            }
        }

        /// <summary>
        /// Copies a directory and all its contents recursively.
        /// </summary>
        /// <param name="sourceDir">The source directory path.</param>
        /// <param name="destDir">The destination directory path.</param>
        /// <returns>Task representing the copy operation.</returns>
        private async Task CopyDirectoryAsync(string sourceDir, string destDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            var dirs = dir.GetDirectories();

            // Create the destination directory if it doesn't exist
            Directory.CreateDirectory(destDir);

            // Copy all files
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var targetFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // Recursively copy subdirectories
            foreach (var subDir in dirs)
            {
                var targetSubDir = Path.Combine(destDir, subDir.Name);
                await CopyDirectoryAsync(subDir.FullName, targetSubDir);
            }
        }

        /// <summary>
        /// Checks if the current process is running with administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
