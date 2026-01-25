namespace WinAiDbg.Setup
{
    /// <summary>
    /// Defines the interface for product installation operations.
    /// </summary>
    public interface IProductInstallation
    {
        /// <summary>
        /// Installs a Windows service using configuration settings.
        /// </summary>
        /// <returns>True if installation succeeded, false otherwise.</returns>
        Task<bool> InstallServiceAsync();

        /// <summary>
        /// Updates an existing Windows service using configuration settings.
        /// </summary>
        /// <returns>True if update succeeded, false otherwise.</returns>
        Task<bool> UpdateServiceAsync();

        /// <summary>
        /// Uninstalls the Windows service and removes application files.
        /// </summary>
        /// <returns>True if uninstall succeeded, false otherwise.</returns>
        Task<bool> UninstallServiceAsync();
    }
}
