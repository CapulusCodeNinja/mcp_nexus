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
        /// Performs detailed privilege analysis and reports specific issues
        /// </summary>
        /// <param name="logger">Optional logger for detailed reporting</param>
        /// <returns>Detailed privilege analysis result</returns>
        public static PrivilegeAnalysisResult AnalyzeCurrentPrivileges(ILogger? logger = null)
        {
            var result = new PrivilegeAnalysisResult();

            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);

                result.UserName = identity.Name ?? "Unknown";
                result.AuthenticationType = identity.AuthenticationType ?? "Unknown";
                result.IsAuthenticated = identity.IsAuthenticated;

                // Check administrator role
                result.IsInAdministratorsGroup = principal.IsInRole(WindowsBuiltInRole.Administrator);
                result.IsInPowerUsersGroup = principal.IsInRole(WindowsBuiltInRole.PowerUser);
                result.IsInUsersGroup = principal.IsInRole(WindowsBuiltInRole.User);

                // Check token elevation level
                result.TokenElevationType = GetTokenElevationType();
                result.IsElevated = result.TokenElevationType == TokenElevationType.Full;

                // Check specific service-related permissions
                result.CanAccessServiceControlManager = CanAccessServiceControlManager();
                result.CanWriteToInstallDirectory = CanWriteToInstallDirectory();
                result.CanAccessRegistry = CanAccessServiceRegistry();

                // Overall assessment - Limited tokens might be sufficient for some operations
                result.HasSufficientPrivileges = result.IsInAdministratorsGroup &&
                                                (result.IsElevated || result.TokenElevationType == TokenElevationType.Limited) &&
                                                result.CanAccessServiceControlManager;

                LogPrivilegeAnalysis(result, logger);
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                OperationLogger.LogError(logger, OperationLogger.Operations.Install,
                    "Failed to analyze privileges: {Error}", ex.Message);
                return result;
            }
        }

        /// <summary>
        /// Gets the token elevation type for the current process
        /// </summary>
        private static TokenElevationType GetTokenElevationType()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);

                // If not in admin group at all, it's a standard user token
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                    return TokenElevationType.Standard;

                // User IS in admin group - now determine elevation level
                try
                {
                    // Test if we can access privileged resources
                    var scm = Win32ServiceManager.OpenServiceControlManager();
                    if (scm != IntPtr.Zero)
                    {
                        // Can access SCM - fully elevated
                        return TokenElevationType.Full;
                    }
                    else
                    {
                        // In admin group but can't access SCM - UAC limited token
                        return TokenElevationType.Limited;
                    }
                }
                catch
                {
                    // In admin group but privileged operations fail - UAC limited token
                    return TokenElevationType.Limited;
                }
            }
            catch
            {
                return TokenElevationType.Unknown;
            }
        }

        /// <summary>
        /// Tests if we can access the Service Control Manager
        /// </summary>
        private static bool CanAccessServiceControlManager()
        {
            try
            {
                var scm = Win32ServiceManager.OpenServiceControlManager();
                return scm != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests if we can write to the installation directory
        /// </summary>
        private static bool CanWriteToInstallDirectory()
        {
            try
            {
                var installDir = ServiceConfiguration.InstallFolder;
                return HasWriteAccessToDirectory(installDir);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests if we can access service-related registry keys
        /// </summary>
        private static bool CanAccessServiceRegistry()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services", writable: true);
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Logs detailed privilege analysis results
        /// </summary>
        private static void LogPrivilegeAnalysis(PrivilegeAnalysisResult result, ILogger? logger)
        {
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "🔍 PRIVILEGE ANALYSIS:");
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "  User: {UserName} ({AuthType})", result.UserName, result.AuthenticationType);
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "  In Administrators Group: {IsAdmin}", result.IsInAdministratorsGroup);
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "  Token Elevation: {TokenType}", result.TokenElevationType);
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "  Can Access SCM: {CanAccessSCM}", result.CanAccessServiceControlManager);
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "  Can Write Install Dir: {CanWrite}", result.CanWriteToInstallDirectory);
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "  Can Access Registry: {CanRegistry}", result.CanAccessRegistry);
            OperationLogger.LogInfo(logger, OperationLogger.Operations.Install,
                "  ✅ Sufficient Privileges: {HasPrivileges}", result.HasSufficientPrivileges);

            if (!result.HasSufficientPrivileges)
            {
                OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                    "❌ MISSING REQUIREMENTS:");
                if (!result.IsInAdministratorsGroup)
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                        "  - User is not in Administrators group");
                if (!result.IsElevated)
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                        "  - Process token is not fully elevated (UAC issue)");
                if (!result.CanAccessServiceControlManager)
                    OperationLogger.LogWarning(logger, OperationLogger.Operations.Install,
                        "  - Cannot access Service Control Manager");
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
