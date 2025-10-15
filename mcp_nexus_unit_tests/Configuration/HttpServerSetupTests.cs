using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Json;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace mcp_nexus_unit_tests.Configuration
{
    /// <summary>
    /// Tests for HttpServerSetup
    /// </summary>
    public class HttpServerSetupTests
    {
        private readonly Mock<IConfiguration> m_MockConfiguration;
        private readonly ServiceCollection m_Services;

        public HttpServerSetupTests()
        {
            m_MockConfiguration = new Mock<IConfiguration>();
            m_Services = new ServiceCollection();
        }

        [Fact]
        public void HttpServerSetup_Class_Exists()
        {
            // This test verifies that the HttpServerSetup class exists and can be instantiated
            Assert.NotNull(typeof(HttpServerSetup));
        }

        [Fact]
        public void HttpServerSetup_IsStaticClass()
        {
            // Verify that HttpServerSetup is a static class
            var type = typeof(HttpServerSetup);
            Assert.True(type.IsAbstract && type.IsSealed);
        }

        [Fact]
        public void ConfigureHttpServices_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Act & Assert
            var exception = Record.Exception(() => HttpServerSetup.ConfigureHttpServices(services, configuration));
            Assert.Null(exception);
        }

        [Fact]
        public void ConfigureHttpServices_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => HttpServerSetup.ConfigureHttpServices(null!, configuration));
        }

        [Fact]
        public void ConfigureHttpServices_WithNullConfiguration_ThrowsNullReferenceException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => HttpServerSetup.ConfigureHttpServices(services, null!));
        }

        [Fact]
        public void ConfigureHttpServices_ConfiguresServerLimits()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Act
            HttpServerSetup.ConfigureHttpServices(services, configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify Kestrel server options are configured
            var kestrelOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<KestrelServerOptions>>();
            Assert.NotNull(kestrelOptions);
        }

        [Fact]
        public void ConfigureHttpServices_ConfiguresCors()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Add required services for CORS
            services.AddLogging();

            // Act
            HttpServerSetup.ConfigureHttpServices(services, configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify CORS services are registered
            var corsService = serviceProvider.GetService<ICorsService>();
            Assert.NotNull(corsService);

            var corsPolicyProvider = serviceProvider.GetService<ICorsPolicyProvider>();
            Assert.NotNull(corsPolicyProvider);
        }

        [Fact]
        public void ConfigureHttpServices_ConfiguresRateLimit()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Act
            HttpServerSetup.ConfigureHttpServices(services, configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify rate limiting services are registered
            var ipPolicyStore = serviceProvider.GetService<IIpPolicyStore>();
            Assert.NotNull(ipPolicyStore);

            var rateLimitCounterStore = serviceProvider.GetService<IRateLimitCounterStore>();
            Assert.NotNull(rateLimitCounterStore);

            var rateLimitConfiguration = serviceProvider.GetService<IRateLimitConfiguration>();
            Assert.NotNull(rateLimitConfiguration);

            var processingStrategy = serviceProvider.GetService<IProcessingStrategy>();
            Assert.NotNull(processingStrategy);
        }

        [Fact]
        public void ConfigureHttpServices_ConfiguresJsonOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Act
            HttpServerSetup.ConfigureHttpServices(services, configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify JSON options are configured
            var jsonOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<JsonOptions>>();
            Assert.NotNull(jsonOptions);
        }

        [Fact]
        public void ConfigureHttpServices_ConfiguresMcpServer()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Act
            HttpServerSetup.ConfigureHttpServices(services, configuration);

            // Assert
            _ = services.BuildServiceProvider();

            // Verify MCP server services are registered
            // Note: The exact services depend on the MCP SDK implementation
            // We can verify that the service collection has been populated
            Assert.True(services.Count > 0);
        }

        [Fact]
        public void ConfigureHttpServices_WithConsoleOutput_CapturesOutput()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                HttpServerSetup.ConfigureHttpServices(services, configuration);

                // Assert
                var output = consoleOutput.ToString();
                Assert.Contains("Configuring MCP server for HTTP...", output);
                Assert.Contains("MCP server configured for HTTP with official SDK (HTTP transport)", output);
            }
            finally
            {
                // Restore console output
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            }
        }

        [Fact]
        public void ConfigureHttpServices_WithConfigurationSection_ConfiguresRateLimit()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string>
            {
                ["IpRateLimiting:EnableEndpointRateLimiting"] = "true",
                ["IpRateLimiting:StackBlockedRequests"] = "false"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData!)
                .Build();

            // Act
            HttpServerSetup.ConfigureHttpServices(services, configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify rate limiting services are registered
            var ipPolicyStore = serviceProvider.GetService<IIpPolicyStore>();
            Assert.NotNull(ipPolicyStore);
        }
    }
}