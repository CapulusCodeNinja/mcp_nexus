using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using Xunit;
using mcp_nexus.Engine;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.Models;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;

namespace mcp_nexus.Engine.UnitTests;

/// <summary>
/// Unit tests for the DebugEngine class.
/// </summary>
public class DebugEngineTests : IDisposable
{
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly DebugEngine m_Engine;

    public DebugEngineTests()
    {
        m_LoggerFactory = NullLoggerFactory.Instance;

        m_Configuration = new DebugEngineConfiguration
        {
            MaxConcurrentSessions = 5,
            DefaultCommandTimeout = TimeSpan.FromMinutes(5),
            SessionInitializationTimeout = TimeSpan.FromMinutes(2)
        };

        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();

        // Setup default mock behaviors
        SetupDefaultMocks();

        m_Engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        m_Engine.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugEngine(null!, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("loggerFactory");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugEngine(m_LoggerFactory, null!, m_MockFileSystem.Object, m_MockProcessManager.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullFileSystem_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugEngine(m_LoggerFactory, m_Configuration, null!, m_MockProcessManager.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    [Fact]
    public void Constructor_WithNullProcessManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("processManager");
    }

    [Fact]
    public async Task Dispose_WhenCalled_ShouldDisposeEngine()
    {
        // Act
        m_Engine.Dispose();

        // Assert
        var action = async () => await m_Engine.CreateSessionAsync("test.dmp");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        var action = () =>
        {
            m_Engine.Dispose();
            m_Engine.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public async Task CreateSessionAsync_WithValidDumpFile_ShouldReturnSessionId()
    {
        // Arrange
        var dumpFilePath = @"C:\Test\test.dmp";
        var symbolPath = @"C:\Symbols";

        // Act
        var sessionId = await m_Engine.CreateSessionAsync(dumpFilePath, symbolPath);

        // Assert
        sessionId.Should().NotBeNull();
        sessionId.Should().NotBeEmpty();
        sessionId.Should().StartWith("sess-");
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullDumpFile_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await m_Engine.CreateSessionAsync(null!);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public async Task CreateSessionAsync_WithEmptyDumpFile_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await m_Engine.CreateSessionAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public async Task CreateSessionAsync_WithWhitespaceDumpFile_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await m_Engine.CreateSessionAsync("   ");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("dumpFilePath");
    }

    [Fact]
    public async Task CreateSessionAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = async () => await m_Engine.CreateSessionAsync("test.dmp");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CloseSessionAsync_WithValidSessionId_ShouldComplete()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act
        await m_Engine.CloseSessionAsync(sessionId);

        // Assert
        // Should complete without throwing
    }

    [Fact]
    public async Task CloseSessionAsync_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await m_Engine.CloseSessionAsync(null!);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public async Task CloseSessionAsync_WithEmptySessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await m_Engine.CloseSessionAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public async Task CloseSessionAsync_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = async () => await m_Engine.CloseSessionAsync("invalid-session");
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CloseSessionAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = async () => await m_Engine.CloseSessionAsync("test-session");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task EnqueueCommand_WithValidParameters_ShouldReturnCommandId()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        var command = "lm";

        // Act
        var commandId = m_Engine.EnqueueCommand(sessionId, command);

        // Assert
        commandId.Should().NotBeNull();
        commandId.Should().NotBeEmpty();
        commandId.Should().StartWith("cmd-");
    }

    [Fact]
    public void EnqueueCommand_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand(null!, "lm");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public async Task EnqueueCommand_WithNullCommand_ShouldThrowArgumentException()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act & Assert
        var action = () => m_Engine.EnqueueCommand(sessionId, null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("invalid-session", "lm");
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnqueueCommand_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("test-session", "lm");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithValidCommandId_ShouldReturnCommandInfo()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        var commandId = m_Engine.EnqueueCommand(sessionId, "lm");

        // Act
        var commandInfo = await m_Engine.GetCommandInfoAsync(sessionId, commandId);

        // Assert
        commandInfo.Should().NotBeNull();
        commandInfo.CommandId.Should().Be(commandId);
        commandInfo.Command.Should().Be("lm");
        commandInfo.State.Should().BeOneOf(CommandState.Queued, CommandState.Executing);
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await m_Engine.GetCommandInfoAsync(null!, "cmd-123");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithNullCommandId_ShouldThrowArgumentException()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act & Assert
        var action = async () => await m_Engine.GetCommandInfoAsync(sessionId, null!);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("commandId");
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = async () => await m_Engine.GetCommandInfoAsync("invalid-session", "cmd-123");
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithInvalidCommandId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act & Assert
        var action = async () => await m_Engine.GetCommandInfoAsync(sessionId, "invalid-command");
        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetCommandInfoAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = async () => await m_Engine.GetCommandInfoAsync("test-session", "cmd-123");
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetCommandInfo_WithValidCommandId_ShouldReturnCommandInfo()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        var commandId = m_Engine.EnqueueCommand(sessionId, "lm");

        // Act
        var commandInfo = m_Engine.GetCommandInfo(sessionId, commandId);

        // Assert
        commandInfo.Should().NotBeNull();
        commandInfo!.CommandId.Should().Be(commandId);
        commandInfo.Command.Should().Be("lm");
    }

    [Fact]
    public void GetCommandInfo_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.GetCommandInfo(null!, "cmd-123");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetCommandInfo_WithNullCommandId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.GetCommandInfo("session-123", null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("commandId");
    }

    [Fact]
    public void GetCommandInfo_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => m_Engine.GetCommandInfo("invalid-session", "cmd-123");
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetCommandInfo_WithInvalidCommandId_ShouldReturnNull()
    {
        // Arrange
        var sessionId = "session-123";

        // Act
        var commandInfo = m_Engine.GetCommandInfo(sessionId, "invalid-command");

        // Assert
        commandInfo.Should().BeNull();
    }

    [Fact]
    public void GetCommandInfo_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.GetCommandInfo("test-session", "cmd-123");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetAllCommandInfos_WithValidSessionId_ShouldReturnCommandInfos()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        m_Engine.EnqueueCommand(sessionId, "lm");
        m_Engine.EnqueueCommand(sessionId, "!threads");

        // Act
        var commandInfos = m_Engine.GetAllCommandInfos(sessionId);

        // Assert
        commandInfos.Should().NotBeNull();
        commandInfos.Should().HaveCount(2);
    }

    [Fact]
    public void GetAllCommandInfos_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.GetAllCommandInfos(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetAllCommandInfos_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => m_Engine.GetAllCommandInfos("invalid-session");
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetAllCommandInfos_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.GetAllCommandInfos("test-session");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task CancelCommand_WithValidCommandId_ShouldReturnTrue()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        var commandId = m_Engine.EnqueueCommand(sessionId, "lm");

        // Act
        var result = m_Engine.CancelCommand(sessionId, commandId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelCommandAsync_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.CancelCommand(null!, "cmd-123");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public async Task CancelCommandAsync_WithNullCommandId_ShouldThrowArgumentException()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act & Assert
        var action = () => m_Engine.CancelCommand(sessionId, null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("commandId");
    }

    [Fact]
    public async Task CancelCommandAsync_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => m_Engine.CancelCommand("invalid-session", "cmd-123");
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task CancelCommandAsync_WithInvalidCommandId_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act
        var result = m_Engine.CancelCommand(sessionId, "invalid-command");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelCommandAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.CancelCommand("test-session", "cmd-123");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task CancelAllCommandsAsync_WithValidSessionId_ShouldReturnCount()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        m_Engine.EnqueueCommand(sessionId, "lm");
        m_Engine.EnqueueCommand(sessionId, "!threads");

        // Act
        var count = m_Engine.CancelAllCommands(sessionId);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task CancelAllCommandsAsync_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.CancelAllCommands(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public async Task CancelAllCommandsAsync_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => m_Engine.CancelAllCommands("invalid-session");
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task CancelAllCommandsAsync_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.CancelAllCommands("test-session");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task IsSessionActive_WithValidSessionId_ShouldReturnTrue()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act
        var isActive = m_Engine.IsSessionActive(sessionId);

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsSessionActive_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.IsSessionActive(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void IsSessionActive_WithInvalidSessionId_ShouldReturnFalse()
    {
        // Act
        var isActive = m_Engine.IsSessionActive("invalid-session");

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task IsSessionActive_AfterClosingSession_ShouldReturnFalse()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await m_Engine.CloseSessionAsync(sessionId);

        // Act
        var isActive = m_Engine.IsSessionActive(sessionId);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsSessionActive_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.IsSessionActive("test-session");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task GetSessionState_WithValidSessionId_ShouldReturnSessionState()
    {
        // Arrange
        var sessionId = await m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");

        // Act
        var sessionState = m_Engine.GetSessionState(sessionId);

        // Assert
        sessionState.Should().NotBeNull();
        sessionState.Should().BeOneOf(SessionState.Initializing, SessionState.Active);
    }

    [Fact]
    public void GetSessionState_WithNullSessionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.GetSessionState(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetSessionState_WithInvalidSessionId_ShouldReturnNull()
    {
        // Act
        var sessionState = m_Engine.GetSessionState("invalid-session");

        // Assert
        sessionState.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.GetSessionState("test-session");
        action.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        m_Engine?.Dispose();
    }

    private void SetupDefaultMocks()
    {
        // Setup file system mocks
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);
        
        m_MockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("\\", paths));

        // Setup process manager mocks - create a mock Process that simulates a started process
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns(() => {
                var mockProcess = new Mock<System.Diagnostics.Process>();
                mockProcess.Setup(p => p.StandardInput).Returns(new Mock<System.IO.StreamWriter>().Object);
                mockProcess.Setup(p => p.StandardOutput).Returns(new Mock<System.IO.StreamReader>().Object);
                mockProcess.Setup(p => p.StandardError).Returns(new Mock<System.IO.StreamReader>().Object);
                mockProcess.Setup(p => p.Id).Returns(12345);
                mockProcess.Setup(p => p.HasExited).Returns(false);
                return mockProcess.Object;
            });
    }
}
