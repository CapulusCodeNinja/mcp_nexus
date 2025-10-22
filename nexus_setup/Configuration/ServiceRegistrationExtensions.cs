using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using nexus.setup.Core;

namespace nexus.setup.Configuration;

/// <summary>
/// Extension methods for registering nexus.setup services.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds nexus.setup services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddNexusSetupServices(this IServiceCollection services)
    {
        services.AddTransient<IServiceInstaller, ServiceInstaller>();
        services.AddTransient<IServiceUpdater, ServiceUpdater>();
        return services;
    }
}

