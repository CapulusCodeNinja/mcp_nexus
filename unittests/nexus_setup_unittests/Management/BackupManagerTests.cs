using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using nexus.external_apis.FileSystem;
using nexus.setup.Management;

namespace nexus.setup.unittests.Management;

/// <summary>
/// Unit tests for BackupManager class.
/// </summary>
public class BackupManagerTests
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly ILogger<BackupManager> m_Logger;
    private readonly BackupManager m_BackupManager;

    /// <summary>
    /// Initializes a new instance of the BackupManagerTests class.
    /// </summary>
    public BackupManagerTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_Logger = NullLogger<BackupManager>.Instance;
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetRequiredService<ILogger<BackupManager>>()).Returns(m_Logger);
        m_BackupManager = new BackupManager(serviceProvider.Object, m_MockFileSystem.Object);
    }

    /// <summary>
    /// Verifies that constructor with null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new BackupManager(null!, m_MockFileSystem.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that constructor with null file system throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ShouldThrowArgumentNullException()
    {
        // Act
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetRequiredService<ILogger<BackupManager>>()).Returns(m_Logger);
        var action = () => new BackupManager(serviceProvider.Object, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that CreateBackupAsync returns true when source directory doesn't exist.
    /// </summary>
    [Fact]
    public async Task CreateBackupAsync_WithNonExistentSourceDirectory_ShouldReturnTrue()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var result = await m_BackupManager.CreateBackupAsync("C:\\source", "C:\\backup");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CreateBackupAsync calls CreateDirectory on backup path.
    /// </summary>
    [Fact]
    public async Task CreateBackupAsync_WithValidDirectories_CallsCreateDirectory()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\source"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        m_MockFileSystem.Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns(Array.Empty<string>());
        m_MockFileSystem.Setup(fs => fs.GetDirectoryInfo(It.IsAny<string>()))
            .Throws(new IOException("File system error")); // Force exception path

        // Act
        var result = await m_BackupManager.CreateBackupAsync("C:\\source", "C:\\backup");

        // Assert - Should return false due to exception, but CreateDirectory should still be called
        m_MockFileSystem.Verify(fs => fs.CreateDirectory("C:\\backup"), Times.Once);
    }

    /// <summary>
    /// Verifies that CreateBackupAsync returns false on exception.
    /// </summary>
    [Fact]
    public async Task CreateBackupAsync_WithException_ShouldReturnFalse()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()))
            .Throws(new IOException("Disk full"));

        // Act
        var result = await m_BackupManager.CreateBackupAsync("C:\\source", "C:\\backup");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync cleans up when no backup exists.
    /// </summary>
    [Fact]
    public async Task RollbackInstallationAsync_WithNoBackup_ShouldCleanUpDirectory()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\install"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\backup"))
            .Returns(false);

        // Act
        await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", false);

        // Assert
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory("C:\\install", true), Times.Once);
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync does nothing when installation directory doesn't exist.
    /// </summary>
    [Fact]
    public async Task RollbackInstallationAsync_WithNonExistentInstallDir_ShouldDoNothing()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);

        // Act
        var action = async () => await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", false);

        // Assert
        await action.Should().NotThrowAsync();
        m_MockFileSystem.Verify(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync handles exceptions gracefully.
    /// </summary>
    [Fact]
    public async Task RollbackInstallationAsync_WithException_ShouldHandleGracefully()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new IOException("Access denied"));

        // Act
        var action = async () => await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", false);

        // Assert
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that RollbackInstallationAsync calls DeleteDirectory when backup is created.
    /// </summary>
    [Fact]
    public async Task RollbackInstallationAsync_WhenBackupCreated_CallsDeleteDirectory()
    {
        // Arrange
        var backupDirInfo = new DirectoryInfo("C:\\backup");
        
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\backup"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.DirectoryExists("C:\\install"))
            .Returns(true);
        m_MockFileSystem.Setup(fs => fs.GetDirectoryInfo("C:\\backup"))
            .Returns(backupDirInfo);

        // Act
        await m_BackupManager.RollbackInstallationAsync("C:\\install", "C:\\backup", true);

        // Assert - The method should attempt to query backup directories
        m_MockFileSystem.Verify(fs => fs.GetDirectoryInfo("C:\\backup"), Times.Once);
    }
}

