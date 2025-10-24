using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nexus.config.Internal;
using nexus.config.Models;

namespace nexus.config
{
    public class Settings : ISettingsLoader, ISettings
    {
        private static Settings? m_Instance;

        private ConfigurationLoader? m_ConfigurationLoader;
        private LoggingConfiguration m_LoggingConfiguration = new();

        public static ISettings GetInstance()
        {
            return m_Instance ??= new Settings();
        }

        public static ISettingsLoader GetLoader()
        {
            return m_Instance ??= new Settings();
        }

        public void ConfigureLogging(ILoggingBuilder logging, IConfiguration configuration, bool isServiceMode)
        {
            m_LoggingConfiguration.ConfigureLogging(logging, configuration, isServiceMode);
        }

        public void LoadConfiguration(string? configPath = null)
        {
            m_ConfigurationLoader = new ConfigurationLoader(configPath);
        }

        public SharedConfiguration Get()
        {
            return m_ConfigurationLoader?.GetSharedConfiguration()
                ?? throw new InvalidOperationException("Configuration has not been loaded");
        }
    }
}
