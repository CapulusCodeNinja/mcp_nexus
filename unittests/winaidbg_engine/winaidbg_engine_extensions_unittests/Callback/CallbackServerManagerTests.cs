using FluentAssertions;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.Engine.Extensions.Callback;
using WinAiDbg.Engine.Extensions.Security;
using WinAiDbg.Engine.Share;

using Xunit;

namespace WinAiDbg.Engine.Extensions.Unittests.Callback;

/// <summary>
/// Unit tests for the <see cref="CallbackServerManager"/> class.
/// Tests server lifecycle, port management, and error scenarios.
/// </summary>
public class CallbackServerManagerTests : IAsyncDisposable
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IDebugEngine> m_MockEngine;
    private CallbackServerManager? m_ServerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackServerManagerTests"/> class.
    /// </summary>
    public CallbackServerManagerTests()
    {
        m_Settings = new Mock<ISettings>();
        m_MockEngine = new Mock<IDebugEngine>();

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
    /// Cleans up test resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (m_ServerManager != null)
        {
            await m_ServerManager.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that constructor with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var tokenValidator = new TokenValidator();

        // Act
        var manager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);

        // Assert
        _ = manager.Should().NotBeNull();
        _ = manager.IsRunning.Should().BeFalse();
        _ = manager.Port.Should().Be(0);
        _ = manager.CallbackUrl.Should().BeEmpty();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that constructor with null engine throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullEngine_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new CallbackServerManager(null!, new TokenValidator(), m_Settings.Object));
    }

    /// <summary>
    /// Verifies that constructor with null tokenValidator throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTokenValidator_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new CallbackServerManager(m_MockEngine.Object, null!, m_Settings.Object));
    }

    /// <summary>
    /// Verifies that GetTokenValidator returns the token validator instance.
    /// </summary>
    [Fact]
    public void GetTokenValidator_ReturnsTokenValidator()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        var manager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);

        // Act
        var returnedValidator = manager.GetTokenValidator();

        // Assert
        _ = returnedValidator.Should().BeSameAs(tokenValidator);
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that StartAsync starts the server and sets IsRunning to true.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_StartsServerAndSetsIsRunning()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        m_ServerManager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);

        // Act
        await m_ServerManager.StartAsync();

        // Assert
        _ = m_ServerManager.IsRunning.Should().BeTrue();
        _ = m_ServerManager.Port.Should().BeGreaterThan(0);
        _ = m_ServerManager.CallbackUrl.Should().NotBeEmpty();
        _ = m_ServerManager.CallbackUrl.Should().StartWith("http://127.0.0.1:");
        _ = m_ServerManager.CallbackUrl.Should().EndWith("/extension-callback");
    }

    /// <summary>
    /// Verifies that StartAsync when already running does not throw.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WhenAlreadyRunning_DoesNotThrow()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        m_ServerManager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);
        await m_ServerManager.StartAsync();

        // Act
        await m_ServerManager.StartAsync(); // Should not throw

        // Assert
        _ = m_ServerManager.IsRunning.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that StopAsync stops the server and sets IsRunning to false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_StopsServerAndSetsIsRunningToFalse()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        m_ServerManager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);
        await m_ServerManager.StartAsync();

        // Act
        await m_ServerManager.StopAsync();

        // Assert
        _ = m_ServerManager.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StopAsync when not running does not throw.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNotThrow()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        m_ServerManager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);

        // Act
        await m_ServerManager.StopAsync(); // Should not throw

        // Assert
        _ = m_ServerManager.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StartAsync and StopAsync can be called multiple times.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsyncAndStopAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        m_ServerManager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);

        // Act & Assert
        await m_ServerManager.StartAsync();
        _ = m_ServerManager.IsRunning.Should().BeTrue();

        await m_ServerManager.StopAsync();
        _ = m_ServerManager.IsRunning.Should().BeFalse();

        await m_ServerManager.StartAsync();
        _ = m_ServerManager.IsRunning.Should().BeTrue();

        await m_ServerManager.StopAsync();
        _ = m_ServerManager.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that DisposeAsync disposes resources and sets IsRunning to false.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DisposeAsync_DisposesResources()
    {
        // Arrange
        var tokenValidator = new TokenValidator();
        m_ServerManager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);
        await m_ServerManager.StartAsync();

        // Act
        await m_ServerManager.DisposeAsync();

        // Assert
        _ = m_ServerManager.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that StartAsync with configured port uses that port.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WithConfiguredPort_UsesConfiguredPort()
    {
        // Arrange
        const int configuredPort = 9001;
        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = configuredPort,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
        var tokenValidator = new TokenValidator();
        m_ServerManager = new CallbackServerManager(m_MockEngine.Object, tokenValidator, m_Settings.Object);

        // Act
        await m_ServerManager.StartAsync();

        // Assert
        _ = m_ServerManager.Port.Should().Be(configuredPort);
        _ = m_ServerManager.CallbackUrl.Should().Contain($":{configuredPort}");
    }
}
