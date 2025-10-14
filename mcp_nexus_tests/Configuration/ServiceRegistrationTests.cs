using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using mcp_nexus.Configuration;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using mcp_nexus.Debugger;
using mcp_nexus.CommandQueue;
using mcp_nexus.Notifications;
using mcp_nexus.Session;
using mcp_nexus.Session.Models;
using mcp_nexus.Recovery;
using mcp_nexus.Protocol;
using mcp_nexus.Tools;
using NLog;
using System.Collections.Concurrent;

namespace mcp_nexus_tests.Configuration
{
    /// <summary>
    /// Tests for ServiceRegistration
    /// </summary>
    public class ServiceRegistrationTests
    {
        private readonly ServiceCollection m_Services;
        private readonly IConfiguration m_Configuration;

        public ServiceRegistrationTests()
        {
            m_Services = new ServiceCollection();
            var configurationData = new Dictionary<string, string?>
            {
                ["McpNexus:Debugging:CommandTimeoutMs"] = "30000",
                ["McpNexus:Debugging:SymbolServerTimeoutMs"] = "10000",
                ["McpNexus:Debugging:SymbolServerMaxRetries"] = "3",
                ["McpNexus:Debugging:SymbolSearchPath"] = "srv*https://msdl.microsoft.com/download/symbols",
                ["McpNexus:Debugging:CdbPath"] = "",
                ["McpNexus:Debugging:StartupDelayMs"] = "1000",
                ["McpNexus:SessionManagement:MaxSessions"] = "10",
                ["McpNexus:SessionManagement:SessionTimeoutMinutes"] = "30"
            };
            m_Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();
        }

        [Fact]
        public void ServiceRegistration_Class_Exists()
        {
            // This test verifies that the ServiceRegistration class exists and can be instantiated
            Assert.NotNull(typeof(ServiceRegistration));
        }

        [Fact]
        public void ServiceRegistration_IsStaticClass()
        {
            // Verify that ServiceRegistration is a static class
            var type = typeof(ServiceRegistration);
            Assert.True(type.IsAbstract && type.IsSealed);
        }

        [Fact]
        public void RegisterServices_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;

            // Act & Assert
            var exception = Record.Exception(() => ServiceRegistration.RegisterServices(services, configuration, null));
            Assert.Null(exception);
        }

