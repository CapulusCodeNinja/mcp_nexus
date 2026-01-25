using System.Runtime.Versioning;

using NLog;

using WinAiDbg.Config;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.External.Apis.Security;
using WinAiDbg.External.Apis.ServiceManagement;
using WinAiDbg.Setup.Core;
using WinAiDbg.Setup.Management;
using WinAiDbg.Setup.Models;
using WinAiDbg.Setup.Validation;

namespace WinAiDbg.Setup
{
    /// <summary>
    /// Handles product installation operations including file copying and service installation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ProductInstallation : IProductInstallation
    {
        private readonly Logger m_Logger;
        private readonly ISettings m_Settings;
        private readonly ServiceInstaller m_Installer;
        private readonly ServiceUpdater m_Updater;
        private readonly InstallationValidator m_InstallationValidator;
        private readonly UninstallValidator m_UninstallValidator;
        private readonly BackupManager m_BackupManager;
        private readonly FileManager m_FileManager;
        private readonly IFileSystem m_FileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductInstallation"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="processManager">Process manager abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        /// <param name="adminChecker">Administrative right checker.</param>
        /// <param name="settings">The product settings.</param>
        public ProductInstallation(
            IFileSystem fileSystem,
            IProcessManager processManager,
            IServiceController serviceController,
            IAdministratorChecker adminChecker,
            ISettings settings)
        {
            m_FileSystem = fileSystem;

            m_Logger = LogManager.GetCurrentClassLogger();

            m_Installer = new ServiceInstaller(m_FileSystem, processManager, serviceController);
            m_Updater = new ServiceUpdater(m_FileSystem, processManager, serviceController);

            m_InstallationValidator = new InstallationValidator(fileSystem, serviceController, adminChecker);
            m_UninstallValidator = new UninstallValidator(fileSystem, serviceController, adminChecker);

            m_BackupManager = new BackupManager(fileSystem);
            m_FileManager = new FileManager(fileSystem);
            m_Settings = settings;
        }

        /// <summary>
        /// Installs a Windows service using configuration settings.
        /// </summary>
        /// <returns>True if installation succeeded, false otherwise.</returns>
        public async Task<bool> InstallServiceAsync()
        {
            return await InstallServiceAsync(new ServiceControllerWrapper());
        }

        /// <summary>
        /// Installs a Windows service using configuration settings.
        /// </summary>
        /// <param name="serviceController">Service controller abstraction.</param>
        /// <returns>True if installation succeeded, false otherwise.</returns>
        internal async Task<bool> InstallServiceAsync(IServiceController serviceController)
        {
            var serviceName = m_Settings.Get().WinAiDbg.Service.ServiceName;
            var displayName = m_Settings.Get().WinAiDbg.Service.DisplayName;
            var startMode = ServiceStartMode.Automatic; // Fixed value, not configurable

            m_Logger.Info("Installing {ServiceName} as Windows Service...", serviceName);

            var installationDirectory = m_Settings.Get().WinAiDbg.Service.InstallPath;
            var backupDirectory = m_Settings.Get().WinAiDbg.Service.BackupPath;
            var installedExecutablePath = Path.Combine(installationDirectory, "WinAiDbg.exe");
            var sourceDirectory = AppContext.BaseDirectory;

            // PHASE 1: Pre-installation validation
            if (!m_InstallationValidator.ValidateInstallation(m_Settings.Get(), sourceDirectory))
            {
                return false;
            }

            // PHASE 2: Installation execution (with rollback protection)
            var backupCreated = false;
            var filesCopied = false;
            var serviceInstalled = false;

            try
            {
                // Step 1: Create backup of existing installation (if any)
                if (m_FileSystem.DirectoryExists(installationDirectory))
                {
                    m_Logger.Info("Creating backup of existing installation...");
                    backupCreated = await m_BackupManager.CreateBackupAsync(installationDirectory, backupDirectory);
                    if (!backupCreated)
                    {
                        m_Logger.Warn("Failed to create backup, but continuing with installation");
                    }
                }

                // Step 2: Copy application files
                m_Logger.Info("Copying application files to: {InstallationDirectory}", installationDirectory);
                filesCopied = await m_FileManager.CopyApplicationFilesAsync(sourceDirectory, installationDirectory);

                if (!filesCopied)
                {
                    m_Logger.Error("Failed to copy application files to Program Files");
                    m_Logger.Error("Please ensure you have administrator privileges and the target directory is accessible");
                    return false;
                }

                // Step 3: Install service
                var options = new ServiceInstallationOptions
                {
                    ServiceName = serviceName,
                    DisplayName = displayName,
                    Description = "Model Context Protocol server for Windows debugging tools",
                    ExecutablePath = installedExecutablePath,
                    StartMode = startMode,
                    Account = ServiceAccount.LocalSystem,
                };

                var result = await m_Installer.InstallServiceAsync(options);

                if (result.Success)
                {
                    serviceInstalled = true;
                    m_Logger.Info("{Message}", result.Message);
                    m_Logger.Info("Service '{ServiceName}' installed successfully.", serviceName);

                    // Start the service
                    m_Logger.Info("Starting service '{ServiceName}'...", serviceName);
                    serviceController.StartService(serviceName);
                    m_Logger.Info("Service '{ServiceName}' started successfully.", serviceName);

                    return true;
                }
                else
                {
                    m_Logger.Error("{Message}", result.Message);
                    if (!string.IsNullOrEmpty(result.ErrorDetails))
                    {
                        m_Logger.Error("Details: {ErrorDetails}", result.ErrorDetails);
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Unexpected error during installation");
                return false;
            }
            finally
            {
                // Rollback on failure
                if (!serviceInstalled && filesCopied)
                {
                    m_Logger.Warn("Installation failed, attempting rollback...");
                    await m_BackupManager.RollbackInstallationAsync(installationDirectory, backupDirectory, backupCreated);
                }
            }
        }

        /// <summary>
        /// Updates an existing Windows service using configuration settings.
        /// </summary>
        /// <returns>True if update succeeded, false otherwise.</returns>
        public async Task<bool> UpdateServiceAsync()
        {
            var serviceName = m_Settings.Get().WinAiDbg.Service.ServiceName;

            m_Logger.Info("Updating Windows Service '{ServiceName}'...", serviceName);

            var newExecutablePath = Path.Combine(AppContext.BaseDirectory, "WinAiDbg.exe");
            var result = await m_Updater.UpdateServiceAsync(serviceName, newExecutablePath);

            if (result.Success)
            {
                m_Logger.Info("{Message}", result.Message);
                m_Logger.Info("Service '{ServiceName}' updated successfully.", serviceName);
                return true;
            }
            else
            {
                m_Logger.Error("{Message}", result.Message);
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    m_Logger.Error("Details: {ErrorDetails}", result.ErrorDetails);
                }

                return false;
            }
        }

        /// <summary>
        /// Uninstalls the Windows service and removes application files.
        /// </summary>
        /// <returns>True if uninstall succeeded, false otherwise.</returns>
        public async Task<bool> UninstallServiceAsync()
        {
            return await UninstallServiceAsync(new ServiceControllerWrapper());
        }

        /// <summary>
        /// Uninstalls the Windows service and removes application files.
        /// </summary>
        /// <param name="serviceController">Service controller abstraction.</param>
        /// <returns>True if uninstall succeeded, false otherwise.</returns>
        internal async Task<bool> UninstallServiceAsync(IServiceController serviceController)
        {
            var serviceName = m_Settings.Get().WinAiDbg.Service.ServiceName;
            var installationDirectory = m_Settings.Get().WinAiDbg.Service.InstallPath;
            var backupDirectory = m_Settings.Get().WinAiDbg.Service.BackupPath;

            m_Logger.Info("Uninstalling {ServiceName} Windows Service...", serviceName);

            // PHASE 1: Pre-uninstall validation
            if (!m_UninstallValidator.ValidateUninstall(m_Settings.Get()))
            {
                // Check if it's the "nothing to uninstall" case (success)
                var isServiceInstalled = serviceController.IsServiceInstalled(serviceName);
                if (!isServiceInstalled)
                {
                    return true; // Not an error - service is already uninstalled
                }

                return false;
            }

            // PHASE 2: Uninstall execution
            var serviceRemoved = false;
            var filesRemoved = false;
            var backupCreated = false;

            try
            {
                // Step 1: Stop the service if it's running
                m_Logger.Info("Stopping service {ServiceName}...", serviceName);
                try
                {
                    serviceController.StopService(serviceName);
                    m_Logger.Info("Service {ServiceName} stopped successfully", serviceName);
                }
                catch (Exception ex)
                {
                    m_Logger.Warn(ex, "Failed to stop service {ServiceName}, continuing with uninstall", serviceName);
                }

                // Step 2: Create backup of current installation (if directory exists)
                if (m_FileSystem.DirectoryExists(installationDirectory))
                {
                    m_Logger.Info("Creating backup of current installation...");
                    backupCreated = await m_BackupManager.CreateBackupAsync(installationDirectory, backupDirectory);
                    if (!backupCreated)
                    {
                        m_Logger.Warn("Failed to create backup, but continuing with uninstall");
                    }
                }

                // Step 3: Remove the Windows service
                m_Logger.Info("Removing Windows service {ServiceName}...", serviceName);
                var uninstallResult = await m_Installer.UninstallServiceAsync(serviceName);

                if (uninstallResult.Success)
                {
                    serviceRemoved = true;
                    m_Logger.Info("{Message}", uninstallResult.Message);
                    m_Logger.Info("Service {ServiceName} removed successfully", serviceName);
                }
                else
                {
                    m_Logger.Error("{Message}", uninstallResult.Message);
                    if (!string.IsNullOrEmpty(uninstallResult.ErrorDetails))
                    {
                        m_Logger.Error("Details: {ErrorDetails}", uninstallResult.ErrorDetails);
                    }

                    return false;
                }

                // Step 4: Remove application files (if directory exists)
                filesRemoved = m_FileManager.RemoveApplicationFiles(installationDirectory);
                if (!filesRemoved)
                {
                    m_Logger.Error("Failed to remove application files from {InstallationDirectory}", installationDirectory);
                    m_Logger.Error("You may need to manually remove the directory");
                    return false;
                }

                m_Logger.Info("Uninstall completed successfully");
                m_Logger.Info("Service {ServiceName} has been completely removed", serviceName);

                if (backupCreated)
                {
                    m_Logger.Info("Backup created at: {BackupDirectory}", backupDirectory);
                }

                return true;
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Unexpected error during uninstall");

                // Attempt partial rollback if possible
                if (serviceRemoved && !filesRemoved && m_FileSystem.DirectoryExists(backupDirectory))
                {
                    m_Logger.Warn("Attempting to restore from backup...");
                    try
                    {
                        await m_BackupManager.RollbackInstallationAsync(installationDirectory, backupDirectory, true);
                        m_Logger.Info("Files restored from backup");
                    }
                    catch (Exception rollbackEx)
                    {
                        m_Logger.Error(rollbackEx, "Failed to restore from backup");
                    }
                }

                return false;
            }
        }
    }
}
