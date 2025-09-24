using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using mcp_nexus.Helper;
using mcp_nexus.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace mcp_nexus_tests.Integration
{
    public class DependencyInjectionIntegrationTests
    {
        [Fact]
        public void ServiceCollection_RegisterAllServices_NoCircularDependencies()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add core services
            services.AddLogging(builder => builder.AddConsole());
            
            // Add mock CdbSession to prevent real process spawning
            services.AddSingleton<ICdbSession>(serviceProvider =>
            {
                var mock = new Mock<ICdbSession>();
                mock.Setup(s => s.IsActive).Returns(false);
                mock.Setup(s => s.StartSession(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
                mock.Setup(s => s.StopSession()).ReturnsAsync(false);
                mock.Setup(s => s.ExecuteCommand(It.IsAny<string>())).ReturnsAsync("mock result");
                mock.Setup(s => s.ExecuteCommand(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("mock result");
                return mock.Object;
            });
            
            // Add application services
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            services.AddSingleton<ICommandTimeoutService, CommandTimeoutService>();
            services.AddSingleton<IMcpNotificationService, McpNotificationService>();
            
            // Add recovery service with callback to break circular dependency
            services.AddSingleton<ICdbSessionRecoveryService>(serviceProvider =>
            {
                var cdbSession = serviceProvider.GetRequiredService<ICdbSession>();
                var logger = serviceProvider.GetRequiredService<ILogger<CdbSessionRecoveryService>>();
                var notificationService = serviceProvider.GetService<IMcpNotificationService>();
                
                Func<string, int> cancelAllCommandsCallback = reason =>
                {
                    var commandQueueService = serviceProvider.GetRequiredService<ICommandQueueService>();
                    return commandQueueService.CancelAllCommands(reason);
                };
                
                return new CdbSessionRecoveryService(cdbSession, logger, cancelAllCommandsCallback, notificationService);
            });
            
            services.AddSingleton<ICommandQueueService, ResilientCommandQueueService>();
            
            // Act & Assert - Should not throw circular dependency exception
            var serviceProvider = services.BuildServiceProvider();
            
            // Verify critical services can be resolved
            var commandQueue = serviceProvider.GetRequiredService<ICommandQueueService>();
            var recoveryService = serviceProvider.GetRequiredService<ICdbSessionRecoveryService>();
            var timeoutService = serviceProvider.GetRequiredService<ICommandTimeoutService>();
            var notificationService = serviceProvider.GetRequiredService<IMcpNotificationService>();
            var cdbSession = serviceProvider.GetRequiredService<ICdbSession>();
            
            Assert.NotNull(commandQueue);
            Assert.NotNull(recoveryService);
            Assert.NotNull(timeoutService);
            Assert.NotNull(notificationService);
            Assert.NotNull(cdbSession);
            
            serviceProvider.Dispose();
        }

        [Fact]
        public void ServiceCollection_ResolveServices_PerformanceTest()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            
            // Add mock CdbSession
            services.AddSingleton<ICdbSession>(serviceProvider =>
            {
                var mock = new Mock<ICdbSession>();
                mock.Setup(s => s.IsActive).Returns(false);
                return mock.Object;
            });
            
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act - Measure resolution time
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                var service = serviceProvider.GetRequiredService<ICommandQueueService>();
                Assert.NotNull(service);
            }
            
            stopwatch.Stop();
            
            // Assert - Should be very fast (under 100ms for 100 resolutions)
            Assert.True(stopwatch.ElapsedMilliseconds < 100, 
                $"Service resolution took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
            
            serviceProvider.Dispose();
        }

        [Fact]
        public void ServiceCollection_SingletonLifetime_SameInstanceReturned()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            
            services.AddSingleton<ICdbSession>(serviceProvider =>
            {
                var mock = new Mock<ICdbSession>();
                return mock.Object;
            });
            
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act
            var service1 = serviceProvider.GetRequiredService<ICommandQueueService>();
            var service2 = serviceProvider.GetRequiredService<ICommandQueueService>();
            
            // Assert
            Assert.Same(service1, service2);
            
            serviceProvider.Dispose();
        }

        [Fact]
        public void ServiceCollection_WithMockedDependencies_AllServicesResolvable()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            
            // Mock all external dependencies
            var mockCdbSession = new Mock<ICdbSession>();
            mockCdbSession.Setup(s => s.IsActive).Returns(true);
            services.AddSingleton(mockCdbSession.Object);
            
            // Add all application services
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            services.AddSingleton<ICommandTimeoutService, CommandTimeoutService>();
            services.AddSingleton<IMcpNotificationService, McpNotificationService>();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Act & Assert - All services should resolve without issues
            var resolvedServices = new object[]
            {
                serviceProvider.GetRequiredService<ICommandQueueService>(),
                serviceProvider.GetRequiredService<ICommandTimeoutService>(),
                serviceProvider.GetRequiredService<IMcpNotificationService>(),
                serviceProvider.GetRequiredService<ICdbSession>()
            };
            
            foreach (var service in resolvedServices)
            {
                Assert.NotNull(service);
            }
            
            serviceProvider.Dispose();
        }

        [Fact]
        public void ServiceProvider_Dispose_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<ICdbSession>(serviceProvider => new Mock<ICdbSession>().Object);
            services.AddSingleton<ICommandQueueService, CommandQueueService>();
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Get a service to ensure it's instantiated
            var service = serviceProvider.GetRequiredService<ICommandQueueService>();
            Assert.NotNull(service);
            
            // Act & Assert - Should not throw
            serviceProvider.Dispose();
            
            // Multiple dispose calls should be safe
            serviceProvider.Dispose();
        }
    }
}
