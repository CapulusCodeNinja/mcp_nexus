using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages service update operations including backup and rollback
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServiceUpdateManager
    {
        /// <summary>
        /// Performs the complete service update process
        /// </summary>
        /// <param name="logger">Optional logger for update operations</param>
        /// <returns>True if update was successful, false otherwise</returns>
        public static async Task<bool> PerformUpdateAsync(ILogger? logger = null)
        {
            string? backupPath = null;
            
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Starting service update process");

                // Step 1: Create backup
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Creating backup of current installation");
                backupPath = await BackupManager.CreateBackupAsync(logger);
                if (backupPath == null)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Update, "Backup creation failed, continuing with update");
                }

                // Step 2: Stop the service if it's running
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Stopping service for update");
                var stopCommand = ServiceConfiguration.GetServiceStopCommand();
                await ServiceRegistryManager.RunScCommandAsync(stopCommand, logger);
                
                // Wait for service to stop
                await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

                // Step 3: Build and deploy new version
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Building new version");
                if (!await ProjectBuilder.BuildProjectForDeploymentAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Build failed during update");
                    await RollbackUpdate(backupPath, logger);
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Deploying new version");
                if (!await FileOperationsManager.CopyApplicationFilesAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "File deployment failed during update");
                    await RollbackUpdate(backupPath, logger);
                    return false;
                }

                // Step 4: Validate new installation
                if (!FileOperationsManager.ValidateInstallationFiles(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Installation validation failed during update");
                    await RollbackUpdate(backupPath, logger);
                    return false;
                }

                // Step 5: Start the service
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Starting updated service");
                var startCommand = ServiceConfiguration.GetServiceStartCommand();
                await ServiceRegistryManager.RunScCommandAsync(startCommand, logger);

                // Step 6: Cleanup old backups
                await BackupManager.CleanupOldBackupsAsync(ServiceConfiguration.MaxBackupsToKeep, logger);

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service update completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, "Exception during service update");
                await RollbackUpdate(backupPath, logger);
                return false;
            }
        }

        /// <summary>
        /// Rolls back an update using the provided backup
        /// </summary>
        /// <param name="backupPath">Path to the backup directory</param>
        /// <param name="logger">Optional logger for rollback operations</param>
        private static async Task RollbackUpdate(string? backupPath, ILogger? logger = null)
        {
            try
            {
                if (string.IsNullOrEmpty(backupPath) || !Directory.Exists(backupPath))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Cannot rollback: backup not available");
                    return;
                }

                OperationLogger.LogWarning(logger, OperationLogger.Operations.Update, "Rolling back update from backup: {BackupPath}", backupPath);

                // Stop service
                var stopCommand = ServiceConfiguration.GetServiceStopCommand();
                await ServiceRegistryManager.RunScCommandAsync(stopCommand, logger);
                await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

                // Restore files from backup
                await FileOperationsManager.CopyDirectoryAsync(backupPath, ServiceConfiguration.InstallFolder, logger);

                // Start service
                var startCommand = ServiceConfiguration.GetServiceStartCommand();
                await ServiceRegistryManager.RunScCommandAsync(startCommand, logger);

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Rollback completed successfully");
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, "Exception during rollback");
            }
        }

        /// <summary>
        /// Checks if an update is needed by comparing versions
        /// </summary>
        /// <param name="logger">Optional logger for operations</param>
        /// <returns>True if update is needed, false otherwise</returns>
        public static bool IsUpdateNeeded(ILogger? logger = null)
        {
            try
            {
                // This is a simplified check - in a real scenario, you might compare
                // version numbers from the installed service vs. the current build
                var installedExecutable = Path.Combine(ServiceConfiguration.InstallFolder, ServiceConfiguration.ExecutableName);
                
                if (!File.Exists(installedExecutable))
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service executable not found, update needed");
                    return true;
                }

                // For now, always assume update is needed if requested
                // In practice, you would compare file versions, timestamps, or version metadata
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Update check completed");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, "Exception during update check");
                return true; // Assume update is needed if we can't determine
            }
        }
    }
}
