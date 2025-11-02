using System.Runtime.Versioning;

using Nexus.Config.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.Security;
using Nexus.External.Apis.ServiceManagement;

using NLog;

namespace Nexus.Setup.Validation
{
    /// <summary>
    /// Validates pre-installation conditions for service installation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class InstallationValidator : BaseValidator
    {
        /// <summary>
        /// Logger for pre-installation validation steps and outcomes.
        /// </summary>
        private readonly Logger m_Logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallationValidator"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        /// <param name="administratorChecker">Administrator checker abstraction. If null, uses default implementation.</param>
        public InstallationValidator(IFileSystem fileSystem, IServiceController serviceController, IAdministratorChecker administratorChecker)
            : base(fileSystem, serviceController, administratorChecker)
        {
            m_Logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Validates all pre-installation conditions.
        /// </summary>
        /// <param name="configuration">Configuration settings.</param>
        /// <param name="sourceDirectory">Source directory path.</param>
        /// <returns>True if all validations pass, false otherwise.</returns>
        public bool ValidateInstallation(SharedConfiguration configuration, string sourceDirectory)
        {
            m_Logger.Info("Performing pre-installation validation...");

            // Check 1: Administrator privileges
            if (!ValidateAdministratorPrivileges())
            {
                return false;
            }

            // Check 2: Service not already installed
            if (!ValidateServiceNotInstalled(configuration.McpNexus.Service.ServiceName))
            {
                return false;
            }

            // Check 3: Source directory and files
            if (!ValidateSourceFiles(sourceDirectory))
            {
                return false;
            }

            // Check 4: Target directory permissions
            if (!ValidateTargetDirectoryPermissions(configuration.McpNexus.Service.InstallPath))
            {
                return false;
            }

            // Check 5: Backup directory permissions
            if (!ValidateBackupDirectoryPermissions(configuration.McpNexus.Service.BackupPath))
            {
                return false;
            }

            m_Logger.Info("Pre-installation validation completed successfully");
            return true;
        }

        /// <summary>
        /// Validates that the service is not already installed.
        /// </summary>
        /// <param name="serviceName">Service name to check.</param>
        /// <returns>True if service is not installed, false otherwise.</returns>
        private bool ValidateServiceNotInstalled(string serviceName)
        {
            var isServiceInstalled = ServiceController.IsServiceInstalled(serviceName);
            if (isServiceInstalled)
            {
                m_Logger.Warn("Service {ServiceName} is already installed", serviceName);
                m_Logger.Info("Use --update command to update an existing installation");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that source directory exists and contains required files.
        /// </summary>
        /// <param name="sourceDirectory">Source directory path.</param>
        /// <returns>True if source files are valid, false otherwise.</returns>
        private bool ValidateSourceFiles(string sourceDirectory)
        {
            if (!FileSystem.DirectoryExists(sourceDirectory))
            {
                m_Logger.Error("Source directory does not exist: {SourceDirectory}", sourceDirectory);
                return false;
            }

            var sourceExecutablePath = Path.Combine(sourceDirectory, "nexus.exe");
            if (!FileSystem.FileExists(sourceExecutablePath))
            {
                m_Logger.Error("Source executable not found: {SourceExecutablePath}", sourceExecutablePath);
                m_Logger.Error("Please ensure the application is properly built before installation");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates target directory permissions.
        /// </summary>
        /// <param name="installationDirectory">Target installation directory.</param>
        /// <returns>True if permissions are valid, false otherwise.</returns>
        private bool ValidateTargetDirectoryPermissions(string installationDirectory)
        {
            return ValidateDirectoryPermissions(installationDirectory, "Installation");
        }

        /// <summary>
        /// Validates backup directory permissions.
        /// </summary>
        /// <param name="backupDirectory">Backup directory path.</param>
        /// <returns>True if permissions are valid, false otherwise.</returns>
        private bool ValidateBackupDirectoryPermissions(string backupDirectory)
        {
            return ValidateDirectoryPermissions(backupDirectory, "Backup");
        }
    }
}
