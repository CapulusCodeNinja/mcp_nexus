using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Nexus.Protocol.Configuration;

using Xunit;

namespace Nexus.Protocol.Unittests.Configuration;

/// <summary>
/// Unit tests for the <see cref="HttpServerSetup"/> class.
/// </summary>
public class HttpServerSetupTests
{

    /// <summary>
    /// Verifies that ConfigureHttpServices with default configuration succeeds.
    /// </summary>
    [Fact]
    public void ConfigureHttpServices_WithDefaults_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        HttpServerSetup.ConfigureHttpServices(services);

        // Assert
        Assert.NotNull(services);
        Assert.True(services.Count > 0);
    }

    /// <summary>
    /// Verifies that ConfigureHttpServices with custom configuration succeeds.
    /// </summary>
    [Fact]
    public void ConfigureHttpServices_WithCustomConfig_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new HttpServerConfiguration
        {
            EnableCors = true,
            EnableRateLimit = true,
            MaxRequestBodySize = 1024 * 1024,
        };

        // Act
        HttpServerSetup.ConfigureHttpServices(services, config);

        // Assert
        Assert.NotNull(services);
        Assert.True(services.Count > 0);
    }

    /// <summary>
    /// Verifies that ConfigureHttpServices with CORS disabled succeeds.
    /// </summary>
    [Fact]
    public void ConfigureHttpServices_WithCorsDisabled_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new HttpServerConfiguration
        {
            EnableCors = false,
            EnableRateLimit = false,
        };

        // Act
        HttpServerSetup.ConfigureHttpServices(services, config);

        // Assert
        Assert.NotNull(services);
    }



    /// <summary>
    /// Verifies that ConfigureStdioServices configures services successfully.
    /// </summary>
    [Fact]
    public void ConfigureStdioServices_WithValidServices_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        HttpServerSetup.ConfigureStdioServices(services);

        // Assert
        Assert.NotNull(services);
        Assert.True(services.Count > 0);
    }

    /// <summary>
    /// Verifies that ConfigureStdioServices throws when services is null.
    /// </summary>
    [Fact]
    public void ConfigureStdioServices_WithNullServices_Throws()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            HttpServerSetup.ConfigureStdioServices(null!));
    }



    /// <summary>
    /// Verifies that CreateConfiguredHost creates host in non-service mode.
    /// </summary>
    [Fact]
    public void CreateConfiguredHost_WithNonServiceMode_CreatesHost()
    {
        // Act
        var host = HttpServerSetup.CreateConfiguredHost(false);

        // Assert
        Assert.NotNull(host);
        _ = Assert.IsAssignableFrom<IHost>(host);
    }

    /// <summary>
    /// Verifies that CreateConfiguredHost creates host in service mode.
    /// </summary>
    [Fact]
    public void CreateConfiguredHost_WithServiceMode_CreatesHost()
    {
        // Act
        var host = HttpServerSetup.CreateConfiguredHost(true);

        // Assert
        Assert.NotNull(host);
        _ = Assert.IsAssignableFrom<IHost>(host);
    }
}
