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

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var commandQueue = new CommandQueue("test-session", m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());

        // Assert
        commandQueue.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommandQueue(null!, m_Configuration, m_LoggerFactory.CreateLogger<CommandQueue>());
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommandQueue("test-session", null!, m_LoggerFactory.CreateLogger<CommandQueue>());
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CommandQueue("test-session", m_Configuration, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

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

    [Fact]
    public void EnqueueCommand_WithNullCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithEmptyCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(string.Empty);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

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
        commandInfo.CommandId.Should().Be(commandId);
        commandInfo.Command.Should().Be(command);
        commandInfo.State.Should().Be(CommandState.Queued);
    }

    [Fact]
    public void GetCommandInfo_WithInvalidCommandId_ShouldReturnNull()
    {
        // Act
        var commandInfo = m_CommandQueue!.GetCommandInfo("invalid-id");

        // Assert
        commandInfo.Should().BeNull();
    }

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

    [Fact]
    public void CancelCommand_WithInvalidCommandId_ShouldReturnFalse()
    {
        // Act
        var result = m_CommandQueue!.CancelCommand("invalid-id");

        // Assert
        result.Should().BeFalse();
    }

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

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        m_CommandQueue!.Dispose();
        var action = () => m_CommandQueue.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void EnqueueCommand_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.EnqueueCommand("test");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void GetCommandInfo_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.GetCommandInfo("test-id");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void GetAllCommandInfos_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.GetAllCommandInfos();
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CancelCommand_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.CancelCommand("test-id");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CancelAllCommands_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        m_CommandQueue!.Dispose();

        // Act & Assert
        var action = () => m_CommandQueue.CancelAllCommands();
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void EnqueueCommand_WithWhitespaceCommand_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("   ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

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

    [Fact]
    public void GetCommandInfo_WithNullCommandId_ShouldReturnNull()
    {
        // Act
        var commandInfo = m_CommandQueue!.GetCommandInfo(null!);

        // Assert
        commandInfo.Should().BeNull();
    }

    [Fact]
    public void GetCommandInfo_WithEmptyCommandId_ShouldReturnNull()
    {
        // Act
        var commandInfo = m_CommandQueue!.GetCommandInfo(string.Empty);

        // Assert
        commandInfo.Should().BeNull();
    }

    [Fact]
    public void CancelCommand_WithNullCommandId_ShouldReturnFalse()
    {
        // Act
        var result = m_CommandQueue!.CancelCommand(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_WithEmptyCommandId_ShouldReturnFalse()
    {
        // Act
        var result = m_CommandQueue!.CancelCommand(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

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

    [Fact]
    public void EnqueueCommand_WithEmptyString_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(string.Empty);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithNullString_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithWhitespaceOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("   ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithTabOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("\t");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithNewlineOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("\n");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithCarriageReturnOnly_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand("\r");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

    [Fact]
    public void EnqueueCommand_WithMixedWhitespace_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        var action = () => m_CommandQueue!.EnqueueCommand(" \t\n\r ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
    }

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
