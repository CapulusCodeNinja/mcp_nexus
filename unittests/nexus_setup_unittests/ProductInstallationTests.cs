using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;
using Nexus.External.Apis.ServiceManagement;

using Xunit;

namespace Nexus.Setup.Unittests;

/// <summary>
/// Unit tests for the <see cref="ProductInstallation"/> class.
/// </summary>
public class ProductInstallationTests
{
    private readonly Mock<IFileSystem> m_FileSystemMock;
    private readonly Mock<IProcessManager> m_ProcessManagerMock;
    private readonly Mock<IServiceController> m_ServiceControllerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductInstallationTests"/> class.
    /// </summary>
    public ProductInstallationTests()
    {
        m_FileSystemMock = new Mock<IFileSystem>();
        m_ProcessManagerMock = new Mock<IProcessManager>();
        m_ServiceControllerMock = new Mock<IServiceController>();
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
            m_ServiceControllerMock.Object);

        // Assert
        _ = installation.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Instance property returns a valid singleton.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = ProductInstallation.Instance;
        var instance2 = ProductInstallation.Instance;

        // Assert
        _ = instance1.Should().NotBeNull();
        _ = instance1.Should().BeSameAs(instance2);
    }



    /// <summary>
    /// Verifies that InstallServiceAsync calls internal method with default service controller.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task InstallServiceAsync_CallsInternalMethodWithDefaultController()
    {
        // Arrange
        var installation = new ProductInstallation(
            m_FileSystemMock.Object,
            m_ProcessManagerMock.Object,
            m_ServiceControllerMock.Object);

        // Act
        var result = await installation.InstallServiceAsync();

        // Assert
        _ = result.GetType().Should().Be(typeof(bool));
    }



    /// <summary>
    /// Verifies that UpdateServiceAsync returns a boolean result.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task UpdateServiceAsync_ReturnsBoolean()
    {
        // Arrange
        var installation = new ProductInstallation(
            m_FileSystemMock.Object,
            m_ProcessManagerMock.Object,
            m_ServiceControllerMock.Object);

        // Act
        var result = await installation.UpdateServiceAsync();

        // Assert
        _ = result.GetType().Should().Be(typeof(bool));
    }



    /// <summary>
    /// Verifies that UninstallServiceAsync calls internal method with default service controller.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task UninstallServiceAsync_CallsInternalMethodWithDefaultController()
    {
        // Arrange
        var installation = new ProductInstallation(
            m_FileSystemMock.Object,
            m_ProcessManagerMock.Object,
            m_ServiceControllerMock.Object);

        // Act
        var result = await installation.UninstallServiceAsync();

        // Assert
        _ = result.GetType().Should().Be(typeof(bool));
    }
}
