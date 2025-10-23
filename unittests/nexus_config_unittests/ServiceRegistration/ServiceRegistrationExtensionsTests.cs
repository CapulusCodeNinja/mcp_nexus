using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using nexus.config;
using nexus.config.Internal;
using nexus.config.Models;
using nexus.config.ServiceRegistration;
using Xunit;
using NexusConfigProvider = nexus.config.IConfigurationProvider;

namespace nexus.config_unittests.ServiceRegistration;

/// <summary>
/// Unit tests for ServiceRegistrationExtensions.
/// </summary>
public class ServiceRegistrationExtensionsTests
{
    /// <summary>
    /// Tests that AddNexusConfiguration registers all required services.
    /// </summary>
    [Fact]
    public void AddNexusConfiguration_WithValidServices_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNexusConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<IConfiguration>().Should().NotBeNull();
        serviceProvider.GetService<NexusConfigProvider>().Should().NotBeNull();
        serviceProvider.GetService<SharedConfiguration>().Should().NotBeNull();
        serviceProvider.GetService<ILoggingConfigurator>().Should().NotBeNull();
    }

    /// <summary>
    /// Tests that AddNexusConfiguration with custom path works correctly.
    /// </summary>
    [Fact]
    public void AddNexusConfiguration_WithCustomPath_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var customPath = AppContext.BaseDirectory; // Use the test output directory

        // Act
        services.AddNexusConfiguration(customPath);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<IConfiguration>().Should().NotBeNull();
        serviceProvider.GetService<NexusConfigProvider>().Should().NotBeNull();
        serviceProvider.GetService<SharedConfiguration>().Should().NotBeNull();
        serviceProvider.GetService<ILoggingConfigurator>().Should().NotBeNull();
    }

    /// <summary>
    /// Tests that AddNexusLogging configures logging correctly.
    /// </summary>
    [Fact]
    public void AddNexusLogging_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        mockLoggingBuilder.Setup(x => x.Services).Returns(new ServiceCollection());

        // Act & Assert
        var action = () => mockLoggingBuilder.Object.AddNexusLogging(mockConfiguration.Object, false);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that AddNexusLogging with service mode works correctly.
    /// </summary>
    [Fact]
    public void AddNexusLogging_WithServiceMode_ShouldNotThrow()
    {
        // Arrange
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLoggingBuilder = new Mock<ILoggingBuilder>();
        mockLoggingBuilder.Setup(x => x.Services).Returns(new ServiceCollection());

        // Act & Assert
        var action = () => mockLoggingBuilder.Object.AddNexusLogging(mockConfiguration.Object, true);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that registered services are singletons.
    /// </summary>
    [Fact]
    public void AddNexusConfiguration_RegisteredServices_ShouldBeSingletons()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNexusConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        var config1 = serviceProvider.GetService<IConfiguration>();
        var config2 = serviceProvider.GetService<IConfiguration>();
        config1.Should().BeSameAs(config2);

        var provider1 = serviceProvider.GetService<NexusConfigProvider>();
        var provider2 = serviceProvider.GetService<NexusConfigProvider>();
        provider1.Should().BeSameAs(provider2);

        var sharedConfig1 = serviceProvider.GetService<SharedConfiguration>();
        var sharedConfig2 = serviceProvider.GetService<SharedConfiguration>();
        sharedConfig1.Should().BeSameAs(sharedConfig2);

        var loggingConfigurator1 = serviceProvider.GetService<ILoggingConfigurator>();
        var loggingConfigurator2 = serviceProvider.GetService<ILoggingConfigurator>();
        loggingConfigurator1.Should().BeSameAs(loggingConfigurator2);
    }

    /// <summary>
    /// Tests that ILoggingConfigurator is implemented by LoggingConfiguration.
    /// </summary>
    [Fact]
    public void AddNexusConfiguration_LoggingConfigurator_ShouldBeLoggingConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNexusConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var loggingConfigurator = serviceProvider.GetService<ILoggingConfigurator>();
        loggingConfigurator.Should().BeOfType<LoggingConfiguration>();
    }

    /// <summary>
    /// Tests that IConfigurationProvider is implemented by ConfigurationLoader.
    /// </summary>
    [Fact]
    public void AddNexusConfiguration_ConfigurationProvider_ShouldBeConfigurationLoader()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNexusConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var configurationProvider = serviceProvider.GetService<NexusConfigProvider>();
        configurationProvider.Should().BeOfType<ConfigurationLoader>();
    }
}
