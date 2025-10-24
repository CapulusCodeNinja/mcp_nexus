using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.engine;
using nexus.engine.batch;
using nexus.engine.batch.Internal;
using nexus.engine.Configuration;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;

namespace nexus.engine.ServiceRegistration;

/// <summary>
/// Extension methods for registering nexus.engine services.
/// </summary>
internal static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds nexus engine services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexusServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Configure debug engine from config
        var engineConfig = new DebugEngineConfiguration();
        configuration.GetSection("McpNexus:DebugEngine").Bind(engineConfig);
        services.AddSingleton(engineConfig);

        // Register batch processor
        services.AddSingleton<IBatchProcessor>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new BatchProcessor(
                engineConfig.Batching.Enabled,
                engineConfig.Batching.MinBatchSize,
                engineConfig.Batching.MaxBatchSize,
                engineConfig.Batching.ExcludedCommands,
                loggerFactory);
        });

        // Register debug engine
        services.AddSingleton<IDebugEngine>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            var processManager = sp.GetRequiredService<IProcessManager>();
            var batchProcessor = sp.GetRequiredService<IBatchProcessor>();
            return new DebugEngine(loggerFactory, engineConfig, fileSystem, processManager, batchProcessor);
        });

        return services;
    }
}
