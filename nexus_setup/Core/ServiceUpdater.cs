using System.Runtime.Versioning;
using System.ServiceProcess;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Setup.Interfaces;
using Nexus.Setup.Models;
using Nexus.Setup.Utilities;

using NLog;

namespace Nexus.Setup.Core;

/// <summary>
/// Implements Windows service update functionality.
/// </summary>
[SupportedOSPlatform("windows")]
internal class ServiceUpdater
{
    private readonly Logger m_Logger;
    private readonly IServiceInstaller m_ServiceInstaller;
    private readonly IFileSystem m_FileSystem;
    private readonly IServiceController m_ServiceController;
    private readonly DirectoryCopyUtility m_DirectoryCopyUtility;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUpdater"/> class.
    /// </summary>
    /// <param name="fileSystem">File system abstraction.</param>
    /// <param name="processManager">Process manager abstraction.</param>
    /// <param name="serviceController">Service controller abstraction.</param>
    public ServiceUpdater(IFileSystem fileSystem, IProcessManager processManager, IServiceController serviceController)
    {
        m_Logger = LogManager.GetCurrentClassLogger();

        m_ServiceInstaller = new ServiceInstaller(fileSystem, processManager, serviceController);

        m_FileSystem = fileSystem;
        m_ServiceController = serviceController;

        m_DirectoryCopyUtility = new DirectoryCopyUtility(fileSystem);
    }

