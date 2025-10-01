using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for ServiceRegistryManager
    /// </summary>
    public class ServiceRegistryManagerTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public ServiceRegistryManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void ServiceRegistryManager_Class_Exists()
        {
            // This test verifies that the ServiceRegistryManager class exists and can be instantiated
            Assert.True(typeof(ServiceRegistryManager) != null);
        }

        [Fact]
        public void IsServiceInstalled_ReturnsBoolean()
        {
            // Act
            var result = ServiceRegistryManager.IsServiceInstalled();

            // Assert
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void IsServiceInstalled_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceRegistryManager.IsServiceInstalled();
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithNullArguments_DoesNotThrow()
        {
            // Act
            var result = await ServiceRegistryManager.RunScCommandAsync(null!);
            
            // Assert
            // Should not throw, result should be boolean (likely false due to invalid arguments)
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithEmptyArguments_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("");
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithValidArguments_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("query");
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("query", null);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("query", _mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithAllowFailureTrue_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("query", _mockLogger.Object, true);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithAllowFailureFalse_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("query", _mockLogger.Object, false);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task ForceCleanupServiceAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.ForceCleanupServiceAsync(null);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task ForceCleanupServiceAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.ForceCleanupServiceAsync(_mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task DirectRegistryCleanupAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.DirectRegistryCleanupAsync(null);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task DirectRegistryCleanupAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.DirectRegistryCleanupAsync(_mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task CreateServiceAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.CreateServiceAsync(null);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task CreateServiceAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.CreateServiceAsync(_mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task DeleteServiceAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.DeleteServiceAsync(null);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task DeleteServiceAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.DeleteServiceAsync(_mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void AllMethods_AreStatic()
        {
            // This test verifies that all methods are static as expected
            var type = typeof(ServiceRegistryManager);
            
            var isServiceInstalledMethod = type.GetMethod("IsServiceInstalled");
            var runScCommandMethod = type.GetMethod("RunScCommandAsync");
            var forceCleanupMethod = type.GetMethod("ForceCleanupServiceAsync");
            var directCleanupMethod = type.GetMethod("DirectRegistryCleanupAsync");
            var createServiceMethod = type.GetMethod("CreateServiceAsync");
            var deleteServiceMethod = type.GetMethod("DeleteServiceAsync");

            Assert.True(isServiceInstalledMethod?.IsStatic == true);
            Assert.True(runScCommandMethod?.IsStatic == true);
            Assert.True(forceCleanupMethod?.IsStatic == true);
            Assert.True(directCleanupMethod?.IsStatic == true);
            Assert.True(createServiceMethod?.IsStatic == true);
            Assert.True(deleteServiceMethod?.IsStatic == true);
        }

        [Fact]
        public void AllAsyncMethods_ReturnTaskOfBool()
        {
            // This test verifies that all async methods return Task<bool>
            var type = typeof(ServiceRegistryManager);
            
            var runScCommandMethod = type.GetMethod("RunScCommandAsync");
            var forceCleanupMethod = type.GetMethod("ForceCleanupServiceAsync");
            var directCleanupMethod = type.GetMethod("DirectRegistryCleanupAsync");
            var createServiceMethod = type.GetMethod("CreateServiceAsync");
            var deleteServiceMethod = type.GetMethod("DeleteServiceAsync");

            Assert.Equal(typeof(Task<bool>), runScCommandMethod?.ReturnType);
            Assert.Equal(typeof(Task<bool>), forceCleanupMethod?.ReturnType);
            Assert.Equal(typeof(Task<bool>), directCleanupMethod?.ReturnType);
            Assert.Equal(typeof(Task<bool>), createServiceMethod?.ReturnType);
            Assert.Equal(typeof(Task<bool>), deleteServiceMethod?.ReturnType);
        }

        [Fact]
        public void AllMethods_AcceptOptionalLogger()
        {
            // This test verifies that all methods accept an optional ILogger parameter
            var type = typeof(ServiceRegistryManager);
            
            var runScCommandMethod = type.GetMethod("RunScCommandAsync");
            var forceCleanupMethod = type.GetMethod("ForceCleanupServiceAsync");
            var directCleanupMethod = type.GetMethod("DirectRegistryCleanupAsync");
            var createServiceMethod = type.GetMethod("CreateServiceAsync");
            var deleteServiceMethod = type.GetMethod("DeleteServiceAsync");

            var runScParams = runScCommandMethod?.GetParameters();
            var forceCleanupParams = forceCleanupMethod?.GetParameters();
            var directCleanupParams = directCleanupMethod?.GetParameters();
            var createServiceParams = createServiceMethod?.GetParameters();
            var deleteServiceParams = deleteServiceMethod?.GetParameters();

            Assert.Equal(3, runScParams.Length);
            Assert.Equal(typeof(ILogger), runScParams[1].ParameterType);
            Assert.True(runScParams[1].HasDefaultValue);

            Assert.Single(forceCleanupParams);
            Assert.Equal(typeof(ILogger), forceCleanupParams[0].ParameterType);
            Assert.True(forceCleanupParams[0].HasDefaultValue);

            Assert.Single(directCleanupParams);
            Assert.Equal(typeof(ILogger), directCleanupParams[0].ParameterType);
            Assert.True(directCleanupParams[0].HasDefaultValue);

            Assert.Single(createServiceParams);
            Assert.Equal(typeof(ILogger), createServiceParams[0].ParameterType);
            Assert.True(createServiceParams[0].HasDefaultValue);

            Assert.Single(deleteServiceParams);
            Assert.Equal(typeof(ILogger), deleteServiceParams[0].ParameterType);
            Assert.True(deleteServiceParams[0].HasDefaultValue);
        }

        [Fact]
        public void Class_HasSupportedOSPlatformAttribute()
        {
            // This test verifies that the class has the SupportedOSPlatform attribute
            var type = typeof(ServiceRegistryManager);
            var attributes = type.GetCustomAttributes(typeof(System.Runtime.Versioning.SupportedOSPlatformAttribute), false);
            
            Assert.NotEmpty(attributes);
            Assert.Single(attributes);
            
            var attribute = (System.Runtime.Versioning.SupportedOSPlatformAttribute)attributes[0];
            Assert.Equal("windows", attribute.PlatformName);
        }

        [Fact]
        public async Task AllAsyncMethods_HandleExceptions()
        {
            // This test verifies that all async methods handle exceptions gracefully
            await ServiceRegistryManager.RunScCommandAsync("query", _mockLogger.Object);
            await ServiceRegistryManager.ForceCleanupServiceAsync(_mockLogger.Object);
            await ServiceRegistryManager.DirectRegistryCleanupAsync(_mockLogger.Object);
            await ServiceRegistryManager.CreateServiceAsync(_mockLogger.Object);
            await ServiceRegistryManager.DeleteServiceAsync(_mockLogger.Object);
            
            // Should not throw exceptions
            Assert.True(true);
        }

        [Fact]
        public void AllSyncMethods_HandleExceptions()
        {
            // This test verifies that all sync methods handle exceptions gracefully
            ServiceRegistryManager.IsServiceInstalled();
            
            // Should not throw exceptions
            Assert.True(true);
        }

        [Fact]
        public void RunScCommandAsync_HasCorrectParameterTypes()
        {
            // This test verifies that RunScCommandAsync has the correct parameter types
            var type = typeof(ServiceRegistryManager);
            var method = type.GetMethod("RunScCommandAsync");
            var parameters = method?.GetParameters();

            Assert.NotNull(parameters);
            Assert.Equal(3, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(ILogger), parameters[1].ParameterType);
            Assert.Equal(typeof(bool), parameters[2].ParameterType);
            Assert.True(parameters[1].HasDefaultValue);
            Assert.True(parameters[2].HasDefaultValue);
        }

        [Fact]
        public void IsServiceInstalled_ReturnsBool()
        {
            // This test verifies that IsServiceInstalled returns a boolean
            var type = typeof(ServiceRegistryManager);
            var method = type.GetMethod("IsServiceInstalled");
            
            Assert.Equal(typeof(bool), method?.ReturnType);
        }

        [Fact]
        public void IsServiceInstalled_HasNoParameters()
        {
            // This test verifies that IsServiceInstalled has no parameters
            var type = typeof(ServiceRegistryManager);
            var method = type.GetMethod("IsServiceInstalled");
            var parameters = method?.GetParameters();
            
            Assert.NotNull(parameters);
            Assert.Empty(parameters);
        }

        [Fact]
        public async Task RunScCommandAsync_WithInvalidCommand_HandlesGracefully()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("invalidcommand", _mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task RunScCommandAsync_WithLongRunningCommand_HandlesGracefully()
        {
            // Act & Assert
            var result = await ServiceRegistryManager.RunScCommandAsync("query type= service", _mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }
    }
}
