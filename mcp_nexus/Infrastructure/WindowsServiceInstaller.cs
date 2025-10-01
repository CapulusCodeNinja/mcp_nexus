namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Windows service installer - maintains compatibility with existing code
    /// </summary>
    public static class WindowsServiceInstaller
    {
        /// <summary>
        /// Installs the Windows service
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Service description</param>
        public static void Install(string serviceName, string displayName, string description)
        {
            // Placeholder implementation for compatibility
            // In a real implementation, this would use ServiceInstaller
            Console.WriteLine($"Installing Windows service: {serviceName}");
        }

        /// <summary>
        /// Uninstalls the Windows service
        /// </summary>
        /// <param name="serviceName">Service name</param>
        public static void Uninstall(string serviceName)
        {
            // Placeholder implementation for compatibility
            Console.WriteLine($"Uninstalling Windows service: {serviceName}");
        }

        /// <summary>
        /// Starts the Windows service
        /// </summary>
        /// <param name="serviceName">Service name</param>
        public static void Start(string serviceName)
        {
            // Placeholder implementation for compatibility
            Console.WriteLine($"Starting Windows service: {serviceName}");
        }

        /// <summary>
        /// Stops the Windows service
        /// </summary>
        /// <param name="serviceName">Service name</param>
        public static void Stop(string serviceName)
        {
            // Placeholder implementation for compatibility
            Console.WriteLine($"Stopping Windows service: {serviceName}");
        }

        /// <summary>
        /// Installs the Windows service asynchronously
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Service description</param>
        public static async Task InstallServiceAsync(string serviceName, string displayName, string description)
        {
            await Task.Run(() => Install(serviceName, displayName, description));
        }

        /// <summary>
        /// Uninstalls the Windows service asynchronously
        /// </summary>
        /// <param name="serviceName">Service name</param>
        public static async Task UninstallServiceAsync(string serviceName)
        {
            await Task.Run(() => Uninstall(serviceName));
        }

        /// <summary>
        /// Force uninstalls the Windows service asynchronously
        /// </summary>
        /// <param name="serviceName">Service name</param>
        public static async Task ForceUninstallServiceAsync(string serviceName)
        {
            await Task.Run(() => Uninstall(serviceName));
        }

        /// <summary>
        /// Updates the Windows service asynchronously
        /// </summary>
        /// <param name="serviceName">Service name</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">Service description</param>
        public static async Task UpdateServiceAsync(string serviceName, string displayName, string description)
        {
            await Task.Run(() => Install(serviceName, displayName, description));
        }
    }
}
