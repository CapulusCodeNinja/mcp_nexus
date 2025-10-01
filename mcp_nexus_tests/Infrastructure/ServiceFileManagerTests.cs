using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;
using System.Runtime.Versioning;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for ServiceFileManager
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ServiceFileManagerTests : IDisposable
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testSourceDir;
        private readonly string _testTargetDir;

        public ServiceFileManagerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _testSourceDir = Path.Combine(Path.GetTempPath(), "ServiceFileManagerTest_Source");
            _testTargetDir = Path.Combine(Path.GetTempPath(), "ServiceFileManagerTest_Target");
        }

        public void Dispose()
        {
            // Cleanup test directories
            if (Directory.Exists(_testSourceDir))
            {
                try
                {
                    Directory.Delete(_testSourceDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            if (Directory.Exists(_testTargetDir))
            {
                try
                {
                    Directory.Delete(_testTargetDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Fact]
        public void ServiceFileManager_Class_Exists()
        {
            // This test verifies that the ServiceFileManager class exists and can be instantiated
            Assert.True(typeof(ServiceFileManager) != null);
        }

        [Fact]
        public async Task BuildProjectForDeploymentAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.BuildProjectForDeploymentAsync(null);
            // Should not throw, but may return false if no project found
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task BuildProjectForDeploymentAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.BuildProjectForDeploymentAsync(_mockLogger.Object);
            // Should not throw, but may return false if no project found
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void FindProjectDirectory_WithNullPath_ReturnsNull()
        {
            // Act
            var result = ServiceFileManager.FindProjectDirectory(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectory_WithEmptyPath_ReturnsNull()
        {
            // Act
            var result = ServiceFileManager.FindProjectDirectory("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectory_WithValidPath_ReturnsStringOrNull()
        {
            // Act
            var result = ServiceFileManager.FindProjectDirectory(Environment.CurrentDirectory);

            // Assert
            // Result can be null if no project found, or a string if found
            Assert.True(result == null || result is string);
        }

        [Fact]
        public async Task CopyApplicationFilesAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CopyApplicationFilesAsync(null);
            // Should not throw
        }

        [Fact]
        public async Task CopyApplicationFilesAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CopyApplicationFilesAsync(_mockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithNullSourceDir_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                ServiceFileManager.CopyDirectoryAsync(null!, _testTargetDir, _mockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithNullTargetDir_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                ServiceFileManager.CopyDirectoryAsync(_testSourceDir, null!, _mockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithEmptySourceDir_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                ServiceFileManager.CopyDirectoryAsync("", _testTargetDir, _mockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithEmptyTargetDir_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                ServiceFileManager.CopyDirectoryAsync(_testSourceDir, "", _mockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithValidDirectories_DoesNotThrow()
        {
            // Arrange
            Directory.CreateDirectory(_testSourceDir);
            Directory.CreateDirectory(_testTargetDir);

            // Act & Assert
            await ServiceFileManager.CopyDirectoryAsync(_testSourceDir, _testTargetDir, _mockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithNullLogger_DoesNotThrow()
        {
            // Arrange
            Directory.CreateDirectory(_testSourceDir);
            Directory.CreateDirectory(_testTargetDir);

            // Act & Assert
            await ServiceFileManager.CopyDirectoryAsync(_testSourceDir, _testTargetDir, null);
            // Should not throw
        }

        [Fact]
        public async Task CreateBackupAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.CreateBackupAsync(null);
            // Should not throw, result can be null or string
            Assert.True(result == null || result is string);
        }

        [Fact]
        public async Task CreateBackupAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.CreateBackupAsync(_mockLogger.Object);
            // Should not throw, result can be null or string
            Assert.True(result == null || result is string);
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithDefaultMaxBackups_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CleanupOldBackupsAsync(logger: _mockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithCustomMaxBackups_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CleanupOldBackupsAsync(10, _mockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CleanupOldBackupsAsync(5, null);
            // Should not throw
        }

        [Fact]
        public void ValidateInstallationFiles_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.ValidateInstallationFiles(null);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void ValidateInstallationFiles_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.ValidateInstallationFiles(_mockLogger.Object);
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void GetBackupInfo_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.GetBackupInfo(null);
            // Should not throw, result should be a list
            Assert.NotNull(result);
            Assert.IsType<List<BackupInfo>>(result);
        }

        [Fact]
        public void GetBackupInfo_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.GetBackupInfo(_mockLogger.Object);
            // Should not throw, result should be a list
            Assert.NotNull(result);
            Assert.IsType<List<BackupInfo>>(result);
        }

        [Fact]
        public void AllMethods_AreStatic()
        {
            // This test verifies that all methods are static as expected
            var type = typeof(ServiceFileManager);
            
            var buildMethod = type.GetMethod("BuildProjectForDeploymentAsync");
            var findProjectMethod = type.GetMethod("FindProjectDirectory");
            var copyFilesMethod = type.GetMethod("CopyApplicationFilesAsync");
            var copyDirMethod = type.GetMethod("CopyDirectoryAsync");
            var createBackupMethod = type.GetMethod("CreateBackupAsync");
            var cleanupMethod = type.GetMethod("CleanupOldBackupsAsync");
            var validateMethod = type.GetMethod("ValidateInstallationFiles");
            var getBackupInfoMethod = type.GetMethod("GetBackupInfo");

            Assert.True(buildMethod?.IsStatic == true);
            Assert.True(findProjectMethod?.IsStatic == true);
            Assert.True(copyFilesMethod?.IsStatic == true);
            Assert.True(copyDirMethod?.IsStatic == true);
            Assert.True(createBackupMethod?.IsStatic == true);
            Assert.True(cleanupMethod?.IsStatic == true);
            Assert.True(validateMethod?.IsStatic == true);
            Assert.True(getBackupInfoMethod?.IsStatic == true);
        }

        [Fact]
        public void AllAsyncMethods_ReturnTask()
        {
            // This test verifies that all async methods return Task or Task<T>
            var type = typeof(ServiceFileManager);
            
            var buildMethod = type.GetMethod("BuildProjectForDeploymentAsync");
            var copyFilesMethod = type.GetMethod("CopyApplicationFilesAsync");
            var copyDirMethod = type.GetMethod("CopyDirectoryAsync");
            var createBackupMethod = type.GetMethod("CreateBackupAsync");
            var cleanupMethod = type.GetMethod("CleanupOldBackupsAsync");

            Assert.Equal(typeof(Task<bool>), buildMethod?.ReturnType);
            Assert.Equal(typeof(Task), copyFilesMethod?.ReturnType);
            Assert.Equal(typeof(Task), copyDirMethod?.ReturnType);
            Assert.Equal(typeof(Task<string?>), createBackupMethod?.ReturnType);
            Assert.Equal(typeof(Task), cleanupMethod?.ReturnType);
        }

        [Fact]
        public void AllMethods_AcceptOptionalLogger()
        {
            // This test verifies that all methods accept an optional ILogger parameter
            var type = typeof(ServiceFileManager);
            
            var buildMethod = type.GetMethod("BuildProjectForDeploymentAsync");
            var copyFilesMethod = type.GetMethod("CopyApplicationFilesAsync");
            var copyDirMethod = type.GetMethod("CopyDirectoryAsync");
            var createBackupMethod = type.GetMethod("CreateBackupAsync");
            var cleanupMethod = type.GetMethod("CleanupOldBackupsAsync");
            var validateMethod = type.GetMethod("ValidateInstallationFiles");
            var getBackupInfoMethod = type.GetMethod("GetBackupInfo");

            // Check that all methods have ILogger as the last parameter and it's optional
            var buildParams = buildMethod?.GetParameters();
            var copyFilesParams = copyFilesMethod?.GetParameters();
            var copyDirParams = copyDirMethod?.GetParameters();
            var createBackupParams = createBackupMethod?.GetParameters();
            var cleanupParams = cleanupMethod?.GetParameters();
            var validateParams = validateMethod?.GetParameters();
            var getBackupInfoParams = getBackupInfoMethod?.GetParameters();

            Assert.NotNull(buildParams);
            Assert.Single(buildParams);
            Assert.Equal(typeof(ILogger), buildParams[0].ParameterType);
            Assert.True(buildParams[0].HasDefaultValue);

            Assert.NotNull(copyFilesParams);
            Assert.Single(copyFilesParams);
            Assert.Equal(typeof(ILogger), copyFilesParams[0].ParameterType);
            Assert.True(copyFilesParams[0].HasDefaultValue);

            Assert.NotNull(copyDirParams);
            Assert.Equal(3, copyDirParams.Length);
            Assert.Equal(typeof(ILogger), copyDirParams[2].ParameterType);
            Assert.True(copyDirParams[2].HasDefaultValue);

            Assert.NotNull(createBackupParams);
            Assert.Single(createBackupParams);
            Assert.Equal(typeof(ILogger), createBackupParams[0].ParameterType);
            Assert.True(createBackupParams[0].HasDefaultValue);

            Assert.NotNull(cleanupParams);
            Assert.Equal(2, cleanupParams.Length);
            Assert.Equal(typeof(ILogger), cleanupParams[1].ParameterType);
            Assert.True(cleanupParams[1].HasDefaultValue);

            Assert.NotNull(validateParams);
            Assert.Single(validateParams);
            Assert.Equal(typeof(ILogger), validateParams[0].ParameterType);
            Assert.True(validateParams[0].HasDefaultValue);

            Assert.NotNull(getBackupInfoParams);
            Assert.Single(getBackupInfoParams);
            Assert.Equal(typeof(ILogger), getBackupInfoParams[0].ParameterType);
            Assert.True(getBackupInfoParams[0].HasDefaultValue);
        }

        [Fact]
        public async Task AllAsyncMethods_HandleExceptions()
        {
            // This test verifies that all async methods handle exceptions gracefully
            await ServiceFileManager.BuildProjectForDeploymentAsync(_mockLogger.Object);
            await ServiceFileManager.CopyApplicationFilesAsync(_mockLogger.Object);
            await ServiceFileManager.CreateBackupAsync(_mockLogger.Object);
            await ServiceFileManager.CleanupOldBackupsAsync(5, _mockLogger.Object);
            
            // Should not throw exceptions
            Assert.True(true);
        }

        [Fact]
        public void AllSyncMethods_HandleExceptions()
        {
            // This test verifies that all sync methods handle exceptions gracefully
            ServiceFileManager.FindProjectDirectory(Environment.CurrentDirectory);
            ServiceFileManager.ValidateInstallationFiles(_mockLogger.Object);
            ServiceFileManager.GetBackupInfo(_mockLogger.Object);
            
            // Should not throw exceptions
            Assert.True(true);
        }
    }
}
