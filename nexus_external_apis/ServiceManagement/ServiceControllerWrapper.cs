using System.Runtime.Versioning;
using System.ServiceProcess;

using Microsoft.Win32;

using Nexus.External.Apis.Registry;

namespace Nexus.External.Apis.ServiceManagement;

/// <summary>
/// Concrete implementation of IServiceController that uses the real Windows Service Controller.
/// </summary>
[SupportedOSPlatform("windows")]
public class ServiceControllerWrapper : IServiceController
{
    private readonly IRegistryService m_RegistryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceControllerWrapper"/> class.
    /// </summary>
    public ServiceControllerWrapper()
        : this(new RegistryService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceControllerWrapper"/> class.
    /// </summary>
    /// <param name="registryService">The registry service for reading service configuration.</param>
    protected ServiceControllerWrapper(IRegistryService registryService)
    {
        m_RegistryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
    }

    /// <summary>
    /// Checks if a service is installed.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>True if installed, false otherwise.</returns>
    public bool IsServiceInstalled(string serviceName)
    {
        try
        {
            using var controller = new ServiceController(serviceName);
            _ = controller.Status; // This will throw if service doesn't exist
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current status of a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service status, or null if service not found.</returns>
    public ServiceControllerStatus? GetServiceStatus(string serviceName)
    {
        try
        {
            using var controller = new ServiceController(serviceName);
            return controller.Status;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Starts a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    public void StartService(string serviceName)
    {
        using var controller = new ServiceController(serviceName);
        controller.Start();
    }

    /// <summary>
    /// Stops a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    public void StopService(string serviceName)
    {
        using var controller = new ServiceController(serviceName);

        if (controller.Status != ServiceControllerStatus.Stopped)
        {
            controller.Stop();
        }
    }

    /// <summary>
    /// Waits for a service to reach a target status.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="targetStatus">The target status.</param>
    /// <param name="timeout">The timeout period.</param>
    public void WaitForServiceStatus(string serviceName, ServiceControllerStatus targetStatus, TimeSpan timeout)
    {
        using var controller = new ServiceController(serviceName);
        controller.WaitForStatus(targetStatus, timeout);
    }

    /// <summary>
    /// Gets the executable path of a Windows service using the registry.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service executable path, or null if not found.</returns>
    public string? GetServiceExecutablePath(string serviceName)
    {
        try
        {
            // Read ImagePath from registry: HKLM\SYSTEM\CurrentControlSet\Services\{ServiceName}
            var keyPath = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
            var imagePath = m_RegistryService.ReadString(RegistryHive.LocalMachine, keyPath, "ImagePath");

            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            // Remove quotes and arguments
            imagePath = imagePath.Trim('\"');
            var spaceIndex = imagePath.IndexOf(" --", StringComparison.Ordinal);
            if (spaceIndex > 0)
            {
                imagePath = imagePath[..spaceIndex];
            }

            return imagePath.Trim();
        }
        catch
        {
            return null;
        }
    }
}
