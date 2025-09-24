using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mcp_nexus.Controllers;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using mcp_nexus.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace mcp_nexus_tests.Integration
{
    /// <summary>
    /// Integration tests for dependency injection container to catch circular dependencies,
    /// missing registrations, and other DI configuration issues.
    /// </summary>
    public class DependencyInjectionIntegrationTests
    {
        private readonly ITestOutputHelper m_output;

        public DependencyInjectionIntegrationTests(ITestOutputHelper output)
        {
            m_output = output;
        }

        [Fact]
        public void StdioServices_CanBuildServiceProvider_WithoutCircularDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            // Register services exactly like the real application does for stdio mode
            RegisterServicesLikeRealApp(services, null);
            ConfigureStdioServicesLikeRealApp(services);

            // Act & Assert - This will throw if there are circular dependencies
            var serviceProvider = services.BuildServiceProvider();
            
            m_output.WriteLine("✅ Stdio DI container built successfully without circular dependencies");
            
            // Cleanup
            serviceProvider.Dispose();
        }

        [Fact]
        public void HttpServices_CanBuildServiceProvider_WithoutCircularDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            // Register services exactly like the real application does for HTTP mode
            RegisterServicesLikeRealApp(services, null);
            ConfigureHttpServicesLikeRealApp(services);

            // Act & Assert - This will throw if there are circular dependencies
            var serviceProvider = services.BuildServiceProvider();
            
            m_output.WriteLine("✅ HTTP DI container built successfully without circular dependencies");
            
            // Cleanup
            serviceProvider.Dispose();
        }

        [Fact]
        public void AllRegisteredServices_CanBeResolved_WithoutExceptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            RegisterServicesLikeRealApp(services, null);
            ConfigureHttpServicesLikeRealApp(services);

            var serviceProvider = services.BuildServiceProvider();

            // Get all registered service types (excluding built-in framework services)
            var registeredServices = services
                .Where(s => !IsFrameworkService(s.ServiceType))
                .ToList();

            m_output.WriteLine($"Testing resolution of {registeredServices.Count} registered services:");

            // Act & Assert
            var resolvedCount = 0;
            var failures = new List<string>();

            foreach (var serviceDescriptor in registeredServices)
            {
                try
                {
                    var serviceType = serviceDescriptor.ServiceType;
                    var resolvedService = serviceProvider.GetRequiredService(serviceType);
                    
                    Assert.NotNull(resolvedService);
                    m_output.WriteLine($"✅ {serviceType.Name}");
                    resolvedCount++;
                }
                catch (Exception ex)
                {
                    var error = $"❌ {serviceDescriptor.ServiceType.Name}: {ex.Message}";
                    failures.Add(error);
                    m_output.WriteLine(error);
                }
            }

            // Cleanup
            serviceProvider.Dispose();

            // Assert no failures
            if (failures.Any())
            {
                Assert.Fail($"Failed to resolve {failures.Count} services:\n{string.Join("\n", failures)}");
            }

            m_output.WriteLine($"✅ Successfully resolved all {resolvedCount} registered services");
        }

        [Fact]
        public void CriticalServices_CanBeResolvedAndUsed_InBothModes()
        {
            // Test both stdio and HTTP modes
            TestCriticalServicesInMode("Stdio", ConfigureStdioServicesLikeRealApp);
            TestCriticalServicesInMode("HTTP", ConfigureHttpServicesLikeRealApp);
        }

        [Fact]
        public void RecoveryServices_DontHaveCircularDependency_AfterFix()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            // Register only the recovery-related services to test the specific fix
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            
            // Register the problematic services that had circular dependency
            services.AddSingleton<ICommandTimeoutService, CommandTimeoutService>();
            
            // This registration should NOT cause circular dependency anymore
            RegisterCdbSessionRecoveryServiceLikeRealApp(services);
            
            services.AddSingleton<ICommandQueueService, ResilientCommandQueueService>();
            
            // Register ICdbSession (required dependency)
            services.AddSingleton<ICdbSession>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSession>>();
                return new CdbSession(logger, 30000, null, 30000, 1, null);
            });

            // Act & Assert - This should NOT throw a circular dependency exception
            var serviceProvider = services.BuildServiceProvider();
            
            // Try to resolve the services that were in the circular dependency
            var recoveryService = serviceProvider.GetRequiredService<ICdbSessionRecoveryService>();
            var queueService = serviceProvider.GetRequiredService<ICommandQueueService>();
            
            Assert.NotNull(recoveryService);
            Assert.NotNull(queueService);
            
            m_output.WriteLine("✅ Recovery services resolved without circular dependency");
            
            // Cleanup
            serviceProvider.Dispose();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(@"C:\CustomPath\cdb.exe")]
        public void Services_WorkWithDifferentCdbPaths(string? customCdbPath)
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            RegisterServicesLikeRealApp(services, customCdbPath);
            ConfigureStdioServicesLikeRealApp(services);

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var cdbSession = serviceProvider.GetRequiredService<ICdbSession>();

            // Assert
            Assert.NotNull(cdbSession);
            m_output.WriteLine($"✅ CdbSession created with custom path: {customCdbPath ?? "auto-detect"}");
            
            // Cleanup
            serviceProvider.Dispose();
        }

        [Fact]
        public void ServiceLifetimes_AreConfiguredCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            RegisterServicesLikeRealApp(services, null);
            ConfigureHttpServicesLikeRealApp(services);

            // Act
            var serviceProvider = services.BuildServiceProvider();

            // Assert - Test that singletons are actually singletons
            var cdbSession1 = serviceProvider.GetRequiredService<ICdbSession>();
            var cdbSession2 = serviceProvider.GetRequiredService<ICdbSession>();
            Assert.Same(cdbSession1, cdbSession2);

            var queueService1 = serviceProvider.GetRequiredService<ICommandQueueService>();
            var queueService2 = serviceProvider.GetRequiredService<ICommandQueueService>();
            Assert.Same(queueService1, queueService2);

            var recoveryService1 = serviceProvider.GetRequiredService<ICdbSessionRecoveryService>();
            var recoveryService2 = serviceProvider.GetRequiredService<ICdbSessionRecoveryService>();
            Assert.Same(recoveryService1, recoveryService2);

            m_output.WriteLine("✅ Singleton lifetimes are working correctly");

            // Cleanup
            serviceProvider.Dispose();
        }

        #region Helper Methods

        private void TestCriticalServicesInMode(string modeName, Action<IServiceCollection> configureModeServices)
        {
            var services = new ServiceCollection();
            var configuration = CreateTestConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            RegisterServicesLikeRealApp(services, null);
            configureModeServices(services);

            var serviceProvider = services.BuildServiceProvider();

            // Test critical services
            var criticalServices = new[]
            {
                typeof(ICdbSession),
                typeof(ICommandQueueService),
                typeof(ICdbSessionRecoveryService),
                typeof(ICommandTimeoutService),
                typeof(WindbgTool),
                typeof(McpToolDefinitionService),
                typeof(McpToolExecutionService)
            };

            foreach (var serviceType in criticalServices)
            {
                var service = serviceProvider.GetRequiredService(serviceType);
                Assert.NotNull(service);
                m_output.WriteLine($"✅ {modeName}: {serviceType.Name} resolved successfully");
            }

            serviceProvider.Dispose();
        }

        private static IConfiguration CreateTestConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpNexus:Debugging:CommandTimeoutMs"] = "30000",
                ["McpNexus:Debugging:SymbolServerTimeoutMs"] = "30000",
                ["McpNexus:Debugging:SymbolServerMaxRetries"] = "1"
            });
            return configBuilder.Build();
        }

        private static bool IsFrameworkService(Type serviceType)
        {
            var frameworkNamespaces = new[]
            {
                "Microsoft.Extensions.",
                "Microsoft.AspNetCore.",
                "System.Net.Http",
                "System.Text.Json"
            };

            return frameworkNamespaces.Any(ns => serviceType.FullName?.StartsWith(ns) == true);
        }

        #endregion

        #region Real Application Service Registration (Copied from Program.cs)

        /// <summary>
        /// Register services exactly like RegisterServices() method in Program.cs
        /// </summary>
        private static void RegisterServicesLikeRealApp(IServiceCollection services, string? customCdbPath)
        {
            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

            // Register automated recovery services for unattended operation
            services.AddSingleton<ICommandTimeoutService, CommandTimeoutService>();

            RegisterCdbSessionRecoveryServiceLikeRealApp(services);

            // Use resilient command queue for automated recovery
            services.AddSingleton<ICommandQueueService, ResilientCommandQueueService>();

            services.AddSingleton<ICdbSession>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSession>>();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var commandTimeoutMs = configuration.GetValue("McpNexus:Debugging:CommandTimeoutMs", 30000);
                var symbolServerTimeoutMs = configuration.GetValue("McpNexus:Debugging:SymbolServerTimeoutMs", 30000);
                var symbolServerMaxRetries = configuration.GetValue("McpNexus:Debugging:SymbolServerMaxRetries", 1);
                var symbolSearchPath = configuration.GetValue<string?>("McpNexus:Debugging:SymbolSearchPath");
                return new CdbSession(logger, commandTimeoutMs, customCdbPath, symbolServerTimeoutMs, symbolServerMaxRetries, symbolSearchPath);
            });

            services.AddSingleton<WindbgTool>();
        }

        /// <summary>
        /// Register CdbSessionRecoveryService exactly like in Program.cs (with the circular dependency fix)
        /// </summary>
        private static void RegisterCdbSessionRecoveryServiceLikeRealApp(IServiceCollection services)
        {
            services.AddSingleton<ICdbSessionRecoveryService>(serviceProvider =>
            {
                var cdbSession = serviceProvider.GetRequiredService<ICdbSession>();
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSessionRecoveryService>>();
                var notificationService = serviceProvider.GetService<IMcpNotificationService>();

                // Create a callback that will be resolved when the command queue service is available
                Func<string, int> cancelAllCommandsCallback = reason =>
                {
                    var commandQueueService = serviceProvider.GetRequiredService<ICommandQueueService>();
                    return commandQueueService.CancelAllCommands(reason);
                };

                return new CdbSessionRecoveryService(cdbSession, logger, cancelAllCommandsCallback, notificationService);
            });
        }

        /// <summary>
        /// Configure HTTP services exactly like ConfigureHttpServices() method in Program.cs
        /// </summary>
        private static void ConfigureHttpServicesLikeRealApp(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Register MCP services
            services.AddSingleton<McpToolDefinitionService>();
            services.AddSingleton<McpToolExecutionService>();
            services.AddSingleton<McpProtocolService>();
            services.AddSingleton<IMcpNotificationService, McpNotificationService>();
        }

        /// <summary>
        /// Configure stdio services exactly like ConfigureStdioServices() method in Program.cs
        /// </summary>
        private static void ConfigureStdioServicesLikeRealApp(IServiceCollection services)
        {
            // Add the MCP protocol service
            services.AddSingleton<McpProtocolService>();
            services.AddSingleton<McpToolDefinitionService>();
            services.AddSingleton<McpToolExecutionService>();

            // Note: We don't add the full MCP server here as it requires more complex setup
            // but we test the core services that would be registered
        }

        #endregion
    }
}
