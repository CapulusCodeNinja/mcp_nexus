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
        private static Settings? m_Instance;
        private static readonly object m_Lock = new();

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
            if (m_Instance == null)
            {
                lock (m_Lock)
                {
                    m_Instance ??= new Settings();
                }
            }
            return m_Instance;
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
        /// </summary>
        /// <returns>The shared configuration model.</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration wasn't loaded.</exception>
        public SharedConfiguration Get()
        {
            return m_ConfigurationLoader!.GetSharedConfiguration();
        }
    }
}
