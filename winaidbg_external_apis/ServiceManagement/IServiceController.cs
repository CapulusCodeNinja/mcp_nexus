using System.Runtime.Versioning;
using System.ServiceProcess;

namespace WinAiDbg.External.Apis.ServiceManagement;

/// <summary>
/// Interface for Windows Service Controller operations to enable mocking in tests.
/// </summary>
[SupportedOSPlatform("windows")]
public interface IServiceController
{
    /// <summary>
    /// Checks if a service is installed.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>True if installed, false otherwise.</returns>
    bool IsServiceInstalled(string serviceName);

    /// <summary>
    /// Gets the current status of a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service status, or null if service not found.</returns>
    ServiceControllerStatus? GetServiceStatus(string serviceName);

    /// <summary>
    /// Starts a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    void StartService(string serviceName);

    /// <summary>
    /// Stops a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    void StopService(string serviceName);

    /// <summary>
    /// Waits for a service to reach a target status.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="targetStatus">The target status.</param>
    /// <param name="timeout">The timeout period.</param>
    void WaitForServiceStatus(string serviceName, ServiceControllerStatus targetStatus, TimeSpan timeout);

    /// <summary>
    /// Gets the executable path of a Windows service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service executable path, or null if not found.</returns>
    string? GetServiceExecutablePath(string serviceName);
}
