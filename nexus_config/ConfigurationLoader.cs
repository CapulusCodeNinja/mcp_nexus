using Microsoft.Extensions.Configuration;
using nexus.config.Models;

namespace nexus.config;

/// <summary>
/// Internal implementation of configuration loading functionality.
/// </summary>
public class ConfigurationLoader : IConfigurationProvider
{
    private readonly IConfiguration m_Configuration;

    private static IConfigurationProvider? m_Instance;

    public static IConfigurationProvider GetInstance(string? configPath = null)
    {
        return m_Instance ??= new ConfigurationLoader(configPath);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoader"/> class.
    /// </summary>
    /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
    private ConfigurationLoader(string? configPath = null)
    {
        m_Configuration = LoadConfiguration(configPath);
    }

    /// <summary>
    /// Loads configuration from the specified path or default location.
    /// </summary>
    /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
    /// <returns>Loaded configuration.</returns>
    public virtual IConfiguration LoadConfiguration(string? configPath = null)
    {
        var basePath = configPath ?? AppContext.BaseDirectory;

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Gets the shared configuration settings.
    /// </summary>
    /// <returns>Shared configuration object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when configuration cannot be loaded.</exception>
    public virtual SharedConfiguration GetSharedConfiguration()
    {
        return m_Configuration.Get<SharedConfiguration>()
            ?? throw new InvalidOperationException("Failed to load configuration");
    }
}
