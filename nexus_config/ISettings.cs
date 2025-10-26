using Microsoft.Extensions.Logging;

using Nexus.Config.Models;

namespace Nexus.Config
{
    /// <summary>
    /// Provides access to shared application settings loaded from configuration.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Configures logging for the application.
        /// </summary>
        /// <param name="logging">The logging builder to configure.</param>
        /// <param name="isServiceMode">Whether the application is running in service mode.</param>
        void ConfigureLogging(ILoggingBuilder logging, bool isServiceMode);

        /// <summary>
        /// Gets the shared configuration snapshot.
        /// </summary>
        /// <returns>The <see cref="SharedConfiguration"/> instance loaded by the configuration loader.</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration has not been loaded yet.</exception>
        SharedConfiguration Get();
    }
}
