using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using nexus.setup.Core;
using nexus.setup.Interfaces;
using nexus.utilities.FileSystem;
using nexus.utilities.ProcessManagement;
using nexus.utilities.Registry;
using nexus.utilities.ServiceManagement;

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
        // Register utility services
        services.AddSingleton<IFileSystem, nexus.utilities.FileSystem.FileSystem>();
        services.AddSingleton<IProcessManager, ProcessManager>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IServiceController, ServiceControllerWrapper>();

        // Register setup services
        services.AddTransient<IProductInstallation, ProductInstallation>();
        
        return services;
    }
}

