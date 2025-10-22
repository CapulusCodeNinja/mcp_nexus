using nexus.setup.Models;

namespace nexus.setup;

/// <summary>
/// Interface for installing Windows services.
/// </summary>
public interface IServiceInstaller
{
    /// <summary>
    /// Installs a Windows service.
    /// </summary>
    /// <param name="options">Installation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installation result.</returns>
    Task<ServiceInstallationResult> InstallServiceAsync(ServiceInstallationOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls a Windows service.
    /// </summary>
    /// <param name="serviceName">The name of the service to uninstall.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The uninstallation result.</returns>
    Task<ServiceInstallationResult> UninstallServiceAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a service is installed.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>True if the service is installed, false otherwise.</returns>
    bool IsServiceInstalled(string serviceName);

    /// <summary>
    /// Gets the status of a service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The service status, or null if the service doesn't exist.</returns>
    string? GetServiceStatus(string serviceName);
}

