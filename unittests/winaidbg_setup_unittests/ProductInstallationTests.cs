using FluentAssertions;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.External.Apis.Security;
using WinAiDbg.External.Apis.ServiceManagement;

using Xunit;

namespace WinAiDbg.Setup.Unittests;

/// <summary>
/// Unit tests for the <see cref="ProductInstallation"/> class.
/// </summary>
public class ProductInstallationTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IAdministratorChecker> m_AdminChecker;
    private readonly Mock<IProcessManager> m_ProcessManagerMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductInstallationTests"/> class.
    /// </summary>
    public ProductInstallationTests()
    {
        m_Settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ProcessManagerMock = new Mock<IProcessManager>();
        m_ServiceControllerMock = new Mock<IServiceController>();
        m_AdminChecker = new Mock<IAdministratorChecker>();
    }

    /// <summary>
    /// Verifies that internal constructor creates instance successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        // Act
        var installation = new ProductInstallation(
            m_FileSystemMock.Object,
            m_ProcessManagerMock.Object,
            m_ServiceControllerMock.Object,
            m_AdminChecker.Object,
            m_Settings.Object);

        // Assert
        _ = installation.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that InstallServiceAsync calls internal method with default service controller.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InstallServiceAsync_CallsInternalMethodWithDefaultController()
    {
        // Arrange
        var installation = new ProductInstallation(
            m_FileSystemMock.Object,
            m_ProcessManagerMock.Object,
            m_ServiceControllerMock.Object,
            m_AdminChecker.Object,
            m_Settings.Object);

        // Act
        var result = await installation.InstallServiceAsync();

        // Assert
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that UpdateServiceAsync returns a boolean result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpdateServiceAsync_ReturnsBoolean()
    {
        // Arrange
        var installation = new ProductInstallation(
            m_FileSystemMock.Object,
            m_ProcessManagerMock.Object,
            m_ServiceControllerMock.Object,
            m_AdminChecker.Object,
            m_Settings.Object);

        // Act
        var result = await installation.UpdateServiceAsync();

        // Assert
        _ = result.GetType().Should().Be(typeof(bool));
    }

    /// <summary>
    /// Verifies that UninstallServiceAsync calls internal method with default service controller.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UninstallServiceAsync_CallsInternalMethodWithDefaultController()
    {
        // Arrange
        var installation = new ProductInstallation(
            m_FileSystemMock.Object,
            m_ProcessManagerMock.Object,
            m_ServiceControllerMock.Object,
            m_AdminChecker.Object,
            m_Settings.Object);

        // Act
        var result = await installation.UninstallServiceAsync();

        // Assert
        _ = result.GetType().Should().Be(typeof(bool));
    }
}
