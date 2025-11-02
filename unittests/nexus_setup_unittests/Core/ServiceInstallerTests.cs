using System.ServiceProcess;

using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.External.Apis.ServiceManagement;
using Nexus.Setup.Core;
using Nexus.Setup.Models;

using Xunit;

namespace Nexus.Setup.Unittests.Core;

/// <summary>
/// Unit tests for the <see cref="ServiceInstaller"/> class.
/// </summary>
public class ServiceInstallerTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IProcessManager> m_ProcessManagerMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstallerTests"/> class.
    /// </summary>
    public ServiceInstallerTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ProcessManagerMock = new Mock<IProcessManager>();
        m_ServiceControllerMock = new Mock<IServiceController>();
    }

    /// <summary>
    /// Verifies that parameterless constructor creates installer successfully.
    /// </summary>
    [Fact]
    public void Constructor_Parameterless_Succeeds()
    {
        // Act
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Assert
        _ = installer.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that constructor with parameters creates installer successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithParameters_Succeeds()
    {
        // Act
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Assert
        _ = installer.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that InstallServiceAsync throws ArgumentNullException when options is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InstallServiceAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => installer.InstallServiceAsync(null!));
    }

    /// <summary>
    /// Verifies that InstallServiceAsync throws ArgumentException when service name is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InstallServiceAsync_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var options = new ServiceInstallationOptions { ServiceName = string.Empty };

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => installer.InstallServiceAsync(options));
    }

    /// <summary>
    /// Verifies that InstallServiceAsync throws ArgumentException when executable path is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InstallServiceAsync_WithEmptyExecutablePath_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var options = new ServiceInstallationOptions { ServiceName = "TestService", ExecutablePath = string.Empty };

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => installer.InstallServiceAsync(options));
    }

    /// <summary>
    /// Verifies that InstallServiceAsync returns failure when executable file not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InstallServiceAsync_WhenExecutableNotFound_ReturnsFailure()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var options = new ServiceInstallationOptions
        {
            ServiceName = "TestService",
            ExecutablePath = "C:\\nonexistent\\test.exe",
        };

        _ = m_FileSystemMock.Setup(fs => fs.FileExists(options.ExecutablePath)).Returns(false);

        // Act
        var result = await installer.InstallServiceAsync(options);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("not found");
    }

    /// <summary>
    /// Verifies that InstallServiceAsync returns failure when service is already installed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InstallServiceAsync_WhenServiceAlreadyInstalled_ReturnsFailure()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var options = new ServiceInstallationOptions
        {
            ServiceName = "TestService",
            ExecutablePath = "C:\\test\\test.exe",
        };

        _ = m_FileSystemMock.Setup(fs => fs.FileExists(options.ExecutablePath)).Returns(true);
        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(options.ServiceName)).Returns(true);

        // Act
        var result = await installer.InstallServiceAsync(options);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("already installed");
    }

    /// <summary>
    /// Verifies that UninstallServiceAsync throws ArgumentException when service name is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UninstallServiceAsync_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => installer.UninstallServiceAsync(string.Empty));
    }

    /// <summary>
    /// Verifies that UninstallServiceAsync returns failure when service is not installed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UninstallServiceAsync_WhenServiceNotInstalled_ReturnsFailure()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(serviceName)).Returns(false);

        // Act
        var result = await installer.UninstallServiceAsync(serviceName);

        // Assert
        _ = result.Success.Should().BeFalse();
        _ = result.Message.Should().Contain("not installed");
    }

    /// <summary>
    /// Verifies that IsServiceInstalled returns false for null service name.
    /// </summary>
    [Fact]
    public void IsServiceInstalled_WithNullServiceName_ReturnsFalse()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act
        var result = installer.IsServiceInstalled(null!);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsServiceInstalled returns false for empty service name.
    /// </summary>
    [Fact]
    public void IsServiceInstalled_WithEmptyServiceName_ReturnsFalse()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act
        var result = installer.IsServiceInstalled(string.Empty);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsServiceInstalled delegates to service controller.
    /// </summary>
    [Fact]
    public void IsServiceInstalled_WithValidServiceName_DelegatesToController()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";

        _ = m_ServiceControllerMock.Setup(sc => sc.IsServiceInstalled(serviceName)).Returns(true);

        // Act
        var result = installer.IsServiceInstalled(serviceName);

        // Assert
        _ = result.Should().BeTrue();
        m_ServiceControllerMock.Verify(sc => sc.IsServiceInstalled(serviceName), Times.Once);
    }

    /// <summary>
    /// Verifies that GetServiceStatus returns null for null service name.
    /// </summary>
    [Fact]
    public void GetServiceStatus_WithNullServiceName_ReturnsNull()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act
        var result = installer.GetServiceStatus(null!);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetServiceStatus returns null for empty service name.
    /// </summary>
    [Fact]
    public void GetServiceStatus_WithEmptyServiceName_ReturnsNull()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act
        var result = installer.GetServiceStatus(string.Empty);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetServiceStatus delegates to service controller.
    /// </summary>
    [Fact]
    public void GetServiceStatus_WithValidServiceName_DelegatesToController()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";

        _ = m_ServiceControllerMock.Setup(sc => sc.GetServiceStatus(serviceName))
            .Returns(ServiceControllerStatus.Running);

        // Act
        var result = installer.GetServiceStatus(serviceName);

        // Assert
        _ = result.Should().Be(ServiceControllerStatus.Running);
        m_ServiceControllerMock.Verify(sc => sc.GetServiceStatus(serviceName), Times.Once);
    }

    /// <summary>
    /// Verifies that WaitForServiceStatusAsync throws ArgumentException when service name is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WaitForServiceStatusAsync_WithEmptyServiceName_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            installer.WaitForServiceStatusAsync(string.Empty, "Running", TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Verifies that WaitForServiceStatusAsync throws ArgumentException when target status is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WaitForServiceStatusAsync_WithEmptyTargetStatus_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            installer.WaitForServiceStatusAsync("TestService", string.Empty, TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Verifies that WaitForServiceStatusAsync returns true when service reaches target status.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WaitForServiceStatusAsync_WhenServiceReachesStatus_ReturnsTrue()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";

        _ = m_ServiceControllerMock.Setup(sc => sc.GetServiceStatus(serviceName))
            .Returns(ServiceControllerStatus.Running);

        // Act
        var result = await installer.WaitForServiceStatusAsync(serviceName, "Running", TimeSpan.FromSeconds(5));

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that WaitForServiceStatusAsync returns false on timeout.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WaitForServiceStatusAsync_WhenTimeout_ReturnsFalse()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";

        _ = m_ServiceControllerMock.Setup(sc => sc.GetServiceStatus(serviceName))
            .Returns(ServiceControllerStatus.Stopped);

        // Act
        var result = await installer.WaitForServiceStatusAsync(serviceName, "Running", TimeSpan.FromMilliseconds(100));

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that WaitForServiceStatusAsync returns false on cancellation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task WaitForServiceStatusAsync_WhenCancelled_ReturnsFalse()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var serviceName = "TestService";
        var cts = new CancellationTokenSource();

        _ = m_ServiceControllerMock.Setup(sc => sc.GetServiceStatus(serviceName))
            .Returns(ServiceControllerStatus.Stopped);

        cts.Cancel();

        // Act
        var result = await installer.WaitForServiceStatusAsync(serviceName, "Running", TimeSpan.FromSeconds(5), cts.Token);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that BuildProjectAsync throws ArgumentException when project path is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BuildProjectAsync_WithEmptyProjectPath_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => installer.BuildProjectAsync(string.Empty));
    }

    /// <summary>
    /// Verifies that BuildProjectAsync throws ArgumentException when configuration is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task BuildProjectAsync_WithEmptyConfiguration_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => installer.BuildProjectAsync("test.csproj", string.Empty));
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync throws ArgumentException when source is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithEmptySource_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => installer.CopyApplicationFilesAsync(string.Empty, "target"));
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync throws ArgumentException when target is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyApplicationFilesAsync_WithEmptyTarget_ThrowsArgumentException()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => installer.CopyApplicationFilesAsync("source", string.Empty));
    }

    /// <summary>
    /// Verifies that CopyApplicationFilesAsync returns false when source directory does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CopyApplicationFilesAsync_WhenSourceNotExist_ReturnsFalse()
    {
        // Arrange
        var installer = new ServiceInstaller(m_FileSystemMock.Object, m_ProcessManagerMock.Object, m_ServiceControllerMock.Object);
        var source = "C:\\nonexistent";
        var target = "C:\\target";

        _ = m_FileSystemMock.Setup(fs => fs.DirectoryExists(source)).Returns(false);

        // Act
        var result = await installer.CopyApplicationFilesAsync(source, target);

        // Assert
        _ = result.Should().BeFalse();
    }
}
