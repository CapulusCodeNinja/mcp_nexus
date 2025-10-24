using Microsoft.Extensions.DependencyInjection;
using nexus.protocol.Notifications;
using nexus.protocol.Services;

namespace nexus.protocol.ServiceRegistration;

/// <summary>
/// Extension methods for registering nexus.protocol services.
/// </summary>
internal static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds all MCP protocol services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNexusServices(
        this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

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
