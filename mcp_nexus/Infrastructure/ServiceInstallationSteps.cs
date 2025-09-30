using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Individual installation steps for service deployment
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServiceInstallationSteps
    {
        /// <summary>
        /// Performs the core installation steps
        /// </summary>
        /// <param name="logger">Optional logger for installation operations</param>
        /// <returns>True if installation steps completed successfully, false otherwise</returns>
        public static async Task<bool> PerformInstallationStepsAsync(ILogger? logger = null)
        {
            try
            {
                // Step 1: Create installation directory
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                    "Creating installation directory: {InstallFolder}", ServiceConfiguration.InstallFolder);

                if (Directory.Exists(ServiceConfiguration.InstallFolder))
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Installation directory already exists, cleaning up");
                    Directory.Delete(ServiceConfiguration.InstallFolder, recursive: true);
                }
                Directory.CreateDirectory(ServiceConfiguration.InstallFolder);

                // Step 2: Build the project
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Building project for deployment");
                if (!await ProjectBuilder.BuildProjectForDeploymentAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Project build failed");
                    return false;
                }

                // Step 3: Copy application files
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Copying application files");
                if (!await FileOperationsManager.CopyApplicationFilesAsync(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "File copy failed");
                    return false;
                }

                // Step 4: Validate installation files
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Validating installation files");
                if (!FileOperationsManager.ValidateInstallationFiles(logger))
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Installation file validation failed");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, ex, "Exception during installation steps");
                return false;
            }
        }

        /// <summary>
        /// Performs service registration
        /// </summary>
        /// <param name="logger">Optional logger for registration operations</param>
        /// <returns>True if registration completed successfully, false otherwise</returns>
        public static async Task<bool> RegisterServiceAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Registering Windows service");

                var executablePath = Path.Combine(ServiceConfiguration.InstallFolder, ServiceConfiguration.ExecutableName);
                var createCommand = ServiceConfiguration.GetCreateServiceCommand(executablePath);

                OperationLogger.LogDebug(logger, OperationLogger.Operations.Install, "Service creation command: {Command}", createCommand);

                var result = await ServiceRegistryManager.RunScCommandAsync(createCommand, logger);
                if (!result)
                {
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Service registration failed");
                    return false;
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Service registered successfully");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, ex, "Exception during service registration");
                return false;
            }
        }

        /// <summary>
        /// Performs service unregistration
        /// </summary>
        /// <param name="logger">Optional logger for unregistration operations</param>
        /// <returns>True if unregistration completed successfully, false otherwise</returns>
        public static async Task<bool> UnregisterServiceAsync(ILogger? logger = null)
        {
            try
            {
                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Unregistering Windows service");

                var deleteCommand = ServiceConfiguration.GetDeleteServiceCommand();
                OperationLogger.LogDebug(logger, OperationLogger.Operations.Uninstall, "Service deletion command: {Command}", deleteCommand);

                var result = await ServiceRegistryManager.RunScCommandAsync(deleteCommand, logger);
                if (!result)
                {
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Uninstall, "Service unregistration may have failed, attempting cleanup");
                    await ServiceRegistryManager.ForceCleanupServiceAsync(logger);
                }

                OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Service unregistration completed");
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Uninstall, ex, "Exception during service unregistration");
                return false;
            }
        }

        /// <summary>
        /// Performs cleanup of installation files
        /// </summary>
        /// <param name="logger">Optional logger for cleanup operations</param>
        /// <returns>True if cleanup completed successfully, false otherwise</returns>
        public static async Task<bool> CleanupInstallationAsync(ILogger? logger = null)
        {
            try
            {
                if (Directory.Exists(ServiceConfiguration.InstallFolder))
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall,
                        "Removing installation directory: {InstallFolder}", ServiceConfiguration.InstallFolder);

                    Directory.Delete(ServiceConfiguration.InstallFolder, recursive: true);
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Installation directory removed successfully");
                }
                else
                {
                    OperationLogger.LogInfo(logger, OperationLogger.Operations.Uninstall, "Installation directory does not exist, no cleanup needed");
                }

                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Uninstall, ex, "Exception during installation cleanup");
                return false;
            }
        }
    }
}
