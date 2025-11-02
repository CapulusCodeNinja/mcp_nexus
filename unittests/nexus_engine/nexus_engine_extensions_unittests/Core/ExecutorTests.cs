using FluentAssertions;

using Moq;

using Nexus.Config;
using Nexus.Config.Models;
using Nexus.Engine.Extensions.Core;
using Nexus.Engine.Extensions.Security;
using Nexus.Engine.Share.Models;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Engine.Extensions.Tests.Core;

/// <summary>
/// Unit tests for the <see cref="Executor"/> class.
/// Tests extension execution, process management, and error handling.
/// </summary>
public class ExecutorTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IProcessManager> m_MockProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutorTests"/> class.
    /// </summary>
    public ExecutorTests()
    {
        m_Settings = new Mock<ISettings>();
        m_MockProcessManager = new Mock<IProcessManager>();

        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Server = new ServerSettings
                {
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
    /// Verifies that constructor with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Act
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);

        // Assert
        _ = executor.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that constructor with null manager throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullManager_ThrowsArgumentNullException()
    {
        // Arrange
        var tokenValidator = new TokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Executor(null!, tokenValidator, m_MockProcessManager.Object, m_Settings.Object));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that constructor with null tokenValidator throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTokenValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Executor(manager, null!, m_MockProcessManager.Object, m_Settings.Object));
    }

    /// <summary>
    /// Verifies that constructor with null processManager throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProcessManager_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new Executor(manager, tokenValidator, null!, m_Settings.Object));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync with non-existent extension returns failed CommandInfo.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNonExistentExtension_ReturnsFailedCommandInfo()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);

        // Act
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.State.Should().Be(CommandState.Failed);
        _ = result.ErrorMessage.Should().Contain("not found");
        _ = result.Command.Should().Contain("NonExistentExtension");
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with valid URL succeeds.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithValidUrl_Succeeds()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);
        var callbackUrl = "http://127.0.0.1:9001/extension-callback";

        // Act
        executor.UpdateCallbackUrl(callbackUrl);

        // Assert - Should not throw
        _ = executor.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with null URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithNullUrl_ThrowsArgumentException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => executor.UpdateCallbackUrl(null!));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with empty URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => executor.UpdateCallbackUrl(string.Empty));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl with whitespace URL throws ArgumentException.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_WithWhitespaceUrl_ThrowsArgumentException()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => executor.UpdateCallbackUrl("   "));
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that UpdateCallbackUrl can be called multiple times.
    /// </summary>
    [Fact]
    public void UpdateCallbackUrl_CanBeCalledMultipleTimes()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);

        // Act
        executor.UpdateCallbackUrl("http://127.0.0.1:9001/extension-callback");
        executor.UpdateCallbackUrl("http://127.0.0.1:9002/extension-callback");
        executor.UpdateCallbackUrl("http://127.0.0.1:9003/extension-callback");

        // Assert - Should not throw
        _ = executor.Should().NotBeNull();
        tokenValidator.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteAsync returns CommandInfo with correct command text.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExecuteAsync_ReturnsCommandInfoWithCommandText()
    {
        // Arrange
        var manager = new Manager(new Mock<IFileSystem>().Object, m_Settings.Object);
        var tokenValidator = new TokenValidator();
        var executor = new Executor(manager, tokenValidator, m_MockProcessManager.Object, m_Settings.Object);

        // Act
        var result = await executor.ExecuteAsync("NonExistentExtension", "session-123", null, "cmd-456");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Command.Should().Contain("NonExistentExtension");
        _ = result.SessionId.Should().Be("session-123");
        _ = result.CommandId.Should().Be("cmd-456");
        tokenValidator.Dispose();
    }
}
