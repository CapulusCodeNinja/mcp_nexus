using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.config.Models;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ServiceManagement;
using System.Runtime.Versioning;

namespace nexus.setup.Validation
{
    /// <summary>
    /// Validates pre-installation conditions for service installation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class InstallationValidator : BaseValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstallationValidator"/> class.
        /// </summary>
        public InstallationValidator(IServiceProvider serviceProvider) : this(serviceProvider, new FileSystem(), new ServiceControllerWrapper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallationValidator"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        internal InstallationValidator(IServiceProvider serviceProvider, IFileSystem fileSystem, IServiceController serviceController) 
            : base(serviceProvider.GetRequiredService<ILogger<InstallationValidator>>(), fileSystem, serviceController)
        {

        }
        

        /// <summary>
        /// Validates all pre-installation conditions.
        /// </summary>
        /// <param name="configuration">Configuration settings.</param>
        /// <param name="sourceDirectory">Source directory path.</param>
        /// <returns>True if all validations pass, false otherwise.</returns>
        public bool ValidateInstallation(SharedConfiguration configuration, string sourceDirectory)
        {
            m_Logger.LogInformation("Performing pre-installation validation...");

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

            m_Logger.LogInformation("Pre-installation validation completed successfully");
            return true;
        }


        /// <summary>
        /// Validates that the service is not already installed.
        /// </summary>
        /// <param name="serviceName">Service name to check.</param>
        /// <returns>True if service is not installed, false otherwise.</returns>
        private bool ValidateServiceNotInstalled(string serviceName)
        {
            var isServiceInstalled = m_ServiceController.IsServiceInstalled(serviceName);
            if (isServiceInstalled)
            {
                m_Logger.LogWarning("Service {ServiceName} is already installed", serviceName);
                m_Logger.LogInformation("Use --update command to update an existing installation");
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
            if (!m_FileSystem.DirectoryExists(sourceDirectory))
            {
                m_Logger.LogError("Source directory does not exist: {SourceDirectory}", sourceDirectory);
                return false;
            }

            var sourceExecutablePath = Path.Combine(sourceDirectory, "nexus.exe");
            if (!m_FileSystem.FileExists(sourceExecutablePath))
            {
                m_Logger.LogError("Source executable not found: {SourceExecutablePath}", sourceExecutablePath);
                m_Logger.LogError("Please ensure the application is properly built before installation");
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
