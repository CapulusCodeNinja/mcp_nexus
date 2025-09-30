using System.Runtime.Versioning;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Validates prerequisites and conditions for service installation
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class InstallationValidator
    {
        /// <summary>
        /// Validates all prerequisites for service installation
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if all prerequisites are met, false otherwise</returns>
        public static async Task<bool> ValidateInstallationPrerequisitesAsync(ILogger? logger = null)
        {
            // Step 1: Validate administrator privileges
            if (!await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Installation", logger))
                return false;

            // Step 2: Validate installation directory access
            if (!await ServicePermissionValidator.ValidateInstallationDirectoryAccessAsync(logger))
                return false;

            return true;
        }

        /// <summary>
        /// Validates prerequisites for service uninstallation
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if prerequisites are met, false otherwise</returns>
        public static async Task<bool> ValidateUninstallationPrerequisitesAsync(ILogger? logger = null)
        {
            // Validate administrator privileges
            return await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Uninstallation", logger);
        }

        /// <summary>
        /// Validates prerequisites for service updates
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if prerequisites are met, false otherwise</returns>
        public static async Task<bool> ValidateUpdatePrerequisitesAsync(ILogger? logger = null)
        {
            // Step 1: Validate administrator privileges
            if (!await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Update", logger))
                return false;

            // Step 2: Check if service is installed
            if (!ServiceRegistryManager.IsServiceInstalled())
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Service is not installed, cannot update");
                return false;
            }

            // Step 3: Validate installation directory access
            if (!await ServicePermissionValidator.ValidateInstallationDirectoryAccessAsync(logger))
                return false;

            return true;
        }

        /// <summary>
        /// Validates that the installation was successful
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if installation is valid, false otherwise</returns>
        public static bool ValidateInstallationSuccess(ILogger? logger = null)
        {
            // Check if service is registered
            if (!ServiceRegistryManager.IsServiceInstalled())
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Service registration validation failed");
                return false;
            }

            // Check if required files exist
            if (!FileOperationsManager.ValidateInstallationFiles(logger))
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install, "Installation files validation failed");
                return false;
            }

            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, "Installation validation successful");
            return true;
        }
    }
}
