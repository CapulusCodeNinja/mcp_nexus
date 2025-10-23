using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using nexus.setup;
using nexus.setup.Core;
using nexus.setup.Models;
using nexus.utilities.FileSystem;
using nexus.utilities.ProcessManagement;
using nexus.utilities.ServiceManagement;
using Xunit;

namespace nexus.setup_unittests.Core;

/// <summary>
/// Unit tests for ServiceInstaller.
/// </summary>
public class ServiceInstallerTests
{
    private readonly ILogger<ServiceInstaller> m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstallerTests"/> class.
    /// </summary>
    public ServiceInstallerTests()
    {
        m_Logger = NullLogger<ServiceInstaller>.Instance;
    }

    /// <summary>
    /// Creates a ServiceInstaller instance with mocked dependencies.
    /// </summary>
    /// <returns>A tuple containing the installer and mocks.</returns>
    private (ServiceInstaller installer, Mock<IFileSystem> fileSystem, Mock<IProcessManager> processManager, Mock<IServiceController> serviceController) CreateServiceInstaller()
    {
        var mockFileSystem = new Mock<IFileSystem>();
        var mockProcessManager = new Mock<IProcessManager>();
        var mockServiceController = new Mock<IServiceController>();

        var installer = new ServiceInstaller(
            m_Logger,
            mockFileSystem.Object,
            mockProcessManager.Object,
            mockServiceController.Object);

        return (installer, mockFileSystem, mockProcessManager, mockServiceController);
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        var mockFileSystem = new Mock<IFileSystem>();
        var mockProcessManager = new Mock<IProcessManager>();
        var mockServiceController = new Mock<IServiceController>();

        Assert.Throws<ArgumentNullException>(() => new ServiceInstaller(
            null!,
            mockFileSystem.Object,
            mockProcessManager.Object,
            mockServiceController.Object));
    }

