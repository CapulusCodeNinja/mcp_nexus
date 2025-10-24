using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexus.setup.Core;
using nexus.setup.Interfaces;

namespace nexus.Configuration;

/// <summary>
/// Service registration for setup-specific services.
/// </summary>
public static class SetupServiceRegistration
{
    /// <summary>
    /// Registers setup-specific services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddSetupServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Add setup services
        services.AddTransient<IProductInstallation, ProductInstallation>();

        return services;
    }
}
