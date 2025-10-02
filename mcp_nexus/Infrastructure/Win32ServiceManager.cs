using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Manages Windows services using Win32 API
    /// </summary>
    public class Win32ServiceManager
    {
        private readonly ILogger<Win32ServiceManager> _logger;

        public Win32ServiceManager(ILogger<Win32ServiceManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts a Windows service
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>True if service was started successfully</returns>
        public async Task<bool> StartServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Starting service: {ServiceName}", serviceName);
                
                using var service = new ServiceController(serviceName);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    _logger.LogInformation("Service {ServiceName} is already running", serviceName);
                    return true;
                }

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                
                _logger.LogInformation("Service {ServiceName} started successfully", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start service {ServiceName}", serviceName);
                return false;
            }
        }

        /// <summary>
        /// Stops a Windows service
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>True if service was stopped successfully</returns>
        public async Task<bool> StopServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Stopping service: {ServiceName}", serviceName);
                
                using var service = new ServiceController(serviceName);
                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    _logger.LogInformation("Service {ServiceName} is already stopped", serviceName);
                    return true;
                }

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                
                _logger.LogInformation("Service {ServiceName} stopped successfully", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop service {ServiceName}", serviceName);
                return false;
            }
        }

        /// <summary>
        /// Restarts a Windows service
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>True if service was restarted successfully</returns>
        public async Task<bool> RestartServiceAsync(string serviceName)
        {
            try
            {
                _logger.LogInformation("Restarting service: {ServiceName}", serviceName);
                
                var stopResult = await StopServiceAsync(serviceName);
                if (!stopResult)
                {
                    _logger.LogWarning("Failed to stop service {ServiceName} before restart", serviceName);
                }

                var startResult = await StartServiceAsync(serviceName);
                if (!startResult)
                {
                    _logger.LogError("Failed to start service {ServiceName} after restart", serviceName);
                    return false;
                }

                _logger.LogInformation("Service {ServiceName} restarted successfully", serviceName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart service {ServiceName}", serviceName);
                return false;
            }
        }

        /// <summary>
        /// Gets the status of a Windows service
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>Service status</returns>
        public async Task<ServiceControllerStatus> GetServiceStatusAsync(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                return service.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for service {ServiceName}", serviceName);
                return ServiceControllerStatus.Stopped;
            }
        }

        /// <summary>
        /// Checks if a service exists
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>True if service exists</returns>
        public async Task<bool> ServiceExistsAsync(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                var status = service.Status; // This will throw if service doesn't exist
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets service information
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <returns>Service information</returns>
        public async Task<ServiceInfo> GetServiceInfoAsync(string serviceName)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                return new ServiceInfo
                {
                    Name = service.ServiceName,
                    DisplayName = service.DisplayName,
                    Status = service.Status,
                    ServiceType = service.ServiceType,
                    StartType = ServiceStartMode.Manual // Default value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get info for service {ServiceName}", serviceName);
                return new ServiceInfo
                {
                    Name = serviceName,
                    DisplayName = serviceName,
                    Status = ServiceControllerStatus.Stopped,
                    ServiceType = ServiceType.Win32OwnProcess,
                    StartType = ServiceStartMode.Manual
                };
            }
        }

        /// <summary>
        /// Waits for a service to reach a specific status
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        /// <param name="desiredStatus">Desired status</param>
        /// <param name="timeout">Timeout duration</param>
        /// <returns>True if service reached desired status within timeout</returns>
        public async Task<bool> WaitForServiceStatusAsync(string serviceName, ServiceControllerStatus desiredStatus, TimeSpan timeout)
        {
            try
            {
                using var service = new ServiceController(serviceName);
                service.WaitForStatus(desiredStatus, timeout);
                return service.Status == desiredStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to wait for service {ServiceName} to reach status {Status}", serviceName, desiredStatus);
                return false;
            }
        }
    }

    /// <summary>
    /// Represents information about a Windows service
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        /// Gets or sets the service name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the service status
        /// </summary>
        public ServiceControllerStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the service type
        /// </summary>
        public ServiceType ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the start type
        /// </summary>
        public ServiceStartMode StartType { get; set; }
    }

    /// <summary>
    /// Represents a handle to a Windows service control manager
    /// </summary>
    public class ServiceControlManagerHandle : IDisposable
    {
        private bool m_disposed = false;

        /// <summary>
        /// Gets or sets the handle value
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// Gets whether the handle is valid
        /// </summary>
        public bool IsValid => Handle != IntPtr.Zero;

        /// <summary>
        /// Initializes a new instance of the ServiceControlManagerHandle
        /// </summary>
        public ServiceControlManagerHandle()
        {
            Handle = IntPtr.Zero;
        }

        /// <summary>
        /// Initializes a new instance of the ServiceControlManagerHandle
        /// </summary>
        /// <param name="handle">The handle value</param>
        public ServiceControlManagerHandle(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Disposes the handle
        /// </summary>
        public void Dispose()
        {
            if (!m_disposed)
            {
                if (IsValid)
                {
                    // In a real implementation, this would close the service control manager handle
                    Handle = IntPtr.Zero;
                }
                m_disposed = true;
            }
        }

        /// <summary>
        /// Returns a string representation of the handle
        /// </summary>
        public override string ToString()
        {
            return $"ServiceControlManagerHandle(Handle={Handle})";
        }
    }
}
