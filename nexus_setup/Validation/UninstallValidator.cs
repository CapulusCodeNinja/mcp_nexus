using Microsoft.Extensions.Logging;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ServiceManagement;
using nexus.config.Models;
using System.Runtime.Versioning;

namespace nexus.setup.Validation
{
    /// <summary>
    /// Validates pre-uninstall conditions for service uninstallation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class UninstallValidator : BaseValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UninstallValidator"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        public UninstallValidator(
            ILogger<UninstallValidator> logger,
            IFileSystem fileSystem,
            IServiceController serviceController)
            : base(logger, fileSystem, serviceController)
        {
        }

        /// <summary>
        /// Validates all pre-uninstall conditions.
        /// </summary>
        /// <param name="configuration">Configuration settings.</param>
        /// <returns>True if all validations pass, false otherwise.</returns>
        public bool ValidateUninstall(SharedConfiguration configuration)
        {
            m_Logger.LogInformation("Performing pre-uninstall validation...");

            // Check 1: Administrator privileges
            if (!ValidateAdministratorPrivileges())
            {
                return false;
            }

            // Check 2: Service must be installed
            if (!ValidateServiceInstalled(configuration.McpNexus.Service.ServiceName))
            {
                return false;
            }

            // Check 3: Installation directory validation
            ValidateInstallationDirectory(configuration.McpNexus.Service.InstallPath);

            m_Logger.LogInformation("Pre-uninstall validation completed successfully");
            return true;
        }


        /// <summary>
        /// Validates that the service is installed.
        /// </summary>
        /// <param name="serviceName">Service name to check.</param>
        /// <returns>True if service is installed, false otherwise.</returns>
        private bool ValidateServiceInstalled(string serviceName)
        {
            var isServiceInstalled = m_ServiceController.IsServiceInstalled(serviceName);
            if (!isServiceInstalled)
            {
                m_Logger.LogWarning("Service {ServiceName} is not installed", serviceName);
                m_Logger.LogInformation("Nothing to uninstall");
                return false; // This is actually a success case, but we return false to indicate "nothing to do"
            }
            return true;
        }

        /// <summary>
        /// Validates installation directory (warning only, not an error).
        /// </summary>
        /// <param name="installationDirectory">Installation directory path.</param>
        private void ValidateInstallationDirectory(string installationDirectory)
        {
            if (!m_FileSystem.DirectoryExists(installationDirectory))
            {
                m_Logger.LogWarning("Installation directory does not exist: {InstallationDirectory}", installationDirectory);
                m_Logger.LogInformation("Service will be removed but no files to clean up");
            }
        }

    }
}
