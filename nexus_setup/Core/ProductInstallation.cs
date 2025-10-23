using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.setup.Models;
using nexus.setup.Interfaces;
using nexus.utilities.FileSystem;
using nexus.utilities.ProcessManagement;
using nexus.utilities.ServiceManagement;
using System.Runtime.Versioning;

namespace nexus.setup.Core
{
    /// <summary>
    /// Handles product installation operations including file copying and service installation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ProductInstallation : IProductInstallation
    {
        private readonly ILogger<ProductInstallation> m_Logger;
        private readonly ServiceInstaller m_Installer;
        private readonly ServiceUpdater m_Updater;
        private readonly IServiceController m_ServiceController;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductInstallation"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger factory instance.</param>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="processManager">Process manager abstraction.</param>
        /// <param name="serviceController">Service controller abstraction.</param>
        public ProductInstallation(
            ILoggerFactory loggerFactory,
            IFileSystem fileSystem,
            IProcessManager processManager,
            IServiceController serviceController)
        {
            m_Logger = loggerFactory.CreateLogger<ProductInstallation>();
            m_ServiceController = serviceController ?? throw new ArgumentNullException(nameof(serviceController));
            
            // Create internal dependencies
            m_Installer = new ServiceInstaller(
                loggerFactory.CreateLogger<ServiceInstaller>(),
                fileSystem,
                processManager,
                serviceController);
            
            m_Updater = new ServiceUpdater(
                loggerFactory.CreateLogger<ServiceUpdater>(),
                m_Installer,
                fileSystem,
                serviceController);
        }

        /// <summary>
        /// Installs a Windows service with the specified options.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="displayName">Display name of the service.</param>
        /// <param name="startMode">Service start mode.</param>
        /// <returns>True if installation succeeded, false otherwise.</returns>
        public async Task<bool> InstallServiceAsync(string serviceName, string displayName, ServiceStartMode startMode)
        {
            m_Logger.LogInformation("Installing {ServiceName} as Windows Service...", serviceName);

            // Define installation directory in Program Files
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var installationDirectory = Path.Combine(programFilesPath, "MCP-Nexus");
            var installedExecutablePath = Path.Combine(installationDirectory, "nexus.exe");
            
            m_Logger.LogInformation("Copying application files to: {InstallationDirectory}", installationDirectory);
            
            // Check if running as administrator
            var isAdmin = IsRunningAsAdministrator();
            if (!isAdmin)
            {
                m_Logger.LogError("Administrator privileges required to install to Program Files");
                m_Logger.LogError("Please run this command as Administrator (Run as Administrator)");
                return false;
            }
            
            // Copy application files to Program Files
            var sourceDirectory = AppContext.BaseDirectory;
            var copyResult = await m_Installer.CopyApplicationFilesAsync(sourceDirectory, installationDirectory);
            
            if (!copyResult)
            {
                m_Logger.LogError("Failed to copy application files to Program Files");
                m_Logger.LogError("Please ensure you have administrator privileges and the target directory is accessible");
                return false;
            }
            
            m_Logger.LogInformation("Application files copied successfully");
            
            var options = new ServiceInstallationOptions
            {
                ServiceName = serviceName,
                DisplayName = displayName,
                Description = "Model Context Protocol server for Windows debugging tools",
                ExecutablePath = installedExecutablePath,
                StartMode = startMode,
                Account = ServiceAccount.LocalSystem
            };

            var result = await m_Installer.InstallServiceAsync(options);

            if (result.Success)
            {
                m_Logger.LogInformation("{Message}", result.Message);
                m_Logger.LogInformation("Service '{ServiceName}' installed successfully.", serviceName);
                m_Logger.LogInformation("Starting service '{ServiceName}'...", serviceName);
                
                // Start the service
                m_ServiceController.StartService(serviceName);
                
                m_Logger.LogInformation("Service '{ServiceName}' started successfully.", serviceName);
                return true;
            }
            else
            {
                m_Logger.LogError("{Message}", result.Message);
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    m_Logger.LogError("Details: {ErrorDetails}", result.ErrorDetails);
                }
                return false;
            }
        }

        /// <summary>
        /// Updates an existing Windows service.
        /// </summary>
        /// <param name="serviceName">Name of the service to update.</param>
        /// <returns>True if update succeeded, false otherwise.</returns>
        public async Task<bool> UpdateServiceAsync(string serviceName)
        {
            m_Logger.LogInformation("Updating Windows Service '{ServiceName}'...", serviceName);

            var newExecutablePath = Path.Combine(AppContext.BaseDirectory, "nexus.exe");
            var result = await m_Updater.UpdateServiceAsync(serviceName, newExecutablePath);

            if (result.Success)
            {
                m_Logger.LogInformation("{Message}", result.Message);
                m_Logger.LogInformation("Service '{ServiceName}' updated successfully.", serviceName);
                return true;
            }
            else
            {
                m_Logger.LogError("{Message}", result.Message);
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                {
                    m_Logger.LogError("Details: {ErrorDetails}", result.ErrorDetails);
                }
                return false;
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
