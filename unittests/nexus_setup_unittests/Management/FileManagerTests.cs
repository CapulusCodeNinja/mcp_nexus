using System;
using System.IO;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using nexus.external_apis.FileSystem;
using nexus.setup.Management;

using NLog;

using Xunit;

namespace nexus.setup.unittests.Management;

/// <summary>
/// Unit tests for FileManager class.
/// </summary>
public class FileManagerTests
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Logger m_Logger;
    private readonly FileManager m_FileManager;

    /// <summary>
    /// Initializes a new instance of the FileManagerTests class.
    /// </summary>
    public FileManagerTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_Logger = LogManager.GetCurrentClassLogger();
        m_FileManager = new FileManager(m_MockFileSystem.Object);
    }

    /// <summary>
    /// Verifies that constructor with null file system throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new FileManager(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync calls CreateDirectory.
    /// </summary>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithValidDirectories_CallsCreateDirectory()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        m_MockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns(Array.Empty<string>());
        m_MockFileSystem.Setup(fs => fs.GetDirectoryInfo(It.IsAny<string>()))
            .Throws(new IOException("File system error")); // Force exception path

        // Act
        var result = await m_FileManager.CopyApplicationFilesAsync("C:\\source", "C:\\dest");

        // Assert - CreateDirectory may be called multiple times (by FileManager and DirectoryCopyUtility)
        m_MockFileSystem.Verify(fs => fs.CreateDirectory("C:\\dest"), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync returns false on exception.
    /// </summary>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()))
            .Throws(new IOException("Access denied"));

        // Act
        var result = await m_FileManager.CopyApplicationFilesAsync("C:\\source", "C:\\dest");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RemoveApplicationFiles removes directory successfully.
    /// </summary>
    [Fact]
    public void RemoveApplicationFiles_WithExistingDirectory_ShouldReturnTrue()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\app"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DeleteDirectory("C:\\app", true));

        // Act
        var result = m_FileManager.RemoveApplicationFiles("C:\\app");

        // Assert
        result.Should().BeTrue();
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory("C:\\app", true), Times.Once);
    }

    /// <summary>
    /// Verifies that RemoveApplicationFiles returns true when directory doesn't exist.
    /// </summary>
    [Fact]
    public void RemoveApplicationFiles_WithNonExistentDirectory_ShouldReturnTrue()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = m_FileManager.RemoveApplicationFiles("C:\\app");

        // Assert
        result.Should().BeTrue();
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RemoveApplicationFiles returns false on exception.
    /// </summary>
    [Fact]
    public void RemoveApplicationFiles_WithException_ShouldReturnFalse()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Access denied"));

        // Act
        var result = m_FileManager.RemoveApplicationFiles("C:\\app");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync with empty source still calls CreateDirectory.
    /// </summary>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithEmptySource_CallsCreateDirectory()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        m_MockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Throws(new IOException("Path error")); // Force exception with empty/invalid path

        // Act
        var result = await m_FileManager.CopyApplicationFilesAsync("", "C:\\dest");

        // Assert - CreateDirectory should be called before the exception
        m_MockFileSystem.Verify(fs => fs.CreateDirectory("C:\\dest"), Times.Once);
    }

    /// <summary>
    /// Verifies that RemoveApplicationFiles with empty path returns true.
    /// </summary>
    [Fact]
    public void RemoveApplicationFiles_WithEmptyPath_ShouldReturnTrue()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(""))
            .Returns(false);

        // Act
        var result = m_FileManager.RemoveApplicationFiles("");

        // Assert
        result.Should().BeTrue();
    }
}