    /// <summary>
    /// Updates an installed service with new binaries.
    /// </summary>
    /// <param name="serviceName">The name of the service to update.</param>
    /// <param name="newExecutablePath">Path to the new executable.</param>
    /// <returns>The update result.</returns>
    public async Task<ServiceInstallationResult> UpdateServiceAsync(string serviceName, string newExecutablePath)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));
        }

        if (string.IsNullOrWhiteSpace(newExecutablePath))
        {
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(newExecutablePath));
        }

        if (!m_FileSystem.FileExists(newExecutablePath))
        {
            return ServiceInstallationResult.CreateFailure(serviceName, "New executable file not found", newExecutablePath);
        }

        m_Logger.Info("Updating service: {ServiceName}", serviceName);

        if (!m_ServiceInstaller.IsServiceInstalled(serviceName))
        {
            m_Logger.Warn("Service {ServiceName} is not installed", serviceName);
            return ServiceInstallationResult.CreateFailure(serviceName, "Service is not installed");
        }

        try
        {
            // Get current executable path
            var currentPath = m_ServiceController.GetServiceExecutablePath(serviceName);
            if (string.IsNullOrEmpty(currentPath))
            {
                return ServiceInstallationResult.CreateFailure(serviceName, "Could not determine current executable path");
            }

            m_Logger.Info("Current service path: {CurrentPath}", currentPath);
            m_Logger.Info("New service path: {NewPath}", newExecutablePath);

            // Create backup directory
            var backupDir = m_FileSystem.CombinePaths(m_FileSystem.GetDirectoryName(currentPath)!, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            m_FileSystem.CreateDirectory(backupDir);

            // Backup current binaries
            m_Logger.Info("Creating backup in: {BackupDir}", backupDir);
            var backupSuccess = await BackupServiceAsync(serviceName, backupDir);
            if (!backupSuccess)
            {
                m_Logger.Warn("Backup failed, continuing with update anyway");
            }

            // Check original service state and stop if running
            var originalStatus = m_ServiceController.GetServiceStatus(serviceName);
            var wasRunning = originalStatus == ServiceControllerStatus.Running;

            if (wasRunning)
            {
                m_Logger.Info("Service {ServiceName} is running - stopping it for update...", serviceName);
                m_ServiceController.StopService(serviceName);
                m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                await Task.Delay(500);
            }
            else
            {
                m_Logger.Info("Service {ServiceName} is not running - no need to stop it", serviceName);
            }

            // Copy new binaries
            m_Logger.Info("Copying new binaries...");
            var sourceDir = m_FileSystem.GetDirectoryName(newExecutablePath)!;
            var targetDir = m_FileSystem.GetDirectoryName(currentPath)!;

            await m_DirectoryCopyUtility.CopyDirectoryAsync(sourceDir, targetDir);

            // Only start the service if it was running before the update
            if (wasRunning)
            {
                m_Logger.Info("Service {ServiceName} was running before update - starting it again...", serviceName);
                m_ServiceController.StartService(serviceName);
                m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            }
            else
            {
                m_Logger.Info("Service {ServiceName} was not running before update - leaving it stopped", serviceName);
            }

            m_Logger.Info("Service {ServiceName} updated successfully", serviceName);
            return ServiceInstallationResult.CreateSuccess(serviceName, $"Service updated successfully. Backup created at: {backupDir}");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Exception while updating service {ServiceName}", serviceName);
            return ServiceInstallationResult.CreateFailure(serviceName, "Exception during update", ex.Message);
        }
    }

    /// <summary>
    /// Backs up the current service binaries.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="backupPath">Path where backup should be created.</param>
    /// <returns>True if backup succeeded, false otherwise.</returns>
    public async Task<bool> BackupServiceAsync(string serviceName, string backupPath)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));
        }

        if (string.IsNullOrWhiteSpace(backupPath))
        {
            throw new ArgumentException("Backup path cannot be null or empty.", nameof(backupPath));
        }

        try
        {
            var executablePath = m_ServiceController.GetServiceExecutablePath(serviceName);
            if (string.IsNullOrEmpty(executablePath))
            {
                m_Logger.Error("Could not determine executable path for service {ServiceName}", serviceName);
                return false;
            }

            var sourceDir = m_FileSystem.GetDirectoryName(executablePath)!;

            m_Logger.Info("Backing up service from {Source} to {Backup}", sourceDir, backupPath);

            m_FileSystem.CreateDirectory(backupPath);
            await m_DirectoryCopyUtility.CopyDirectoryAsync(sourceDir, backupPath);

            m_Logger.Info("Backup completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Failed to backup service {ServiceName}", serviceName);
            return false;
        }
    }

    /// <summary>
    /// Restores service binaries from a backup.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="backupPath">Path to the backup.</param>
    /// <returns>The restore result.</returns>
    public async Task<ServiceInstallationResult> RestoreServiceAsync(string serviceName, string backupPath)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));
        }

        if (string.IsNullOrWhiteSpace(backupPath))
        {
            throw new ArgumentException("Backup path cannot be null or empty.", nameof(backupPath));
        }

        if (!m_FileSystem.DirectoryExists(backupPath))
        {
            return ServiceInstallationResult.CreateFailure(serviceName, "Backup directory not found", backupPath);
        }

        m_Logger.Info("Restoring service {ServiceName} from backup: {BackupPath}", serviceName, backupPath);

        try
        {
            var executablePath = m_ServiceController.GetServiceExecutablePath(serviceName);
            if (string.IsNullOrEmpty(executablePath))
            {
                return ServiceInstallationResult.CreateFailure(serviceName, "Could not determine current executable path");
            }

            var targetDir = m_FileSystem.GetDirectoryName(executablePath)!;

            // Stop the service
            m_Logger.Info("Stopping service {ServiceName}...", serviceName);
            var status = m_ServiceController.GetServiceStatus(serviceName);
            if (status is not null and not ServiceControllerStatus.Stopped)
            {
                m_ServiceController.StopService(serviceName);
                m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }

            // Restore binaries
            m_Logger.Info("Restoring binaries...");
            await m_DirectoryCopyUtility.CopyDirectoryAsync(backupPath, targetDir);

            // Start the service
            m_Logger.Info("Starting service {ServiceName}...", serviceName);
            m_ServiceController.StartService(serviceName);
            m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

            m_Logger.Info("Service {ServiceName} restored successfully", serviceName);
            return ServiceInstallationResult.CreateSuccess(serviceName, "Service restored successfully");
        }
        catch (Exception ex)
        {
            m_Logger.Error(ex, "Exception while restoring service {ServiceName}", serviceName);
            return ServiceInstallationResult.CreateFailure(serviceName, "Exception during restore", ex.Message);
        }
    }
}
