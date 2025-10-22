using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using Xunit;
using mcp_nexus.Engine.Configuration;
using mcp_nexus.Engine.Models;
using mcp_nexus.Engine.UnitTests.TestHelpers;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Unit tests for the CommandQueue class.
/// </summary>
public class CommandQueueTests : IDisposable
{
    private readonly ILoggerFactory m_LoggerFactory;
    private readonly DebugEngineConfiguration m_Configuration;
    private readonly string m_TestSessionId = "sess-test-123";
    private mcp_nexus.Engine.Internal.CommandQueue? m_CommandQueue;

    public CommandQueueTests()
    {
        m_LoggerFactory = NullLoggerFactory.Instance;
        m_Configuration = TestDataBuilder.CreateDebugEngineConfiguration();
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CommandQueue>();
        m_CommandQueue = new mcp_nexus.Engine.Internal.CommandQueue(
            m_TestSessionId, 
            m_Configuration, 
            logger);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        m_CommandQueue.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSessionId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CommandQueue>();
        var action = () => new mcp_nexus.Engine.Internal.CommandQueue(
            null!, 
            m_Configuration, 
            logger);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sessionId");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var logger = m_LoggerFactory.CreateLogger<mcp_nexus.Engine.Internal.CommandQueue>();
        var action = () => new mcp_nexus.Engine.Internal.CommandQueue(
            m_TestSessionId, 
            null!, 
            logger);
        
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new mcp_nexus.Engine.Internal.CommandQueue(
            m_TestSessionId, 
            m_Configuration, 
            null!);
        
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
        commandId.Should().NotBeNull();
        commandId.Should().NotBeEmpty();
        commandId.Should().StartWith("cmd-");
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
        var action = () => m_CommandQueue!.EnqueueCommand("");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("command");
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

    [Fact]
    public void GetCommandInfo_WithInvalidCommandId_ShouldReturnNull()
    {
        // Act
        var commandInfo = m_CommandQueue!.GetCommandInfo("invalid-id");

        // Assert
        commandInfo.Should().BeNull();
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
    public void GetAllCommandInfos_WithNoCommands_ShouldReturnEmptyDictionary()
    {
        // Act
        var commandInfos = m_CommandQueue!.GetAllCommandInfos();

        // Assert
        commandInfos.Should().NotBeNull();
        commandInfos.Should().BeEmpty();
    }

    [Fact]
    public void GetAllCommandInfos_WithQueuedCommands_ShouldReturnCommandInfos()
    {
        // Arrange
        var command1 = "lm";
        var command2 = "!threads";
        var commandId1 = m_CommandQueue!.EnqueueCommand(command1);
        var commandId2 = m_CommandQueue.EnqueueCommand(command2);

        // Act
        var commandInfos = m_CommandQueue.GetAllCommandInfos();

        // Assert
        commandInfos.Should().NotBeNull();
        commandInfos.Should().HaveCount(2);
        commandInfos.Should().ContainKey(commandId1);
        commandInfos.Should().ContainKey(commandId2);
        commandInfos[commandId1].Command.Should().Be(command1);
        commandInfos[commandId2].Command.Should().Be(command2);
    }

    [Fact]
    public void CancelCommand_WithValidCommandId_ShouldReturnTrue()
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
    public void CancelAllCommands_WithNoCommands_ShouldReturnZero()
    {
        // Act
        var count = m_CommandQueue!.CancelAllCommands();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void CancelAllCommands_WithQueuedCommands_ShouldReturnCount()
    {
        // Arrange
        m_CommandQueue!.EnqueueCommand("lm");
        m_CommandQueue.EnqueueCommand("!threads");

        // Act
        var count = m_CommandQueue.CancelAllCommands();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void CancelAllCommands_WithReason_ShouldReturnCount()
    {
        // Arrange
        m_CommandQueue!.EnqueueCommand("lm");
        m_CommandQueue.EnqueueCommand("!threads");

        // Act
        var count = m_CommandQueue.CancelAllCommands("Test cancellation");

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void Dispose_WhenCalled_ShouldDisposeQueue()
    {
        // Act
        m_CommandQueue!.Dispose();

        // Assert
        var action = () => m_CommandQueue.EnqueueCommand("test");
        action.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        var action = () =>
        {
            m_CommandQueue!.Dispose();
            m_CommandQueue.Dispose();
        };
        action.Should().NotThrow();
    }

    public void Dispose()
    {
        m_CommandQueue?.Dispose();
    }
}
