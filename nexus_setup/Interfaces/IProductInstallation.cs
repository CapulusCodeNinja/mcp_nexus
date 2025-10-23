using nexus.setup.Models;

namespace nexus.setup.Interfaces
{
    /// <summary>
    /// Defines the interface for product installation operations.
    /// </summary>
    public interface IProductInstallation
    {
        /// <summary>
        /// Installs a Windows service with the specified options.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="displayName">Display name of the service.</param>
        /// <param name="startMode">Service start mode.</param>
        /// <returns>True if installation succeeded, false otherwise.</returns>
        Task<bool> InstallServiceAsync(string serviceName, string displayName, ServiceStartMode startMode);

        /// <summary>
        /// Updates an existing Windows service.
        /// </summary>
        /// <param name="serviceName">Name of the service to update.</param>
        /// <returns>True if update succeeded, false otherwise.</returns>
        Task<bool> UpdateServiceAsync(string serviceName);
    }
}

