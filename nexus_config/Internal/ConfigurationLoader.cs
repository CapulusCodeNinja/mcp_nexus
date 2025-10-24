using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using Nexus.Config.Models;

namespace Nexus.Config.Internal;

/// <summary>
/// Internal implementation of configuration loading functionality.
/// </summary>
internal class ConfigurationLoader : IConfigurationProvider
{
    private readonly IConfiguration m_Configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoader"/> class.
    /// </summary>
    /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
    public ConfigurationLoader(string? configPath = null)
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

    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        throw new NotImplementedException();
    }

    public IChangeToken GetReloadToken()
    {
        throw new NotImplementedException();
    }

    public void Load()
    {
        throw new NotImplementedException();
    }

    public void Set(string key, string? value)
    {
        throw new NotImplementedException();
    }

    public bool TryGet(string key, out string? value)
    {
        throw new NotImplementedException();
    }
}
