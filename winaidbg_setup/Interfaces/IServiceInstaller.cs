using System.Runtime.Versioning;
using System.ServiceProcess;

using WinAiDbg.Setup.Models;

namespace WinAiDbg.Setup.Interfaces;

/// <summary>
/// Interface for Windows service installation operations.
/// </summary>
[SupportedOSPlatform("windows")]
internal interface IServiceInstaller
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
    /// <param name="serviceName">The name of the service to check.</param>
    /// <returns>True if the service is installed; otherwise, false.</returns>
    bool IsServiceInstalled(string serviceName);

    /// <summary>
    /// Gets the current status of a service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The service status, or null if the service is not found.</returns>
    ServiceControllerStatus? GetServiceStatus(string serviceName);

    /// <summary>
    /// Waits for a service to reach a target status.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="targetStatus">The target status to wait for.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service reached the target status; otherwise, false.</returns>
    Task<bool> WaitForServiceStatusAsync(string serviceName, string targetStatus, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a .NET project.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="configuration">The build configuration (e.g., "Release", "Debug").</param>
    /// <param name="outputPath">The output path for build artifacts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the build succeeded; otherwise, false.</returns>
    Task<bool> BuildProjectAsync(string projectPath, string configuration = "Release", string? outputPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies application files from source to target directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory.</param>
    /// <param name="targetDirectory">The target directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the copy succeeded; otherwise, false.</returns>
    Task<bool> CopyApplicationFilesAsync(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken = default);
}
