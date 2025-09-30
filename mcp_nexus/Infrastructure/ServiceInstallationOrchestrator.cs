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
                    Console.WriteLine("Existing service found, uninstalling first...");
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service already installed. Uninstalling first");
                    await UninstallServiceAsync(logger); // Continue even if uninstall has issues

                    // Wait and check again
                    await Task.Delay(ServiceConfiguration.ServiceStopDelayMs);

                    if (ServiceRegistryManager.IsServiceInstalled())
                    {
                        OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                            "Service still exists after uninstall attempt. This may be normal if it's marked for deletion");
                    }
                    Console.WriteLine("✓ Previous service uninstalled");
                }

                // Step 3: Perform installation steps
                Console.WriteLine("Performing installation steps...");
                if (!await ServiceInstallationSteps.PerformInstallationStepsAsync(logger))
                {
                    Console.WriteLine("✗ Installation steps failed");
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Installation steps failed");
                    return false;
                }
                Console.WriteLine("✓ Installation steps completed");

                // Step 4: Register the service
                Console.WriteLine("Registering Windows service...");
                if (!await ServiceInstallationSteps.RegisterServiceAsync(logger))
                {
                    Console.WriteLine("✗ Service registration failed");
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Service registration failed");
                    return false;
                }
                Console.WriteLine("✓ Service registered successfully");

                // Step 5: Validate installation
                Console.WriteLine("Validating installation...");
                if (!InstallationValidator.ValidateInstallationSuccess(logger))
                {
                    Console.WriteLine("✗ Installation validation failed");
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Installation validation failed");
                    return false;
                }
                Console.WriteLine("✓ Installation validated successfully");

                // Step 6: Start the service
                Console.WriteLine("Starting service...");
                var startSuccess = await ServiceRegistryManager.RunScCommandAsync($"start \"{ServiceConfiguration.ServiceName}\"", logger);
                if (!startSuccess)
                {
                    Console.WriteLine("⚠ Warning: Service installed but failed to start");
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install, "Service start failed");
                }
                else
                {
                    Console.WriteLine("✓ Service started successfully");
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service started successfully");
                    
                    // Give the service a moment to start
                    await Task.Delay(2000);
                }

                Console.WriteLine();
                Console.WriteLine("🎉 Service installation completed successfully!");
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
                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("                    MCP NEXUS SERVICE UPDATE");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();
                
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Updating MCP Nexus Windows service");
                
                if (!await InstallationValidator.ValidateUpdatePrerequisitesAsync(logger))
                    return false;

                // Step 2: Check if update is needed
                if (!ServiceUpdateManager.IsUpdateNeeded(logger))
                {
                    Console.WriteLine("Service is already up to date.");
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Update, "Service is already up to date");
                    return true;
                }

                // Step 3: Perform the update
                if (!await ServiceUpdateManager.PerformUpdateAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Service update failed");
                    return false;
                }
                
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