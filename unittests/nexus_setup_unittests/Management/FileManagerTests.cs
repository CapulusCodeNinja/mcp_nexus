using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;
using Nexus.Setup.Management;

using NLog;

using Xunit;

namespace Nexus.Setup.Unittests.Management;

/// <summary>
/// Unit tests for FileManager class.
/// </summary>
public class FileManagerTests
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Logger m_Logger;
    private readonly FileManager m_FileManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileManagerTests"/> class.
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
        _ = action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync calls CreateDirectory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithValidDirectories_CallsCreateDirectory()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        _ = m_MockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns(Array.Empty<string>());
        _ = m_MockFileSystem.Setup(fs => fs.GetDirectoryInfo(It.IsAny<string>()))
            .Throws(new IOException("File system error")); // Force exception path

        // Act
        var result = await m_FileManager.CopyApplicationFilesAsync("C:\\source", "C:\\dest");

        // Assert - CreateDirectory may be called multiple times (by FileManager and DirectoryCopyUtility)
        m_MockFileSystem.Verify(fs => fs.CreateDirectory("C:\\dest"), Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync returns false on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()))
            .Throws(new IOException("Access denied"));

        // Act
        var result = await m_FileManager.CopyApplicationFilesAsync("C:\\source", "C:\\dest");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RemoveApplicationFiles removes directory successfully.
    /// </summary>
    [Fact]
    public void RemoveApplicationFiles_WithExistingDirectory_ShouldReturnTrue()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\app"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteDirectory("C:\\app", true));

        // Act
        var result = m_FileManager.RemoveApplicationFiles("C:\\app");

        // Assert
        _ = result.Should().BeTrue();
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory("C:\\app", true), Times.Once);
    }

    /// <summary>
    /// Verifies that RemoveApplicationFiles returns true when directory doesn't exist.
    /// </summary>
    [Fact]
    public void RemoveApplicationFiles_WithNonExistentDirectory_ShouldReturnTrue()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = m_FileManager.RemoveApplicationFiles("C:\\app");

        // Assert
        _ = result.Should().BeTrue();
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RemoveApplicationFiles returns false on exception.
    /// </summary>
    [Fact]
    public void RemoveApplicationFiles_WithException_ShouldReturnFalse()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Access denied"));

        // Act
        var result = m_FileManager.RemoveApplicationFiles("C:\\app");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync with empty source still calls CreateDirectory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithEmptySource_CallsCreateDirectory()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        _ = m_MockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Throws(new IOException("Path error")); // Force exception with empty/invalid path

        // Act
        var result = await m_FileManager.CopyApplicationFilesAsync(string.Empty, "C:\\dest");

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
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(string.Empty))
            .Returns(false);

        // Act
        var result = m_FileManager.RemoveApplicationFiles(string.Empty);

        // Assert
        _ = result.Should().BeTrue();
    }
}
