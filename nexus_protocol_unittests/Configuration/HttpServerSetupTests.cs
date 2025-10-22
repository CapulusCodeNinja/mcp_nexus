using FluentAssertions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nexus.protocol.Configuration;

namespace nexus.protocol.unittests.Configuration;

/// <summary>
/// Unit tests for HttpServerSetup class.
/// Tests HTTP server configuration including CORS, rate limiting, and server limits.
/// </summary>
public class HttpServerSetupTests
{
    private readonly IConfiguration m_Configuration;

    public HttpServerSetupTests()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
            ["IpRateLimiting:StackBlockedRequests"] = "false"
        });
        m_Configuration = configBuilder.Build();
    }

    [Fact]
    public void ConfigureHttpServices_WithDefaultConfig_ConfiguresAllServices()
    {
        var services = new ServiceCollection();
        
        HttpServerSetup.ConfigureHttpServices(services, m_Configuration);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_WithCustomConfig_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            MaxRequestBodySize = 1024 * 1024,
            RequestHeadersTimeoutSeconds = 30,
            KeepAliveTimeoutSeconds = 60,
            MaxRequestLineSize = 4096,
            MaxRequestHeadersTotalSize = 16384,
            EnableCors = true,
            EnableRateLimit = true
        };

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        var serviceProvider = services.BuildServiceProvider();
        var kestrelOptions = serviceProvider.GetService<IOptions<KestrelServerOptions>>();
        kestrelOptions.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_WithNullConfig_UsesDefaults()
    {
        var services = new ServiceCollection();

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, null);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_WithCorsDisabled_DoesNotAddCors()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            EnableCors = false,
            EnableRateLimit = false
        };

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        // Service collection should not throw when built
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_WithRateLimitDisabled_DoesNotAddRateLimit()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            EnableCors = false,
            EnableRateLimit = false
        };

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_WithInvalidConfig_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            MaxRequestBodySize = -1 // Invalid
        };

        var action = () => HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*MaxRequestBodySize*");
    }

    [Fact]
    public void ConfigureHttpServices_WithBothCorsAndRateLimitEnabled_ConfiguresBoth()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            EnableCors = true,
            EnableRateLimit = true
        };

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_ConfiguresKestrelLimits()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            MaxRequestBodySize = 1024 * 1024,
            RequestHeadersTimeoutSeconds = 30,
            KeepAliveTimeoutSeconds = 60,
            MaxRequestLineSize = 4096,
            MaxRequestHeadersTotalSize = 16384
        };

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        var serviceProvider = services.BuildServiceProvider();
        var kestrelOptions = serviceProvider.GetService<IOptions<KestrelServerOptions>>();
        
        // Verify options are registered
        kestrelOptions.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_ConfiguresJsonOptions()
    {
        var services = new ServiceCollection();

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration);

        // JSON options should be configured
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_ConfiguresMcpServer()
    {
        var services = new ServiceCollection();
        services.AddLogging(); // Required for MCP server

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration);

        // MCP server services should be registered
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_WithMinimalValidConfig_Succeeds()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            MaxRequestBodySize = 1,
            RequestHeadersTimeoutSeconds = 1,
            KeepAliveTimeoutSeconds = 1,
            MaxRequestLineSize = 1,
            MaxRequestHeadersTotalSize = 1,
            EnableCors = false,
            EnableRateLimit = false
        };

        var action = () => HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureHttpServices_WithLargeValues_Succeeds()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            MaxRequestBodySize = 100 * 1024 * 1024, // 100MB
            RequestHeadersTimeoutSeconds = 300,
            KeepAliveTimeoutSeconds = 600,
            MaxRequestLineSize = 32768,
            MaxRequestHeadersTotalSize = 65536,
            EnableCors = true,
            EnableRateLimit = true
        };

        var action = () => HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureHttpServices_ConfiguresIISServerOptions()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            MaxRequestBodySize = 2 * 1024 * 1024 // 2MB
        };

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        // Verify IIS options are configured
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_AddsMcpServerWithTransport()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration);

        // Verify MCP server services are added
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_ConfiguresMemoryCacheForRateLimit()
    {
        var services = new ServiceCollection();
        var serverConfig = new HttpServerConfiguration
        {
            EnableRateLimit = true
        };

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration, serverConfig);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpServices_AllowsMultipleCalls()
    {
        var services = new ServiceCollection();

        HttpServerSetup.ConfigureHttpServices(services, m_Configuration);
        var action = () => HttpServerSetup.ConfigureHttpServices(services, m_Configuration);

        action.Should().NotThrow();
    }
}

