using Microsoft.Extensions.Configuration;
using nexus.config.Models;

namespace nexus.config;

/// <summary>
/// Provides configuration loading and access functionality.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Loads configuration from the specified path or default location.
    /// </summary>
    /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
    /// <returns>Loaded configuration.</returns>
    IConfiguration LoadConfiguration(string? configPath = null);

    /// <summary>
    /// Gets the shared configuration settings.
    /// </summary>
    /// <returns>Shared configuration object.</returns>
    SharedConfiguration GetSharedConfiguration();
}
