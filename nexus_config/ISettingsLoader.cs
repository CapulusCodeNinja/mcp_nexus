using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace nexus.config
{
    /// <summary>
    /// Loads application configuration and configures logging infrastructure.
    /// </summary>
    public interface ISettingsLoader
    {
        /// <summary>
        /// Configures logging for the application.
        /// </summary>
        /// <param name="logging">The logging builder to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="isServiceMode">Whether the application is running in service mode.</param>
        void ConfigureLogging(ILoggingBuilder logging, IConfiguration configuration, bool isServiceMode);

        /// <summary>
        /// Loads configuration from the specified path or default location.
        /// </summary>
        /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
        void LoadConfiguration(string? configPath = null);
    }
}