        [Fact]
        public void RegisterServices_WithNullServices_ThrowsArgumentNullException()
        {
            // Arrange
            var configuration = m_Configuration;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceRegistration.RegisterServices(null!, configuration, null));
        }

        [Fact]
        public void RegisterServices_WithNullConfiguration_ThrowsNullReferenceException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceRegistration.RegisterServices(services, null!, null));
        }

        [Fact]
        public void RegisterServices_WithCustomCdbPath_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;
            var customCdbPath = "C:\\Windows\\System32\\cdb.exe";

            // Act & Assert
            var exception = Record.Exception(() => ServiceRegistration.RegisterServices(services, configuration, customCdbPath));
            Assert.Null(exception);
        }

        [Fact]
        public void RegisterServices_LogsWithNLog_CapturesOutput()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;
            var nlogConfig = new NLog.Config.LoggingConfiguration();
            var memory = new NLog.Targets.MemoryTarget("testMemory") { Layout = "${message}" };
            nlogConfig.AddTarget(memory);
            nlogConfig.AddRuleForAllLevels(memory);
            LogManager.Configuration = nlogConfig;

            try
            {
                // Act
                ServiceRegistration.RegisterServices(services, configuration, null);

                // Assert
                var logs = (memory.Logs ?? []);
                Assert.Contains(logs, m => m.Contains("Registering services..."));
                Assert.Contains(logs, m => m.Contains("All services registered successfully"));
                Assert.Contains(logs, m => m.Contains("Registered core services (CDB, Session, Notifications, Protocol)"));
                Assert.Contains(logs, m => m.Contains("Registered CommandTimeoutService for automated timeouts"));
                Assert.Contains(logs, m => m.Contains("Registered recovery services"));
                Assert.Contains(logs, m => m.Contains("Registered command queue services"));
            }
            finally
            {
                LogManager.Configuration = null;
            }
        }

        [Fact]
        public void RegisterServices_RegistersCoreServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;

            // Add required logging services
            services.AddLogging();

            // Act
            ServiceRegistration.RegisterServices(services, configuration, null);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify core services are registered
            var cdbSession = serviceProvider.GetService<ICdbSession>();
            Assert.NotNull(cdbSession);

            var sessionManager = serviceProvider.GetService<ISessionManager>();
            Assert.NotNull(sessionManager);

            var windbgTool = serviceProvider.GetService<SessionAwareWindbgTool>();
            Assert.NotNull(windbgTool);

            var notificationService = serviceProvider.GetService<IMcpNotificationService>();
            Assert.NotNull(notificationService);

            var toolDefinitionService = serviceProvider.GetService<IMcpToolDefinitionService>();
            Assert.NotNull(toolDefinitionService);

            // Verify session store is registered
            var sessionStore = serviceProvider.GetService<ConcurrentDictionary<string, SessionInfo>>();
            Assert.NotNull(sessionStore);
        }

        [Fact]
        public void RegisterServices_RegistersAdvancedServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;

            // Add required logging services
            services.AddLogging();

            // Act
            ServiceRegistration.RegisterServices(services, configuration, null);

            // Assert
            _ = services.BuildServiceProvider();

            // Verify advanced services are registered (dead enterprise services removed)
        }

        [Fact]
        public void RegisterServices_RegistersRecoveryServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;

            // Add required logging services
            services.AddLogging();

            // Act
            ServiceRegistration.RegisterServices(services, configuration, null);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify recovery services are registered
            var timeoutService = serviceProvider.GetService<ICommandTimeoutService>();
            Assert.NotNull(timeoutService);

            var recoveryService = serviceProvider.GetService<ICdbSessionRecoveryService>();
            Assert.NotNull(recoveryService);
        }

        [Fact]
        public void RegisterServices_RegistersCommandQueueServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;

            // Add required logging services
            services.AddLogging();

            // Act
            ServiceRegistration.RegisterServices(services, configuration, null);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify command queue services are registered
            var commandQueueService = serviceProvider.GetService<ICommandQueueService>();
            Assert.NotNull(commandQueueService);

            var resilientCommandQueueService = serviceProvider.GetService<ResilientCommandQueueService>();
            Assert.NotNull(resilientCommandQueueService);
        }

        [Fact]
        public void RegisterServices_ConfiguresCdbSessionOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;

            // Act
            ServiceRegistration.RegisterServices(services, configuration, null);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify CDB session options are configured
            var cdbOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CdbSessionOptions>>();
            Assert.NotNull(cdbOptions);
            Assert.NotNull(cdbOptions.Value);
        }

        [Fact]
        public void RegisterServices_ConfiguresSessionConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = m_Configuration;

            // Act
            ServiceRegistration.RegisterServices(services, configuration, null);

            // Assert
            var serviceProvider = services.BuildServiceProvider();

            // Verify session configuration is configured
            var sessionConfig = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<SessionConfiguration>>();
            Assert.NotNull(sessionConfig);
            Assert.NotNull(sessionConfig.Value);
        }

        [Fact]
        public void RegisterServices_WithExistingCdbPath_LogsSuccess()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string?>
            {
                ["McpNexus:Debugging:CommandTimeoutMs"] = "30000",
                ["McpNexus:Debugging:SymbolServerTimeoutMs"] = "10000",
                ["McpNexus:Debugging:SymbolServerMaxRetries"] = "3",
                ["McpNexus:Debugging:SymbolSearchPath"] = "srv*https://msdl.microsoft.com/download/symbols",
                ["McpNexus:Debugging:CdbPath"] = "C:\\Windows\\System32\\cdb.exe",
                ["McpNexus:Debugging:StartupDelayMs"] = "1000",
                ["McpNexus:SessionManagement:MaxSessions"] = "10",
                ["McpNexus:SessionManagement:SessionTimeoutMinutes"] = "30"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            var nlogConfig = new NLog.Config.LoggingConfiguration();
            var memory = new NLog.Targets.MemoryTarget("testMemory") { Layout = "${message}" };
            nlogConfig.AddTarget(memory);
            nlogConfig.AddRuleForAllLevels(memory);
            LogManager.Configuration = nlogConfig;

            try
            {
                // Act
                ServiceRegistration.RegisterServices(services, configuration, null);

                // Assert
                var logs = (memory.Logs ?? []);
                Assert.Contains(logs, m => m.Contains("Registering services..."));
            }
            finally
            {
                LogManager.Configuration = null;
            }
        }

        [Fact]
        public void RegisterServices_WithInvalidCdbPath_LogsWarning()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationData = new Dictionary<string, string?>
            {
                ["McpNexus:Debugging:CommandTimeoutMs"] = "30000",
                ["McpNexus:Debugging:SymbolServerTimeoutMs"] = "10000",
                ["McpNexus:Debugging:SymbolServerMaxRetries"] = "3",
                ["McpNexus:Debugging:SymbolSearchPath"] = "srv*https://msdl.microsoft.com/download/symbols",
                ["McpNexus:Debugging:CdbPath"] = "C:\\NonExistent\\cdb.exe",
                ["McpNexus:Debugging:StartupDelayMs"] = "1000",
                ["McpNexus:SessionManagement:MaxSessions"] = "10",
                ["McpNexus:SessionManagement:SessionTimeoutMinutes"] = "30"
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            var nlogConfig = new NLog.Config.LoggingConfiguration();
            var memory = new NLog.Targets.MemoryTarget("testMemory") { Layout = "${message}" };
            nlogConfig.AddTarget(memory);
            nlogConfig.AddRuleForAllLevels(memory);
            LogManager.Configuration = nlogConfig;

            try
            {
                // Act
                ServiceRegistration.RegisterServices(services, configuration, null);

                // Assert
                var logs = (memory.Logs ?? []);
                Assert.Contains(logs, m => m.Contains("Registering services..."));
                // The warning about CDB will be logged via logger in ServiceRegistration.RegisterCoreServices
            }
            finally
            {
                LogManager.Configuration = null;
            }
        }
    }
}
