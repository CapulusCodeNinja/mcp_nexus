namespace nexus.config
{
    using Models;

    /// <summary>
    /// Provides access to shared application settings loaded from configuration.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Gets the shared configuration snapshot.
        /// </summary>
        /// <returns>The <see cref="SharedConfiguration"/> instance loaded by the configuration loader.</returns>
        /// <exception cref="InvalidOperationException">Thrown when configuration has not been loaded yet.</exception>
        SharedConfiguration Get();
    }
}
