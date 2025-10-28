using Microsoft.Extensions.Logging;

using Nexus.Config.Internal;
using Nexus.Config.Models;

namespace Nexus.Config
{
    /// <summary>
    /// Concrete settings facade providing access to configuration and logging setup.
    /// </summary>
    public class Settings : ISettings
    {
        private ConfigurationLoader m_ConfigurationLoader;
        private readonly LoggingConfiguration m_LoggingConfiguration;
        private SharedConfiguration? m_CachedConfiguration;

        /// <summary>
        /// Gets the singleton instance of the settings.
        /// </summary>
        public static ISettings Instance { get; } = new Settings();

        private Settings()
        {
            m_ConfigurationLoader = new ConfigurationLoader();
            m_LoggingConfiguration = new LoggingConfiguration();
        }

        /// <summary>
        /// Configures logging using NLog and provided configuration.
        /// </summary>
        /// <param name="logging">The logging builder.</param>
        /// <param name="isServiceMode">Whether running as Windows Service.</param>
        public void ConfigureLogging(ILoggingBuilder logging, bool isServiceMode)
        {
            m_LoggingConfiguration.ConfigureLogging(logging, isServiceMode);
        }

        /// <summary>
        /// Loads configuration from file system using an optional explicit path.
        /// </summary>
        /// <param name="configPath">Optional configuration path.</param>
        public void LoadConfiguration(string? configPath = null)
        {
            m_ConfigurationLoader = new ConfigurationLoader(configPath);
        }

        /// <summary>
        /// Returns the shared configuration. Requires <see cref="LoadConfiguration"/> to be called first.
        /// Configuration is cached after first access for performance.
        /// </summary>
        /// <returns>The shared configuration model.</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration wasn't loaded.</exception>
        public SharedConfiguration Get()
        {
            return m_CachedConfiguration ??= m_ConfigurationLoader!.GetSharedConfiguration();
        }
    }
}
