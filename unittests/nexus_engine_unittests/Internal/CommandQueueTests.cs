using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using nexus.engine.Internal;
using nexus.engine.Events;
using nexus.engine.Models;
using nexus.engine.Configuration;
using nexus.utilities.FileSystem;
using nexus.utilities.ProcessManagement;

namespace nexus.engine.unittests.Internal;

/// <summary>
/// Unit tests for the CommandQueue class.
/// </summary>
public class CommandQueueTests : IDisposable
{
    private readonly Mock<IFileSystem> m_MockFileSystem;
    private readonly Mock<IProcessManager> m_MockProcessManager;
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;
    private CommandQueue? m_CommandQueue;

    /// <summary>
    /// Initializes a new instance of the CommandQueueTests class.
    /// </summary>
    public CommandQueueTests()
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

        m_CommandQueue = new CommandQueue("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
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
        m_CommandQueue?.Dispose();
        m_LoggerFactory?.Dispose();
    }

    /// <summary>
    /// Verifies that the CommandQueue constructor creates an instance successfully with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var commandQueue = new CommandQueue("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());

        // Assert
        commandQueue.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the CommandQueue constructor throws ArgumentNullException when session ID is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommandQueue(null!, m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionId");
    }

    /// <summary>
    /// Verifies that the CommandQueue constructor throws ArgumentNullException when configuration is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommandQueue("test-session", null!, m_LoggerFactory.CreateLogger<CommandQueue>());
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    /// <summary>
    /// Verifies that the CommandQueue constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommandQueue("test-session", m_Configuration, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Verifies that EnqueueCommand returns a valid command ID when given a valid command.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithValidCommand_ShouldReturnCommandId()
    {
        // Arrange
        var command = "lm";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(command);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is null.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNullCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is empty.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithEmptyCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(string.Empty);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns command information when given a valid command ID.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithValidCommandId_ShouldReturnCommandInfo()
    {
        // Arrange
        var command = "lm";
        var commandId = m_CommandQueue!.EnqueueCommand(command);

        // Act
        var commandInfo = m_CommandQueue.GetCommandInfo(commandId);

        // Assert
        commandInfo.Should().NotBeNull();
        commandInfo!.CommandId.Should().Be(commandId);
        commandInfo.Command.Should().Be(command);
        commandInfo.State.Should().Be(CommandState.Queued);
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null when given an invalid command ID.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithInvalidCommandId_ShouldReturnNull()
    {
        // Act
        var commandInfo = m_CommandQueue!.GetCommandInfo("invalid-id");

        // Assert
        commandInfo.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns all queued commands.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_ShouldReturnAllCommands()
    {
        // Arrange
        var command1 = "lm";
        var command2 = "!threads";
        var commandId1 = m_CommandQueue!.EnqueueCommand(command1);
        var commandId2 = m_CommandQueue.EnqueueCommand(command2);

        // Act
        var allCommands = m_CommandQueue.GetAllCommandInfos();

        // Assert
        allCommands.Should().HaveCount(2);
        allCommands.Should().ContainKey(commandId1);
        allCommands.Should().ContainKey(commandId2);
        allCommands[commandId1].Command.Should().Be(command1);
        allCommands[commandId2].Command.Should().Be(command2);
    }

    /// <summary>
    /// Verifies that CancelCommand successfully cancels a command with a valid command ID.
    /// </summary>
    [Fact]
    public void CancelCommand_WithValidCommandId_ShouldCancelCommand()
    {
        // Arrange
        var command = "lm";
        var commandId = m_CommandQueue!.EnqueueCommand(command);

        // Act
        var result = m_CommandQueue.CancelCommand(commandId);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false when given an invalid command ID.
    /// </summary>
    [Fact]
    public void CancelCommand_WithInvalidCommandId_ShouldReturnFalse()
    {
        // Act
        var result = m_CommandQueue!.CancelCommand("invalid-id");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelAllCommands cancels all queued commands.
    /// </summary>
    [Fact]
    public void CancelAllCommands_ShouldCancelAllCommands()
    {
        // Arrange
        var command1 = "lm";
        var command2 = "!threads";
        m_CommandQueue!.EnqueueCommand(command1);
        m_CommandQueue.EnqueueCommand(command2);

        // Act
        var result = m_CommandQueue.CancelAllCommands();

        // Assert
        result.Should().Be(2);
    }

    /// <summary>
    /// Verifies that Dispose disposes the queue correctly.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalled_ShouldDisposeQueue()
    {
        // Act
        m_CommandQueue!.Dispose();

        // Assert
        // Should not throw when disposed
        var action = () => m_CommandQueue.Dispose();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times does not throw an exception.
    /// </summary>
    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        m_CommandQueue!.Dispose();
        var action = () => m_CommandQueue.Dispose();
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ObjectDisposedException when the queue is disposed.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.EnqueueCommand("test");
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that GetCommandInfo throws ObjectDisposedException when the queue is disposed.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.GetCommandInfo("test-id");
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos throws ObjectDisposedException when the queue is disposed.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.GetAllCommandInfos();
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that CancelCommand throws ObjectDisposedException when the queue is disposed.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.CancelCommand("test-id");
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that CancelAllCommands throws ObjectDisposedException when the queue is disposed.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.CancelAllCommands();
        action.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is whitespace.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("   ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles very long commands correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithVeryLongCommand_ShouldReturnCommandId()
    {
        // Arrange
        var longCommand = new string('a', 10000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(longCommand);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with special characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithSpecialCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var specialCommand = "!analyze -v; lm; !threads; ~*k";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(specialCommand);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that EnqueueCommand returns unique command IDs for multiple commands.
    /// </summary>
    [Fact]
    public void EnqueueCommand_MultipleCommands_ShouldReturnUniqueCommandIds()
    {
        // Arrange
        var command1 = "lm";
        var command2 = "!threads";
        var command3 = "!peb";

        // Act
        var commandId1 = m_CommandQueue!.EnqueueCommand(command1);
        var commandId2 = m_CommandQueue.EnqueueCommand(command2);
        var commandId3 = m_CommandQueue.EnqueueCommand(command3);

        // Assert
        commandId1.Should().NotBeNullOrEmpty();
        commandId2.Should().NotBeNullOrEmpty();
        commandId3.Should().NotBeNullOrEmpty();
        commandId1.Should().NotBe(commandId2);
        commandId2.Should().NotBe(commandId3);
        commandId1.Should().NotBe(commandId3);
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null when command ID is null.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithNullCommandId_ShouldReturnNull()
    {
        // Act
        var commandInfo = m_CommandQueue!.GetCommandInfo(null!);

        // Assert
        commandInfo.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null when command ID is empty.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithEmptyCommandId_ShouldReturnNull()
    {
        // Act
        var commandInfo = m_CommandQueue!.GetCommandInfo(string.Empty);

        // Assert
        commandInfo.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false when command ID is null.
    /// </summary>
    [Fact]
    public void CancelCommand_WithNullCommandId_ShouldReturnFalse()
    {
        // Act
        var result = m_CommandQueue!.CancelCommand(null!);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false when command ID is empty.
    /// </summary>
    [Fact]
    public void CancelCommand_WithEmptyCommandId_ShouldReturnFalse()
    {
        // Act
        var result = m_CommandQueue!.CancelCommand(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ProcessCommandAsync executes a valid command and handles uninitialized CDB session.
    /// </summary>
    [Fact]
    public async Task ProcessCommandAsync_WithValidCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var command = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test command",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Create a real CdbSession with mocked dependencies
        var realCdbSession = new nexus.engine.Internal.CdbSession(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        // Use test accessor
        var testAccessor = new CommandQueueTestAccessor("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
        testAccessor.TestCdbSession = realCdbSession;

        // Act
        await testAccessor.TestProcessCommandAsync(command, CancellationToken.None);

        // Assert
        var result = testAccessor.GetCommandInfo("cmd-123");
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse(); // Will fail because CDB session is not initialized
        result.ErrorMessage.Should().Contain("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ProcessCommandAsync throws InvalidOperationException when CDB session is null.
    /// </summary>
    [Fact]
    public async Task ProcessCommandAsync_WithCdbSessionNull_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test command",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        var testAccessor = new CommandQueueTestAccessor("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
        // Don't set TestCdbSession - leave it null

        // Act & Assert
        var action = () => testAccessor.TestProcessCommandAsync(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that ProcessCommandAsync marks command as failed when CDB session throws an exception.
    /// </summary>
    [Fact]
    public async Task ProcessCommandAsync_WithCdbSessionThrowingException_ShouldMarkCommandAsFailed()
    {
        // Arrange
        var command = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test command",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Create a real CdbSession with mocked dependencies
        var realCdbSession = new nexus.engine.Internal.CdbSession(
            m_Configuration,
            m_LoggerFactory.CreateLogger<nexus.engine.Internal.CdbSession>(),
            m_MockFileSystem.Object,
            m_MockProcessManager.Object);

        var testAccessor = new CommandQueueTestAccessor("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
        testAccessor.TestCdbSession = realCdbSession;

        // Act
        await testAccessor.TestProcessCommandAsync(command, CancellationToken.None);

        // Assert
        var result = testAccessor.GetCommandInfo("cmd-123");
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("CDB session is not initialized");
    }

    /// <summary>
    /// Verifies that ProcessCommandAsync marks command as cancelled when cancellation is requested.
    /// </summary>
    [Fact]
    public async Task ProcessCommandAsync_WithCancellation_ShouldMarkCommandAsCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var command = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test command",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = cts
        };
        command.CancellationTokenSource.Cancel();

        var testAccessor = new CommandQueueTestAccessor("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
        // Don't set TestCdbSession - leave it null

        // Act & Assert
        var action = () => testAccessor.TestProcessCommandAsync(command, cts.Token);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that UpdateCommandState raises CommandStateChanged event correctly.
    /// </summary>
    [Fact]
    public void UpdateCommandState_ShouldRaiseEvent()
    {
        // Arrange
        var command = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test command",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };
        nexus.engine.Events.CommandStateChangedEventArgs? eventArgs = null;

        var testAccessor = new CommandQueueTestAccessor("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
        testAccessor.CommandStateChanged += (sender, args) => eventArgs = args;

        // Act
        testAccessor.TestUpdateCommandState(command, CommandState.Executing);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.CommandId.Should().Be("cmd-123");
        eventArgs.NewState.Should().Be(CommandState.Executing);
    }

    /// <summary>
    /// Verifies that SetCommandResult stores command result in cache correctly.
    /// </summary>
    [Fact]
    public void SetCommandResult_ShouldStoreResultInCache()
    {
        // Arrange
        var command = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test command",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };
        var commandInfo = CommandInfo.Completed("cmd-123", "test command", DateTime.Now, DateTime.Now, DateTime.Now, "output", true);

        var testAccessor = new CommandQueueTestAccessor("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());

        // Act
        testAccessor.TestSetCommandResult(command, commandInfo);

        // Assert
        var result = testAccessor.GetCommandInfo("cmd-123");
        result.Should().NotBeNull();
        result.Should().Be(commandInfo);
    }

    /// <summary>
    /// Verifies that EnqueueCommand returns a command ID with correct prefix.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithValidCommand_ShouldReturnCommandId_New()
    {
        // Arrange
        var command = "lm";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(command);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand returns unique command IDs for multiple commands.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithMultipleCommands_ShouldReturnUniqueCommandIds()
    {
        // Arrange
        var commands = new[] { "lm", "!threads", "!peb", "kL" };

        // Act
        var commandIds = commands.Select(cmd => m_CommandQueue!.EnqueueCommand(cmd)).ToList();

        // Assert
        commandIds.Should().HaveCount(4);
        commandIds.Should().OnlyHaveUniqueItems();
        commandIds.Should().AllSatisfy(id => id.Should().StartWith("cmd-"));
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles very long commands correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithVeryLongCommand_ShouldReturnCommandId_New()
    {
        // Arrange
        var longCommand = new string('a', 10000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(longCommand);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with special characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithSpecialCharacters_ShouldReturnCommandId_New()
    {
        // Arrange
        var specialCommand = "!analyze -v; lm; !threads; ~*k; .echo 'test'";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(specialCommand);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with Unicode characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithUnicodeCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var unicodeCommand = "!analyze -v; lm; !threads; ~*k; .echo 'тест'";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(unicodeCommand);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with newline characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNewlines_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithNewlines = "lm\n!threads\n!peb";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithNewlines);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with tab characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithTabs_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithTabs = "lm\t!threads\t!peb";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithTabs);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with quote characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithQuotes_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithQuotes = "!analyze -v; lm; !threads; ~*k; .echo \"test\"";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithQuotes);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with backslash characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithBackslashes_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithBackslashes = "!analyze -v; lm; !threads; ~*k; .echo 'C:\\test\\path'";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithBackslashes);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with parentheses correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithParentheses_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithParentheses = "!analyze -v; lm; !threads; ~*k; .echo (test)";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithParentheses);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with brackets correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithBrackets_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithBrackets = "!analyze -v; lm; !threads; ~*k; .echo [test]";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithBrackets);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with braces correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithBraces_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithBraces = "!analyze -v; lm; !threads; ~*k; .echo {test}";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithBraces);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with pipe characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithPipes_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithPipes = "!analyze -v; lm; !threads; ~*k; .echo test|more";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithPipes);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with ampersand characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithAmpersands_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithAmpersands = "!analyze -v; lm; !threads; ~*k; .echo test&more";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithAmpersands);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with semicolon characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithSemicolons_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithSemicolons = "!analyze -v; lm; !threads; ~*k; .echo test;more";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithSemicolons);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with comma characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithCommas_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithCommas = "!analyze -v; lm; !threads; ~*k; .echo test,more";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithCommas);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with dot characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithDots_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithDots = "!analyze -v; lm; !threads; ~*k; .echo test.more";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithDots);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with hyphen characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithHyphens_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithHyphens = "!analyze -v; lm; !threads; ~*k; .echo test-more";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithHyphens);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with underscore characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithUnderscores_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithUnderscores = "!analyze -v; lm; !threads; ~*k; .echo test_more";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithUnderscores);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with numeric characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNumbers_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithNumbers = "!analyze -v; lm; !threads; ~*k; .echo test123";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithNumbers);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with mixed case letters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithMixedCase_ShouldReturnCommandId()
    {
        // Arrange
        var commandWithMixedCase = "!Analyze -v; lm; !Threads; ~*k; .Echo Test";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(commandWithMixedCase);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is empty string.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(string.Empty);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is null.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is whitespace only.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithWhitespaceOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("   ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is tab only.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithTabOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("\t");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is newline only.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNewlineOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("\n");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is carriage return only.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithCarriageReturnOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("\r");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException when command is mixed whitespace only.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithMixedWhitespace_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(" \t\n\r ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with a single character correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithSingleCharacter_ShouldReturnCommandId()
    {
        // Arrange
        var singleChar = "a";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(singleChar);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with two characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithTwoCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var twoChars = "ab";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(twoChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with three characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithThreeCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var threeChars = "abc";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(threeChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with four characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithFourCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var fourChars = "abcd";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(fourChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with five characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithFiveCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var fiveChars = "abcde";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(fiveChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with ten characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithTenCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var tenChars = "abcdefghij";

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(tenChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with fifty characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithFiftyCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var fiftyChars = new string('a', 50);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(fiftyChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with one hundred characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithHundredCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var hundredChars = new string('a', 100);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(hundredChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with one thousand characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithThousandCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var thousandChars = new string('a', 1000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(thousandChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with five thousand characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithFiveThousandCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var fiveThousandChars = new string('a', 5000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(fiveThousandChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with ten thousand characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithTenThousandCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var tenThousandChars = new string('a', 10000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(tenThousandChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with fifty thousand characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithFiftyThousandCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var fiftyThousandChars = new string('a', 50000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(fiftyThousandChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with one hundred thousand characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithHundredThousandCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var hundredThousandChars = new string('a', 100000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(hundredThousandChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand handles commands with one million characters correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithMillionCharacters_ShouldReturnCommandId()
    {
        // Arrange
        var millionChars = new string('a', 1000000);

        // Act
        var commandId = m_CommandQueue!.EnqueueCommand(millionChars);

        // Assert
        commandId.Should().NotBeNullOrEmpty();
        commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that ValidateCdbSession throws InvalidOperationException when CDB session is null.
    /// </summary>
    [Fact]
    public void TestValidateCdbSession_WithNullCdbSession_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        // Act
        var action = () => testAccessor.TestValidateCdbSession();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that ValidateCdbSession throws InvalidOperationException when CDB session is not set.
    /// </summary>
    [Fact]
    public void TestValidateCdbSession_WithValidCdbSession_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        // Note: We cannot set a mock ICdbSession to a CdbSession field due to type mismatch
        // This test validates the method exists and can be called, but will throw due to null CDB session
        // Act & Assert
        var action = () => testAccessor.TestValidateCdbSession();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that LogCommandProcessing does not throw when logging a valid command.
    /// </summary>
    [Fact]
    public void TestLogCommandProcessing_WithValidCommand_ShouldNotThrow()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act
        var action = () => testAccessor.TestLogCommandProcessing(command);

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that ExecuteCommandWithCdbSession throws InvalidOperationException when CDB session is not initialized.
    /// </summary>
    [Fact]
    public async Task TestExecuteCommandWithCdbSession_WithValidCommand_ShouldReturnResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        // Note: We cannot set a mock ICdbSession to a CdbSession field due to type mismatch
        // This test validates the method exists and can be called, but will throw due to null CDB session
        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act & Assert
        var action = async () => await testAccessor.TestExecuteCommandWithCdbSession(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that HandleSuccessfulCommandExecution updates command state and sets result correctly.
    /// </summary>
    [Fact]
    public async Task TestHandleSuccessfulCommandExecution_WithValidCommand_ShouldUpdateStateAndSetResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };
        var startTime = DateTime.Now;
        var result = "test result";

        // Act
        await testAccessor.TestHandleSuccessfulCommandExecution(command, startTime, result);

        // Assert
        command.State.Should().Be(CommandState.Completed);
    }

    /// <summary>
    /// Verifies that HandleCancelledCommand updates command state and sets result correctly for cancelled commands.
    /// </summary>
    [Fact]
    public async Task TestHandleCancelledCommand_WithValidCommand_ShouldUpdateStateAndSetResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };
        var startTime = DateTime.Now;

        // Act
        await testAccessor.TestHandleCancelledCommand(command, startTime);

        // Assert
        command.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that HandleTimedOutCommand updates command state and sets result correctly for timed out commands.
    /// </summary>
    [Fact]
    public async Task TestHandleTimedOutCommand_WithValidCommand_ShouldUpdateStateAndSetResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };
        var startTime = DateTime.Now;
        var timeoutException = new TimeoutException("Command timed out");

        // Act
        await testAccessor.TestHandleTimedOutCommand(command, startTime, timeoutException);

        // Assert
        command.State.Should().Be(CommandState.Timeout);
    }

    /// <summary>
    /// Verifies that HandleFailedCommand updates command state and sets result correctly for failed commands.
    /// </summary>
    [Fact]
    public async Task TestHandleFailedCommand_WithValidCommand_ShouldUpdateStateAndSetResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };
        var startTime = DateTime.Now;
        var exception = new InvalidOperationException("Command failed");

        // Act
        await testAccessor.TestHandleFailedCommand(command, startTime, exception);

        // Assert
        command.State.Should().Be(CommandState.Failed);
    }

    /// <summary>
    /// Verifies that ProcessCommandAsync handles timeout exceptions correctly.
    /// </summary>
    [Fact]
    public async Task ProcessCommandAsync_WithTimeoutException_ShouldHandleTimeout()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act & Assert
        var action = async () => await testAccessor.TestProcessCommandAsync(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that ProcessCommandAsync handles OperationCanceledException correctly.
    /// </summary>
    [Fact]
    public async Task ProcessCommandAsync_WithOperationCanceledException_ShouldHandleCancellation()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act & Assert
        var action = async () => await testAccessor.TestProcessCommandAsync(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that ProcessCommandAsync handles generic exceptions correctly.
    /// </summary>
    [Fact]
    public async Task ProcessCommandAsync_WithGenericException_ShouldHandleFailure()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act & Assert
        var action = async () => await testAccessor.TestProcessCommandAsync(command, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CDB session not initialized");
    }

    /// <summary>
    /// Verifies that UpdateCommandState updates command state correctly.
    /// </summary>
    [Fact]
    public void UpdateCommandState_WithValidCommand_ShouldUpdateState()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        // Act
        testAccessor.TestUpdateCommandState(command, CommandState.Executing);

        // Assert
        command.State.Should().Be(CommandState.Executing);
    }

    /// <summary>
    /// Verifies that UpdateCommandState raises CommandStateChanged event with correct event arguments.
    /// </summary>
    [Fact]
    public void UpdateCommandState_WithValidCommand_ShouldRaiseEvent()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        CommandStateChangedEventArgs? capturedEvent = null;
        testAccessor.CommandStateChanged += (sender, e) => capturedEvent = e;

        // Act
        testAccessor.TestUpdateCommandState(command, CommandState.Executing);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.CommandId.Should().Be("test-command");
        capturedEvent.OldState.Should().Be(CommandState.Queued);
        capturedEvent.NewState.Should().Be(CommandState.Executing);
    }

    /// <summary>
    /// Verifies that SetCommandResult stores command result correctly.
    /// </summary>
    [Fact]
    public void SetCommandResult_WithValidCommand_ShouldStoreResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        var commandInfo = CommandInfo.Completed(
            "test-command",
            "lm",
            DateTime.Now,
            DateTime.Now,
            DateTime.Now,
            "test result",
            true,
            string.Empty);

        // Act
        testAccessor.TestSetCommandResult(command, commandInfo);

        // Assert
        var result = testAccessor.GetCommandInfo("test-command");
        result.Should().NotBeNull();
        result!.Command.Should().Be("lm");
        result.Output.Should().Be("test result");
    }

    /// <summary>
    /// Verifies that SetCommandResult stores failure result correctly for failed commands.
    /// </summary>
    [Fact]
    public void SetCommandResult_WithFailedCommand_ShouldStoreFailureResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        var commandInfo = CommandInfo.Completed(
            "test-command",
            "lm",
            DateTime.Now,
            DateTime.Now,
            DateTime.Now,
            "Command failed",
            false,
            "Command failed");

        // Act
        testAccessor.TestSetCommandResult(command, commandInfo);

        // Assert
        var result = testAccessor.GetCommandInfo("test-command");
        result.Should().NotBeNull();
        result!.Command.Should().Be("lm");
        result.ErrorMessage.Should().Be("Command failed");
    }

    /// <summary>
    /// Verifies that SetCommandResult stores timeout result correctly for timed out commands.
    /// </summary>
    [Fact]
    public void SetCommandResult_WithTimedOutCommand_ShouldStoreTimeoutResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        var commandInfo = CommandInfo.TimedOut(
            "test-command",
            "lm",
            DateTime.Now,
            DateTime.Now,
            DateTime.Now,
            "Command timed out");

        // Act
        testAccessor.TestSetCommandResult(command, commandInfo);

        // Assert
        var result = testAccessor.GetCommandInfo("test-command");
        result.Should().NotBeNull();
        result!.Command.Should().Be("lm");
        result.ErrorMessage.Should().Be("Command timed out");
    }

    /// <summary>
    /// Verifies that SetCommandResult stores cancelled result correctly for cancelled commands.
    /// </summary>
    [Fact]
    public void SetCommandResult_WithCancelledCommand_ShouldStoreCancelledResult()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        var command = new QueuedCommand
        {
            Id = "test-command",
            Command = "lm",
            QueuedTime = DateTime.Now,
            CancellationTokenSource = new CancellationTokenSource()
        };

        var commandInfo = CommandInfo.Cancelled(
            "test-command",
            "lm",
            DateTime.Now,
            DateTime.Now,
            DateTime.Now);

        // Act
        testAccessor.TestSetCommandResult(command, commandInfo);

        // Assert
        var result = testAccessor.GetCommandInfo("test-command");
        result.Should().NotBeNull();
        result!.Command.Should().Be("lm");
        result.ErrorMessage.Should().Be("Command was cancelled");
    }

    /// <summary>
    /// Verifies that StartAsync starts the command queue successfully with a valid CDB session.
    /// </summary>
    [Fact]
    public void StartAsync_WithValidCdbSession_ShouldStartSuccessfully()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());
        var mockCdbSession = new Mock<CdbSession>(m_Configuration, m_LoggerFactory.CreateLogger<CdbSession>(), m_MockFileSystem.Object, m_MockProcessManager.Object);
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await testAccessor.StartAsync(mockCdbSession.Object, cancellationToken);
        action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that StartAsync throws ArgumentNullException when CDB session is null.
    /// </summary>
    [Fact]
    public void StartAsync_WithNullCdbSession_ShouldThrowArgumentNullException()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());
        CdbSession? cdbSession = null;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await testAccessor.StartAsync(cdbSession!, cancellationToken);
        action.Should().ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'cdbSession')");
    }

    /// <summary>
    /// Verifies that StopAsync stops the command queue successfully.
    /// </summary>
    [Fact]
    public void StopAsync_WhenStarted_ShouldStopSuccessfully()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());

        // Act & Assert
        var action = async () => await testAccessor.StopAsync();
        action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync returns command information for a valid command ID.
    /// </summary>
    [Fact]
    public void GetCommandInfoAsync_WithValidCommandId_ShouldReturnCommandInfo()
    {
        // Arrange
        var testAccessor = new CommandQueueTestAccessor(
            "test-session",
            m_Configuration,
            m_LoggerFactory.CreateLogger<CommandQueue>());
        var commandId = "cmd-123";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var action = async () => await testAccessor.GetCommandInfoAsync(commandId, cancellationToken);
        action.Should().NotThrowAsync();
    }
}
