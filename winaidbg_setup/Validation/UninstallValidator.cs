using System.Runtime.Versioning;

using NLog;

using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.Security;
using WinAiDbg.External.Apis.ServiceManagement;

namespace WinAiDbg.Setup.Validation
{
    /// <summary>
    /// Validates pre-uninstall conditions for service uninstallation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class UninstallValidator : BaseValidator
    {
        /// <summary>
        /// Logger for pre-uninstall validation steps and outcomes.
        /// </summary>
        private readonly Logger m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UninstallValidator"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        /// <param name="administratorChecker">Administrator checker abstraction. If null, uses default implementation.</param>
        public UninstallValidator(IFileSystem fileSystem, IServiceController serviceController, IAdministratorChecker administratorChecker)
            : base(fileSystem, serviceController, administratorChecker)
        {
            m_Logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Validates all pre-uninstall conditions.
        /// </summary>
        /// <param name="configuration">Configuration settings.</param>
        /// <returns>True if all validations pass, false otherwise.</returns>
        public bool ValidateUninstall(SharedConfiguration configuration)
        {
            m_Logger.Info("Performing pre-uninstall validation...");

            // Check 1: Administrator privileges
            if (!ValidateAdministratorPrivileges())
            {
                return false;
            }

            // Check 2: Service must be installed
            if (!ValidateServiceInstalled(configuration.WinAiDbg.Service.ServiceName))
            {
                return false;
            }

            // Check 3: Installation directory validation
            ValidateInstallationDirectory(configuration.WinAiDbg.Service.InstallPath);

            m_Logger.Info("Pre-uninstall validation completed successfully");
            return true;
        }

        /// <summary>
        /// Validates that the service is installed.
        /// </summary>
        /// <param name="serviceName">Service name to check.</param>
        /// <returns>True if service is installed, false otherwise.</returns>
        private bool ValidateServiceInstalled(string serviceName)
        {
            var isServiceInstalled = ServiceController.IsServiceInstalled(serviceName);
            if (!isServiceInstalled)
            {
                m_Logger.Warn("Service {ServiceName} is not installed", serviceName);
                m_Logger.Info("Nothing to uninstall");
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
            if (!FileSystem.DirectoryExists(installationDirectory))
            {
                m_Logger.Warn("Installation directory does not exist: {InstallationDirectory}", installationDirectory);
                m_Logger.Info("Service will be removed but no files to clean up");
            }
        }
    }
}
