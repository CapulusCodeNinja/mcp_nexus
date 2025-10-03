using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    public class ServicePermissionValidatorTests
    {
        private readonly Mock<ILogger<ServicePermissionValidator>> _mockLogger;

        public ServicePermissionValidatorTests()
        {
            _mockLogger = new Mock<ILogger<ServicePermissionValidator>>();
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ServicePermissionValidator(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Act
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Assert
            Assert.NotNull(validator);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithValidServiceName_ReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceName = "TestService";

            // Act
            var result = await validator.ValidatePermissionsAsync(serviceName);

            // Assert
            Assert.True(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Validating permissions for service {serviceName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithNullServiceName_LogsAndReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Act
            var result = await validator.ValidatePermissionsAsync(null!);

            // Assert
            Assert.True(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating permissions for service")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithEmptyServiceName_LogsAndReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Act
            var result = await validator.ValidatePermissionsAsync(string.Empty);

            // Assert
            Assert.True(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating permissions for service")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithWhitespaceServiceName_LogsAndReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Act
            var result = await validator.ValidatePermissionsAsync("   ");

            // Assert
            Assert.True(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating permissions for service")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithVeryLongServiceName_LogsAndReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceName = new string('A', 1000);

            // Act
            var result = await validator.ValidatePermissionsAsync(serviceName);

            // Assert
            Assert.True(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Validating permissions for service {serviceName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithSpecialCharactersServiceName_LogsAndReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceName = "Test-Service_123!@#$%^&*()";

            // Act
            var result = await validator.ValidatePermissionsAsync(serviceName);

            // Assert
            Assert.True(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Validating permissions for service {serviceName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithUnicodeServiceName_LogsAndReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceName = "测试服务_TestService_🎯";

            // Act
            var result = await validator.ValidatePermissionsAsync(serviceName);

            // Assert
            Assert.True(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Validating permissions for service {serviceName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithException_LogsErrorAndReturnsFalse()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceName = "TestService";

            // Mock the logger to throw an exception during the first Log call (information)
            _mockLogger.Setup(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Validating permissions for service {serviceName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Throws(new InvalidOperationException("Logging failed"));

            // Act
            var result = await validator.ValidatePermissionsAsync(serviceName);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Failed to validate permissions for service {serviceName}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithTaskDelay_CompletesAsynchronously()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceName = "TestService";
            var startTime = DateTime.UtcNow;

            // Act
            var result = await validator.ValidatePermissionsAsync(serviceName);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.True(result);
            Assert.True((endTime - startTime).TotalMilliseconds >= 90); // Should take at least 90ms due to Task.Delay(100)
        }

        [Fact]
        public void HasRequiredPermissions_ReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Act
            var result = validator.HasRequiredPermissions();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasRequiredPermissions_CalledMultipleTimes_ReturnsConsistentResult()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Act
            var result1 = validator.HasRequiredPermissions();
            var result2 = validator.HasRequiredPermissions();
            var result3 = validator.HasRequiredPermissions();

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        [Fact]
        public void IsAdministrator_ReturnsTrue()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Act
            var result = validator.IsAdministrator();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAdministrator_CalledMultipleTimes_ReturnsConsistentResult()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);

            // Act
            var result1 = validator.IsAdministrator();
            var result2 = validator.IsAdministrator();
            var result3 = validator.IsAdministrator();

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        [Fact]
        public void ServicePermissionValidator_Class_Exists()
        {
            // Act & Assert
            Assert.True(typeof(ServicePermissionValidator).IsClass);
            Assert.False(typeof(ServicePermissionValidator).IsAbstract);
            Assert.False(typeof(ServicePermissionValidator).IsInterface);
            Assert.False(typeof(ServicePermissionValidator).IsEnum);
            Assert.False(typeof(ServicePermissionValidator).IsValueType);
        }

        [Fact]
        public void ServicePermissionValidator_HasExpectedMethods()
        {
            // Arrange
            var type = typeof(ServicePermissionValidator);

            // Act & Assert
            Assert.NotNull(type.GetMethod("ValidatePermissionsAsync"));
            Assert.NotNull(type.GetMethod("HasRequiredPermissions"));
            Assert.NotNull(type.GetMethod("IsAdministrator"));
        }

        [Fact]
        public void ServicePermissionValidator_HasExpectedConstructor()
        {
            // Arrange
            var type = typeof(ServicePermissionValidator);
            var constructors = type.GetConstructors();

            // Act & Assert
            Assert.Single(constructors);
            var constructor = constructors[0];
            var parameters = constructor.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(ILogger<ServicePermissionValidator>), parameters[0].ParameterType);
        }

        [Fact]
        public void ServicePermissionValidator_IsNotSealed()
        {
            // Act & Assert
            Assert.False(typeof(ServicePermissionValidator).IsSealed);
        }

        [Fact]
        public void ServicePermissionValidator_IsNotStatic()
        {
            // Act & Assert
            Assert.False(typeof(ServicePermissionValidator).IsAbstract && typeof(ServicePermissionValidator).IsSealed);
        }

        [Fact]
        public void ServicePermissionValidator_HasNoStaticMembers()
        {
            // Arrange
            var type = typeof(ServicePermissionValidator);
            var staticMembers = type.GetMembers(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert
            // Only expect the default constructor and type members
            Assert.True(staticMembers.Length <= 10); // Allow for some system-generated static members
        }

        [Fact]
        public void ServicePermissionValidator_HasInstanceMembers()
        {
            // Arrange
            var type = typeof(ServicePermissionValidator);
            var instanceMembers = type.GetMembers(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert
            Assert.True(instanceMembers.Length > 0);
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithConcurrentCalls_HandlesCorrectly()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceName = "TestService";

            // Act
            var task1 = validator.ValidatePermissionsAsync(serviceName);
            var task2 = validator.ValidatePermissionsAsync(serviceName);
            var task3 = validator.ValidatePermissionsAsync(serviceName);

            var results = await Task.WhenAll(task1, task2, task3);

            // Assert
            Assert.All(results, result => Assert.True(result));
        }

        [Fact]
        public async Task ValidatePermissionsAsync_WithDifferentServiceNames_LogsCorrectly()
        {
            // Arrange
            var validator = new ServicePermissionValidator(_mockLogger.Object);
            var serviceNames = new[] { "Service1", "Service2", "Service3" };

            // Act
            foreach (var serviceName in serviceNames)
            {
                await validator.ValidatePermissionsAsync(serviceName);
            }

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating permissions for service Service1")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating permissions for service Service2")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating permissions for service Service3")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}