using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using nexus.setup;
using nexus.setup.Core;
using nexus.setup.Interfaces;
using nexus.setup.Models;
using nexus.utilities.FileSystem;
using nexus.utilities.ServiceManagement;
using Xunit;

namespace nexus.setup_unittests.Core;

/// <summary>
/// Unit tests for ServiceUpdater.
/// </summary>
public class ServiceUpdaterTests
{
    private readonly ILogger<ServiceUpdater> m_Logger;
    private readonly Mock<IServiceInstaller> m_MockInstaller;
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IServiceController> m_MockServiceController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUpdaterTests"/> class.
    /// </summary>
    public ServiceUpdaterTests()
    {
        m_Logger = NullLogger<ServiceUpdater>.Instance;
        m_MockInstaller = new Mock<IServiceInstaller>();
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockServiceController = new Mock<IServiceController>();
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceUpdater(
            null!,
            m_MockInstaller.Object,
            m_MockFileSystem.Object,
            m_MockServiceController.Object));
    }

    /// <summary>
    /// Verifies constructor throws when service installer is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenServiceInstallerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceUpdater(
            m_Logger,
            null!,
            m_MockFileSystem.Object,
            m_MockServiceController.Object));
    }

    /// <summary>
    /// Verifies UpdateServiceAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateServiceAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var updater = new ServiceUpdater(m_Logger, m_MockInstaller.Object, m_MockFileSystem.Object, m_MockServiceController.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => updater.UpdateServiceAsync(serviceName!, "test.exe"));
    }

    /// <summary>
    /// Verifies UpdateServiceAsync throws when executable path is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateServiceAsync_ThrowsArgumentException_WhenExecutablePathIsNullOrEmpty(string? executablePath)
    {
        // Arrange
        var updater = new ServiceUpdater(m_Logger, m_MockInstaller.Object, m_MockFileSystem.Object, m_MockServiceController.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => updater.UpdateServiceAsync("TestService", executablePath!));
    }

    /// <summary>
    /// Verifies BackupServiceAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BackupServiceAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var updater = new ServiceUpdater(m_Logger, m_MockInstaller.Object, m_MockFileSystem.Object, m_MockServiceController.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => updater.BackupServiceAsync(serviceName!, @"C:\backup"));
    }

    /// <summary>
    /// Verifies BackupServiceAsync throws when backup path is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BackupServiceAsync_ThrowsArgumentException_WhenBackupPathIsNullOrEmpty(string? backupPath)
    {
        // Arrange
        var updater = new ServiceUpdater(m_Logger, m_MockInstaller.Object, m_MockFileSystem.Object, m_MockServiceController.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => updater.BackupServiceAsync("TestService", backupPath!));
    }

    /// <summary>
    /// Verifies RestoreServiceAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RestoreServiceAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var updater = new ServiceUpdater(m_Logger, m_MockInstaller.Object, m_MockFileSystem.Object, m_MockServiceController.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => updater.RestoreServiceAsync(serviceName!, @"C:\backup"));
    }

    /// <summary>
    /// Verifies RestoreServiceAsync throws when backup path is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RestoreServiceAsync_ThrowsArgumentException_WhenBackupPathIsNullOrEmpty(string? backupPath)
    {
        // Arrange
        var updater = new ServiceUpdater(m_Logger, m_MockInstaller.Object, m_MockFileSystem.Object, m_MockServiceController.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => updater.RestoreServiceAsync("TestService", backupPath!));
    }
}

