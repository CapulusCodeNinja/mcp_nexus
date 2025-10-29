using Microsoft.Extensions.Logging;

using Nexus.Config.Internal;
using Nexus.Config.Models;

namespace Nexus.Config
{
    /// <summary>
    /// Concrete settings facade providing access to configuration and logging setup.
    /// </summary>
    public class Settings : ISettings, IDisposable
    {
        private readonly LoggingConfiguration m_LoggingConfiguration;
        private readonly ReaderWriterLockSlim m_ConfigLock = new();
        private ConfigurationLoader m_ConfigurationLoader;
        private SharedConfiguration? m_CachedConfiguration;
        private bool m_Disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        private Settings()
        {
            m_ConfigurationLoader = new ConfigurationLoader();
            m_LoggingConfiguration = new LoggingConfiguration();
        }

        /// <summary>
        /// Gets the singleton instance of the settings.
        /// </summary>
        public static ISettings Instance { get; } = new Settings();

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
            m_ConfigLock.EnterWriteLock();
            try
            {
                m_ConfigurationLoader = new ConfigurationLoader(configPath);
                m_CachedConfiguration = null; // Clear cache
            }
            finally
            {
                m_ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the shared configuration. Requires <see cref="LoadConfiguration"/> to be called first.
        /// Configuration is cached after first access for performance.
        /// </summary>
        /// <returns>The shared configuration model.</returns>
        /// <exception cref="InvalidOperationException">Thrown if configuration wasn't loaded.</exception>
        public SharedConfiguration Get()
        {
            m_ConfigLock.EnterReadLock();
            try
            {
                if (m_CachedConfiguration != null)
                {
                    return m_CachedConfiguration;
                }
            }
            finally
            {
                m_ConfigLock.ExitReadLock();
            }

            m_ConfigLock.EnterWriteLock();
            try
            {
                if (m_CachedConfiguration != null)
                {
                    return m_CachedConfiguration;
                }

                if (m_ConfigurationLoader == null)
                {
                    throw new InvalidOperationException("Configuration not loaded. Call LoadConfiguration() first.");
                }

                // We know it's null here, so just assign
                m_CachedConfiguration = m_ConfigurationLoader.GetSharedConfiguration();

                return m_CachedConfiguration;
            }
            finally
            {
                m_ConfigLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Disposes of the settings and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_ConfigLock?.Dispose();
            m_Disposed = true;
        }
    }
}
