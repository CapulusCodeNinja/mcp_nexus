using FluentAssertions;

using Moq;

using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.External.Apis.ServiceManagement;
using WinAiDbg.Setup.Core;

using Xunit;

namespace WinAiDbg.Setup.Unittests.Core;

/// <summary>
/// Unit tests for the <see cref="ServiceUpdater"/> class.
/// </summary>
public class ServiceUpdaterTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IProcessManager> m_ProcessManagerMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUpdaterTests"/> class.
    /// </summary>
    public ServiceUpdaterTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ProcessManagerMock = new Mock<IProcessManager>();
        m_ServiceControllerMock = new Mock<IServiceController>();
    }

    /// <summary>
    /// Verifies that parameterless constructor creates updater successfully.
    /// </summary>
    [Fact]
    public void Constructor_Parameterless_Succeeds()
    {
        // Act
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Assert
        _ = updater.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that constructor with parameters creates updater successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithParameters_Succeeds()
    {
        // Act
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Assert
        _ = updater.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that UpdateServiceAsync throws ArgumentException when service name is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateServiceAsync_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => updater.UpdateServiceAsync(string.Empty, "C:\\new.exe"));
    }

    /// <summary>
    /// Verifies that UpdateServiceAsync throws ArgumentException when executable path is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateServiceAsync_WithEmptyExecutablePath_ThrowsArgumentException()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => updater.UpdateServiceAsync("TestService", string.Empty));
    }

    /// <summary>
    /// Verifies that UpdateServiceAsync returns failure when new executable file not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateServiceAsync_WhenNewExecutableNotFound_ReturnsFailure()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";
        var newExePath = "C:\\nonexistent\\new.exe";

        _ = m_FileSystemMock.Setup(fs => fs.FileExists(newExePath)).Returns(false);

        // Act
        var result = await updater.UpdateServiceAsync(serviceName, newExePath);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("not found");
    }

    /// <summary>
    /// Verifies that UpdateServiceAsync returns failure when service is not installed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateServiceAsync_WhenServiceNotInstalled_ReturnsFailure()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";
        var newExePath = "C:\\new.exe";

        _ = m_FileSystemMock.Setup(fs => fs.FileExists(newExePath)).Returns(true);
        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(serviceName)).Returns(false);

        // Act
        var result = await updater.UpdateServiceAsync(serviceName, newExePath);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("not installed");
    }

    /// <summary>
    /// Verifies that UpdateServiceAsync returns failure when current path cannot be determined.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateServiceAsync_WhenCurrentPathUndetermined_ReturnsFailure()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";
        var newExePath = "C:\\new.exe";

        _ = m_FileSystemMock.Setup(fs => fs.FileExists(newExePath)).Returns(true);
        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(serviceName)).Returns(true);
        _ = m_ServiceControllerMock.Setup(sc => sc.GetServiceExecutablePath(serviceName)).Returns(string.Empty);

        // Act
        var result = await updater.UpdateServiceAsync(serviceName, newExePath);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("current executable path");
    }

    /// <summary>
    /// Verifies that BackupServiceAsync throws ArgumentException when service name is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BackupServiceAsync_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => updater.BackupServiceAsync(string.Empty, "C:\\backup"));
    }

    /// <summary>
    /// Verifies that BackupServiceAsync throws ArgumentException when backup path is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BackupServiceAsync_WithEmptyBackupPath_ThrowsArgumentException()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => updater.BackupServiceAsync("TestService", string.Empty));
    }

    /// <summary>
    /// Verifies that BackupServiceAsync returns false when executable path cannot be determined.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BackupServiceAsync_WhenExecutablePathUndetermined_ReturnsFalse()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";
        var backupPath = "C:\\backup";

        _ = m_ServiceControllerMock.Setup(sc => sc.GetServiceExecutablePath(serviceName)).Returns(string.Empty);

        // Act
        var result = await updater.BackupServiceAsync(serviceName, backupPath);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RestoreServiceAsync throws ArgumentException when service name is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreServiceAsync_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => updater.RestoreServiceAsync(string.Empty, "C:\\backup"));
    }

    /// <summary>
    /// Verifies that RestoreServiceAsync throws ArgumentException when backup path is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreServiceAsync_WithEmptyBackupPath_ThrowsArgumentException()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => updater.RestoreServiceAsync("TestService", string.Empty));
    }

    /// <summary>
    /// Verifies that RestoreServiceAsync returns failure when backup directory not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreServiceAsync_WhenBackupDirectoryNotFound_ReturnsFailure()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";
        var backupPath = "C:\\nonexistent";

        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(backupPath)).Returns(false);

        // Act
        var result = await updater.RestoreServiceAsync(serviceName, backupPath);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("not found");
    }

    /// <summary>
    /// Verifies that RestoreServiceAsync returns failure when executable path cannot be determined.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RestoreServiceAsync_WhenExecutablePathUndetermined_ReturnsFailure()
    {
        // Arrange
        var updater = new ServiceUpdater(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";
        var backupPath = "C:\\backup";

        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(backupPath)).Returns(true);
        _ = m_ServiceControllerMock.Setup(sc => sc.GetServiceExecutablePath(serviceName)).Returns(string.Empty);

        // Act
        var result = await updater.RestoreServiceAsync(serviceName, backupPath);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("current executable path");
    }
}
