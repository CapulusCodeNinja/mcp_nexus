using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nexus.protocol.Configuration;
using nexus.protocol.Middleware;
using nexus.protocol.Notifications;
using nexus.protocol.Services;

namespace nexus.protocol.unittests.Configuration;

/// <summary>
/// Unit tests for ProtocolServiceRegistration class.
/// Tests service registration and dependency injection configuration.
/// </summary>
public class ProtocolServiceRegistrationTests
{
    /// <summary>
    /// Verifies that AddProtocolServices registers all required services.
    /// </summary>
    [Fact]
    public void AddProtocolServices_RegistersAllRequiredServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging(); // Required for ILogger<T>

        services.AddProtocolServices(configuration);

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<IProtocolServer>().Should().NotBeNull();
        serviceProvider.GetService<INotificationBridge>().Should().NotBeNull();
        serviceProvider.GetService<IMcpNotificationService>().Should().NotBeNull();
        serviceProvider.GetService<IMcpToolDefinitionService>().Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that AddProtocolServices registers middleware components.
    /// </summary>
    [Fact]
    public void AddProtocolServices_RegistersMiddleware()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging(); // Required for ILogger<T>

        services.AddProtocolServices(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Don't try to instantiate middleware - they need RequestDelegate which isn't in DI
        // Just verify they are registered
        services.Should().Contain(sd => sd.ServiceType == typeof(ContentTypeValidationMiddleware));
        services.Should().Contain(sd => sd.ServiceType == typeof(JsonRpcLoggingMiddleware));
        services.Should().Contain(sd => sd.ServiceType == typeof(ResponseFormattingMiddleware));
    }

    /// <summary>
    /// Verifies that AddProtocolServices throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddProtocolServices_WithNullServices_ThrowsArgumentNullException()
    {
        var configuration = new ConfigurationBuilder().Build();

        var action = () => ProtocolServiceRegistration.AddProtocolServices(null!, configuration);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Verifies that AddProtocolServices throws ArgumentNullException when configuration is null.
    /// </summary>
    [Fact]
    public void AddProtocolServices_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var action = () => services.AddProtocolServices(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// Verifies that AddProtocolServices returns the same service collection for chaining.
    /// </summary>
    [Fact]
    public void AddProtocolServices_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var result = services.AddProtocolServices(configuration);

        result.Should().BeSameAs(services);
    }

    /// <summary>
    /// Verifies that AddProtocolServices registers ProtocolServer as singleton.
    /// </summary>
    [Fact]
    public void AddProtocolServices_RegistersProtocolServerAsSingleton()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();

        services.AddProtocolServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var instance1 = serviceProvider.GetService<IProtocolServer>();
        var instance2 = serviceProvider.GetService<IProtocolServer>();

        instance1.Should().BeSameAs(instance2);
    }

    /// <summary>
    /// Verifies that AddProtocolServices registers notification service as singleton.
    /// </summary>
    [Fact]
    public void AddProtocolServices_RegistersNotificationServiceAsSingleton()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();

        services.AddProtocolServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        var instance1 = serviceProvider.GetService<IMcpNotificationService>();
        var instance2 = serviceProvider.GetService<IMcpNotificationService>();

        instance1.Should().BeSameAs(instance2);
    }

    /// <summary>
    /// Verifies that AddProtocolServices registers middleware as transient.
    /// </summary>
    [Fact]
    public void AddProtocolServices_RegistersMiddlewareAsTransient()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();

        services.AddProtocolServices(configuration);

        // Check that middleware are registered as transient (not singleton)
        var middlewareDescriptors = services.Where(sd => 
            sd.ServiceType == typeof(ContentTypeValidationMiddleware) ||
            sd.ServiceType == typeof(JsonRpcLoggingMiddleware) ||
            sd.ServiceType == typeof(ResponseFormattingMiddleware));

        middlewareDescriptors.Should().AllSatisfy(sd => sd.Lifetime.Should().Be(ServiceLifetime.Transient));
    }
}
