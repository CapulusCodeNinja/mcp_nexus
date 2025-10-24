using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nexus.engine;
using nexus.engine.batch;
using nexus.engine.batch.Internal;
using nexus.engine.Configuration;
using nexus.protocol.Notifications;
using nexus.protocol.Services;
using nexus.external_apis.FileSystem;
using nexus.external_apis.ProcessManagement;

namespace nexus.protocol.Configuration;

/// <summary>
/// Static helper class for registering protocol services in the dependency injection container.
/// Provides extension methods to add all MCP protocol services at once.
/// </summary>
public static class ProtocolServiceRegistration
{
    /// <summary>
    /// Adds all MCP protocol services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProtocolServices(
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

        // Register debug engine (depends on centrally registered IFileSystem and IProcessManager)
        services.AddSingleton<IDebugEngine>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            var processManager = sp.GetRequiredService<IProcessManager>();
            var batchProcessor = sp.GetRequiredService<IBatchProcessor>();
            return new DebugEngine(loggerFactory, engineConfig, fileSystem, processManager, batchProcessor);
        });

        services.AddSingleton<IProtocolServer, ProtocolServer>();
        services.AddSingleton<INotificationBridge, StdioNotificationBridge>();
        services.AddSingleton<IMcpNotificationService, McpNotificationService>();
        services.AddSingleton<IMcpToolDefinitionService, McpToolDefinitionService>();

        services.AddTransient<Middleware.ContentTypeValidationMiddleware>();
        services.AddTransient<Middleware.JsonRpcLoggingMiddleware>();
        services.AddTransient<Middleware.ResponseFormattingMiddleware>();

        return services;
    }
}

