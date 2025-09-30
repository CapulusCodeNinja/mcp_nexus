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
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Installing MCP Nexus as Windows service");

                // Step 1: Validate prerequisites
                if (!await InstallationValidator.ValidateInstallationPrerequisitesAsync(logger))
                    return false;

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

                // Step 3: Perform installation steps
                if (!await ServiceInstallationSteps.PerformInstallationStepsAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Installation steps failed");
                    return false;
                }

                // Step 4: Register the service
                if (!await ServiceInstallationSteps.RegisterServiceAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Service registration failed");
                    return false;
                }

                // Step 5: Validate installation
                if (!InstallationValidator.ValidateInstallationSuccess(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Installation validation failed");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service installation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, ex, "Exception during service installation");
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
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Uninstalling MCP Nexus Windows service");

                // Step 1: Validate prerequisites
                if (!await InstallationValidator.ValidateUninstallationPrerequisitesAsync(logger))
                    return false;

                // Step 2: Check if service exists
                if (!ServiceRegistryManager.IsServiceInstalled())
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Service is not installed, nothing to uninstall");
                    return true;
                }

                // Step 3: Stop and unregister service
                if (!await ServiceInstallationSteps.UnregisterServiceAsync(logger))
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, "Service unregistration failed, continuing with cleanup");
                }

                // Step 4: Wait for service to be fully removed
                await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

                // Step 5: Clean up installation files
                if (!await ServiceInstallationSteps.CleanupInstallationAsync(logger))
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, "Installation cleanup failed");
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Service uninstallation completed");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Uninstall, ex, "Exception during service uninstallation");
                return false;
            }
        }

        /// <summary>
        /// Forces uninstallation of the service, including registry cleanup
        /// </summary>
        /// <param name="logger">Optional logger for uninstallation operations</param>
        /// <returns>True if force uninstallation was successful, false otherwise</returns>
        public static async Task<bool> ForceUninstallServiceAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Force uninstalling MCP Nexus Windows service");

                // Step 1: Validate prerequisites
                if (!await InstallationValidator.ValidateUninstallationPrerequisitesAsync(logger))
                    return false;

                // Step 2: Force cleanup service registration
                await ServiceRegistryManager.ForceCleanupServiceAsync(logger);

                // Step 3: Clean up installation files
                await ServiceInstallationSteps.CleanupInstallationAsync(logger);

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Force uninstallation completed");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Uninstall, ex, "Exception during force uninstallation");
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
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Starting service update");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Updating MCP Nexus Windows service");

                // Step 1: Validate prerequisites
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Validating prerequisites");
                Console.Error.Flush();
                
                if (!await InstallationValidator.ValidateUpdatePrerequisitesAsync(logger))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Prerequisites validation failed");
                    Console.Error.Flush();
                    return false;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Prerequisites validated successfully");
                Console.Error.Flush();

                // Step 2: Check if update is needed
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Checking if update is needed");
                Console.Error.Flush();
                
                if (!ServiceUpdateManager.IsUpdateNeeded(logger))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Service is already up to date");
                    Console.Error.Flush();
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service is already up to date");
                    return true;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Update is needed, starting update process");
                Console.Error.Flush();

                // Step 3: Perform the update
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: Calling PerformUpdateAsync");
                Console.Error.Flush();
                
                if (!await ServiceUpdateManager.PerformUpdateAsync(logger))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: PerformUpdateAsync failed");
                    Console.Error.Flush();
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Service update failed");
                    return false;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: PerformUpdateAsync completed successfully");
                Console.Error.Flush();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service update completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] UpdateServiceAsync: EXCEPTION - {ex.Message}");
                Console.Error.Flush();
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, ex, "Exception during service update");
                return false;
            }
        }
    }
}