    /// <summary>
    /// Verifies InstallServiceAsync throws when options is null.
    /// </summary>
    [Fact]
    public async Task InstallServiceAsync_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => installer.InstallServiceAsync(null!));
    }

    /// <summary>
    /// Verifies InstallServiceAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstallServiceAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();
        var options = new ServiceInstallationOptions { ServiceName = serviceName!, ExecutablePath = "test.exe" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => installer.InstallServiceAsync(options));
    }

    /// <summary>
    /// Verifies InstallServiceAsync throws when executable path is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InstallServiceAsync_ThrowsArgumentException_WhenExecutablePathIsNullOrEmpty(string? executablePath)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();
        var options = new ServiceInstallationOptions { ServiceName = "TestService", ExecutablePath = executablePath! };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => installer.InstallServiceAsync(options));
    }

    /// <summary>
    /// Verifies UninstallServiceAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UninstallServiceAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => installer.UninstallServiceAsync(serviceName!));
    }

    /// <summary>
    /// Verifies IsServiceInstalled returns false for null or empty service name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsServiceInstalled_ReturnsFalse_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act
        var result = installer.IsServiceInstalled(serviceName!);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies IsServiceInstalled returns false for non-existent service.
    /// </summary>
    [Fact]
    public void IsServiceInstalled_ReturnsFalse_ForNonExistentService()
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act
        var result = installer.IsServiceInstalled("NonExistentService_" + Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies GetServiceStatus returns null for null or empty service name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetServiceStatus_ReturnsNull_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act
        var result = installer.GetServiceStatus(serviceName!);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies GetServiceStatus returns null for non-existent service.
    /// </summary>
    [Fact]
    public void GetServiceStatus_ReturnsNull_ForNonExistentService()
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act
        var result = installer.GetServiceStatus("NonExistentService_" + Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies WaitForServiceStatusAsync throws when service name is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WaitForServiceStatusAsync_ThrowsArgumentException_WhenServiceNameIsNullOrEmpty(string? serviceName)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            installer.WaitForServiceStatusAsync(serviceName!, "Running", TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Verifies WaitForServiceStatusAsync throws when target status is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WaitForServiceStatusAsync_ThrowsArgumentException_WhenTargetStatusIsNullOrEmpty(string? targetStatus)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            installer.WaitForServiceStatusAsync("TestService", targetStatus!, TimeSpan.FromSeconds(1)));
    }

    /// <summary>
    /// Verifies WaitForServiceStatusAsync returns false for non-existent service.
    /// </summary>
    [Fact]
    public async Task WaitForServiceStatusAsync_ReturnsFalse_ForNonExistentService()
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act
        var result = await installer.WaitForServiceStatusAsync("NonExistentService_" + Guid.NewGuid(), "Running", TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies BuildProjectAsync throws when project path is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuildProjectAsync_ThrowsArgumentException_WhenProjectPathIsNullOrEmpty(string? projectPath)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => installer.BuildProjectAsync(projectPath!));
    }

    /// <summary>
    /// Verifies BuildProjectAsync throws when configuration is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuildProjectAsync_ThrowsArgumentException_WhenConfigurationIsNullOrEmpty(string? configuration)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            installer.BuildProjectAsync("test.csproj", configuration!));
    }

    /// <summary>
    /// Verifies CopyApplicationFilesAsync throws when source directory is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CopyApplicationFilesAsync_ThrowsArgumentException_WhenSourceDirectoryIsNullOrEmpty(string? sourceDirectory)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            installer.CopyApplicationFilesAsync(sourceDirectory!, @"C:\target"));
    }

    /// <summary>
    /// Verifies CopyApplicationFilesAsync throws when target directory is empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CopyApplicationFilesAsync_ThrowsArgumentException_WhenTargetDirectoryIsNullOrEmpty(string? targetDirectory)
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            installer.CopyApplicationFilesAsync(@"C:\source", targetDirectory!));
    }

    /// <summary>
    /// Verifies CopyApplicationFilesAsync returns false when source directory doesn't exist.
    /// </summary>
    [Fact]
    public async Task CopyApplicationFilesAsync_ReturnsFalse_WhenSourceDirectoryDoesNotExist()
    {
        // Arrange
        var (installer, _, _, _) = CreateServiceInstaller();
        var sourcePath = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid());
        var targetPath = Path.Combine(Path.GetTempPath(), "Target_" + Guid.NewGuid());

        try
        {
            // Act
            var result = await installer.CopyApplicationFilesAsync(sourcePath, targetPath);

            // Assert
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(targetPath))
                Directory.Delete(targetPath, true);
        }
    }

    /// <summary>
    /// Verifies CopyApplicationFilesAsync succeeds with valid directories.
    /// </summary>
    [Fact]
    public async Task CopyApplicationFilesAsync_Succeeds_WithValidDirectories()
    {
        // Arrange
        var (installer, mockFileSystem, _, _) = CreateServiceInstaller();
        var sourcePath = "C:\\Source";
        var targetPath = "C:\\Target";

        // Setup mocks to simulate successful file copying
        mockFileSystem.Setup(fs => fs.DirectoryExists(It.Is<string>(s => s == sourcePath))).Returns(true);
        mockFileSystem.Setup(fs => fs.DirectoryExists(It.Is<string>(s => s != sourcePath))).Returns(false);
        mockFileSystem.Setup(fs => fs.CreateDirectory(It.IsAny<string>()));
        mockFileSystem.Setup(fs => fs.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            .Returns(new[] { Path.Combine(sourcePath, "test1.txt"), Path.Combine(sourcePath, "test2.txt") });
        mockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => Path.Combine(paths));
        mockFileSystem.Setup(fs => fs.GetDirectoryName(It.IsAny<string>())).Returns<string>(path => Path.GetDirectoryName(path));
        mockFileSystem.Setup(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await installer.CopyApplicationFilesAsync(sourcePath, targetPath);

        // Assert
        Assert.True(result);
        mockFileSystem.Verify(fs => fs.GetFiles(sourcePath, "*", SearchOption.AllDirectories), Times.Once);
        mockFileSystem.Verify(fs => fs.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

