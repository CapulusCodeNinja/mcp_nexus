using System.Runtime.Versioning;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using nexus.setup.Models;
using nexus.utilities.FileSystem;
using nexus.utilities.ServiceManagement;

namespace nexus.setup.Core;

/// <summary>
/// Implements Windows service update functionality.
/// </summary>
[SupportedOSPlatform("windows")]
internal class ServiceUpdater
{
    private readonly ILogger<ServiceUpdater> m_Logger;
    private readonly ServiceInstaller m_ServiceInstaller;
    private readonly IFileSystem m_FileSystem;
    private readonly IServiceController m_ServiceController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUpdater"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceInstaller">Service installer instance.</param>
    /// <param name="fileSystem">File system abstraction.</param>
    /// <param name="serviceController">Service controller abstraction.</param>
    public ServiceUpdater(
        ILogger<ServiceUpdater> logger,
        ServiceInstaller serviceInstaller,
        IFileSystem fileSystem,
        IServiceController serviceController)
    {
        m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        m_ServiceInstaller = serviceInstaller ?? throw new ArgumentNullException(nameof(serviceInstaller));
        m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        m_ServiceController = serviceController ?? throw new ArgumentNullException(nameof(serviceController));
    }

    /// <summary>
    /// Updates an installed service with new binaries.
    /// </summary>
    /// <param name="serviceName">The name of the service to update.</param>
    /// <param name="newExecutablePath">Path to the new executable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The update result.</returns>
    public async Task<ServiceInstallationResult> UpdateServiceAsync(string serviceName, string newExecutablePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(newExecutablePath))
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(newExecutablePath));

        if (!m_FileSystem.FileExists(newExecutablePath))
            return ServiceInstallationResult.CreateFailure(serviceName, "New executable file not found", newExecutablePath);

        m_Logger.LogInformation("Updating service: {ServiceName}", serviceName);

        if (!m_ServiceInstaller.IsServiceInstalled(serviceName))
        {
            m_Logger.LogWarning("Service {ServiceName} is not installed", serviceName);
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

            m_Logger.LogInformation("Current service path: {CurrentPath}", currentPath);
            m_Logger.LogInformation("New service path: {NewPath}", newExecutablePath);

            // Create backup directory
            var backupDir = m_FileSystem.CombinePaths(m_FileSystem.GetDirectoryName(currentPath)!, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            m_FileSystem.CreateDirectory(backupDir);

            // Backup current binaries
            m_Logger.LogInformation("Creating backup in: {BackupDir}", backupDir);
            var backupSuccess = await BackupServiceAsync(serviceName, backupDir);
            if (!backupSuccess)
            {
                m_Logger.LogWarning("Backup failed, continuing with update anyway");
            }

            // Stop the service
            m_Logger.LogInformation("Stopping service {ServiceName}...", serviceName);
            var status = m_ServiceController.GetServiceStatus(serviceName);
            if (status != null && status != ServiceControllerStatus.Stopped.ToString())
            {
                m_ServiceController.StopService(serviceName);
                m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }

            // Copy new binaries
            m_Logger.LogInformation("Copying new binaries...");
            var sourceDir = m_FileSystem.GetDirectoryName(newExecutablePath)!;
            var targetDir = m_FileSystem.GetDirectoryName(currentPath)!;

            CopyDirectory(sourceDir, targetDir, overwrite: true);

            // Start the service
            m_Logger.LogInformation("Starting service {ServiceName}...", serviceName);
            m_ServiceController.StartService(serviceName);
            m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

            m_Logger.LogInformation("Service {ServiceName} updated successfully", serviceName);
            return ServiceInstallationResult.CreateSuccess(serviceName, $"Service updated successfully. Backup created at: {backupDir}");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Exception while updating service {ServiceName}", serviceName);
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
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("Backup path cannot be null or empty.", nameof(backupPath));

        try
        {
            var executablePath = m_ServiceController.GetServiceExecutablePath(serviceName);
            if (string.IsNullOrEmpty(executablePath))
            {
                m_Logger.LogError("Could not determine executable path for service {ServiceName}", serviceName);
                return false;
            }

            var sourceDir = m_FileSystem.GetDirectoryName(executablePath)!;

            m_Logger.LogInformation("Backing up service from {Source} to {Backup}", sourceDir, backupPath);

            m_FileSystem.CreateDirectory(backupPath);
            await Task.Run(() => CopyDirectory(sourceDir, backupPath, overwrite: false));

            m_Logger.LogInformation("Backup completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Failed to backup service {ServiceName}", serviceName);
            return false;
        }
    }

    /// <summary>
    /// Restores service binaries from a backup.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="backupPath">Path to the backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restore result.</returns>
    public async Task<ServiceInstallationResult> RestoreServiceAsync(string serviceName, string backupPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be null or empty.", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("Backup path cannot be null or empty.", nameof(backupPath));

        if (!m_FileSystem.DirectoryExists(backupPath))
            return ServiceInstallationResult.CreateFailure(serviceName, "Backup directory not found", backupPath);

        m_Logger.LogInformation("Restoring service {ServiceName} from backup: {BackupPath}", serviceName, backupPath);

        try
        {
            var executablePath = m_ServiceController.GetServiceExecutablePath(serviceName);
            if (string.IsNullOrEmpty(executablePath))
            {
                return ServiceInstallationResult.CreateFailure(serviceName, "Could not determine current executable path");
            }

            var targetDir = m_FileSystem.GetDirectoryName(executablePath)!;

            // Stop the service
            m_Logger.LogInformation("Stopping service {ServiceName}...", serviceName);
            var status = m_ServiceController.GetServiceStatus(serviceName);
            if (status != null && status != ServiceControllerStatus.Stopped.ToString())
            {
                m_ServiceController.StopService(serviceName);
                m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }

            // Restore binaries
            m_Logger.LogInformation("Restoring binaries...");
            await Task.Run(() => CopyDirectory(backupPath, targetDir, overwrite: true), cancellationToken);

            // Start the service
            m_Logger.LogInformation("Starting service {ServiceName}...", serviceName);
            m_ServiceController.StartService(serviceName);
            m_ServiceController.WaitForServiceStatus(serviceName, ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

            m_Logger.LogInformation("Service {ServiceName} restored successfully", serviceName);
            return ServiceInstallationResult.CreateSuccess(serviceName, "Service restored successfully");
        }
        catch (Exception ex)
        {
            m_Logger.LogError(ex, "Exception while restoring service {ServiceName}", serviceName);
            return ServiceInstallationResult.CreateFailure(serviceName, "Exception during restore", ex.Message);
        }
    }

    /// <summary>
    /// Copies a directory and all its contents.
    /// </summary>
    /// <param name="sourceDir">Source directory.</param>
    /// <param name="targetDir">Target directory.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    private void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
    {
        var dir = m_FileSystem.GetDirectoryInfo(sourceDir);
        
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        m_FileSystem.CreateDirectory(targetDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = m_FileSystem.CombinePaths(targetDir, file.Name);
            m_FileSystem.CopyFile(file.FullName, targetFilePath, overwrite);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            var newTargetDir = m_FileSystem.CombinePaths(targetDir, subDir.Name);
            CopyDirectory(subDir.FullName, newTargetDir, overwrite);
        }
    }
}

