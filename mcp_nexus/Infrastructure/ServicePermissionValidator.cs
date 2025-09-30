using System.Runtime.Versioning;
using System.Security.Principal;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Validates permissions and privileges required for service operations
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ServicePermissionValidator
    {
        /// <summary>
        /// Checks if the current process is running with administrator privileges
        /// </summary>
        /// <returns>True if running as administrator, false otherwise</returns>
        public static bool IsRunAsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Validates administrator privileges and logs appropriate error messages
        /// </summary>
        /// <param name="operation">The operation being performed (for logging)</param>
        /// <param name="logger">Optional logger for error reporting</param>
        /// <returns>True if administrator privileges are available, false otherwise</returns>
        public static async Task<bool> ValidateAdministratorPrivilegesAsync(string operation, ILogger? logger = null)
        {
            if (IsRunAsAdministrator())
                return true;
                
            var errorMsg = $"{operation} requires administrator privileges. Please run the command as administrator.";
            OperationLogger.LogError(logger, GetOperationFromString(operation), "{ErrorMsg}", errorMsg);
            await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
            return false;
        }
        
        /// <summary>
        /// Validates administrator privileges with a custom error message
        /// </summary>
        /// <param name="operation">The operation being performed</param>
        /// <param name="customErrorMessage">Custom error message to display</param>
        /// <param name="logger">Optional logger for error reporting</param>
        /// <returns>True if administrator privileges are available, false otherwise</returns>
        public static async Task<bool> ValidateAdministratorPrivilegesAsync(
            string operation, 
            string customErrorMessage, 
            ILogger? logger = null)
        {
            if (IsRunAsAdministrator())
                return true;
                
            OperationLogger.LogError(logger, GetOperationFromString(operation), "{ErrorMsg}", customErrorMessage);
            await Console.Error.WriteLineAsync($"ERROR: {customErrorMessage}");
            return false;
        }
        
        /// <summary>
        /// Checks if the current user has write access to the installation directory
        /// </summary>
        /// <param name="directoryPath">The directory path to check</param>
        /// <returns>True if write access is available, false otherwise</returns>
        public static bool HasWriteAccessToDirectory(string directoryPath)
        {
            try
            {
                // Try to create a temporary file in the directory
                var tempFile = Path.Combine(directoryPath, Path.GetRandomFileName());
                using (var fs = File.Create(tempFile, 1, FileOptions.DeleteOnClose))
                {
                    // File created successfully, we have write access
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Validates that the installation directory can be accessed and written to
        /// </summary>
        /// <param name="logger">Optional logger for error reporting</param>
        /// <returns>True if directory access is available, false otherwise</returns>
        public static async Task<bool> ValidateInstallationDirectoryAccessAsync(ILogger? logger = null)
        {
            try
            {
                var installDir = ServiceConfiguration.InstallFolder;
                
                // Check if directory exists or can be created
                if (!Directory.Exists(installDir))
                {
                    try
                    {
                        Directory.CreateDirectory(installDir);
                        OperationLogger.LogInfo(logger, OperationLogger.Operations.Install, 
                            "Created installation directory: {InstallDir}", installDir);
                    }
                    catch (Exception ex)
                    {
                        OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                            "Failed to create installation directory {InstallDir}: {Error}", installDir, ex.Message);
                        await Console.Error.WriteLineAsync($"ERROR: Cannot create installation directory: {ex.Message}");
                        return false;
                    }
                }
                
                // Check write access
                if (!HasWriteAccessToDirectory(installDir))
                {
                    var errorMsg = $"No write access to installation directory: {installDir}";
                    OperationLogger.LogError(logger, OperationLogger.Operations.Install, "{ErrorMsg}", errorMsg);
                    await Console.Error.WriteLineAsync($"ERROR: {errorMsg}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                    "Error validating installation directory access: {Error}", ex.Message);
                await Console.Error.WriteLineAsync($"ERROR: Directory access validation failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Converts a string operation name to the corresponding OperationLogger.Operations constant
        /// </summary>
        private static string GetOperationFromString(string operation)
        {
            return operation.ToLowerInvariant() switch
            {
                "installation" or "install" => OperationLogger.Operations.Install,
                "uninstallation" or "uninstall" => OperationLogger.Operations.Uninstall,
                "update" => OperationLogger.Operations.Update,
                "force uninstallation" or "force uninstall" => OperationLogger.Operations.ForceUninstall,
                _ => OperationLogger.Operations.Install // Default fallback
            };
        }
    }
}
