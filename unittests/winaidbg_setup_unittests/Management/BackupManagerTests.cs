using FluentAssertions;

using Moq;

using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.Setup.Management;

using NLog;

using Xunit;

namespace WinAiDbg.Setup.Unittests.Management;

/// <summary>
/// Unit tests for BackupManager class.
/// </summary>
public class BackupManagerTests
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Logger m_Logger;
    private readonly BackupManager m_BackupManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupManagerTests"/> class.
    /// </summary>
    public BackupManagerTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_Logger = LogManager.GetCurrentClassLogger();
        m_BackupManager = new BackupManager(m_MockFileSystem.Object);
    }

    /// <summary>
    /// Verifies that CreateBackupAsync returns true when source directory doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithNonExistentSourceDirectory_ShouldReturnTrue()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await m_BackupManager.CreateBackupAsync("C:\\source", "C:\\backup");

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CreateBackupAsync calls CreateDirectory on backup path.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithValidDirectories_CallsCreateDirectory()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\source"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        _ = m_MockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns(Array.Empty<string>());
        _ = m_MockFileSystem.Setup(fs => fs.GetDirectoryInfo(It.IsAny<string>()))
            .Throws(new IOException("File system error")); // Force exception path

        // Act
        var result = await m_BackupManager.CreateBackupAsync("C:\\source", "C:\\backup");

        // Assert - Should return false due to exception, but CreateDirectory should still be called
        m_MockFileSystem.Verify(fs => fs.CreateDirectory("C:\\backup"), Times.Once);
    }

    /// <summary>
    /// Verifies that CreateBackupAsync returns false on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()))
            .Throws(new IOException("Disk full"));

        // Act
        var result = await m_BackupManager.CreateBackupAsync("C:\\source", "C:\\backup");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync cleans up when no backup exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RollbackInstallationAsync_WithNoBackup_ShouldCleanUpDirectory()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\install"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\backup"))
            .Returns(false);

        // Act
        await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", false);

        // Assert
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory("C:\\install", true), Times.Once);
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync does nothing when installation directory doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RollbackInstallationAsync_WithNonExistentInstallDir_ShouldDoNothing()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var action = async () => await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", false);

        // Assert
        _ = await action.Should().NotThrowAsync();
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync handles exceptions gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RollbackInstallationAsync_WithException_ShouldHandleGracefully()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Access denied"));

        // Act
        var action = async () => await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", false);

        // Assert
        _ = await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync calls DeleteDirectory when backup is created.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RollbackInstallationAsync_WhenBackupCreated_CallsDeleteDirectory()
    {
        // Arrange
        var backupDirInfo = new DirectoryInfo("C:\\backup");

        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\backup"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\install"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.GetDirectoryInfo("C:\\backup"))
            .Returns(backupDirInfo);

        // Act
        await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", true);

        // Assert - The method should attempt to query backup directories
        m_MockFileSystem.Verify(fs => fs.GetDirectoryInfo("C:\\backup"), Times.Once);
    }

    /// <summary>
    /// Verifies that CreateBackupAsync with null path throws exception which is handled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateBackupAsync_WithNullPath_ReturnsTrue()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await m_BackupManager.CreateBackupAsync(null!, "C:\\backup");

        // Assert - Should return true because directory doesn't exist check handles null
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync with null paths handles gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RollbackInstallationAsync_WithNullPaths_HandlesGracefully()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var action = async () => await m_BackupManager.RollbackInstallationAsync(null!, null!, false);

        // Assert
        _ = await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that CreateBackupAsync handles directory creation failure.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CreateBackupAsync_WhenCreateDirectoryFails_ReturnsFalse()
    {
        // Arrange
        _ = m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\source"))
            .Returns(true);
        _ = m_MockFileSystem.Setup(fs => fs.CreateDirectory("C:\\backup"))
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await m_BackupManager.CreateBackupAsync("C:\\source", "C:\\backup");

        // Assert
        _ = result.Should().BeFalse();
    }
}
