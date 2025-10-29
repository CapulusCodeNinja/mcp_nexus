using System.Runtime.Versioning;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ServiceManagement;

using NLog;

namespace Nexus.Setup.Validation
{
    /// <summary>
    /// Base class for validation operations with common functionality.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal abstract class BaseValidator
    {
        protected readonly Logger m_Logger;
        protected readonly IFileSystem m_FileSystem;
        protected readonly IServiceController m_ServiceController;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseValidator"/> class.
        /// </summary>
        /// <param name="logger">NLog logger instance.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        protected BaseValidator(Logger logger, IFileSystem fileSystem, IServiceController serviceController)
        {
            m_Logger = logger;
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
                m_Logger.Error("Administrator privileges required to install to Program Files");
                m_Logger.Error("Please run this command as Administrator (Run as Administrator)");
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
                // For directories that are inside the installation directory (like backups),
                // we need to check if we can create the installation directory itself
                var installationParentDir = GetInstallationParentDirectory(directoryPath);
                if (!string.IsNullOrEmpty(installationParentDir))
                {
                    // Check if the installation's parent directory exists and is writable
                    if (!m_FileSystem.DirectoryExists(installationParentDir))
                    {
                        m_Logger.Error("{DirectoryName} installation parent directory does not exist: {InstallationParentDirectory}", directoryName, installationParentDir);
                        m_Logger.Error("Please ensure the installation parent directory exists and you have write permissions");
                        return false;
                    }

                    m_Logger.Debug("Installation parent directory validation passed for {DirectoryName}: {InstallationParentDirectory}", directoryName, installationParentDir);
                }
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex, "Failed to validate {DirectoryName} directory path", directoryName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the installation parent directory for validation purposes.
        /// For directories inside the installation directory, returns the installation's parent.
        /// </summary>
        /// <param name="directoryPath">The directory path to analyze.</param>
        /// <returns>The parent directory that should exist for validation.</returns>
        private string GetInstallationParentDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return string.Empty;
            }

            try
            {
                // Normalize the path to handle trailing slashes, mixed separators, etc.
                var normalizedPath = Path.GetFullPath(directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                // Split the normalized path into parts
                var pathParts = normalizedPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                // Find "MCP-Nexus" in the path (case-insensitive)
                var mcpNexusIndex = Array.FindIndex(pathParts, part => part.Equals("MCP-Nexus", StringComparison.OrdinalIgnoreCase));

                if (mcpNexusIndex > 0)
                {
                    // Found MCP-Nexus in the path, return everything up to that point
                    var parentParts = pathParts.Take(mcpNexusIndex).ToArray();
                    if (parentParts.Length > 0)
                    {
                        // Reconstruct the path with proper separators
                        var result = string.Join(Path.DirectorySeparatorChar.ToString(), parentParts);

                        // Ensure we have a drive letter or UNC path
                        if (normalizedPath.Length > 0 && (char.IsLetter(normalizedPath[0]) || normalizedPath.StartsWith("\\\\")))
                        {
                            return result;
                        }
                    }
                }

                // Fallback: use the immediate parent directory
                var fallbackParent = Path.GetDirectoryName(normalizedPath);
                return fallbackParent ?? string.Empty;
            }
            catch (Exception ex)
            {
                m_Logger.Warn(ex, "Failed to parse directory path: {DirectoryPath}", directoryPath);

                // Fallback to immediate parent
                return Path.GetDirectoryName(directoryPath) ?? string.Empty;
            }
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
