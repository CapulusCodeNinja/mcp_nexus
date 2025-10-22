using nexus.setup.Models;

namespace nexus.setup;

/// <summary>
/// Interface for updating installed Windows services.
/// </summary>
public interface IServiceUpdater
{
    /// <summary>
    /// Updates an installed service with new binaries.
    /// </summary>
    /// <param name="serviceName">The name of the service to update.</param>
    /// <param name="newExecutablePath">Path to the new executable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The update result.</returns>
    Task<ServiceInstallationResult> UpdateServiceAsync(string serviceName, string newExecutablePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Backs up the current service binaries.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="backupPath">Path where backup should be created.</param>
    /// <returns>True if backup succeeded, false otherwise.</returns>
    Task<bool> BackupServiceAsync(string serviceName, string backupPath);

    /// <summary>
    /// Restores service binaries from a backup.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="backupPath">Path to the backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The restore result.</returns>
    Task<ServiceInstallationResult> RestoreServiceAsync(string serviceName, string backupPath, CancellationToken cancellationToken = default);
}

