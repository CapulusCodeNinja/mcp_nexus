using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;
using nexus.external_apis.Registry;
using nexus.external_apis.ServiceManagement;

namespace nexus.ServiceRegistration;

/// <summary>
/// Extension methods for registering nexus.setup services.
/// </summary>
internal static class ServiceRegistrationExtensions
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
        // Add utility services
        services.AddSingleton<IFileSystem, nexus.external_apis.FileSystem.FileSystem>();
        services.AddSingleton<IProcessManager, ProcessManager>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IServiceController, ServiceControllerWrapper>();

        return services;
    }
}

