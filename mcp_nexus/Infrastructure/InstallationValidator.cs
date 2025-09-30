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
        /// Validates all prerequisites for service installation with detailed analysis
        /// </summary>
        /// <param name="logger">Optional logger for validation operations</param>
        /// <returns>True if all prerequisites are met, false otherwise</returns>
        public static async Task<bool> ValidateInstallationPrerequisitesAsync(ILogger? logger = null)
        {
            // Step 1: Perform detailed privilege analysis
            var privilegeAnalysis = ServicePermissionValidator.AnalyzeCurrentPrivileges(logger);

            if (!privilegeAnalysis.HasSufficientPrivileges)
            {
                await DisplayDetailedPrivilegeError(privilegeAnalysis, logger);
                return false;
            }

            // Step 2: Validate installation directory access (redundant check, but kept for safety)
            if (!await ServicePermissionValidator.ValidateInstallationDirectoryAccessAsync(logger))
                return false;

            return true;
        }

        /// <summary>
        /// Displays detailed privilege error information to help users understand what's missing
        /// </summary>
        private static async Task DisplayDetailedPrivilegeError(PrivilegeAnalysisResult analysis, ILogger? logger)
        {
            var errorMessage = "Installation requires administrator privileges, but detailed analysis shows:";

            OperationLogger.LogError(logger, OperationLogger.Operations.Install, "{ErrorMessage}", errorMessage);
            await Console.Error.WriteLineAsync($"ERROR: {errorMessage}");

            await Console.Error.WriteLineAsync($"  User: {analysis.UserName} ({analysis.AuthenticationType})");
            await Console.Error.WriteLineAsync($"  In Administrators Group: {analysis.IsInAdministratorsGroup}");
            await Console.Error.WriteLineAsync($"  Token Elevation: {analysis.TokenElevationType}");
            await Console.Error.WriteLineAsync($"  Can Access Service Control Manager: {analysis.CanAccessServiceControlManager}");
            await Console.Error.WriteLineAsync($"  Can Write to Install Directory: {analysis.CanWriteToInstallDirectory}");
            await Console.Error.WriteLineAsync($"  Can Access Registry: {analysis.CanAccessRegistry}");

            await Console.Error.WriteLineAsync();
            await Console.Error.WriteLineAsync("SOLUTION:");

            if (!analysis.IsInAdministratorsGroup)
            {
                await Console.Error.WriteLineAsync("  - Run as a user with Administrator privileges");
            }
            else if (!analysis.IsElevated)
            {
                await Console.Error.WriteLineAsync("  - Right-click Command Prompt and select 'Run as administrator'");
                await Console.Error.WriteLineAsync("  - Or use PowerShell: Start-Process cmd -Verb RunAs");
            }
            else if (!analysis.CanAccessServiceControlManager)
            {
                await Console.Error.WriteLineAsync("  - Ensure Windows Service Control Manager is accessible");
                await Console.Error.WriteLineAsync("  - Check if antivirus or security software is blocking access");
            }
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
            try
            {
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Starting validation");
                Console.Error.Flush();

                // Step 1: Simple admin check (revert to old behavior)
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Checking admin privileges");
                Console.Error.Flush();
                
                if (!await ServicePermissionValidator.ValidateAdministratorPrivilegesAsync("Update", logger))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Admin privileges check failed");
                    Console.Error.Flush();
                    return false;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Admin privileges check passed");
                Console.Error.Flush();

                // Step 2: Check if service is installed
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Checking if service is installed");
                Console.Error.Flush();
                
                if (!ServiceRegistryManager.IsServiceInstalled())
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Service not installed");
                    Console.Error.Flush();
                    OperationLogger.LogError(logger, OperationLogger.Operations.Update, "Service is not installed, cannot update");
                    return false;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Service is installed");
                Console.Error.Flush();

                // Step 3: Validate installation directory access (redundant check, but kept for safety)
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Checking installation directory access");
                Console.Error.Flush();
                
                if (!await ServicePermissionValidator.ValidateInstallationDirectoryAccessAsync(logger))
                {
                    Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: Installation directory access check failed");
                    Console.Error.Flush();
                    return false;
                }

                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: All validation checks passed");
                Console.Error.Flush();
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ValidateUpdatePrerequisitesAsync: EXCEPTION - {ex.Message}");
                Console.Error.Flush();
                throw;
            }
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
