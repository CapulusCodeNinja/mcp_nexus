using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages updates to Windows services
    /// </summary>
    public class ServiceUpdateManager
    {
        private readonly ILogger<ServiceUpdateManager> _logger;
        private readonly ServiceInstallationOrchestrator _installationOrchestrator;
        private readonly Win32ServiceManager _serviceManager;
        private readonly ProjectBuilder _projectBuilder;
        private readonly ServiceFileManager _fileManager;

        public ServiceUpdateManager(
            ILogger<ServiceUpdateManager> logger,
            ServiceInstallationOrchestrator installationOrchestrator,
            Win32ServiceManager serviceManager,
            ProjectBuilder projectBuilder,
            ServiceFileManager fileManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _installationOrchestrator = installationOrchestrator ?? throw new ArgumentNullException(nameof(installationOrchestrator));
            _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
            _projectBuilder = projectBuilder ?? throw new ArgumentNullException(nameof(projectBuilder));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        }

        /// <summary>
        /// Updates a service to a new version
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="projectPath">Path to the project file</param>
        /// <param name="servicePath">Path where the service is installed</param>
        /// <param name="backupPath">Path to store backup</param>
        /// <returns>True if update was successful</returns>
        public async Task<bool> UpdateServiceAsync(string serviceName, string projectPath, string servicePath, string backupPath)
        {
            try
            {
                _logger.LogInformation("Starting service update for {ServiceName}", serviceName);

                // Step 1: Create backup
                var backupResult = await CreateBackupAsync(serviceName, servicePath, backupPath);
                if (!backupResult)
                {
                    _logger.LogError("Failed to create backup for service {ServiceName}", serviceName);
                    return false;
                }

                // Step 2: Stop the service
                var stopResult = await _serviceManager.StopServiceAsync(serviceName);
                if (!stopResult)
                {
                    _logger.LogWarning("Failed to stop service {ServiceName}, continuing with update", serviceName);
                }

                // Step 3: Build the new version
                var buildResult = await _projectBuilder.BuildProjectAsync(projectPath, servicePath, "Release");
                if (!buildResult)
                {
                    _logger.LogError("Failed to build new version for service {ServiceName}", serviceName);
                    await RestoreFromBackupAsync(serviceName, servicePath, backupPath);
                    return false;
                }

                // Step 4: Start the service
                var startResult = await _serviceManager.StartServiceAsync(serviceName);
                if (!startResult)
                {
                    _logger.LogError("Failed to start updated service {ServiceName}", serviceName);
                    await RestoreFromBackupAsync(serviceName, servicePath, backupPath);
                    return false;
                }

                _logger.LogInformation("Service {ServiceName} updated successfully", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update service {ServiceName}", serviceName);
                await RestoreFromBackupAsync(serviceName, servicePath, backupPath);
                return false;
            }
        }

        /// <summary>
        /// Checks if a service update is available
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="currentVersion">Current version of the service</param>
        /// <param name="newVersion">New version to check</param>
        /// <returns>True if update is available</returns>
        public async Task<bool> IsUpdateAvailableAsync(string serviceName, string currentVersion, string newVersion)
        {
            try
            {
                _logger.LogInformation("Checking for updates for service {ServiceName}", serviceName);

                // Simple version comparison - in a real implementation, this would check against a version server
                var currentVersionParts = currentVersion.Split('.');
                var newVersionParts = newVersion.Split('.');

                if (currentVersionParts.Length != newVersionParts.Length)
                {
                    return false;
                }

                for (int i = 0; i < currentVersionParts.Length; i++)
                {
                    if (int.TryParse(currentVersionParts[i], out var currentPart) &&
                        int.TryParse(newVersionParts[i], out var newPart))
                    {
                        if (newPart > currentPart)
                        {
                            return true;
                        }
                        if (newPart < currentPart)
                        {
                            return false;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check for updates for service {ServiceName}", serviceName);
                return false;
            }
        }

        /// <summary>
        /// Rolls back a service to the previous version
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="servicePath">Path where the service is installed</param>
        /// <param name="backupPath">Path to the backup</param>
        /// <returns>True if rollback was successful</returns>
        public async Task<bool> RollbackServiceAsync(string serviceName, string servicePath, string backupPath)
        {
            try
            {
                _logger.LogInformation("Rolling back service {ServiceName}", serviceName);

                // Stop the service
                await _serviceManager.StopServiceAsync(serviceName);

                // Restore from backup
                var restoreResult = await RestoreFromBackupAsync(serviceName, servicePath, backupPath);
                if (!restoreResult)
                {
                    _logger.LogError("Failed to restore service {ServiceName} from backup", serviceName);
                    return false;
                }

                // Start the service
                var startResult = await _serviceManager.StartServiceAsync(serviceName);
                if (!startResult)
                {
                    _logger.LogError("Failed to start rolled back service {ServiceName}", serviceName);
                    return false;
                }

                _logger.LogInformation("Service {ServiceName} rolled back successfully", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback service {ServiceName}", serviceName);
                return false;
            }
        }

        /// <summary>
        /// Validates that a service update is safe to perform
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="projectPath">Path to the project file</param>
        /// <returns>True if update is safe</returns>
        public async Task<bool> ValidateUpdateAsync(string serviceName, string projectPath)
        {
            try
            {
                _logger.LogInformation("Validating update for service {ServiceName}", serviceName);

                // Check if project can be built
                var buildValidation = await _projectBuilder.ValidateProjectAsync(projectPath);
                if (!buildValidation)
                {
                    _logger.LogError("Project validation failed for service {ServiceName}", serviceName);
                    return false;
                }

                // Check if service is in a valid state for update
                var serviceExists = await _serviceManager.ServiceExistsAsync(serviceName);
                if (!serviceExists)
                {
                    _logger.LogError("Service {ServiceName} does not exist", serviceName);
                    return false;
                }

                _logger.LogInformation("Update validation successful for service {ServiceName}", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate update for service {ServiceName}", serviceName);
                return false;
            }
        }

        private async Task<bool> CreateBackupAsync(string serviceName, string servicePath, string backupPath)
        {
            try
            {
                _logger.LogInformation("Creating backup for service {ServiceName}", serviceName);

                if (!Directory.Exists(servicePath))
                {
                    _logger.LogError("Service path does not exist: {ServicePath}", servicePath);
                    return false;
                }

                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                }

                Directory.CreateDirectory(backupPath);

                // Copy service files to backup location
                var copyResult = await _fileManager.CopyServiceFilesAsync(servicePath, backupPath);
                if (!copyResult)
                {
                    _logger.LogError("Failed to copy service files to backup location");
                    return false;
                }

                _logger.LogInformation("Backup created successfully for service {ServiceName}", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create backup for service {ServiceName}", serviceName);
                return false;
            }
        }

        private async Task<bool> RestoreFromBackupAsync(string serviceName, string servicePath, string backupPath)
        {
            try
            {
                _logger.LogInformation("Restoring service {ServiceName} from backup", serviceName);

                if (!Directory.Exists(backupPath))
                {
                    _logger.LogError("Backup path does not exist: {BackupPath}", backupPath);
                    return false;
                }

                // Remove current service files
                if (Directory.Exists(servicePath))
                {
                    Directory.Delete(servicePath, true);
                }

                Directory.CreateDirectory(servicePath);

                // Copy backup files to service location
                var copyResult = await _fileManager.CopyServiceFilesAsync(backupPath, servicePath);
                if (!copyResult)
                {
                    _logger.LogError("Failed to restore service files from backup");
                    return false;
                }

                _logger.LogInformation("Service {ServiceName} restored from backup successfully", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore service {ServiceName} from backup", serviceName);
                return false;
            }
        }
    }
}
