using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Orchestrates the complete service installation, uninstallation, and update processes
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServiceInstallationOrchestrator
    {
        /// <summary>
        /// Performs the complete service installation process
        /// </summary>
        /// <param name="logger">Optional logger for installation operations</param>
        /// <returns>True if installation was successful, false otherwise</returns>
        public static async Task<bool> InstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                // Step 1: Validate administrator privileges
                if (!await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Installation", logger))
                    return false;

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Installing MCP Nexus as Windows service");

                // Step 2: Check if service already exists and uninstall if needed
                if (ServiceRegistryManager.IsServiceInstalled())
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service already installed. Uninstalling first");
                    await UninstallServiceAsync(logger); // Continue even if uninstall has issues

                    // Wait and check again
                    await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

                    if (ServiceRegistryManager.IsServiceInstalled())
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, 
                            "Service still exists after uninstall attempt. This may be normal if it's marked for deletion");
                    }
                }

                // Step 3: Validate installation directory access
                if (!await ServicePermissionValidator.ValidateInstallationDirectoryAccessAsync(logger))
                    return false;

                // Step 4: Create installation directory
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, 
                    "Creating installation directory: {InstallFolder}", ServiceConfiguration.InstallFolder);
                if (Directory.Exists(ServiceConfiguration.InstallFolder))
                {
                    Directory.Delete(ServiceConfiguration.InstallFolder, true);
                }
                Directory.CreateDirectory(ServiceConfiguration.InstallFolder);

                // Step 5: Build the project in Release mode for deployment
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Building project in Release mode for deployment");
                if (!await ServiceFileManager.BuildProjectForDeploymentAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Failed to build project for deployment");
                    return false;
                }

                // Step 6: Copy application files
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Copying application files");
                await ServiceFileManager.CopyApplicationFilesAsync(logger);

                // Step 7: Validate installation files
                if (!ServiceFileManager.ValidateInstallationFiles(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Installation file validation failed");
                    return false;
                }

                // Step 8: Install the service (with retry logic for "marked for deletion")
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Registering Windows service");
                var result = await ServiceRegistryManager.CreateServiceAsync(logger);
                
                if (!result)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, 
                        "Service creation failed. Attempting to clear 'marked for deletion' state");

                    // Try to force cleanup the service registration
                    if (await ServiceRegistryManager.ForceCleanupServiceAsync(logger))
                    {
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, 
                            "Service cleanup successful. Retrying installation");
                        result = await ServiceRegistryManager.CreateServiceAsync(logger);
                    }

                    if (!result)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, 
                            "Trying alternative service cleanup methods");

                        // Alternative method: Try to start and then delete again
                        await ServiceRegistryManager.RunScCommandAsync(ServiceConfiguration.GetServiceStartCommand(), logger, allowFailure: true);
                        await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);
                        await ServiceRegistryManager.RunScCommandAsync(ServiceConfiguration.GetServiceStopCommand(), logger, allowFailure: true);
                        await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);
                        await ServiceRegistryManager.RunScCommandAsync(ServiceConfiguration.GetServiceDeleteCommand(), logger, allowFailure: true);
                        await Task.Delay(ServiceConfiguration.ServiceDeleteDelayMs);

                        // Try one more time
                        result = await ServiceRegistryManager.CreateServiceAsync(logger);
                    }
                }

                if (result)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, 
                        "✅ MCP Nexus service installed successfully!");
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, 
                        "Service can be started with: sc start \"{ServiceName}\"", ServiceConfiguration.ServiceName);
                    return true;
                }
                else
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, 
                        "❌ Failed to install MCP Nexus service after multiple attempts");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, ex, 
                    "Exception during service installation: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Performs the complete service uninstallation process
        /// </summary>
        /// <param name="logger">Optional logger for uninstallation operations</param>
        /// <returns>True if uninstallation was successful, false otherwise</returns>
        public static async Task<bool> UninstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                // Step 1: Validate administrator privileges
                if (!await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Uninstallation", logger))
                    return false;

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Uninstalling MCP Nexus Windows service");

                // Step 2: Check if service exists
                if (!ServiceRegistryManager.IsServiceInstalled())
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Service is not installed");
                    
                    // Still try to clean up installation directory if it exists
                    if (Directory.Exists(ServiceConfiguration.InstallFolder))
                    {
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, 
                            "Cleaning up installation directory: {InstallFolder}", ServiceConfiguration.InstallFolder);
                        Directory.Delete(ServiceConfiguration.InstallFolder, true);
                    }
                    
                    return true;
                }

                // Step 3: Delete the service
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Removing Windows service registration");
                var result = await ServiceRegistryManager.DeleteServiceAsync(logger);

                if (!result)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, 
                        "Standard uninstall failed. Attempting force cleanup");
                    
                    // Try force cleanup
                    result = await ServiceRegistryManager.ForceCleanupServiceAsync(logger);
                }

                // Step 4: Remove installation directory
                if (Directory.Exists(ServiceConfiguration.InstallFolder))
                {
                    try
                    {
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, 
                            "Removing installation directory: {InstallFolder}", ServiceConfiguration.InstallFolder);
                        Directory.Delete(ServiceConfiguration.InstallFolder, true);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Installation directory removed successfully");
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, 
                            "Could not remove installation directory: {Error}", ex.Message);
                        // Don't fail the uninstall just because we couldn't remove the directory
                    }
                }

                if (result)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "✅ MCP Nexus service uninstalled successfully!");
                }
                else
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, 
                        "⚠️ Service uninstallation completed with warnings. Manual cleanup may be required.");
                }

                return result;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Uninstall, ex, 
                    "Exception during service uninstallation: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Performs a force uninstallation with aggressive cleanup
        /// </summary>
        /// <param name="logger">Optional logger for force uninstallation operations</param>
        /// <returns>True if force uninstallation was successful, false otherwise</returns>
        public static async Task<bool> ForceUninstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                // Step 1: Validate administrator privileges
                if (!await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Force uninstallation", logger))
                    return false;

                OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, 
                    "Force uninstalling MCP Nexus Windows service with aggressive cleanup");

                // Step 2: Attempt aggressive service cleanup
                OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, "Performing aggressive service cleanup");
                var serviceCleanupResult = await ServiceRegistryManager.ForceCleanupServiceAsync(logger);

                // Step 3: Force remove installation directory
                if (Directory.Exists(ServiceConfiguration.InstallFolder))
                {
                    try
                    {
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, 
                            "Force removing installation directory: {InstallFolder}", ServiceConfiguration.InstallFolder);
                        
                        // Try to remove read-only attributes first
                        SetDirectoryAttributesRecursively(ServiceConfiguration.InstallFolder, FileAttributes.Normal);
                        
                        Directory.Delete(ServiceConfiguration.InstallFolder, true);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, "Installation directory removed successfully");
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogError(logger, OperationLogger.Operations.ForceUninstall, 
                            "Could not remove installation directory: {Error}", ex.Message);
                    }
                }

                // Step 4: Additional cleanup - try to remove any remaining registry entries
                try
                {
                    await ServiceRegistryManager.DirectRegistryCleanupAsync(logger);
                }
                catch (Exception ex)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.ForceUninstall, 
                        "Additional registry cleanup failed: {Error}", ex.Message);
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.ForceUninstall, 
                    "✅ Force uninstallation completed. Service cleanup result: {ServiceCleanupResult}", serviceCleanupResult);
                
                return true; // Force uninstall always returns true as it's a best-effort operation
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.ForceUninstall, ex, 
                    "Exception during force uninstallation: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Performs the complete service update process
        /// </summary>
        /// <param name="logger">Optional logger for update operations</param>
        /// <returns>True if update was successful, false otherwise</returns>
        public static async Task<bool> UpdateServiceAsync(ILogger? logger = null)
        {
            try
            {
                // Step 1: Validate administrator privileges
                if (!await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Service update", logger))
                    return false;

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Starting MCP Nexus service update");

                // Step 2: Check if service exists
                if (!ServiceRegistryManager.IsServiceInstalled())
                {
                    var errorMsg = "MCP Nexus service is not installed. Use --install to install it first.";
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "{ErrorMsg}", errorMsg);
                    await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
                    return false;
                }

                // Step 3: Stop the service
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Stopping MCP Nexus service for update");
                var stopResult = await ServiceRegistryManager.RunScCommandAsync(ServiceConfiguration.GetServiceStopCommand(), logger, allowFailure: true);
                if (stopResult)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service stopped successfully");
                }
                else
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service was not running or already stopped");
                }

                // Wait for service to fully stop
                await Task.Delay(ServiceConfiguration.ServiceStartDelayMs);

                // Step 4: Build the project in Release mode
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Building project for deployment");
                if (!await ServiceFileManager.BuildProjectForDeploymentAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Failed to build project for update");
                    return false;
                }

                // Step 5: Create backup and update files
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Updating application files");
                var backupFolder = await ServiceFileManager.CreateBackupAsync(logger);
                
                try
                {
                    // Copy new files
                    await ServiceFileManager.CopyApplicationFilesAsync(logger);
                    
                    // Validate new installation
                    if (!ServiceFileManager.ValidateInstallationFiles(logger))
                    {
                        throw new InvalidOperationException("New installation files validation failed");
                    }
                    
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Application files updated successfully");
                }
                catch (Exception ex)
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, "Error updating files: {Error}", ex.Message);
                    
                    // Try to restore from backup if available
                    if (!string.IsNullOrEmpty(backupFolder) && Directory.Exists(backupFolder))
                    {
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Attempting to restore from backup");
                        try
                        {
                            await ServiceFileManager.CopyDirectoryAsync(backupFolder, ServiceConfiguration.InstallFolder, logger);
                            OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Restored from backup successfully");
                        }
                        catch (Exception restoreEx)
                        {
                            OperationLogger.LogError(logger, OperationLogger.Operations.Update, restoreEx, 
                                "Failed to restore from backup: {Error}", restoreEx.Message);
                        }
                    }
                    
                    return false;
                }

                // Step 6: Start the service
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Starting MCP Nexus service");
                var startResult = await ServiceRegistryManager.RunScCommandAsync(ServiceConfiguration.GetServiceStartCommand(), logger, allowFailure: true);
                if (startResult)
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service started successfully");
                }
                else
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Update, 
                        "Service update completed but service failed to start. You may need to start it manually.");
                }

                // Step 7: Cleanup old backups
                await ServiceFileManager.CleanupOldBackupsAsync(5, logger);

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "✅ MCP Nexus service updated successfully!");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, 
                    "Exception during service update: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Recursively sets file attributes for all files and directories
        /// </summary>
        /// <param name="path">The path to process</param>
        /// <param name="attributes">The attributes to set</param>
        private static void SetDirectoryAttributesRecursively(string path, FileAttributes attributes)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    // Set attributes for all files
                    foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(file, attributes);
                        }
                        catch
                        {
                            // Ignore individual file attribute setting failures
                        }
                    }

                    // Set attributes for all directories
                    foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            File.SetAttributes(dir, attributes);
                        }
                        catch
                        {
                            // Ignore individual directory attribute setting failures
                        }
                    }
                }
            }
            catch
            {
                // Ignore failures in attribute setting - this is a best-effort operation
            }
        }
    }
}
