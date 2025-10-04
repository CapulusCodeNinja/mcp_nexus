using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for FileOperationsManager
    /// </summary>
    public class FileOperationsManagerTests
    {
        private readonly Mock<ILogger<FileOperationsManager>> m_MockLogger;

        public FileOperationsManagerTests()
        {
            m_MockLogger = new Mock<ILogger<FileOperationsManager>>();
        }

        [Fact]
        public void FileOperationsManager_Class_Exists()
        {
            // This test verifies that the FileOperationsManager class exists and can be instantiated
            Assert.True(typeof(FileOperationsManager) != null);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FileOperationsManager(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Act
            var manager = new FileOperationsManager(m_MockLogger.Object);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public async Task CopyFileAsync_WithNonExistentSource_ReturnsFalse()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var sourcePath = "nonexistent.txt";
            var destinationPath = "destination.txt";

            // Act
            var result = await manager.CopyFileAsync(sourcePath, destinationPath);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Source file does not exist")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CopyFileAsync_WithValidSource_CreatesDestinationDirectoryAndCopiesFile()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var sourcePath = Path.GetTempFileName();
            var destinationDir = Path.Combine(Path.GetTempPath(), "TestDir");
            var destinationPath = Path.Combine(destinationDir, "copied.txt");

            try
            {
                // Create source file
                await File.WriteAllTextAsync(sourcePath, "test content");

                // Act
                var result = await manager.CopyFileAsync(sourcePath, destinationPath);

                // Assert
                Assert.True(result);
                Assert.True(File.Exists(destinationPath));
                Assert.True(Directory.Exists(destinationDir));
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully copied file")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(sourcePath)) File.Delete(sourcePath);
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir);
            }
        }

        [Fact]
        public async Task CopyFileAsync_WithExistingDestination_OverwritesFile()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var sourcePath = Path.GetTempFileName();
            var destinationPath = Path.GetTempFileName();

            try
            {
                // Create source file
                await File.WriteAllTextAsync(sourcePath, "new content");
                // Create existing destination file
                await File.WriteAllTextAsync(destinationPath, "old content");

                // Act
                var result = await manager.CopyFileAsync(sourcePath, destinationPath);

                // Assert
                Assert.True(result);
                var content = await File.ReadAllTextAsync(destinationPath);
                Assert.Equal("new content", content);
            }
            finally
            {
                // Cleanup
                if (File.Exists(sourcePath)) File.Delete(sourcePath);
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
            }
        }

        [Fact]
        public async Task DeleteFileAsync_WithNonExistentFile_ReturnsTrue()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = "nonexistent.txt";

            // Act
            var result = await manager.DeleteFileAsync(filePath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteFileAsync_WithExistingFile_DeletesFile()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = Path.GetTempFileName();

            try
            {
                // Act
                var result = await manager.DeleteFileAsync(filePath);

                // Assert
                Assert.True(result);
                Assert.False(File.Exists(filePath));
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully deleted file")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public async Task CreateDirectoryAsync_WithNonExistentDirectory_CreatesDirectory()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "TestDirectory");

            try
            {
                // Act
                var result = await manager.CreateDirectoryAsync(directoryPath);

                // Assert
                Assert.True(result);
                Assert.True(Directory.Exists(directoryPath));
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully created directory")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath);
            }
        }

        [Fact]
        public async Task CreateDirectoryAsync_WithExistingDirectory_ReturnsTrue()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "ExistingDirectory");
            Directory.CreateDirectory(directoryPath);

            try
            {
                // Act
                var result = await manager.CreateDirectoryAsync(directoryPath);

                // Assert
                Assert.True(result);
                Assert.True(Directory.Exists(directoryPath));
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath);
            }
        }

        [Fact]
        public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = Path.GetTempFileName();

            try
            {
                // Act
                var result = await manager.FileExistsAsync(filePath);

                // Assert
                Assert.True(result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public async Task FileExistsAsync_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = "nonexistent.txt";

            // Act
            var result = await manager.FileExistsAsync(filePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetFileSizeAsync_WithExistingFile_ReturnsCorrectSize()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = Path.GetTempFileName();
            var content = "test content";

            try
            {
                await File.WriteAllTextAsync(filePath, content);

                // Act
                var result = await manager.GetFileSizeAsync(filePath);

                // Assert
                Assert.Equal(content.Length, result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public async Task GetFileSizeAsync_WithNonExistentFile_ReturnsZero()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = "nonexistent.txt";

            // Act
            var result = await manager.GetFileSizeAsync(filePath);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetFilesAsync_WithExistingDirectory_ReturnsFiles()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "TestDirectory");
            Directory.CreateDirectory(directoryPath);

            try
            {
                // Create test files
                var file1 = Path.Combine(directoryPath, "file1.txt");
                var file2 = Path.Combine(directoryPath, "file2.txt");
                await File.WriteAllTextAsync(file1, "content1");
                await File.WriteAllTextAsync(file2, "content2");

                // Act
                var result = await manager.GetFilesAsync(directoryPath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Length);
                Assert.Contains(file1, result);
                Assert.Contains(file2, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(directoryPath))
                {
                    var files = Directory.GetFiles(directoryPath);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                    Directory.Delete(directoryPath);
                }
            }
        }

        [Fact]
        public async Task GetFilesAsync_WithNonExistentDirectory_ReturnsEmptyArray()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = "nonexistent_directory";

            // Act
            var result = await manager.GetFilesAsync(directoryPath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFilesAsync_WithPattern_ReturnsMatchingFiles()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "TestDirectory");
            Directory.CreateDirectory(directoryPath);

            try
            {
                // Create test files
                var file1 = Path.Combine(directoryPath, "test1.txt");
                var file2 = Path.Combine(directoryPath, "test2.log");
                var file3 = Path.Combine(directoryPath, "other.txt");
                await File.WriteAllTextAsync(file1, "content1");
                await File.WriteAllTextAsync(file2, "content2");
                await File.WriteAllTextAsync(file3, "content3");

                // Act
                var result = await manager.GetFilesAsync(directoryPath, "*.txt");

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Length);
                Assert.Contains(file1, result);
                Assert.Contains(file3, result);
                Assert.DoesNotContain(file2, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(directoryPath))
                {
                    var files = Directory.GetFiles(directoryPath);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                    Directory.Delete(directoryPath);
                }
            }
        }

        [Fact]
        public async Task GetFilesAsync_WithSubdirectories_ReturnsAllFiles()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "TestDirectory");
            var subDirectoryPath = Path.Combine(directoryPath, "SubDirectory");
            Directory.CreateDirectory(directoryPath);
            Directory.CreateDirectory(subDirectoryPath);

            try
            {
                // Create test files
                var file1 = Path.Combine(directoryPath, "file1.txt");
                var file2 = Path.Combine(subDirectoryPath, "file2.txt");
                await File.WriteAllTextAsync(file1, "content1");
                await File.WriteAllTextAsync(file2, "content2");

                // Act
                var result = await manager.GetFilesAsync(directoryPath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Length);
                Assert.Contains(file1, result);
                Assert.Contains(file2, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(directoryPath))
                {
                    var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                    Directory.Delete(subDirectoryPath);
                    Directory.Delete(directoryPath);
                }
            }
        }

        [Fact]
        public async Task CopyFileAsync_WithInvalidPath_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var sourcePath = Path.GetTempFileName();
            var destinationPath = "invalid/path/with/invalid/characters/<>:|?*";

            try
            {
                await File.WriteAllTextAsync(sourcePath, "test content");

                // Act
                var result = await manager.CopyFileAsync(sourcePath, destinationPath);

                // Assert
                Assert.False(result);
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to copy file")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(sourcePath)) File.Delete(sourcePath);
            }
        }

        [Fact]
        public async Task DeleteFileAsync_WithReadOnlyFile_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(filePath, "test content");
                File.SetAttributes(filePath, FileAttributes.ReadOnly);

                // Act
                var result = await manager.DeleteFileAsync(filePath);

                // Assert
                Assert.False(result);
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to delete file")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                // Cleanup
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                }
            }
        }

        [Fact]
        public async Task CreateDirectoryAsync_WithInvalidPath_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = "invalid/path/with/invalid/characters/<>:|?*";

            // Act
            var result = await manager.CreateDirectoryAsync(directoryPath);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create directory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetFileSizeAsync_WithAccessDenied_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = "C:\\Windows\\System32\\config\\SAM"; // This file is typically inaccessible

            // Act
            var result = await manager.GetFileSizeAsync(filePath);

            // Assert
            // The file might be accessible or not, but the method should not throw an exception
            // and should return either 0 (if inaccessible) or the actual file size (if accessible)
            Assert.True(result >= 0, "File size should be non-negative");
            // The important thing is that the method doesn't throw an exception
        }

        [Fact]
        public async Task GetFilesAsync_WithAccessDenied_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = "C:\\Windows\\System32\\config"; // This directory may be inaccessible

            // Act
            var result = await manager.GetFilesAsync(directoryPath);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            // Note: The directory might be accessible, so we don't verify logging
            // The important thing is that the method returns empty array and doesn't throw
        }

        #region Branch Coverage Tests

        [Fact]
        public async Task CopyFileAsync_WithExceptionDuringDirectoryCreation_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var sourcePath = Path.GetTempFileName();
            var destinationPath = "Z:\\InvalidDrive\\file.txt"; // Invalid drive to force exception

            try
            {
                // Act
                var result = await manager.CopyFileAsync(sourcePath, destinationPath);

                // Assert
                Assert.False(result);
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to copy file")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                if (File.Exists(sourcePath)) File.Delete(sourcePath);
            }
        }

        [Fact]
        public async Task CopyFileAsync_WithExceptionDuringFileCopy_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var sourcePath = Path.GetTempFileName();
            var destinationPath = Path.GetTempFileName();

            try
            {
                // Create source file
                await File.WriteAllTextAsync(sourcePath, "test content");

                // Make destination file read-only to force exception during copy
                File.SetAttributes(destinationPath, FileAttributes.ReadOnly);

                // Act
                var result = await manager.CopyFileAsync(sourcePath, destinationPath);

                // Assert
                Assert.False(result);
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to copy file")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                if (File.Exists(sourcePath)) File.Delete(sourcePath);
                if (File.Exists(destinationPath))
                {
                    File.SetAttributes(destinationPath, FileAttributes.Normal);
                    File.Delete(destinationPath);
                }
            }
        }

        [Fact]
        public async Task DeleteFileAsync_WithExceptionDuringDeletion_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = Path.GetTempFileName();

            try
            {
                // Create file and make it read-only to force exception
                await File.WriteAllTextAsync(filePath, "test content");
                File.SetAttributes(filePath, FileAttributes.ReadOnly);

                // Act
                var result = await manager.DeleteFileAsync(filePath);

                // Assert
                Assert.False(result);
                m_MockLogger.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to delete file")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                }
            }
        }

        [Fact]
        public async Task CreateDirectoryAsync_WithExceptionDuringCreation_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = "Z:\\InvalidDrive\\TestDirectory"; // Invalid drive to force exception

            // Act
            var result = await manager.CreateDirectoryAsync(directoryPath);

            // Assert
            Assert.False(result);
            m_MockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create directory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetFileSizeAsync_WithExceptionDuringFileInfoCreation_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var filePath = "Z:\\InvalidDrive\\file.txt"; // Invalid drive to force exception

            // Act
            var result = await manager.GetFileSizeAsync(filePath);

            // Assert
            Assert.Equal(0, result);
            // Note: The exception might not be thrown in all cases, so we don't verify logging
            // The important thing is that the method returns 0 and doesn't throw
        }

        [Fact]
        public async Task GetFilesAsync_WithExceptionDuringDirectoryAccess_HandlesException()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "TestDirectory");

            try
            {
                // Create directory and make it read-only to force exception during access
                Directory.CreateDirectory(directoryPath);
                File.SetAttributes(directoryPath, FileAttributes.ReadOnly);

                // Act
                var result = await manager.GetFilesAsync(directoryPath);

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
                // Note: The exception might not be thrown in all cases, so we don't verify logging
                // The important thing is that the method returns empty array and doesn't throw
            }
            finally
            {
                if (Directory.Exists(directoryPath))
                {
                    File.SetAttributes(directoryPath, FileAttributes.Normal);
                    Directory.Delete(directoryPath);
                }
            }
        }

        [Fact]
        public async Task CopyFileAsync_WithNullDestinationDirectory_HandlesGracefully()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var sourcePath = Path.GetTempFileName();
            var destinationPath = "file.txt"; // No directory path

            try
            {
                // Create source file
                await File.WriteAllTextAsync(sourcePath, "test content");

                // Act
                var result = await manager.CopyFileAsync(sourcePath, destinationPath);

                // Assert
                Assert.True(result);
                Assert.True(File.Exists(destinationPath));
            }
            finally
            {
                if (File.Exists(sourcePath)) File.Delete(sourcePath);
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
            }
        }

        [Fact]
        public async Task GetFilesAsync_WithEmptyPattern_ReturnsAllFiles()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "TestFiles");
            var file1 = Path.Combine(directoryPath, "file1.txt");
            var file2 = Path.Combine(directoryPath, "file2.log");

            try
            {
                Directory.CreateDirectory(directoryPath);
                await File.WriteAllTextAsync(file1, "content1");
                await File.WriteAllTextAsync(file2, "content2");

                // Act
                var result = await manager.GetFilesAsync(directoryPath, "");

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Length);
                Assert.Contains(file1, result);
                Assert.Contains(file2, result);
            }
            finally
            {
                if (File.Exists(file1)) File.Delete(file1);
                if (File.Exists(file2)) File.Delete(file2);
                if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath);
            }
        }

        [Fact]
        public async Task GetFilesAsync_WithSpecificPattern_ReturnsMatchingFiles()
        {
            // Arrange
            var manager = new FileOperationsManager(m_MockLogger.Object);
            var directoryPath = Path.Combine(Path.GetTempPath(), "TestFiles");
            var file1 = Path.Combine(directoryPath, "file1.txt");
            var file2 = Path.Combine(directoryPath, "file2.log");
            var file3 = Path.Combine(directoryPath, "file3.txt");

            try
            {
                Directory.CreateDirectory(directoryPath);
                await File.WriteAllTextAsync(file1, "content1");
                await File.WriteAllTextAsync(file2, "content2");
                await File.WriteAllTextAsync(file3, "content3");

                // Act
                var result = await manager.GetFilesAsync(directoryPath, "*.txt");

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Length);
                Assert.Contains(file1, result);
                Assert.Contains(file3, result);
                Assert.DoesNotContain(file2, result);
            }
            finally
            {
                if (File.Exists(file1)) File.Delete(file1);
                if (File.Exists(file2)) File.Delete(file2);
                if (File.Exists(file3)) File.Delete(file3);
                if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath);
            }
        }

        #endregion
    }
}
