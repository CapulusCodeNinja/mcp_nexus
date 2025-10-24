using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexus.setup.Core;
using nexus.setup.Interfaces;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;
using nexus.external_apis.Registry;
using nexus.external_apis.ServiceManagement;
using nexus.config.ServiceRegistration;
using nexus.protocol.Configuration;

namespace nexus.setup.Configuration;

/// <summary>
/// Extension methods for registering nexus.setup services.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds all Nexus services to the dependency injection container.
    /// This is the single entry point for registering all application services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddNexusServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add shared configuration services
        services.AddNexusConfiguration();

        // Add setup services
        services.AddTransient<IProductInstallation, ProductInstallation>();

        // Add protocol services
        services.AddProtocolServices(configuration);

        return services;
    }
}

