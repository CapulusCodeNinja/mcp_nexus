using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;
using nexus.external_apis.Registry;
using nexus.external_apis.ServiceManagement;

namespace nexus.Configuration;

/// <summary>
/// Project-wide service registration for common dependencies.
/// All shared utilities and external API dependencies are registered here.
/// </summary>
public static class ProjectWideServiceRegistration
{
    /// <summary>
    /// Registers all common dependencies that are shared across the application.
    /// This includes external APIs and other cross-cutting concerns used project-wide.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddCommonServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Register external API services (shared utilities)
        services.AddSingleton<IFileSystem, nexus.external_apis.FileSystem.FileSystem>();
        services.AddSingleton<IProcessManager, ProcessManager>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IServiceController, ServiceControllerWrapper>();

        return services;
    }
}
