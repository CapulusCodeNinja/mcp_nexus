using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using nexus.config.Internal;
using nexus.config.Models;

namespace nexus.config
{
    /// <summary>
    /// Concrete settings facade providing access to configuration and logging setup.
    /// </summary>
    public class Settings : ISettings
    {
        private static Settings? m_Instance;

        private ConfigurationLoader m_ConfigurationLoader;
        private readonly LoggingConfiguration m_LoggingConfiguration;

        private Settings()
        {
            m_ConfigurationLoader = new ConfigurationLoader();
            m_LoggingConfiguration = new LoggingConfiguration();
        }

        /// <summary>
        /// Gets a singleton instance exposing the <see cref="ISettings"/> API.
        /// </summary>
        /// <returns>The singleton <see cref="ISettings"/> instance.</returns>
        public static ISettings GetInstance()
        {
            return m_Instance ??= new Settings();
        }

        /// <summary>
        /// Configures logging using NLog and provided configuration.
        /// </summary>
        /// <param name="logging">The logging builder.</param>
        /// <param name="configuration">The configuration root.</param>
        /// <param name="isServiceMode">Whether running as Windows Service.</param>
        public void ConfigureLogging(ILoggingBuilder logging, IConfiguration configuration, bool isServiceMode)
        {
            m_LoggingConfiguration.ConfigureLogging(logging, configuration, isServiceMode);
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
        /// </summary>
        /// <returns>The shared configuration model.</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration wasn't loaded.</exception>
        public SharedConfiguration Get()
        {
            return m_ConfigurationLoader!.GetSharedConfiguration();
        }
    }
}
