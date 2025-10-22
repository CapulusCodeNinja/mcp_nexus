using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mcp_nexus.Protocol.Notifications;
using mcp_nexus.Protocol.Services;
using mcp_nexus.Utilities.FileSystem;

namespace mcp_nexus.Protocol.Configuration;

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

        services.AddSingleton<IProtocolServer, ProtocolServer>();
        services.AddSingleton<INotificationBridge, StdioNotificationBridge>();
        services.AddSingleton<IMcpNotificationService, McpNotificationService>();
        services.AddSingleton<IMcpToolDefinitionService, McpToolDefinitionService>();
        services.AddSingleton<IFileSystem, Utilities.FileSystem.FileSystem>();

        services.AddTransient<Middleware.ContentTypeValidationMiddleware>();
        services.AddTransient<Middleware.JsonRpcLoggingMiddleware>();
        services.AddTransient<Middleware.ResponseFormattingMiddleware>();

        return services;
    }
}

