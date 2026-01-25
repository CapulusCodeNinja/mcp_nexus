using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using WinAiDbg.Config.Models;

namespace WinAiDbg.Config.Internal;

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
            .AddJsonFile("appsettings.Defaults.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.LocalOverrides.json", optional: true, reloadOnChange: false)
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

    /// <summary>
    /// Gets the child keys for the specified section.
    /// </summary>
    /// <param name="earlierKeys">The sequence of keys that have already been returned.</param>
    /// <param name="parentPath">The parent path to the node.</param>
    /// <returns>An ordered sequence of child keys.</returns>
    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a change token that can be used to observe when this provider is reloaded.
    /// </summary>
    /// <returns>An <see cref="IChangeToken"/> that signals when the provider is reloaded.</returns>
    public IChangeToken GetReloadToken()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Loads the configuration data from the underlying source.
    /// </summary>
    public void Load()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value to set.</param>
    public void Set(string key, string? value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Attempts to retrieve a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">When this method returns, contains the configuration value if found; otherwise, null.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public bool TryGet(string key, out string? value)
    {
        throw new NotImplementedException();
    }
}
