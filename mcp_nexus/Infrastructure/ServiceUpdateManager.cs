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
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Starting service update process");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Starting service update process");

                // Step 1: Create backup
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Creating backup of current installation");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Creating backup of current installation");
                backupPath = await BackupManager.CreateBackupAsync(logger);
                
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Backup creation completed, path: {backupPath ?? "null"}");
                Console.Error.Flush();
                
                if (backupPath == null)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Update, "Backup creation failed, continuing with update");
                }

                // Step 2: Stop the service if it's running
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Stopping service for update");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Stopping service for update");
                var stopCommand = ServiceConfiguration.GetServiceStopCommand();
                
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Running stop command: {stopCommand}");
                Console.Error.Flush();
                
                await ServiceRegistryManager.RunScCommandAsync(stopCommand, logger);

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Stop command completed, waiting {ServiceConfiguration.ServiceStopDelayMs}ms");
                Console.Error.Flush();
                
                // Wait for service to stop
                await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Service stop delay completed");
                Console.Error.Flush();

                // Step 3: Build and deploy new version
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Building new version");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Building new version");
                if (!await ProjectBuilder.BuildProjectForDeploymentAsync(logger))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Build failed during update");
                    Console.Error.Flush();
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Build failed during update");
                    await RollbackUpdate(backupPath, logger);
                    return false;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: Build completed successfully, deploying new version");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Deploying new version");
                if (!await FileOperationsManager.CopyApplicationFilesAsync(logger))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] PerformUpdateAsync: File deployment failed during update");
                    Console.Error.Flush();
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
