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

    /// <summary>
    /// Waits for a service to reach a specific status.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="targetStatus">The target status to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service reached the target status, false if timeout occurred.</returns>
    Task<bool> WaitForServiceStatusAsync(string serviceName, string targetStatus, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the project for deployment.
    /// </summary>
    /// <param name="projectPath">Path to the project file or directory.</param>
    /// <param name="configuration">Build configuration (e.g., "Release", "Debug").</param>
    /// <param name="outputPath">Output path for build artifacts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the build succeeded, false otherwise.</returns>
    Task<bool> BuildProjectAsync(string projectPath, string configuration = "Release", string? outputPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies application files from source to installation directory.
    /// </summary>
    /// <param name="sourceDirectory">Source directory containing the application files.</param>
    /// <param name="targetDirectory">Target installation directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the copy operation succeeded, false otherwise.</returns>
    Task<bool> CopyApplicationFilesAsync(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken = default);
}

