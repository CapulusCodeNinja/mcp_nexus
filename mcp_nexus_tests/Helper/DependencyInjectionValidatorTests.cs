using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Helper;
using mcp_nexus.Services;

namespace mcp_nexus_tests.Helper
{
    /// <summary>
    /// Tests for DependencyInjectionValidator - validates DI configuration
    /// </summary>
    public class DependencyInjectionValidatorTests
    {
        private readonly Mock<ILogger> m_mockLogger;

        public DependencyInjectionValidatorTests()
        {
            m_mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void ValidateServiceRegistration_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                DependencyInjectionValidator.ValidateServiceRegistration(null!, m_mockLogger.Object));
        }

        [Fact]
        public void ValidateServiceRegistration_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, null!));
        }

        [Fact]
        public void ValidateServiceRegistration_WithEmptyServiceProvider_ReturnsTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithValidServices_ReturnsTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, MockCdbSession>();
            services.AddSingleton<ICommandQueueService, MockCommandQueueService>();
            services.AddSingleton<ICdbSessionRecoveryService, MockCdbSessionRecoveryService>();
            services.AddSingleton<ICommandTimeoutService, MockCommandTimeoutService>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithCircularDependency_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Create a circular dependency: A depends on B, B depends on A
            services.AddSingleton<CircularDependencyA>();
            services.AddSingleton<CircularDependencyB>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act - Disable application service filtering to test circular dependencies
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithUnresolvableService_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<UnresolvableService>(); // No implementation registered
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithApplicationServicesOnly_ValidatesOnlyApplicationServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, MockCdbSession>(); // Application service
            services.AddLogging(); // Framework service
            services.AddSingleton<ICommandQueueService, MockCommandQueueService>(); // Application service
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithAllServices_ValidatesAllServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, MockCdbSession>();
            services.AddLogging(); // Framework service
            services.AddSingleton<ICommandQueueService, MockCommandQueueService>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateCriticalServices_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                DependencyInjectionValidator.ValidateCriticalServices(null!, m_mockLogger.Object));
        }

        [Fact]
        public void ValidateCriticalServices_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                DependencyInjectionValidator.ValidateCriticalServices(serviceProvider, null!));
        }

        [Fact]
        public void ValidateCriticalServices_WithMissingCriticalServices_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            // Don't register any critical services
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateCriticalServices(serviceProvider, m_mockLogger.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateCriticalServices_WithAllCriticalServices_ReturnsTrue()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, MockCdbSession>();
            services.AddSingleton<ICommandQueueService, MockCommandQueueService>();
            services.AddSingleton<ICdbSessionRecoveryService, MockCdbSessionRecoveryService>();
            services.AddSingleton<ICommandTimeoutService, MockCommandTimeoutService>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateCriticalServices(serviceProvider, m_mockLogger.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateCriticalServices_WithPartialCriticalServices_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, MockCdbSession>();
            services.AddSingleton<ICommandQueueService, MockCommandQueueService>();
            // Missing ICdbSessionRecoveryService and ICommandTimeoutService
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateCriticalServices(serviceProvider, m_mockLogger.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithExceptionDuringValidation_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ThrowingService>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateCriticalServices_WithExceptionDuringValidation_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, ThrowingCdbSession>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateCriticalServices(serviceProvider, m_mockLogger.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithNullResolvedService_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession>(provider => null!); // Return null
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithMixedValidAndInvalidServices_ReturnsFalse()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, MockCdbSession>(); // Valid
            services.AddTransient<UnresolvableService>(); // Invalid
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithLargeNumberOfServices_HandlesEfficiently()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add many services
            for (int i = 0; i < 100; i++)
            {
                services.AddSingleton<MockCdbSession>();
            }
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateServiceRegistration_WithFrameworkServices_ExcludesThemWhenRequested()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ICdbSession, MockCdbSession>(); // Application service
            services.AddLogging(); // Framework service
            services.AddSingleton<ICommandQueueService, MockCommandQueueService>(); // Application service
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var result = DependencyInjectionValidator.ValidateServiceRegistration(serviceProvider, m_mockLogger.Object, true);

            // Assert
            Assert.True(result);
        }

        // Mock implementations for testing
        private class MockCdbSession : ICdbSession
        {
            public bool IsActive => false;
            public void Dispose() { }
            public Task<bool> StartSession(string target, string? arguments) => Task.FromResult(false);
            public Task<bool> StopSession() => Task.FromResult(false);
            public Task<string> ExecuteCommand(string command) => Task.FromResult("mock result");
            public Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken) => Task.FromResult("mock result");
            public void CancelCurrentOperation() { }
        }

        private class MockCommandQueueService : ICommandQueueService
        {
            public string QueueCommand(string command) => "mock-id";
            public Task<string> GetCommandResult(string commandId) => Task.FromResult("mock result");
            public bool CancelCommand(string commandId) => true;
            public int CancelAllCommands(string? reason = null) => 0;
            public IEnumerable<(string Id, string Command, DateTime QueueTime, string Status)> GetQueueStatus() => Enumerable.Empty<(string, string, DateTime, string)>();
            public QueuedCommand? GetCurrentCommand() => null;
            public void Dispose() { }
        }

        private class MockCdbSessionRecoveryService : ICdbSessionRecoveryService
        {
            public Task<bool> RecoverStuckSession(string reason) => Task.FromResult(true);
            public Task<bool> ForceRestartSession(string reason) => Task.FromResult(true);
            public bool IsSessionHealthy() => true;
            public void Dispose() { }
        }

        private class MockCommandTimeoutService : ICommandTimeoutService
        {
            public void StartCommandTimeout(string commandId, TimeSpan timeout, Func<Task> onTimeout) { }
            public void CancelCommandTimeout(string commandId) { }
            public void ExtendCommandTimeout(string commandId, TimeSpan additionalTime) { }
            public void Dispose() { }
        }

        private class CircularDependencyA
        {
            public CircularDependencyA(CircularDependencyB b) { }
        }

        private class CircularDependencyB
        {
            public CircularDependencyB(CircularDependencyA a) { }
        }

        private class UnresolvableService
        {
            // No default constructor, requires parameters
            public UnresolvableService(string requiredParameter) { }
        }

        private class ThrowingService
        {
            public ThrowingService()
            {
                throw new InvalidOperationException("Service construction failed");
            }
        }

        private class ThrowingCdbSession : ICdbSession
        {
            public ThrowingCdbSession()
            {
                throw new InvalidOperationException("CDB Session construction failed");
            }

            public bool IsActive => throw new InvalidOperationException("Property access failed");
            public void Dispose() { }
            public Task<bool> StartSession(string target, string? arguments) => throw new InvalidOperationException("StartSession failed");
            public Task<bool> StopSession() => throw new InvalidOperationException("StopSession failed");
            public Task<string> ExecuteCommand(string command) => throw new InvalidOperationException("ExecuteCommand failed");
            public Task<string> ExecuteCommand(string command, CancellationToken externalCancellationToken) => throw new InvalidOperationException("ExecuteCommand failed");
            public void CancelCurrentOperation() => throw new InvalidOperationException("CancelCurrentOperation failed");
        }
    }
}
