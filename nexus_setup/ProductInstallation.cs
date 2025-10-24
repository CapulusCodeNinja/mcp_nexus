using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.setup.Core;
using nexus.setup.Management;
using nexus.setup.Models;
using nexus.setup.Validation;
using System.Runtime.Versioning;
using nexus.config;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;
using nexus.external_apis.ServiceManagement;

namespace nexus.setup
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
        private readonly IServiceProvider m_ServiceProvider;
        private readonly InstallationValidator m_InstallationValidator;
        private readonly UninstallValidator m_UninstallValidator;
        private readonly BackupManager m_BackupManager;
        private readonly FileManager m_FileManager;
        private readonly IFileSystem m_FileSystem;


        private static IProductInstallation? m_Instance;

        /// <summary>
        /// Gets the singleton instance of the product installation.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection.</param>
        /// <returns>The product installation instance.</returns>
        public static IProductInstallation GetInstance(IServiceProvider serviceProvider)
        {
            return m_Instance ??= new ProductInstallation(serviceProvider);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductInstallation"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection.</param>
        private ProductInstallation(IServiceProvider serviceProvider) : this(serviceProvider, new FileSystem(), new ProcessManager(), new ServiceControllerWrapper())
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ProductInstallation"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="processManager">Process manager abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        internal ProductInstallation(IServiceProvider serviceProvider, IFileSystem fileSystem,
            IProcessManager processManager,
            IServiceController serviceController)
        {
            m_ServiceProvider = serviceProvider;
            m_FileSystem= fileSystem;

            m_Logger = m_ServiceProvider.GetRequiredService<ILogger<ProductInstallation>>();

            m_Installer = new ServiceInstaller(m_ServiceProvider, m_FileSystem, processManager, serviceController);
            m_Updater = new ServiceUpdater(m_ServiceProvider, m_FileSystem, processManager, serviceController);

            m_InstallationValidator = new InstallationValidator(m_ServiceProvider, fileSystem, serviceController);
            m_UninstallValidator = new UninstallValidator(m_ServiceProvider, fileSystem, serviceController);

            m_BackupManager = new BackupManager(m_ServiceProvider, fileSystem);
            m_FileManager = new FileManager(m_ServiceProvider, fileSystem);
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
        /// <returns>True if installation succeeded, false otherwise.</returns>
        internal async Task<bool> InstallServiceAsync(IServiceController serviceController)
        {
            var serviceName = Settings.GetInstance().Get().McpNexus.Service.ServiceName;
            var displayName = Settings.GetInstance().Get().McpNexus.Service.DisplayName;
            var startMode = ServiceStartMode.Automatic; // Fixed value, not configurable

            m_Logger.LogInformation("Installing {ServiceName} as Windows Service...", serviceName);

            var installationDirectory = Settings.GetInstance().Get().McpNexus.Service.InstallPath;
            var backupDirectory = Settings.GetInstance().Get().McpNexus.Service.BackupPath;
            var installedExecutablePath = Path.Combine(installationDirectory, "nexus.exe");
            var sourceDirectory = AppContext.BaseDirectory;

            // PHASE 1: Pre-installation validation
            if (!m_InstallationValidator.ValidateInstallation(Settings.GetInstance().Get(), sourceDirectory))
            {
                return false;
            }

            // PHASE 2: Installation execution (with rollback protection)
            bool backupCreated = false;
            bool filesCopied = false;
            bool serviceInstalled = false;

            try
            {
                // Step 1: Create backup of existing installation (if any)
                if (m_FileSystem.DirectoryExists(installationDirectory))
                {
                    m_Logger.LogInformation("Creating backup of existing installation...");
                    backupCreated = await m_BackupManager.CreateBackupAsync(installationDirectory, backupDirectory);
                    if (!backupCreated)
                    {
                        m_Logger.LogWarning("Failed to create backup, but continuing with installation");
                    }
                }

                // Step 2: Copy application files
                m_Logger.LogInformation("Copying application files to: {InstallationDirectory}", installationDirectory);
                filesCopied = await m_FileManager.CopyApplicationFilesAsync(sourceDirectory, installationDirectory);

                if (!filesCopied)
                {
                    m_Logger.LogError("Failed to copy application files to Program Files");
                    m_Logger.LogError("Please ensure you have administrator privileges and the target directory is accessible");
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
                    serviceController.StartService(serviceName);
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
            var serviceName = Settings.GetInstance().Get().McpNexus.Service.ServiceName;

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
        /// <returns>True if uninstall succeeded, false otherwise.</returns>
        internal async Task<bool> UninstallServiceAsync(IServiceController serviceController)
        {
            var serviceName = Settings.GetInstance().Get().McpNexus.Service.ServiceName;
            var installationDirectory = Settings.GetInstance().Get().McpNexus.Service.InstallPath;
            var backupDirectory = Settings.GetInstance().Get().McpNexus.Service.BackupPath;

            m_Logger.LogInformation("Uninstalling {ServiceName} Windows Service...", serviceName);

            // PHASE 1: Pre-uninstall validation
            if (!m_UninstallValidator.ValidateUninstall(Settings.GetInstance().Get()))
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
            bool serviceRemoved = false;
            bool filesRemoved = false;
            bool backupCreated = false;

            try
            {
                // Step 1: Stop the service if it's running
                m_Logger.LogInformation("Stopping service {ServiceName}...", serviceName);
                try
                {
                    serviceController.StopService(serviceName);
                    m_Logger.LogInformation("Service {ServiceName} stopped successfully", serviceName);
                }
                catch (Exception ex)
                {
                    m_Logger.LogWarning(ex, "Failed to stop service {ServiceName}, continuing with uninstall", serviceName);
                }

                // Step 2: Create backup of current installation (if directory exists)
                if (m_FileSystem.DirectoryExists(installationDirectory))
                {
                    m_Logger.LogInformation("Creating backup of current installation...");
                    backupCreated = await m_BackupManager.CreateBackupAsync(installationDirectory, backupDirectory);
                    if (!backupCreated)
                    {
                        m_Logger.LogWarning("Failed to create backup, but continuing with uninstall");
                    }
                }

                // Step 3: Remove the Windows service
                m_Logger.LogInformation("Removing Windows service {ServiceName}...", serviceName);
                var uninstallResult = await m_Installer.UninstallServiceAsync(serviceName);

                if (uninstallResult.Success)
                {
                    serviceRemoved = true;
                    m_Logger.LogInformation("{Message}", uninstallResult.Message);
                    m_Logger.LogInformation("Service {ServiceName} removed successfully", serviceName);
                }
                else
                {
                    m_Logger.LogError("{Message}", uninstallResult.Message);
                    if (!string.IsNullOrEmpty(uninstallResult.ErrorDetails))
                    {
                        m_Logger.LogError("Details: {ErrorDetails}", uninstallResult.ErrorDetails);
                    }
                    return false;
                }

                // Step 4: Remove application files (if directory exists)
                filesRemoved = m_FileManager.RemoveApplicationFiles(installationDirectory);
                if (!filesRemoved)
                {
                    m_Logger.LogError("Failed to remove application files from {InstallationDirectory}", installationDirectory);
                    m_Logger.LogError("You may need to manually remove the directory");
                    return false;
                }

                m_Logger.LogInformation("Uninstall completed successfully");
                m_Logger.LogInformation("Service {ServiceName} has been completely removed", serviceName);

                if (backupCreated)
                {
                    m_Logger.LogInformation("Backup created at: {BackupDirectory}", backupDirectory);
                }

                return true;
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Unexpected error during uninstall");

                // Attempt partial rollback if possible
                if (serviceRemoved && !filesRemoved && m_FileSystem.DirectoryExists(backupDirectory))
                {
                    m_Logger.LogWarning("Attempting to restore from backup...");
                    try
                    {
                        await m_BackupManager.RollbackInstallationAsync(installationDirectory, backupDirectory, true);
                        m_Logger.LogInformation("Files restored from backup");
                    }
                    catch (Exception rollbackEx)
                    {
                        m_Logger.LogError(rollbackEx, "Failed to restore from backup");
                    }
                }

                return false;
            }
        }
    }
}
