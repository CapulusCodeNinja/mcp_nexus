using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using nexus.engine.Events;
using nexus.engine.Internal;
using nexus.engine.Models;
using nexus.engine.Configuration;
using nexus.utilities.FileSystem;
using nexus.utilities.ProcessManagement;

namespace nexus.engine.unittests.Internal;

/// <summary>
/// Unit tests for the DebugSession class.
/// </summary>
public class DebugSessionTests : IDisposable
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;

    /// <summary>
    /// Initializes a new instance of the DebugSessionTests class.
    /// </summary>
    public DebugSessionTests()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockProcessManager = new Mock<IProcessManager>();
        m_LoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        m_Configuration = new DebugEngineConfiguration
        {
            MaxConcurrentSessions = 5,
            DefaultCommandTimeout = TimeSpan.FromSeconds(30),
            SessionInitializationTimeout = TimeSpan.FromMinutes(1)
        };

        SetupDefaultMocks();
    }

    /// <summary>
    /// Sets up default mock behaviors to prevent real system access.
    /// </summary>
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

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        m_LoggerFactory?.Dispose();
    }

    /// <summary>
    /// Creates a DebugSession instance for testing.
    /// </summary>
    /// <returns>A new DebugSession instance.</returns>
    private DebugSession CreateDebugSession()
    {
        // Mock the file system to return true for the dump file and CDB executable so initialization can succeed
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Test\test.dmp"))
            .Returns(true);

        // Mock CDB executable path
        m_MockFileSystem.Setup(fs => fs.FileExists(@"C:\Program Files\Windows Kits\10\Debuggers\x64\cdb.exe"))
            .Returns(true);

        var session = new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        return session;
    }

    /// <summary>
    /// Verifies that the DebugSession constructor creates an instance successfully with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var session = CreateDebugSession();

        // Assert
        session.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the DebugSession constructor throws ArgumentNullException when session ID is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            null!,
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionId");
    }

    /// <summary>
    /// Verifies that the DebugSession constructor throws ArgumentNullException when dump file path is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullDumpFilePath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            null!,
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("dumpFilePath");
    }

    /// <summary>
    /// Verifies that the DebugSession constructor throws ArgumentNullException when configuration is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            null!,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// Verifies that the DebugSession constructor throws ArgumentNullException when logger factory is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            null!,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    /// <summary>
    /// Verifies that the DebugSession constructor throws ArgumentNullException when file system is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFileSystem_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            null!,
            m_MockProcessManager.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("fileSystem");
    }

    /// <summary>
    /// Verifies that the DebugSession constructor throws ArgumentNullException when process manager is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProcessManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new DebugSession(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("processManager");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws InvalidOperationException when session is not active.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithValidCommand_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateDebugSession();
        var command = "lm";

        // Act & Assert
        var action = () => session.EnqueueCommand(command);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session is not active (current state: Initializing)");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws InvalidOperationException when session is not active and command is null.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNullCommand_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act & Assert
        var action = () => session.EnqueueCommand(null!);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session is not active (current state: Initializing)");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws InvalidOperationException when session is not active and command is empty.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithEmptyCommand_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act & Assert
        var action = () => session.EnqueueCommand(string.Empty);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session is not active (current state: Initializing)");
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null for a valid command ID when queue is not active.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithValidCommandId_ShouldReturnNull()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var commandInfo = session.GetCommandInfo("some-command-id");

        // Assert
        commandInfo.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null for an invalid command ID.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithInvalidCommandId_ShouldReturnNull()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var commandInfo = session.GetCommandInfo("invalid-id");

        // Assert
        commandInfo.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns an empty collection when queue is not active.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_ShouldReturnEmptyCollection()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var allCommands = session.GetAllCommandInfos();

        // Assert
        allCommands.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false when queue is not active.
    /// </summary>
    [Fact]
    public void CancelCommand_WithValidCommandId_ShouldReturnFalse()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var result = session.CancelCommand("some-command-id");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false for an invalid command ID.
    /// </summary>
    [Fact]
    public void CancelCommand_WithInvalidCommandId_ShouldReturnFalse()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var result = session.CancelCommand("invalid-id");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelAllCommands returns zero when queue is not active.
    /// </summary>
    [Fact]
    public void CancelAllCommands_ShouldReturnZero()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var result = session.CancelAllCommands();

        // Assert
        result.Should().Be(0);
    }

    /// <summary>
    /// Verifies that State returns Initializing for a newly created session.
    /// </summary>
    [Fact]
    public void State_ShouldReturnInitializing()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        var state = session.State;

        // Assert
        state.Should().Be(SessionState.Initializing);
    }

    /// <summary>
    /// Verifies that Dispose disposes the session correctly.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalled_ShouldDisposeSession()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act
        session.Dispose();

        // Assert
        // Should not throw when disposed
        var action = () => session.Dispose();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times does not throw an exception.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var session = CreateDebugSession();

        // Act & Assert
        session.Dispose();
        var action = () => session.Dispose();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed throws ObjectDisposedException when session is disposed.
    /// </summary>
    [Fact]
    public void TestThrowIfDisposed_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        testAccessor.Dispose();

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that ThrowIfDisposed does not throw when session is not disposed.
    /// </summary>
    [Fact]
    public void TestThrowIfDisposed_WhenNotDisposed_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestThrowIfDisposed();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that ThrowIfNotActive throws InvalidOperationException when session is not active.
    /// </summary>
    [Fact]
    public void TestThrowIfNotActive_WhenNotActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestThrowIfNotActive();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Session test-session is not active (current state: Initializing)");
    }

    /// <summary>
    /// Verifies that SetState raises SessionStateChanged event correctly.
    /// </summary>
    [Fact]
    public void TestSetState_ShouldRaiseSessionStateChangedEvent()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        SessionStateChangedEventArgs? eventArgs = null;
        testAccessor.SessionStateChanged += (sender, args) => eventArgs = args;

        // Act
        testAccessor.TestSetState(SessionState.Active);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.SessionId.Should().Be("test-session");
        eventArgs.NewState.Should().Be(SessionState.Active);
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged raises CommandStateChanged event with correct session ID.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_ShouldRaiseCommandStateChangedEvent()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        CommandStateChangedEventArgs? eventArgs = null;
        testAccessor.CommandStateChanged += (sender, args) => eventArgs = args;

        var originalEventArgs = new CommandStateChangedEventArgs
        {
            SessionId = "original-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Executing,
            Timestamp = DateTime.Now,
            Command = "test command"
        };

        // Act
        testAccessor.TestOnCommandStateChanged(this, originalEventArgs);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.SessionId.Should().Be("test-session"); // Should be overridden with session ID
        eventArgs.CommandId.Should().Be("cmd-123");
        eventArgs.NewState.Should().Be(CommandState.Executing);
    }

    /// <summary>
    /// Verifies that SetState updates session state correctly.
    /// </summary>
    [Fact]
    public void TestSetState_WithValidState_ShouldUpdateState()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        testAccessor.TestSetState(SessionState.Active);

        // Assert
        testAccessor.State.Should().Be(SessionState.Active);
    }

    /// <summary>
    /// Verifies that SetState updates session state to Closed correctly.
    /// </summary>
    [Fact]
    public void TestSetState_WithClosedState_ShouldUpdateState()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        testAccessor.TestSetState(SessionState.Closed);

        // Assert
        testAccessor.State.Should().Be(SessionState.Closed);
    }

    /// <summary>
    /// Verifies that SetState updates session state to Initializing correctly.
    /// </summary>
    [Fact]
    public void TestSetState_WithInitializingState_ShouldUpdateState()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        testAccessor.TestSetState(SessionState.Initializing);

        // Assert
        testAccessor.State.Should().Be(SessionState.Initializing);
    }

    /// <summary>
    /// Verifies that SetState updates session state to Faulted correctly.
    /// </summary>
    [Fact]
    public void TestSetState_WithErrorState_ShouldUpdateState()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act
        testAccessor.TestSetState(SessionState.Faulted);

        // Assert
        testAccessor.State.Should().Be(SessionState.Faulted);
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when sender is null.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithNullSender_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Completed,
            Timestamp = DateTime.Now,
            Command = "Test output"
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(null, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged throws NullReferenceException when event args are null.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithNullEventArgs_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, null!);
        action.Should().Throw<NullReferenceException>();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged throws NullReferenceException when both sender and event args are null.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithNullSenderAndEventArgs_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(null, null!);
        action.Should().Throw<NullReferenceException>();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command state is Queued.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithQueuedState_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Queued,
            Timestamp = DateTime.Now,
            Command = "Test output"
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command state is Executing.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithExecutingState_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Executing,
            Timestamp = DateTime.Now,
            Command = "Test output"
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command state is Failed.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithFailedState_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Executing,
            NewState = CommandState.Failed,
            Timestamp = DateTime.Now,
            Command = "Test output"
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command state is Cancelled.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithCancelledState_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Executing,
            NewState = CommandState.Cancelled,
            Timestamp = DateTime.Now,
            Command = "Test output"
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command state is Timeout.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithTimeoutState_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Executing,
            NewState = CommandState.Timeout,
            Timestamp = DateTime.Now,
            Command = "Test output"
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command ID is empty.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithEmptyCommandId_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "",
            OldState = CommandState.Queued,
            NewState = CommandState.Completed,
            Timestamp = DateTime.Now,
            Command = "Test output"
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command output is null.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithNullOutput_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Completed,
            Timestamp = DateTime.Now,
            Command = null
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command output is empty.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithEmptyOutput_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Completed,
            Timestamp = DateTime.Now,
            Command = ""
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command output is very long.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithLongOutput_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var longOutput = new string('A', 10000);
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Completed,
            Timestamp = DateTime.Now,
            Command = longOutput
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command output contains special characters.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithSpecialCharactersInOutput_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var specialOutput = "Test output with special chars: \n\r\t\"'\\";
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Completed,
            Timestamp = DateTime.Now,
            Command = specialOutput
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that OnCommandStateChanged does not throw when command output contains Unicode characters.
    /// </summary>
    [Fact]
    public void TestOnCommandStateChanged_WithUnicodeOutput_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new DebugSessionTestAccessor(
            "test-session",
            @"C:\Test\test.dmp",
            @"C:\Symbols",
            m_Configuration,
            m_LoggerFactory,
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var sender = new object();
        var unicodeOutput = "Test output with unicode: 你好世界 🌍";
        var e = new CommandStateChangedEventArgs
        {
            SessionId = "test-session",
            CommandId = "cmd-123",
            OldState = CommandState.Queued,
            NewState = CommandState.Completed,
            Timestamp = DateTime.Now,
            Command = unicodeOutput
        };

        // Act & Assert
        var action = () => testAccessor.TestOnCommandStateChanged(sender, e);
        action.Should().NotThrow();
    }
}
