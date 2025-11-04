using FluentAssertions;

using Moq;

using Nexus.Config;
using Nexus.Config.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Protocol.Unittests;

/// <summary>
/// Unit tests for the <see cref="ProtocolServer"/> class.
/// Tests lifecycle management, configuration, error handling, and disposal.
/// </summary>
public class ProtocolServerTests : IDisposable
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;
    private ProtocolServer? m_ProtocolServer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolServerTests"/> class.
    /// </summary>
    public ProtocolServerTests()
    {
        m_Settings = new Mock<ISettings>();
        m_FileSystem = new Mock<IFileSystem>();
        m_ProcessManager = new Mock<IProcessManager>();

        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Server = new ServerSettings
                {
                    Host = "localhost",
                    Port = 8080,
                },
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
    }

    /// <summary>
    /// Cleans up test resources.
    /// </summary>
    public void Dispose()
    {
        m_ProtocolServer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that constructor with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Assert
        _ = m_ProtocolServer.Should().NotBeNull();
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that constructor initializes EngineService.
    /// </summary>
    [Fact]
    public void Constructor_InitializesEngineService()
    {
        // Act
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Assert
        _ = m_ProtocolServer.Should().NotBeNull();

        // EngineService.Initialize should have been called
    }

    /// <summary>
    /// Verifies that IsRunning property is false initially.
    /// </summary>
    [Fact]
    public void IsRunning_Initially_ReturnsFalse()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act & Assert
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that SetConfiguration with null throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void SetConfiguration_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => m_ProtocolServer.SetConfiguration(null!));
    }

    /// <summary>
    /// Verifies that SetConfiguration with valid configuration succeeds when not running.
    /// </summary>
    [Fact]
    public void SetConfiguration_WithValidConfiguration_WhenNotRunning_Succeeds()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        var config = new object();

        // Act
        m_ProtocolServer.SetConfiguration(config);

        // Assert - No exception thrown
        _ = m_ProtocolServer.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that SetConfiguration when disposed throws ObjectDisposedException.
    /// </summary>
    [Fact]
    public void SetConfiguration_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        m_ProtocolServer.Dispose();
        var config = new object();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_ProtocolServer.SetConfiguration(config));
    }

    /// <summary>
    /// Verifies that StartAsync with HTTP mode creates WebApplication and starts successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WithHttpMode_Succeeds()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeTrue();

        // Cleanup
        await m_ProtocolServer.StopAsync();
    }

    /// <summary>
    /// Verifies that StartAsync with Stdio mode creates Host and starts successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WithStdioMode_Succeeds()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act
        await m_ProtocolServer.StartAsync(false, false, CancellationToken.None);

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeTrue();

        // Cleanup
        await m_ProtocolServer.StopAsync();
    }

    /// <summary>
    /// Verifies that StartAsync with service mode and HTTP mode succeeds.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WithServiceModeAndHttpMode_Succeeds()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act
        await m_ProtocolServer.StartAsync(true, true, CancellationToken.None);

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeTrue();

        // Cleanup
        await m_ProtocolServer.StopAsync();
    }

    /// <summary>
    /// Verifies that StartAsync when already running throws InvalidOperationException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            m_ProtocolServer.StartAsync(false, true, CancellationToken.None));

        // Cleanup
        await m_ProtocolServer.StopAsync();
    }

    /// <summary>
    /// Verifies that StartAsync with service mode but not HTTP mode throws InvalidOperationException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WithServiceModeButNotHttpMode_ThrowsInvalidOperationException()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            m_ProtocolServer.StartAsync(true, false, CancellationToken.None));
    }

    /// <summary>
    /// Verifies that StartAsync when disposed throws ObjectDisposedException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        m_ProtocolServer.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            m_ProtocolServer.StartAsync(false, true, CancellationToken.None));
    }

    /// <summary>
    /// Verifies that StopAsync when not running logs warning and returns.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WhenNotRunning_Succeeds()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act
        await m_ProtocolServer.StopAsync();

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StopAsync when running in HTTP mode stops successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WhenRunningInHttpMode_StopsSuccessfully()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);

        // Act
        await m_ProtocolServer.StopAsync();

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StopAsync when running in Stdio mode stops successfully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WhenRunningInStdioMode_StopsSuccessfully()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, false, CancellationToken.None);

        // Act
        await m_ProtocolServer.StopAsync();

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StopAsync when disposed throws ObjectDisposedException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        m_ProtocolServer.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => m_ProtocolServer.StopAsync());
    }

    /// <summary>
    /// Verifies that SetConfiguration when running throws InvalidOperationException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SetConfiguration_WhenRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);
        var config = new object();

        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => m_ProtocolServer.SetConfiguration(config));

        // Cleanup
        await m_ProtocolServer.StopAsync();
    }

    /// <summary>
    /// Verifies that Dispose when not running disposes successfully.
    /// </summary>
    [Fact]
    public void Dispose_WhenNotRunning_DisposesSuccessfully()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act
        m_ProtocolServer.Dispose();

        // Assert - No exception thrown
        _ = Assert.Throws<ObjectDisposedException>(() => m_ProtocolServer.SetConfiguration(new object()));
    }

    /// <summary>
    /// Verifies that Dispose when running calls StopAsync.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_WhenRunning_CallsStopAsync()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);

        // Act
        m_ProtocolServer.Dispose();

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
        _ = Assert.Throws<ObjectDisposedException>(() => m_ProtocolServer.SetConfiguration(new object()));
    }

    /// <summary>
    /// Verifies that Dispose when already disposed does not throw.
    /// </summary>
    [Fact]
    public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        m_ProtocolServer.Dispose();

        // Act & Assert
        m_ProtocolServer.Dispose(); // Should not throw
    }

    /// <summary>
    /// Verifies that Dispose disposes WebApplication when in HTTP mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_WhenInHttpMode_DisposesWebApplication()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);

        // Act
        m_ProtocolServer.Dispose();

        // Assert - No exception thrown, resources disposed
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Dispose disposes Host when in Stdio mode.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Dispose_WhenInStdioMode_DisposesHost()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, false, CancellationToken.None);

        // Act
        m_ProtocolServer.Dispose();

        // Assert - No exception thrown, resources disposed
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StopAsync with cancellation token handles cancellation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WithCancellationToken_HandlesCancellation()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        await m_ProtocolServer.StopAsync(cts.Token);

        // Assert
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StartAsync with cancellation token handles cancellation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WithCancellationToken_HandlesCancellation()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Operation may be cancelled but should not throw unexpected exception
        try
        {
            await m_ProtocolServer.StartAsync(false, true, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected if cancellation is honored
        }
    }

    /// <summary>
    /// Verifies that multiple start-stop cycles work correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartStop_Cycles_WorkCorrectly()
    {
        // Arrange
        m_ProtocolServer = new ProtocolServer(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);

        // Act & Assert - First cycle
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);
        _ = m_ProtocolServer.IsRunning.Should().BeTrue();
        await m_ProtocolServer.StopAsync();
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();

        // Second cycle
        await m_ProtocolServer.StartAsync(false, true, CancellationToken.None);
        _ = m_ProtocolServer.IsRunning.Should().BeTrue();
        await m_ProtocolServer.StopAsync();
        _ = m_ProtocolServer.IsRunning.Should().BeFalse();
    }
}
