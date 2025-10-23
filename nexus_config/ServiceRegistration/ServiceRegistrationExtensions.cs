using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.config.Internal;

namespace nexus.config.ServiceRegistration;

/// <summary>
/// Extension methods for registering nexus.config services.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds nexus configuration services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configPath">Optional configuration path. If null, uses default location.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexusConfiguration(
        this IServiceCollection services,
        string? configPath = null)
    {
        var configProvider = new ConfigurationLoader(configPath);
        var configuration = configProvider.LoadConfiguration(configPath);
        var sharedConfig = configProvider.GetSharedConfiguration();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IConfigurationProvider>(configProvider);
        services.AddSingleton(sharedConfig);
        services.AddSingleton<ILoggingConfigurator, LoggingConfiguration>();

        return services;
    }

    /// <summary>
    /// Adds nexus logging configuration to the logging builder.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="isServiceMode">Whether the application is running in service mode.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder AddNexusLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        bool isServiceMode)
    {
        var configurator = new LoggingConfiguration();
        configurator.ConfigureLogging(logging, configuration, isServiceMode);
        return logging;
    }
}
