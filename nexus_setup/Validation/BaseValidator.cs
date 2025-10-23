using Microsoft.Extensions.Logging;
using nexus.utilities.FileSystem;
using nexus.utilities.ServiceManagement;
using System.Runtime.Versioning;

namespace nexus.setup.Validation
{
    /// <summary>
    /// Base class for validation operations with common functionality.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal abstract class BaseValidator
    {
        protected readonly ILogger m_Logger;
        protected readonly IFileSystem m_FileSystem;
        protected readonly IServiceController m_ServiceController;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseValidator"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        protected BaseValidator(
            ILogger logger,
            IFileSystem fileSystem,
            IServiceController serviceController)
        {
            m_Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_ServiceController = serviceController ?? throw new ArgumentNullException(nameof(serviceController));
        }

        /// <summary>
        /// Validates that the current process has administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        protected bool ValidateAdministratorPrivileges()
        {
            var isAdmin = IsRunningAsAdministrator();
            if (!isAdmin)
            {
                m_Logger.LogError("Administrator privileges required to install to Program Files");
                m_Logger.LogError("Please run this command as Administrator (Run as Administrator)");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates target directory permissions.
        /// </summary>
        /// <param name="directoryPath">Target directory path.</param>
        /// <param name="directoryName">Name of the directory for logging purposes.</param>
        /// <returns>True if permissions are valid, false otherwise.</returns>
        protected bool ValidateDirectoryPermissions(string directoryPath, string directoryName)
        {
            try
            {
                var parentDir = Path.GetDirectoryName(directoryPath);
                if (!string.IsNullOrEmpty(parentDir) && !m_FileSystem.DirectoryExists(parentDir))
                {
                    m_Logger.LogError("{DirectoryName} parent directory does not exist: {ParentDirectory}", directoryName, parentDir);
                    return false;
                }
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to validate {DirectoryName} directory path", directoryName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the current process is running with administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
