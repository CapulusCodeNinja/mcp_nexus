using Microsoft.Extensions.Configuration;

using Nexus.Config.Internal;
using Nexus.Config.Models;

namespace Nexus.Config_unittests.Internal;

/// <summary>
/// Test accessor for ConfigurationLoader to expose protected methods for testing.
/// </summary>
internal class ConfigurationLoaderTestAccessor : ConfigurationLoader
{
    private readonly IConfiguration? m_MockConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationLoaderTestAccessor"/> class.
    /// </summary>
    /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
    /// <param name="mockConfiguration">Optional mock configuration for testing.</param>
    public ConfigurationLoaderTestAccessor(string? configPath = null, IConfiguration? mockConfiguration = null)
        : base(configPath)
    {
        m_MockConfiguration = mockConfiguration;
    }

    /// <summary>
    /// Exposes the LoadConfiguration method for testing.
    /// </summary>
    /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
    /// <returns>Loaded configuration.</returns>
    public IConfiguration TestLoadConfiguration(string? configPath = null)
    {
        return m_MockConfiguration ?? LoadConfiguration(configPath);
    }

    /// <summary>
    /// Exposes the GetSharedConfiguration method for testing.
    /// </summary>
    /// <returns>Shared configuration object.</returns>
    public SharedConfiguration TestGetSharedConfiguration()
    {
        if (m_MockConfiguration != null)
        {
            var config = m_MockConfiguration.Get<SharedConfiguration>();
            return config ?? new SharedConfiguration();
        }

        return GetSharedConfiguration();
    }
}
