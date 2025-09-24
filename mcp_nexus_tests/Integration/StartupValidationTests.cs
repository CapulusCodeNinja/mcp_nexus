using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace mcp_nexus_tests.Integration
{
    /// <summary>
    /// Tests that validate the application can start up successfully in different modes
    /// and that all dependency injection is configured correctly at startup.
    /// </summary>
    public class StartupValidationTests
    {
        private readonly ITestOutputHelper m_output;

        public StartupValidationTests(ITestOutputHelper output)
        {
            m_output = output;
        }

        [Fact]
        public void Application_CanStartInStdioMode_WithoutErrors()
        {
            // Arrange
            var args = new string[] { };

            // Act & Assert
            var startupTime = MeasureStartupTimeSync(() =>
            {
                var builder = Host.CreateApplicationBuilder(args);
                
                // Configure exactly like the real application
                ConfigureLoggingLikeRealApp(builder.Logging, false);
                RegisterServicesLikeRealApp(builder.Services, null);
                ConfigureStdioServicesLikeRealApp(builder.Services);

                var host = builder.Build();

                // Validate that we can build the host without exceptions
                Assert.NotNull(host);

                // Try to resolve a few key services to make sure DI is working
                var cdbSession = host.Services.GetRequiredService<mcp_nexus.Helper.ICdbSession>();
                var queueService = host.Services.GetRequiredService<mcp_nexus.Services.ICommandQueueService>();
                var recoveryService = host.Services.GetRequiredService<mcp_nexus.Services.ICdbSessionRecoveryService>();

                Assert.NotNull(cdbSession);
                Assert.NotNull(queueService);
                Assert.NotNull(recoveryService);

                // Dispose to clean up
                host.Dispose();
            });

            m_output.WriteLine($"✅ Stdio mode startup successful in {startupTime.TotalMilliseconds:F0}ms");
        }

        [Fact]
        public async Task Application_CanConfigureHttpMode_WithoutErrors()
        {
            // Arrange - We can't actually start Kestrel in tests, but we can validate the DI configuration
            var startupTime = await MeasureStartupTime(async () =>
            {
                var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(new string[] { });
                
                // Configure exactly like the real application for HTTP mode
                ConfigureLoggingLikeRealApp(builder.Logging, false);
                RegisterServicesLikeRealApp(builder.Services, null);
                ConfigureHttpServicesLikeRealApp(builder.Services);

                // Build the app (this validates DI configuration)
                var app = builder.Build();
                
                Assert.NotNull(app);

                // Validate that critical services can be resolved
                var cdbSession = app.Services.GetRequiredService<mcp_nexus.Helper.ICdbSession>();
                var queueService = app.Services.GetRequiredService<mcp_nexus.Services.ICommandQueueService>();
                var recoveryService = app.Services.GetRequiredService<mcp_nexus.Services.ICdbSessionRecoveryService>();
                var notificationService = app.Services.GetRequiredService<mcp_nexus.Services.IMcpNotificationService>();

                Assert.NotNull(cdbSession);
                Assert.NotNull(queueService);
                Assert.NotNull(recoveryService);
                Assert.NotNull(notificationService);

                // Dispose to clean up
                await app.DisposeAsync();
            });

            m_output.WriteLine($"✅ HTTP mode configuration successful in {startupTime.TotalMilliseconds:F0}ms");
        }

        [Theory]
        [InlineData(null, "auto-detect")]
        [InlineData(@"C:\CustomPath\cdb.exe", @"C:\CustomPath\cdb.exe")]
        public void Application_HandlesCustomCdbPaths_Correctly(string? customCdbPath, string expectedDescription)
        {
            // Act
            var builder = Host.CreateApplicationBuilder(new string[] { });
            ConfigureLoggingLikeRealApp(builder.Logging, false);
            RegisterServicesLikeRealApp(builder.Services, customCdbPath);
            ConfigureStdioServicesLikeRealApp(builder.Services);

            var host = builder.Build();
            var cdbSession = host.Services.GetRequiredService<mcp_nexus.Helper.ICdbSession>();

            // Assert
            Assert.NotNull(cdbSession);
            m_output.WriteLine($"✅ CdbSession created successfully with path: {expectedDescription}");

            // Cleanup
            host.Dispose();
        }

        [Fact]
        public void DependencyGraph_ResolutionPerformance_IsAcceptable()
        {
            // Arrange
            var builder = Host.CreateApplicationBuilder(new string[] { });
            ConfigureLoggingLikeRealApp(builder.Logging, false);
            RegisterServicesLikeRealApp(builder.Services, null);
            ConfigureStdioServicesLikeRealApp(builder.Services);

            var host = builder.Build();

            // Act - Measure service resolution time
            var stopwatch = Stopwatch.StartNew();
            
            // Resolve all critical services
            var services = new object[]
            {
                host.Services.GetRequiredService<mcp_nexus.Helper.ICdbSession>(),
                host.Services.GetRequiredService<mcp_nexus.Services.ICommandQueueService>(),
                host.Services.GetRequiredService<mcp_nexus.Services.ICdbSessionRecoveryService>(),
                host.Services.GetRequiredService<mcp_nexus.Services.ICommandTimeoutService>(),
                host.Services.GetRequiredService<mcp_nexus.Tools.WindbgTool>(),
                host.Services.GetRequiredService<mcp_nexus.Services.McpToolDefinitionService>(),
                host.Services.GetRequiredService<mcp_nexus.Services.McpToolExecutionService>(),
                host.Services.GetRequiredService<mcp_nexus.Services.McpProtocolService>()
            };

            stopwatch.Stop();

            // Assert
            foreach (var service in services)
            {
                Assert.NotNull(service);
            }

            // Performance should be reasonable (under 1 second for all services)
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Service resolution took {stopwatch.ElapsedMilliseconds}ms, which is too slow");

            m_output.WriteLine($"✅ Resolved {services.Length} critical services in {stopwatch.ElapsedMilliseconds}ms");

            // Cleanup
            host.Dispose();
        }

        [Fact]
        public void CircularDependency_PreviousIssue_IsFixed()
        {
            // This test specifically validates that the circular dependency between
            // ResilientCommandQueueService and CdbSessionRecoveryService is resolved

            // Arrange
            var builder = Host.CreateApplicationBuilder(new string[] { });
            ConfigureLoggingLikeRealApp(builder.Logging, false);

            // Register the specific services that had circular dependency
            builder.Services.AddSingleton<mcp_nexus.Services.ICommandTimeoutService, mcp_nexus.Services.CommandTimeoutService>();
            
            // This specific registration pattern was causing the circular dependency
            RegisterCdbSessionRecoveryServiceLikeRealApp(builder.Services);
            builder.Services.AddSingleton<mcp_nexus.Services.ICommandQueueService, mcp_nexus.Services.ResilientCommandQueueService>();
            
            // Required dependency
            builder.Services.AddSingleton<mcp_nexus.Helper.ICdbSession>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<mcp_nexus.Helper.CdbSession>>();
                return new mcp_nexus.Helper.CdbSession(logger, 30000, null, 30000, 1, null);
            });

            // Act & Assert - This should NOT throw InvalidOperationException about circular dependency
            Exception? caughtException = null;
            try
            {
                var host = builder.Build();
                
                // Try to resolve the services that were previously in circular dependency
                var recoveryService = host.Services.GetRequiredService<mcp_nexus.Services.ICdbSessionRecoveryService>();
                var queueService = host.Services.GetRequiredService<mcp_nexus.Services.ICommandQueueService>();
                
                Assert.NotNull(recoveryService);
                Assert.NotNull(queueService);
                
                host.Dispose();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert no circular dependency exception
            if (caughtException != null)
            {
                if (caughtException.Message.Contains("circular dependency"))
                {
                    Assert.Fail($"Circular dependency still exists: {caughtException.Message}");
                }
                else
                {
                    // Some other exception - re-throw to see what it is
                    throw caughtException;
                }
            }

            m_output.WriteLine("✅ Circular dependency issue has been resolved");
        }

        #region Helper Methods

        private async Task<TimeSpan> MeasureStartupTime(Func<Task> action)
        {
            var stopwatch = Stopwatch.StartNew();
            await action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private TimeSpan MeasureStartupTimeSync(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private static void ConfigureLoggingLikeRealApp(ILoggingBuilder logging, bool isServiceMode)
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
        }

        private static void RegisterServicesLikeRealApp(IServiceCollection services, string? customCdbPath)
        {
            // Add configuration
            services.AddSingleton<IConfiguration>(CreateTestConfiguration());

            // Register automated recovery services for unattended operation
            services.AddSingleton<mcp_nexus.Services.ICommandTimeoutService, mcp_nexus.Services.CommandTimeoutService>();

            RegisterCdbSessionRecoveryServiceLikeRealApp(services);

            // Use resilient command queue for automated recovery
            services.AddSingleton<mcp_nexus.Services.ICommandQueueService, mcp_nexus.Services.ResilientCommandQueueService>();

            services.AddSingleton<mcp_nexus.Helper.ICdbSession>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<mcp_nexus.Helper.CdbSession>>();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var commandTimeoutMs = configuration.GetValue("McpNexus:Debugging:CommandTimeoutMs", 30000);
                var symbolServerTimeoutMs = configuration.GetValue("McpNexus:Debugging:SymbolServerTimeoutMs", 30000);
                var symbolServerMaxRetries = configuration.GetValue("McpNexus:Debugging:SymbolServerMaxRetries", 1);
                var symbolSearchPath = configuration.GetValue<string?>("McpNexus:Debugging:SymbolSearchPath");
                return new mcp_nexus.Helper.CdbSession(logger, commandTimeoutMs, customCdbPath, symbolServerTimeoutMs, symbolServerMaxRetries, symbolSearchPath);
            });

            services.AddSingleton<mcp_nexus.Tools.WindbgTool>();
        }

        private static void RegisterCdbSessionRecoveryServiceLikeRealApp(IServiceCollection services)
        {
            services.AddSingleton<mcp_nexus.Services.ICdbSessionRecoveryService>(serviceProvider =>
            {
                var cdbSession = serviceProvider.GetRequiredService<mcp_nexus.Helper.ICdbSession>();
                var logger = serviceProvider.GetRequiredService<ILogger<mcp_nexus.Services.CdbSessionRecoveryService>>();
                var notificationService = serviceProvider.GetService<mcp_nexus.Services.IMcpNotificationService>();

                // Create a callback that will be resolved when the command queue service is available
                Func<string, int> cancelAllCommandsCallback = reason =>
                {
                    var commandQueueService = serviceProvider.GetRequiredService<mcp_nexus.Services.ICommandQueueService>();
                    return commandQueueService.CancelAllCommands(reason);
                };

                return new mcp_nexus.Services.CdbSessionRecoveryService(cdbSession, logger, cancelAllCommandsCallback, notificationService);
            });
        }

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
            services.AddSingleton<mcp_nexus.Services.McpToolDefinitionService>();
            services.AddSingleton<mcp_nexus.Services.McpToolExecutionService>();
            services.AddSingleton<mcp_nexus.Services.McpProtocolService>();
            services.AddSingleton<mcp_nexus.Services.IMcpNotificationService, mcp_nexus.Services.McpNotificationService>();
        }

        private static void ConfigureStdioServicesLikeRealApp(IServiceCollection services)
        {
            // Add the MCP protocol service
            services.AddSingleton<mcp_nexus.Services.McpProtocolService>();
            services.AddSingleton<mcp_nexus.Services.McpToolDefinitionService>();
            services.AddSingleton<mcp_nexus.Services.McpToolExecutionService>();
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

        #endregion
    }
}
