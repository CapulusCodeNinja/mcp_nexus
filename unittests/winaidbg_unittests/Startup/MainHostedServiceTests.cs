using FluentAssertions;

using Moq;

using WinAiDbg.CommandLine;
using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.External.Apis.Security;
using WinAiDbg.External.Apis.ServiceManagement;
using WinAiDbg.Startup;

using Xunit;

namespace WinAiDbg.Unittests.Startup;

/// <summary>
/// Unit tests for the <see cref="MainHostedService"/> class.
/// </summary>
public class MainHostedServiceTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;
    private readonly Mock<IAdministratorChecker> m_AdminChecker;
    private readonly Mock<IServiceController> m_ServiceController;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainHostedServiceTests"/> class.
    /// </summary>
    public MainHostedServiceTests()
    {
        m_Settings = new Mock<ISettings>();
        m_FileSystem = new Mock<IFileSystem>();
        m_ProcessManager = new Mock<IProcessManager>();
        m_ServiceController = new Mock<IServiceController>();
        m_AdminChecker = new Mock<IAdministratorChecker>();

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
    }

    /// <summary>
    /// Verifies that constructor creates service successfully.
    /// </summary>
    [Fact]
    public void Constructor_WithValidContext_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());

        // Act
        var service = new MainHostedService(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_AdminChecker.Object, m_Settings.Object);

        // Assert
        _ = service.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that StopAsync completes successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        var service = new MainHostedService(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_AdminChecker.Object, m_Settings.Object);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that StopAsync handles cancellation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WithCancellation_Succeeds()
    {
        // Arrange
        var context = new CommandLineContext(Array.Empty<string>());
        var service = new MainHostedService(context, m_FileSystem.Object, m_ProcessManager.Object, m_ServiceController.Object, m_AdminChecker.Object, m_Settings.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await service.StopAsync(cts.Token);

        // Assert - No exception thrown
    }
}
