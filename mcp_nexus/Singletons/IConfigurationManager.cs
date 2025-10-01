namespace mcp_nexus.Singletons
{
    /// <summary>
    /// Singleton interface for configuration management
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Gets a configuration value by key
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value or null if not found</returns>
        string? GetValue(string key);

        /// <summary>
        /// Gets a configuration value by key with default
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="defaultValue">Default value if key not found</param>
        /// <returns>Configuration value or default</returns>
        string GetValue(string key, string defaultValue);

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value</param>
        void SetValue(string key, string value);

        /// <summary>
        /// Checks if a configuration key exists
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>True if key exists, false otherwise</returns>
        bool ContainsKey(string key);
    }
}
