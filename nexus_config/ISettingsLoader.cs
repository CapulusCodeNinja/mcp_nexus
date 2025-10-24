using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nexus.config
{
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
        /// <returns>Loaded configuration.</returns>
        void LoadConfiguration(string? configPath = null);
    }
}
