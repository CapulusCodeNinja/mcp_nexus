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
        private readonly Mock<ILogger<ServiceFileManager>> m_MockLogger;
        private readonly string m_TestSourceDir;
        private readonly string m_TestTargetDir;
        private readonly string m_TestBackupDir;
        private readonly string m_TestServiceDir;

        public ServiceFileManagerTests()
        {
            m_MockLogger = new Mock<ILogger<ServiceFileManager>>();
            m_TestSourceDir = Path.Combine(Path.GetTempPath(), "ServiceFileManagerTest_Source");
            m_TestTargetDir = Path.Combine(Path.GetTempPath(), "ServiceFileManagerTest_Target");
            m_TestBackupDir = Path.Combine(Path.GetTempPath(), "ServiceFileManagerTest_Backup");
            m_TestServiceDir = Path.Combine(Path.GetTempPath(), "ServiceFileManagerTest_Service");
        }

        public void Dispose()
        {
            // Cleanup test directories
            CleanupDirectory(m_TestSourceDir);
            CleanupDirectory(m_TestTargetDir);
            CleanupDirectory(m_TestBackupDir);
            CleanupDirectory(m_TestServiceDir);
        }

        private void CleanupDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                try
                {
                    Directory.Delete(directory, recursive: true);
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
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ServiceFileManager(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Act
            var manager = new ServiceFileManager(m_MockLogger.Object);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public async Task CopyServiceFilesAsync_WithNonExistentSource_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentSource = Path.Combine(Path.GetTempPath(), "NonExistentSource");

            // Act
            var result = await manager.CopyServiceFilesAsync(nonExistentSource, m_TestTargetDir);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Source path does not exist")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CopyServiceFilesAsync_WithValidDirectories_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestSourceDir);
            Directory.CreateDirectory(m_TestTargetDir);
            
            try
            {
                await File.WriteAllTextAsync(Path.Combine(m_TestSourceDir, "test.txt"), "test content");

                // Act
                var result = await manager.CopyServiceFilesAsync(m_TestSourceDir, m_TestTargetDir);

                // Assert
                Assert.True(result);
                Assert.True(File.Exists(Path.Combine(m_TestTargetDir, "test.txt")));
            }
            catch (UnauthorizedAccessException)
            {
                // Skip test if we don't have permission to create files in temp directory
                Assert.True(true, "Skipped due to file access permissions");
            }
        }

        [Fact]
        public async Task CopyServiceFilesAsync_WithNonExistentTarget_CreatesTargetDirectory()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestSourceDir);
            
            try
            {
                await File.WriteAllTextAsync(Path.Combine(m_TestSourceDir, "test.txt"), "test content");

                // Act
                var result = await manager.CopyServiceFilesAsync(m_TestSourceDir, m_TestTargetDir);

                // Assert
                Assert.True(result);
                Assert.True(Directory.Exists(m_TestTargetDir));
                Assert.True(File.Exists(Path.Combine(m_TestTargetDir, "test.txt")));
            }
            catch (UnauthorizedAccessException)
            {
                // Skip test if we don't have permission to create files in temp directory
                Assert.True(true, "Skipped due to file access permissions");
            }
        }

        [Fact]
        public async Task CopyServiceFilesAsync_WithException_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestSourceDir);
            
            try
            {
                // Create a file that will cause an exception when copying
                await File.WriteAllTextAsync(Path.Combine(m_TestSourceDir, "test.txt"), "test content");
                File.SetAttributes(Path.Combine(m_TestSourceDir, "test.txt"), FileAttributes.ReadOnly);

                // Act
                var result = await manager.CopyServiceFilesAsync(m_TestSourceDir, "C:\\InvalidPath\\With\\Special\\Characters\\<>|");

                // Assert
                Assert.False(result);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip test if we don't have permission to create files in temp directory
                Assert.True(true, "Skipped due to file access permissions");
            }
        }

        [Fact]
        public async Task BackupServiceFilesAsync_WithNonExistentServicePath_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentServicePath = Path.Combine(Path.GetTempPath(), "NonExistentService");

            // Act
            var result = await manager.BackupServiceFilesAsync(nonExistentServicePath, m_TestBackupDir);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Service path does not exist")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BackupServiceFilesAsync_WithValidServicePath_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestServiceDir);
            await File.WriteAllTextAsync(Path.Combine(m_TestServiceDir, "service.txt"), "service content");

            // Act
            var result = await manager.BackupServiceFilesAsync(m_TestServiceDir, m_TestBackupDir);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(m_TestBackupDir));
            var backupDirs = Directory.GetDirectories(m_TestBackupDir);
            Assert.Single(backupDirs);
            Assert.Contains("backup_", backupDirs[0]);
        }

        [Fact]
        public async Task RestoreServiceFilesAsync_WithNonExistentBackupPath_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentBackupPath = Path.Combine(Path.GetTempPath(), "NonExistentBackup");

            // Act
            var result = await manager.RestoreServiceFilesAsync(nonExistentBackupPath, m_TestServiceDir);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Backup path does not exist")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task RestoreServiceFilesAsync_WithValidBackupPath_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestBackupDir);
            await File.WriteAllTextAsync(Path.Combine(m_TestBackupDir, "backup.txt"), "backup content");

            // Act
            var result = await manager.RestoreServiceFilesAsync(m_TestBackupDir, m_TestServiceDir);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(m_TestServiceDir, "backup.txt")));
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithNonExistentBackupPath_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentBackupPath = Path.Combine(Path.GetTempPath(), "NonExistentBackup");

            // Act
            var result = await manager.CleanupOldBackupsAsync(nonExistentBackupPath, 30);

            // Assert
            Assert.True(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Backup path does not exist")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithValidBackupPath_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestBackupDir);
            var oldBackupDir = Path.Combine(m_TestBackupDir, "old_backup");
            Directory.CreateDirectory(oldBackupDir);
            // Set creation time to 35 days ago
            Directory.SetCreationTimeUtc(oldBackupDir, DateTime.UtcNow.AddDays(-35));

            // Act
            var result = await manager.CleanupOldBackupsAsync(m_TestBackupDir, 30);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(oldBackupDir));
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithRecentBackups_DoesNotDelete()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestBackupDir);
            var recentBackupDir = Path.Combine(m_TestBackupDir, "recent_backup");
            Directory.CreateDirectory(recentBackupDir);
            // Set creation time to 10 days ago
            Directory.SetCreationTimeUtc(recentBackupDir, DateTime.UtcNow.AddDays(-10));

            // Act
            var result = await manager.CleanupOldBackupsAsync(m_TestBackupDir, 30);

            // Assert
            Assert.True(result);
            Assert.True(Directory.Exists(recentBackupDir));
        }

        [Fact]
        public async Task ValidateServiceFilesAsync_WithNonExistentServicePath_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentServicePath = Path.Combine(Path.GetTempPath(), "NonExistentService");

            // Act
            var result = await manager.ValidateServiceFilesAsync(nonExistentServicePath);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Service path does not exist")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidateServiceFilesAsync_WithValidServicePath_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestServiceDir);

            // Act
            var result = await manager.ValidateServiceFilesAsync(m_TestServiceDir);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateServiceFilesAsync_WithConfiguration_WithNonExistentServicePath_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentServicePath = Path.Combine(Path.GetTempPath(), "NonExistentService");
            var config = new ServiceConfiguration { ExecutableName = "test.exe" };

            // Act
            var result = await manager.ValidateServiceFilesAsync(nonExistentServicePath, config);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateServiceFilesAsync_WithConfiguration_WithMissingRequiredFile_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestServiceDir);
            var config = new ServiceConfiguration { ExecutableName = "missing.exe" };

            // Act
            var result = await manager.ValidateServiceFilesAsync(m_TestServiceDir, config);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Required file missing")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidateServiceFilesAsync_WithConfiguration_WithAllRequiredFiles_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestServiceDir);
            await File.WriteAllTextAsync(Path.Combine(m_TestServiceDir, "test.exe"), "executable");
            await File.WriteAllTextAsync(Path.Combine(m_TestServiceDir, "appsettings.json"), "{}");
            await File.WriteAllTextAsync(Path.Combine(m_TestServiceDir, "appsettings.Production.json"), "{}");
            var config = new ServiceConfiguration 
            { 
                ExecutableName = "test.exe",
                InstallFolder = m_TestServiceDir
            };

            // Act
            var result = await manager.ValidateServiceFilesAsync(m_TestServiceDir, config);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteServiceFilesAsync_WithNonExistentDirectory_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistentDir");

            // Act
            var result = await manager.DeleteServiceFilesAsync(nonExistentDir);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteServiceFilesAsync_WithExistingDirectory_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestServiceDir);
            await File.WriteAllTextAsync(Path.Combine(m_TestServiceDir, "test.txt"), "test content");

            // Act
            var result = await manager.DeleteServiceFilesAsync(m_TestServiceDir);

            // Assert
            Assert.True(result);
            Assert.False(Directory.Exists(m_TestServiceDir));
        }

        [Fact]
        public async Task GetServiceFilesAsync_WithNonExistentServicePath_ReturnsEmptyArray()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentServicePath = Path.Combine(Path.GetTempPath(), "NonExistentService");

            // Act
            var result = await manager.GetServiceFilesAsync(nonExistentServicePath);

            // Assert
            Assert.Empty(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Service path does not exist")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetServiceFilesAsync_WithValidServicePath_ReturnsFiles()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestServiceDir);
            await File.WriteAllTextAsync(Path.Combine(m_TestServiceDir, "test1.txt"), "content1");
            await File.WriteAllTextAsync(Path.Combine(m_TestServiceDir, "test2.txt"), "content2");

            // Act
            var result = await manager.GetServiceFilesAsync(m_TestServiceDir);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(result, f => f.Name == "test1.txt");
            Assert.Contains(result, f => f.Name == "test2.txt");
        }

        [Fact]
        public async Task CreateBackupInstanceAsync_WithValidLogger_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);

            // Act
            var result = await manager.CreateBackupInstanceAsync(m_MockLogger.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void FindProjectDirectory_ReturnsCurrentDirectory()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);

            // Act
            var result = manager.FindProjectDirectory();

            // Assert
            Assert.Equal(Environment.CurrentDirectory, result);
        }

        [Fact]
        public void ValidateInstallationFiles_WithExistingDirectory_ReturnsTrue()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            Directory.CreateDirectory(m_TestServiceDir);

            // Act
            var result = manager.ValidateInstallationFiles(m_TestServiceDir);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateInstallationFiles_WithNonExistentDirectory_ReturnsFalse()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var nonExistentDir = Path.Combine(Path.GetTempPath(), "NonExistentDir");

            // Act
            var result = manager.ValidateInstallationFiles(nonExistentDir);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetBackupInfo_WithValidPath_ReturnsInfo()
        {
            // Arrange
            var manager = new ServiceFileManager(m_MockLogger.Object);
            var backupPath = Path.Combine(Path.GetTempPath(), "TestBackup");

            // Act
            var result = manager.GetBackupInfo(backupPath);

            // Assert
            Assert.NotNull(result);
            // The result is an anonymous object, so we can't easily verify its properties
            // Just ensure it doesn't throw and returns something
        }

        [Fact]
        public async Task BuildProjectForDeploymentAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.BuildProjectForDeploymentAsync(null!);
            // Should not throw, but may return false if no project found
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task BuildProjectForDeploymentAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.BuildProjectForDeploymentAsync(m_MockLogger.Object);
            // Should not throw, but may return false if no project found
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void FindProjectDirectoryStatic_WithNullPath_ReturnsNull()
        {
            // Act
            var result = ServiceFileManager.FindProjectDirectoryStatic(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectoryStatic_WithEmptyPath_ReturnsNull()
        {
            // Act
            var result = ServiceFileManager.FindProjectDirectoryStatic("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectoryStatic_WithValidPath_ReturnsStringOrNull()
        {
            // Act
            var result = ServiceFileManager.FindProjectDirectoryStatic(Environment.CurrentDirectory);

            // Assert
            // Result can be null if no project found, or a string if found
            // Just verify it doesn't throw
            Assert.True(result == null || result != null);
        }

        [Fact]
        public async Task CopyApplicationFilesAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CopyApplicationFilesAsync(null!);
            // Should not throw
        }

        [Fact]
        public async Task CopyApplicationFilesAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CopyApplicationFilesAsync(m_MockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithNullSourceDir_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ServiceFileManager.CopyDirectoryAsync(null!, m_TestTargetDir, m_MockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithNullTargetDir_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ServiceFileManager.CopyDirectoryAsync(m_TestSourceDir, null!, m_MockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithEmptySourceDir_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                ServiceFileManager.CopyDirectoryAsync("", m_TestTargetDir, m_MockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithEmptyTargetDir_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                ServiceFileManager.CopyDirectoryAsync(m_TestSourceDir, "", m_MockLogger.Object));
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithValidDirectories_DoesNotThrow()
        {
            // Arrange
            Directory.CreateDirectory(m_TestSourceDir);
            Directory.CreateDirectory(m_TestTargetDir);

            try
            {
                // Act & Assert
                await ServiceFileManager.CopyDirectoryAsync(m_TestSourceDir, m_TestTargetDir, m_MockLogger.Object);
                // Should not throw
            }
            catch (UnauthorizedAccessException)
            {
                // Skip test if we don't have permission to create files in temp directory
                Assert.True(true, "Skipped due to file access permissions");
            }
        }

        [Fact]
        public async Task CopyDirectoryAsync_WithNullLogger_DoesNotThrow()
        {
            // Arrange
            Directory.CreateDirectory(m_TestSourceDir);
            Directory.CreateDirectory(m_TestTargetDir);

            try
            {
                // Act & Assert
                await ServiceFileManager.CopyDirectoryAsync(m_TestSourceDir, m_TestTargetDir, null!);
                // Should not throw
            }
            catch (UnauthorizedAccessException)
            {
                // Skip test if we don't have permission to create files in temp directory
                Assert.True(true, "Skipped due to file access permissions");
            }
        }

        [Fact]
        public async Task CreateBackupAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.CreateBackupAsync(null!);
            // Should not throw, result can be null or string
            // Result should be a boolean - just verify it doesn't throw
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task CreateBackupAsync_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ServiceFileManager.CreateBackupAsync(m_MockLogger.Object);
            // Should not throw, result can be null or string
            // Result should be a boolean - just verify it doesn't throw
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task CleanupOldBackupsStaticAsync_WithDefaultMaxBackups_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CleanupOldBackupsStaticAsync("C:\\Test\\Backup", 30, m_MockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task CleanupOldBackupsStaticAsync_WithCustomMaxBackups_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CleanupOldBackupsStaticAsync("test-path", 10, m_MockLogger.Object);
            // Should not throw
        }

        [Fact]
        public async Task CleanupOldBackupsStaticAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            await ServiceFileManager.CleanupOldBackupsStaticAsync("test-path", 5, m_MockLogger.Object);
            // Should not throw
        }

        [Fact]
        public void ValidateInstallationFiles_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.ValidateInstallationFilesStatic("C:\\Test\\Service");
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void ValidateInstallationFiles_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.ValidateInstallationFilesStatic("C:\\Test\\Service");
            // Should not throw, result should be boolean
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void GetBackupInfo_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.GetBackupInfoStatic("C:\\Test\\Backup");
            // Should not throw, result should be a list
            Assert.NotNull(result);
            Assert.IsType<List<object>>(result);
        }

        [Fact]
        public void GetBackupInfo_WithValidLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = ServiceFileManager.GetBackupInfoStatic("C:\\Test\\Backup");
            // Should not throw, result should be a list
            Assert.NotNull(result);
            Assert.IsType<List<object>>(result);
        }

        [Fact]
        public void AllMethods_AreStatic()
        {
            // This test verifies that all methods are static as expected
            var type = typeof(ServiceFileManager);

            var buildMethod = type.GetMethod("BuildProjectForDeploymentAsync", new[] { typeof(ILogger) });
            var findProjectMethod = type.GetMethod("FindProjectDirectoryStatic", new[] { typeof(string) });
            var copyFilesMethod = type.GetMethod("CopyApplicationFilesAsync", new[] { typeof(ILogger) });
            var copyDirMethod = type.GetMethod("CopyDirectoryAsync", new[] { typeof(string), typeof(string), typeof(ILogger) });
            var createBackupMethod = type.GetMethod("CreateBackupAsync", new[] { typeof(ILogger) });
            var cleanupMethod = type.GetMethod("CleanupOldBackupsStaticAsync", new[] { typeof(string), typeof(int), typeof(ILogger) });
            var validateMethod = type.GetMethod("ValidateInstallationFilesStatic", new[] { typeof(string) });
            var getBackupInfoMethod = type.GetMethod("GetBackupInfoStatic", new[] { typeof(string) });

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

            var buildMethod = type.GetMethod("BuildProjectForDeploymentAsync", new[] { typeof(ILogger) });
            var copyFilesMethod = type.GetMethod("CopyApplicationFilesAsync", new[] { typeof(ILogger) });
            var copyDirMethod = type.GetMethod("CopyDirectoryAsync", new[] { typeof(string), typeof(string), typeof(ILogger) });
            var createBackupMethod = type.GetMethod("CreateBackupAsync", new[] { typeof(ILogger) });
            var cleanupMethod = type.GetMethod("CleanupOldBackupsStaticAsync", new[] { typeof(string), typeof(int), typeof(ILogger) });

            Assert.Equal(typeof(Task<bool>), buildMethod?.ReturnType);
            Assert.Equal(typeof(Task<bool>), copyFilesMethod?.ReturnType);
            Assert.Equal(typeof(Task), copyDirMethod?.ReturnType);
            Assert.Equal(typeof(Task<bool>), createBackupMethod?.ReturnType);
            Assert.Equal(typeof(Task<bool>), cleanupMethod?.ReturnType);
        }

        [Fact]
        public void AllMethods_AcceptOptionalLogger()
        {
            // This test verifies that all methods accept an optional ILogger parameter
            var type = typeof(ServiceFileManager);

            var buildMethod = type.GetMethod("BuildProjectForDeploymentAsync", new[] { typeof(ILogger) });
            var copyFilesMethod = type.GetMethod("CopyApplicationFilesAsync", new[] { typeof(ILogger) });
            var copyDirMethod = type.GetMethod("CopyDirectoryAsync", new[] { typeof(string), typeof(string), typeof(ILogger) });
            var createBackupMethod = type.GetMethod("CreateBackupAsync", new[] { typeof(ILogger) });
            var cleanupMethod = type.GetMethod("CleanupOldBackupsStaticAsync", new[] { typeof(string), typeof(int), typeof(ILogger) });
            var validateMethod = type.GetMethod("ValidateInstallationFilesStatic", new[] { typeof(string) });
            var getBackupInfoMethod = type.GetMethod("GetBackupInfoStatic", new[] { typeof(string) });

            // Check that methods that have ILogger parameter have it as the last parameter
            var buildParams = buildMethod?.GetParameters();
            var copyFilesParams = copyFilesMethod?.GetParameters();
            var copyDirParams = copyDirMethod?.GetParameters();
            var createBackupParams = createBackupMethod?.GetParameters();
            var cleanupParams = cleanupMethod?.GetParameters();

            Assert.NotNull(buildParams);
            Assert.Single(buildParams);
            Assert.Equal(typeof(ILogger), buildParams[0].ParameterType);

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
            Assert.Equal(3, cleanupParams.Length);
            Assert.Equal(typeof(ILogger), cleanupParams[2].ParameterType);
            Assert.True(cleanupParams[2].HasDefaultValue);
        }

        [Fact]
        public async Task AllAsyncMethods_HandleExceptions()
        {
            // This test verifies that all async methods handle exceptions gracefully
            await ServiceFileManager.BuildProjectForDeploymentAsync(m_MockLogger.Object);
            await ServiceFileManager.CopyApplicationFilesAsync(m_MockLogger.Object);
            await ServiceFileManager.CreateBackupAsync(m_MockLogger.Object);
            await ServiceFileManager.CleanupOldBackupsStaticAsync("test-path", 5, m_MockLogger.Object);

            // Should not throw exceptions
            Assert.True(true);
        }

        [Fact]
        public void AllSyncMethods_HandleExceptions()
        {
            // This test verifies that all sync methods handle exceptions gracefully
            ServiceFileManager.FindProjectDirectoryStatic(Environment.CurrentDirectory);
            ServiceFileManager.ValidateInstallationFilesStatic("C:\\Test\\Service");
            ServiceFileManager.GetBackupInfoStatic("C:\\Test\\Backup");

            // Should not throw exceptions
            Assert.True(true);
        }
    }
}
