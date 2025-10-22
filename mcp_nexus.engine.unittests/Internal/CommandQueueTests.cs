using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Engine.Internal;
using mcp_nexus.Engine.Models;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Utilities.FileSystem;
using mcp_nexus.Utilities.ProcessManagement;

namespace mcp_nexus.Engine.UnitTests.Internal;

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
}
