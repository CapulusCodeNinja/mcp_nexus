using FluentAssertions;

using Moq;

using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Engine.Unittests;

/// <summary>
/// Unit tests for DebugEngine class.
/// Tests engine coordination, session management, and command handling with mocked dependencies.
/// </summary>
public class DebugEngineTests : IDisposable
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly DebugEngine m_Engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEngineTests"/> class.
    /// </summary>
    public DebugEngineTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();
        m_Engine = new DebugEngine(m_MockFileSystem.Object, m_MockProcessManager.Object);
    }

    /// <summary>
    /// Cleans up test resources.
    /// </summary>
    public void Dispose()
    {
        m_Engine?.Dispose();
        GC.SuppressFinalize(this);
    }


    /// <summary>
    /// Verifies that constructor throws when fileSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new DebugEngine(null!, m_MockProcessManager.Object));
    }

    /// <summary>
    /// Verifies that constructor throws when processManager is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProcessManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new DebugEngine(m_MockFileSystem.Object, null!));
    }

    /// <summary>
    /// Verifies that constructor initializes with valid dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        // Arrange & Act
        using var engine = new DebugEngine(m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Assert
        _ = engine.Should().NotBeNull();
    }



    /// <summary>
    /// Verifies that idle cleanup closes a session that has exceeded the inactivity timeout and has no active commands.
    /// </summary>
    [Fact]
    public void CleanupIdleSessions_WhenSessionIdleAndNoActiveCommands_ClosesSession()
    {
        // Arrange
        using var accessor = new DebugEngineTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Create a debug session without initializing CDB (so no process is started)
        var sessionId = "test-session-id";
        var fileSystem = m_MockFileSystem.Object;
        var processManager = m_MockProcessManager.Object;
        var debugSession = new Nexus.Engine.Unittests.Internal.DebugSessionTestAccessor(
            sessionId,
            @"C:\\dummy\\dump.dmp",
            null,
            fileSystem,
            processManager);

        // Inject the session into engine's session dictionary via reflection
        var sessionsField = typeof(DebugEngine)
            .GetField("m_Sessions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _ = sessionsField.Should().NotBeNull();
        var sessions = (System.Collections.Concurrent.ConcurrentDictionary<string, Nexus.Engine.Internal.DebugSession>)sessionsField!.GetValue(accessor)!;
        _ = sessions.TryAdd(sessionId, debugSession);

        // Backdate last activity to force timeout
        var ticksField = typeof(Nexus.Engine.Internal.DebugSession)
            .GetField("m_LastActivityTicks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _ = ticksField.Should().NotBeNull();
        var oldTicks = DateTime.Now.AddHours(-1).Ticks;
        ticksField!.SetValue(debugSession, oldTicks);

        // Act: run cleanup
        accessor.InvokeCleanupIdleSessions();

        // Assert: session should be closed and no longer active
        var isActive = accessor.IsSessionActive(sessionId);
        _ = isActive.Should().BeFalse();
    }



    /// <summary>
    /// Verifies that CreateSessionAsync throws when dumpFilePath is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CreateSessionAsync_WithNullDumpFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => m_Engine.CreateSessionAsync(null!));
    }

    /// <summary>
    /// Verifies that CreateSessionAsync throws when dumpFilePath is empty.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CreateSessionAsync_WithEmptyDumpFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => m_Engine.CreateSessionAsync(string.Empty));
    }

    /// <summary>
    /// Verifies that CreateSessionAsync throws when dumpFilePath is whitespace.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CreateSessionAsync_WithWhitespaceDumpFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => m_Engine.CreateSessionAsync("   "));
    }

    /// <summary>
    /// Verifies that CreateSessionAsync throws when dump file does not exist.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CreateSessionAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        const string dumpFilePath = @"C:\test\dump.dmp";
        _ = m_MockFileSystem.Setup(fs => fs.FileExists(dumpFilePath)).Returns(false);

        // Act & Assert
        _ = await Assert.ThrowsAsync<FileNotFoundException>(() => m_Engine.CreateSessionAsync(dumpFilePath));
    }

    /// <summary>
    /// Verifies that CreateSessionAsync throws when disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CreateSessionAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => m_Engine.CreateSessionAsync(@"C:\test\dump.dmp"));
    }



    /// <summary>
    /// Verifies that CloseSessionAsync throws when sessionId is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CloseSessionAsync_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => m_Engine.CloseSessionAsync(null!));
    }

    /// <summary>
    /// Verifies that CloseSessionAsync throws when sessionId is empty.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CloseSessionAsync_WithEmptySessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => m_Engine.CloseSessionAsync(string.Empty));
    }

    /// <summary>
    /// Verifies that CloseSessionAsync succeeds when session not found.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CloseSessionAsync_WithNonExistentSession_Succeeds()
    {
        // Act
        await m_Engine.CloseSessionAsync("non-existent-session");

        // Assert - Should not throw
    }

    /// <summary>
    /// Verifies that CloseSessionAsync throws when disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task CloseSessionAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => m_Engine.CloseSessionAsync("test-session"));
    }



    /// <summary>
    /// Verifies that IsSessionActive throws when sessionId is null.
    /// </summary>
    [Fact]
    public void IsSessionActive_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.IsSessionActive(null!));
    }

    /// <summary>
    /// Verifies that IsSessionActive throws when sessionId is empty.
    /// </summary>
    [Fact]
    public void IsSessionActive_WithEmptySessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.IsSessionActive(string.Empty));
    }

    /// <summary>
    /// Verifies that IsSessionActive returns false for non-existent session.
    /// </summary>
    [Fact]
    public void IsSessionActive_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = m_Engine.IsSessionActive("non-existent-session");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsSessionActive throws when disposed.
    /// </summary>
    [Fact]
    public void IsSessionActive_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.IsSessionActive("test-session"));
    }



    /// <summary>
    /// Verifies that EnqueueCommand throws when sessionId is null.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.EnqueueCommand(null!, "!analyze"));
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws when command is null.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNullCommand_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.EnqueueCommand("test-session", null!));
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws when command is empty.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithEmptyCommand_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.EnqueueCommand("test-session", string.Empty));
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws when session not found.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNonExistentSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => m_Engine.EnqueueCommand("non-existent-session", "!analyze"));
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws when disposed.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.EnqueueCommand("test-session", "!analyze"));
    }



    /// <summary>
    /// Verifies that EnqueueExtensionScriptAsync throws when sessionId is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task EnqueueExtensionScriptAsync_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () => await m_Engine.EnqueueExtensionScriptAsync(null!, "test-extension"));
    }

    /// <summary>
    /// Verifies that EnqueueExtensionScriptAsync throws when extensionName is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task EnqueueExtensionScriptAsync_WithNullExtensionName_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () => await m_Engine.EnqueueExtensionScriptAsync("test-session", null!));
    }

    /// <summary>
    /// Verifies that EnqueueExtensionScriptAsync throws when extensionName is empty.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task EnqueueExtensionScriptAsync_WithEmptyExtensionName_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () => await m_Engine.EnqueueExtensionScriptAsync("test-session", string.Empty));
    }

    /// <summary>
    /// Verifies that EnqueueExtensionScriptAsync throws when session not found.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task EnqueueExtensionScriptAsync_WithNonExistentSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(async () => await m_Engine.EnqueueExtensionScriptAsync("non-existent-session", "test-extension"));
    }

    /// <summary>
    /// Verifies that EnqueueExtensionScriptAsync throws when disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task EnqueueExtensionScriptAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await m_Engine.EnqueueExtensionScriptAsync("test-session", "test-extension"));
    }



    /// <summary>
    /// Verifies that GetCommandInfoAsync throws when sessionId is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task GetCommandInfoAsync_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => m_Engine.GetCommandInfoAsync(null!, "cmd-123"));
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws when commandId is null.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task GetCommandInfoAsync_WithNullCommandId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() => m_Engine.GetCommandInfoAsync("test-session", null!));
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws when session not found.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task GetCommandInfoAsync_WithNonExistentSession_ThrowsInvalidOperationException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => m_Engine.GetCommandInfoAsync("non-existent-session", "cmd-123"));
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws when disposed.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task GetCommandInfoAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => m_Engine.GetCommandInfoAsync("test-session", "cmd-123"));
    }



    /// <summary>
    /// Verifies that GetCommandInfo throws when sessionId is null.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.GetCommandInfo(null!, "cmd-123"));
    }

    /// <summary>
    /// Verifies that GetCommandInfo throws when commandId is null.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithNullCommandId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.GetCommandInfo("test-session", null!));
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null for non-existent session.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithNonExistentSession_ReturnsNull()
    {
        // Act
        var result = m_Engine.GetCommandInfo("non-existent-session", "cmd-123");

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetCommandInfo throws when disposed.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.GetCommandInfo("test-session", "cmd-123"));
    }



    /// <summary>
    /// Verifies that GetAllCommandInfos throws when sessionId is null.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.GetAllCommandInfos(null!));
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns empty dictionary for non-existent session.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WithNonExistentSession_ReturnsEmptyDictionary()
    {
        // Act
        var result = m_Engine.GetAllCommandInfos("non-existent-session");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos throws when disposed.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.GetAllCommandInfos("test-session"));
    }



    /// <summary>
    /// Verifies that CancelCommand throws when sessionId is null.
    /// </summary>
    [Fact]
    public void CancelCommand_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.CancelCommand(null!, "cmd-123"));
    }

    /// <summary>
    /// Verifies that CancelCommand throws when commandId is null.
    /// </summary>
    [Fact]
    public void CancelCommand_WithNullCommandId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.CancelCommand("test-session", null!));
    }

    /// <summary>
    /// Verifies that CancelCommand returns false for non-existent session.
    /// </summary>
    [Fact]
    public void CancelCommand_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = m_Engine.CancelCommand("non-existent-session", "cmd-123");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelCommand throws when disposed.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.CancelCommand("test-session", "cmd-123"));
    }



    /// <summary>
    /// Verifies that CancelAllCommands throws when sessionId is null.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.CancelAllCommands(null!));
    }

    /// <summary>
    /// Verifies that CancelAllCommands returns zero for non-existent session.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WithNonExistentSession_ReturnsZero()
    {
        // Act
        var result = m_Engine.CancelAllCommands("non-existent-session");

        // Assert
        _ = result.Should().Be(0);
    }

    /// <summary>
    /// Verifies that CancelAllCommands throws when disposed.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.CancelAllCommands("test-session"));
    }



    /// <summary>
    /// Verifies that GetSessionState throws when sessionId is null.
    /// </summary>
    [Fact]
    public void GetSessionState_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Engine.GetSessionState(null!));
    }

    /// <summary>
    /// Verifies that GetSessionState returns null for non-existent session.
    /// </summary>
    [Fact]
    public void GetSessionState_WithNonExistentSession_ReturnsNull()
    {
        // Act
        var result = m_Engine.GetSessionState("non-existent-session");

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetSessionState throws when disposed.
    /// </summary>
    [Fact]
    public void GetSessionState_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Engine.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.GetSessionState("test-session"));
    }



    /// <summary>
    /// Verifies that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act
        m_Engine.Dispose();
        m_Engine.Dispose();

        // Assert - Should not throw
    }

    /// <summary>
    /// Verifies that Dispose marks engine as disposed.
    /// </summary>
    [Fact]
    public void Dispose_MarksEngineAsDisposed()
    {
        // Act
        m_Engine.Dispose();

        // Assert - All operations should throw ObjectDisposedException
        _ = Assert.Throws<ObjectDisposedException>(() => m_Engine.IsSessionActive("test"));
    }



    /// <summary>
    /// Verifies that CommandStateChanged event can be subscribed to.
    /// </summary>
    [Fact]
    public void CommandStateChanged_CanSubscribe()
    {
        // Arrange
        var eventRaised = false;
        m_Engine.CommandStateChanged += (sender, args) => eventRaised = true;

        // Assert
        _ = eventRaised.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that SessionStateChanged event can be subscribed to.
    /// </summary>
    [Fact]
    public void SessionStateChanged_CanSubscribe()
    {
        // Arrange
        var eventRaised = false;
        m_Engine.SessionStateChanged += (sender, args) => eventRaised = true;

        // Assert
        _ = eventRaised.Should().BeFalse();
    }



    /// <summary>
    /// Verifies that Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void Instance_ReturnsNonNull()
    {
        // Act
        var instance = DebugEngine.Instance;

        // Assert
        _ = instance.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that Instance returns same instance on multiple calls.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Act
        var instance1 = DebugEngine.Instance;
        var instance2 = DebugEngine.Instance;

        // Assert
        _ = instance1.Should().BeSameAs(instance2);
    }



    /// <summary>
    /// Verifies that ValidateSessionId throws when sessionId is null.
    /// </summary>
    [Fact]
    public void ValidateSessionId_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateSessionId(null!, "sessionId"));

        _ = exception.ParamName.Should().Be("sessionId");
        _ = exception.Message.Should().Contain("Session ID cannot be null or empty");
    }

    /// <summary>
    /// Verifies that ValidateSessionId throws when sessionId is empty.
    /// </summary>
    [Fact]
    public void ValidateSessionId_WithEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateSessionId(string.Empty, "sessionId"));

        _ = exception.ParamName.Should().Be("sessionId");
    }

    /// <summary>
    /// Verifies that ValidateSessionId throws when sessionId is whitespace.
    /// </summary>
    [Fact]
    public void ValidateSessionId_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateSessionId("   ", "sessionId"));

        _ = exception.ParamName.Should().Be("sessionId");
    }

    /// <summary>
    /// Verifies that ValidateSessionId succeeds with valid session ID.
    /// </summary>
    [Fact]
    public void ValidateSessionId_WithValidValue_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        DebugEngineTestAccessor.ValidateSessionId("valid-session-id", "sessionId");
    }

    /// <summary>
    /// Verifies that ValidateCommandId throws when commandId is null.
    /// </summary>
    [Fact]
    public void ValidateCommandId_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateCommandId(null!, "commandId"));

        _ = exception.ParamName.Should().Be("commandId");
        _ = exception.Message.Should().Contain("Command ID cannot be null or empty");
    }

    /// <summary>
    /// Verifies that ValidateCommandId throws when commandId is empty.
    /// </summary>
    [Fact]
    public void ValidateCommandId_WithEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateCommandId(string.Empty, "commandId"));

        _ = exception.ParamName.Should().Be("commandId");
    }

    /// <summary>
    /// Verifies that ValidateCommandId succeeds with valid command ID.
    /// </summary>
    [Fact]
    public void ValidateCommandId_WithValidValue_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        DebugEngineTestAccessor.ValidateCommandId("cmd-123", "commandId");
    }

    /// <summary>
    /// Verifies that ValidateCommand throws when command is null.
    /// </summary>
    [Fact]
    public void ValidateCommand_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateCommand(null!, "command"));

        _ = exception.ParamName.Should().Be("command");
        _ = exception.Message.Should().Contain("Command cannot be null or empty");
    }

    /// <summary>
    /// Verifies that ValidateCommand throws when command is empty.
    /// </summary>
    [Fact]
    public void ValidateCommand_WithEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateCommand(string.Empty, "command"));

        _ = exception.ParamName.Should().Be("command");
    }

    /// <summary>
    /// Verifies that ValidateCommand succeeds with valid command.
    /// </summary>
    [Fact]
    public void ValidateCommand_WithValidValue_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        DebugEngineTestAccessor.ValidateCommand("!analyze -v", "command");
    }

    /// <summary>
    /// Verifies that ValidateExtensionName throws when extensionName is null.
    /// </summary>
    [Fact]
    public void ValidateExtensionName_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateExtensionName(null!, "extensionName"));

        _ = exception.ParamName.Should().Be("extensionName");
        _ = exception.Message.Should().Contain("Extension name cannot be null or empty");
    }

    /// <summary>
    /// Verifies that ValidateExtensionName throws when extensionName is empty.
    /// </summary>
    [Fact]
    public void ValidateExtensionName_WithEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            DebugEngineTestAccessor.ValidateExtensionName(string.Empty, "extensionName"));

        _ = exception.ParamName.Should().Be("extensionName");
    }

    /// <summary>
    /// Verifies that ValidateExtensionName succeeds with valid extension name.
    /// </summary>
    [Fact]
    public void ValidateExtensionName_WithValidValue_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        DebugEngineTestAccessor.ValidateExtensionName("test-extension", "extensionName");
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed throws when engine is disposed.
    /// </summary>
    [Fact]
    public void ThrowIfDisposed_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var accessor = new DebugEngineTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);
        accessor.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => accessor.ThrowIfDisposed());
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed does not throw when engine is not disposed.
    /// </summary>
    [Fact]
    public void ThrowIfDisposed_WhenNotDisposed_DoesNotThrow()
    {
        // Arrange
        using var accessor = new DebugEngineTestAccessor(m_MockFileSystem.Object, m_MockProcessManager.Object);

        // Act & Assert - Should not throw
        accessor.ThrowIfDisposed();
    }
}
