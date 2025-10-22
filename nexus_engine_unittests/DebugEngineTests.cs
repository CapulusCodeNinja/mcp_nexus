using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using Xunit;
using nexus.engine;
using nexus.engine.Configuration;
using nexus.engine.Events;
using nexus.engine.Internal;
using nexus.engine.Models;
using nexus.utilities.FileSystem;
using nexus.utilities.ProcessManagement;

namespace nexus.engine.unittests;

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
    public async Task CreateSessionAsync_WithValidDumpFile_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dumpFilePath = @"C:\Test\test.dmp";
        var symbolPath = @"C:\Symbols";

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(dumpFilePath, symbolPath);
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task CloseSessionAsync_WithValidSessionId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task CloseSessionAsync_WithInvalidSessionId_ShouldCompleteWithoutError()
    {
        // Act & Assert
        var action = async () => await m_Engine.CloseSessionAsync("invalid-session");
        await action.Should().NotThrowAsync();
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
    public async Task EnqueueCommand_WithValidParameters_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = "lm";

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task EnqueueCommand_WithNullCommand_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
    }

    [Fact]
    public void EnqueueCommand_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("invalid-session", "lm");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session invalid-session not found");
    }

    [Fact]
    public void EnqueueCommand_WithNullCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("session-1", null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Command cannot be null or empty (Parameter 'command')")
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithEmptyCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("session-1", "");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Command cannot be null or empty (Parameter 'command')")
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("session-1", "   ");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Command cannot be null or empty (Parameter 'command')")
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithTabOnlyCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("session-1", "\t");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Command cannot be null or empty (Parameter 'command')")
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithNewlineOnlyCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("session-1", "\n");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Command cannot be null or empty (Parameter 'command')")
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithCarriageReturnOnlyCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("session-1", "\r");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Command cannot be null or empty (Parameter 'command')")
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithMixedWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_Engine.EnqueueCommand("session-1", " \t\n\r ");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Command cannot be null or empty (Parameter 'command')")
            .WithParameterName("command");
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
    public void GetAllCommandInfos_WithInvalidSessionId_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = m_Engine.GetAllCommandInfos("invalid-session");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
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
    public void GetAllCommandInfos_WithNullSessionId_ShouldThrowArgumentException_New()
    {
        // Act & Assert
        var action = () => m_Engine.GetAllCommandInfos(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetAllCommandInfos_WithEmptySessionId_ShouldThrowArgumentException_New()
    {
        // Act & Assert
        var action = () => m_Engine.GetAllCommandInfos("");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetAllCommandInfos_WithWhitespaceSessionId_ShouldThrowArgumentException_New()
    {
        // Act & Assert
        var action = () => m_Engine.GetAllCommandInfos("   ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithValidCommandId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task GetCommandInfoAsync_WithNullCommandId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithInvalidSessionId_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = async () => await m_Engine.GetCommandInfoAsync("invalid-session", "cmd-123");
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetCommandInfoAsync_WithInvalidCommandId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task GetCommandInfo_WithValidCommandId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public void GetCommandInfo_WithInvalidSessionId_ShouldReturnNull()
    {
        // Act
        var result = m_Engine.GetCommandInfo("invalid-session", "cmd-123");

        // Assert
        result.Should().BeNull();
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
    public async Task GetAllCommandInfos_WithValidSessionId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public void GetAllCommandInfos_WithInvalidSessionId_ShouldReturnEmptyCollection()
    {
        // Act
        var result = m_Engine.GetAllCommandInfos("invalid-session");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllCommandInfos_WhenDisposed_ShouldThrowObjectDisposedException_New()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        var action = () => m_Engine.GetAllCommandInfos("test-session");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task CancelCommand_WithValidCommandId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task CancelCommandAsync_WithNullCommandId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
    }

    [Fact]
    public void CancelCommandAsync_WithInvalidSessionId_ShouldReturnFalse()
    {
        // Act
        var result = m_Engine.CancelCommand("invalid-session", "cmd-123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelCommandAsync_WithInvalidCommandId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task CancelAllCommandsAsync_WithValidSessionId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public void CancelAllCommandsAsync_WithInvalidSessionId_ShouldReturnZero()
    {
        // Act
        var result = m_Engine.CancelAllCommands("invalid-session");

        // Assert
        result.Should().Be(0);
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
    public async Task IsSessionActive_WithValidSessionId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task IsSessionActive_AfterClosingSession_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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
    public async Task GetSessionState_WithValidSessionId_ShouldThrowInvalidOperationException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("Dump file not found: C:\\Test\\test.dmp");
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


    [Fact]
    public void CancelCommand_WithInvalidSessionId_ShouldReturnFalse()
    {
        // Act
        var result = m_Engine.CancelCommand("invalid-session", "test-command-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CancelAllCommands_WithInvalidSessionId_ShouldReturnZero()
    {
        // Act
        var result = m_Engine.CancelAllCommands("invalid-session");

        // Assert
        result.Should().Be(0);
    }

    public void Dispose()
    {
        m_Engine?.Dispose();
    }

    private void SetupDefaultMocks()
    {
        // Setup file system mocks - return false for ALL file existence checks to prevent real system access
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(false);
        
        m_MockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
            .Returns<string[]>(paths => string.Join("\\", paths));

        // Setup ALL other file system methods to prevent real system access
        m_MockFileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
            .Returns("mocked content");
        
        m_MockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Verifiable();
        
        m_MockFileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
            .Verifiable();
        
        m_MockFileSystem.Setup(fs => fs.GetFileName(It.IsAny<string>()))
            .Returns<string>(path => System.IO.Path.GetFileName(path));
        
        m_MockFileSystem.Setup(fs => fs.GetDirectoryName(It.IsAny<string>()))
            .Returns<string>(path => System.IO.Path.GetDirectoryName(path));

        // Setup process manager mocks - return null to avoid process-related issues in tests
        m_MockProcessManager.Setup(pm => pm.StartProcess(It.IsAny<System.Diagnostics.ProcessStartInfo>()))
            .Returns((System.Diagnostics.Process)null!);
        
        m_MockProcessManager.Setup(pm => pm.KillProcess(It.IsAny<System.Diagnostics.Process>()))
            .Verifiable();
    }

    [Fact]
    public void TestThrowIfDisposed_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var testAccessor = new DebugEngineTestAccessor(
            m_LoggerFactory,
            m_Configuration,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        testAccessor.Dispose();

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void TestThrowIfDisposed_WhenNotDisposed_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugEngineTestAccessor(
            m_LoggerFactory,
            m_Configuration,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().NotThrow();
    }

    [Fact]
    public void TestOnSessionCommandStateChanged_ShouldRaiseCommandStateChangedEvent()
    {
        // Arrange
        var testAccessor = new DebugEngineTestAccessor(
            m_LoggerFactory,
            m_Configuration,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        CommandStateChangedEventArgs? eventArgs = null;
        testAccessor.CommandStateChanged += (sender, args) => eventArgs = args;

        var originalEventArgs = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Executing,
            Timestamp = DateTime.Now,
            Command = "test command"
        };

        // Act
        testAccessor.TestOnSessionCommandStateChanged(this, originalEventArgs);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs.Should().Be(originalEventArgs);
    }

    [Fact]
    public void TestOnSessionStateChanged_ShouldRaiseSessionStateChangedEvent()
    {
        // Arrange
        var testAccessor = new DebugEngineTestAccessor(
            m_LoggerFactory,
            m_Configuration,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);
        
        SessionStateChangedEventArgs? eventArgs = null;
        testAccessor.SessionStateChanged += (sender, args) => eventArgs = args;

        var originalEventArgs = new SessionStateChangedEventArgs
        {
            SessionId = "test-session",
            OldState = SessionState.Initializing,
            NewState = SessionState.Active,
            Timestamp = DateTime.Now
        };

        // Act
        testAccessor.TestOnSessionStateChanged(this, originalEventArgs);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs.Should().Be(originalEventArgs);
    }

    [Fact]
    public async Task CreateSessionAsync_WithMaxSessionsReached_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var limitedConfig = new DebugEngineConfiguration
        {
            MaxConcurrentSessions = 0, // Set to 0 to trigger max sessions error immediately
            DefaultCommandTimeout = TimeSpan.FromMinutes(5)
        };
        
        var limitedEngine = new DebugEngine(
            m_LoggerFactory,
            limitedConfig,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Mock file exists
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Act & Assert
        var action = () => limitedEngine.CreateSessionAsync(@"C:\Test\test.dmp");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Maximum number of concurrent sessions (0) reached");
    }

    [Fact]
    public async Task CreateSessionAsync_WithValidDumpFile_ShouldThrowInvalidOperationException_New()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp", @"C:\Symbols");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullSymbolPath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp", null);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task CreateSessionAsync_WithEmptySymbolPath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp", string.Empty);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task CreateSessionAsync_WithWhitespaceSymbolPath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp", "   ");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task CreateSessionAsync_WithVeryLongDumpFilePath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var longPath = @"C:\" + new string('a', 200) + @"\test.dmp";
        m_MockFileSystem.Setup(fs => fs.FileExists(longPath))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(longPath);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task CreateSessionAsync_WithSpecialCharactersInPath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var specialPath = @"C:\Test\test with spaces & symbols!@#.dmp";
        m_MockFileSystem.Setup(fs => fs.FileExists(specialPath))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(specialPath);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task CreateSessionAsync_WithUncPath_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var uncPath = @"\\server\share\test.dmp";
        m_MockFileSystem.Setup(fs => fs.FileExists(uncPath))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(uncPath);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB executable not found. Please install Windows SDK or specify CdbPath in configuration.");
    }

    [Fact]
    public async Task CreateSessionAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);
        
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test.dmp", null, cts.Token);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CreateSessionAsync_MultipleSessions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        m_MockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns(true);

        // Act & Assert
        var action = () => m_Engine.CreateSessionAsync(@"C:\Test\test1.dmp");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to start CDB process");
    }

    [Fact]
    public void GetSessionState_WithValidSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var sessionId = "test-session";

        // Act
        var state = m_Engine.GetSessionState(sessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithNullSessionId_ShouldThrowArgumentException_New()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.GetSessionState(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetSessionState_WithEmptySessionId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.GetSessionState(string.Empty);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetSessionState_WithWhitespaceSessionId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_Engine.GetSessionState("   ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void GetSessionState_WithVeryLongSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var longSessionId = new string('a', 1000);

        // Act
        var state = m_Engine.GetSessionState(longSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithSpecialCharactersSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var specialSessionId = "session-123!@#$%^&*()";

        // Act
        var state = m_Engine.GetSessionState(specialSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithUnicodeSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var unicodeSessionId = "сессия-тест";

        // Act
        var state = m_Engine.GetSessionState(unicodeSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithNumericSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var numericSessionId = "123456789";

        // Act
        var state = m_Engine.GetSessionState(numericSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithMixedCaseSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var mixedCaseSessionId = "Session-123-Test";

        // Act
        var state = m_Engine.GetSessionState(mixedCaseSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithHyphenatedSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var hyphenatedSessionId = "session-123-test";

        // Act
        var state = m_Engine.GetSessionState(hyphenatedSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithUnderscoreSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var underscoreSessionId = "session_123_test";

        // Act
        var state = m_Engine.GetSessionState(underscoreSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithDotSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var dotSessionId = "session.123.test";

        // Act
        var state = m_Engine.GetSessionState(dotSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithColonSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var colonSessionId = "session:123:test";

        // Act
        var state = m_Engine.GetSessionState(colonSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithSemicolonSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var semicolonSessionId = "session;123;test";

        // Act
        var state = m_Engine.GetSessionState(semicolonSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithCommaSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var commaSessionId = "session,123,test";

        // Act
        var state = m_Engine.GetSessionState(commaSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithSpaceSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var spaceSessionId = "session 123 test";

        // Act
        var state = m_Engine.GetSessionState(spaceSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithTabSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var tabSessionId = "session\t123\ttest";

        // Act
        var state = m_Engine.GetSessionState(tabSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithNewlineSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var newlineSessionId = "session\n123\ntest";

        // Act
        var state = m_Engine.GetSessionState(newlineSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithCarriageReturnSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var carriageReturnSessionId = "session\r123\rtest";

        // Act
        var state = m_Engine.GetSessionState(carriageReturnSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithMixedWhitespaceSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var mixedWhitespaceSessionId = "session \t\n\r 123 \t\n\r test";

        // Act
        var state = m_Engine.GetSessionState(mixedWhitespaceSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithBracketSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var bracketSessionId = "session[123]test";

        // Act
        var state = m_Engine.GetSessionState(bracketSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithParenthesisSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var parenthesisSessionId = "session(123)test";

        // Act
        var state = m_Engine.GetSessionState(parenthesisSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithBraceSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var braceSessionId = "session{123}test";

        // Act
        var state = m_Engine.GetSessionState(braceSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithAngleBracketSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var angleBracketSessionId = "session<123>test";

        // Act
        var state = m_Engine.GetSessionState(angleBracketSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithPipeSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var pipeSessionId = "session|123|test";

        // Act
        var state = m_Engine.GetSessionState(pipeSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithBackslashSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var backslashSessionId = "session\\123\\test";

        // Act
        var state = m_Engine.GetSessionState(backslashSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithForwardSlashSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var forwardSlashSessionId = "session/123/test";

        // Act
        var state = m_Engine.GetSessionState(forwardSlashSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithQuestionMarkSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var questionMarkSessionId = "session?123?test";

        // Act
        var state = m_Engine.GetSessionState(questionMarkSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithExclamationMarkSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var exclamationMarkSessionId = "session!123!test";

        // Act
        var state = m_Engine.GetSessionState(exclamationMarkSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithAtSignSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var atSignSessionId = "session@123@test";

        // Act
        var state = m_Engine.GetSessionState(atSignSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithHashSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var hashSessionId = "session#123#test";

        // Act
        var state = m_Engine.GetSessionState(hashSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithDollarSignSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var dollarSignSessionId = "session$123$test";

        // Act
        var state = m_Engine.GetSessionState(dollarSignSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithPercentSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var percentSessionId = "session%123%test";

        // Act
        var state = m_Engine.GetSessionState(percentSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithAmpersandSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var ampersandSessionId = "session&123&test";

        // Act
        var state = m_Engine.GetSessionState(ampersandSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithAsteriskSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var asteriskSessionId = "session*123*test";

        // Act
        var state = m_Engine.GetSessionState(asteriskSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithPlusSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var plusSessionId = "session+123+test";

        // Act
        var state = m_Engine.GetSessionState(plusSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithEqualSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var equalSessionId = "session=123=test";

        // Act
        var state = m_Engine.GetSessionState(equalSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithTildeSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var tildeSessionId = "session~123~test";

        // Act
        var state = m_Engine.GetSessionState(tildeSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetSessionState_WithBacktickSessionId_ShouldReturnCorrectState()
    {
        // Arrange
        var backtickSessionId = "session`123`test";

        // Act
        var state = m_Engine.GetSessionState(backtickSessionId);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void Dispose_WhenNotDisposed_ShouldSetDisposedFlag()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act
        engine.Dispose();

        // Assert
        var disposedField = typeof(DebugEngine).GetField("m_Disposed", BindingFlags.NonPublic | BindingFlags.Instance);
        var isDisposed = (bool)disposedField!.GetValue(engine)!;
        isDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenAlreadyDisposed_ShouldNotThrow()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);
        engine.Dispose();

        // Act & Assert
        var action = () => engine.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow_New()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        var action = () =>
        {
            engine.Dispose();
            engine.Dispose();
            engine.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenNoSessions_ShouldCompleteSuccessfully()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        var action = () => engine.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void EnqueueCommand_WithValidSessionAndCommand_ShouldReturnCommandId_New()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);
        var sessionId = "test-session";
        var command = "lm";

        // Act & Assert
        var action = () => engine.EnqueueCommand(sessionId, command);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session not found");
    }

    [Fact]
    public void EnqueueCommand_WithInactiveSession_ShouldThrowInvalidOperationException_New()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);
        var sessionId = "test-session";
        var command = "lm";

        // Act & Assert
        var action = () => engine.EnqueueCommand(sessionId, command);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session not found");
    }

    [Fact]
    public void EnqueueCommand_WhenDisposed_ShouldThrowObjectDisposedException_New()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);
        engine.Dispose();

        // Act & Assert
        var action = () => engine.EnqueueCommand("test-session", "lm");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void EnqueueCommand_WithNullSessionId_ShouldThrowArgumentException_New()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        var action = () => engine.EnqueueCommand(null!, "lm");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Session ID cannot be null or empty (Parameter 'sessionId')");
    }

    [Fact]
    public void EnqueueCommand_WithEmptySessionId_ShouldThrowArgumentException_New()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        var action = () => engine.EnqueueCommand("", "lm");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Session ID cannot be null or empty (Parameter 'sessionId')");
    }

    [Fact]
    public void EnqueueCommand_WithWhitespaceSessionId_ShouldThrowArgumentException_New()
    {
        // Arrange
        var engine = new DebugEngine(m_LoggerFactory, m_Configuration, m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert
        var action = () => engine.EnqueueCommand("   ", "lm");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Session ID cannot be null or empty (Parameter 'sessionId')");
    }

}
