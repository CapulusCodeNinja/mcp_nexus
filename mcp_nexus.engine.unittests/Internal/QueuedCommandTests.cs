using FluentAssertions;
using Xunit;
using mcp_nexus.Engine.Models;

namespace mcp_nexus.Engine.UnitTests.Internal;

/// <summary>
/// Unit tests for the QueuedCommand class.
/// </summary>
public class QueuedCommandTests
{
    private readonly DateTime m_TestTime = new(2024, 1, 15, 10, 30, 0);
    private readonly string m_TestCommandId = "cmd-123";
    private readonly string m_TestCommand = "lm";

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };

        // Assert
        queuedCommand.Should().NotBeNull();
        queuedCommand.Id.Should().Be(m_TestCommandId);
        queuedCommand.Command.Should().Be(m_TestCommand);
        queuedCommand.QueuedTime.Should().Be(m_TestTime);
        queuedCommand.State.Should().Be(CommandState.Queued);
        // CompletionSource is initialized by default
        queuedCommand.CancellationTokenSource.Should().NotBeNull();
    }

    [Fact]
    public void State_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };

        // Act
        queuedCommand.State = CommandState.Executing;

        // Assert
        queuedCommand.State.Should().Be(CommandState.Executing);
    }

    [Fact]
    public void CompletionSource_ShouldBeInitialized()
    {
        // Arrange
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };

        // Act & Assert
        // CompletionSource is initialized by default
        queuedCommand.CompletionSource.Task.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void CancellationTokenSource_ShouldBeInitialized()
    {
        // Arrange
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };

        // Act & Assert
        queuedCommand.CancellationTokenSource.Should().NotBeNull();
        queuedCommand.CancellationTokenSource.Token.Should().NotBeNull();
        queuedCommand.CancellationTokenSource.Token.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void CancellationTokenSource_WhenCancelled_ShouldReflectCancellation()
    {
        // Arrange
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };

        // Act
        queuedCommand.CancellationTokenSource.Cancel();

        // Assert
        queuedCommand.CancellationTokenSource.Token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task CompletionSource_WhenSetResult_ShouldCompleteTask()
    {
        // Arrange
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };
        var commandInfo = mcp_nexus.Engine.UnitTests.TestHelpers.TestDataBuilder.CreateCompletedCommandInfo(m_TestCommandId, m_TestCommand);

        // Act
        queuedCommand.CompletionSource.SetResult(commandInfo);

        // Assert
        queuedCommand.CompletionSource.Task.IsCompleted.Should().BeTrue();
        var result = await queuedCommand.CompletionSource.Task;
        result.Should().Be(commandInfo);
    }

    [Fact]
    public void CompletionSource_WhenSetException_ShouldCompleteTaskWithException()
    {
        // Arrange
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };
        var exception = new InvalidOperationException("Test exception");

        // Act
        queuedCommand.CompletionSource.SetException(exception);

        // Assert
        queuedCommand.CompletionSource.Task.IsCompleted.Should().BeTrue();
        queuedCommand.CompletionSource.Task.IsFaulted.Should().BeTrue();
        queuedCommand.CompletionSource.Task.Exception!.InnerException.Should().Be(exception);
    }

    [Fact]
    public void CompletionSource_WhenSetCanceled_ShouldCompleteTaskAsCanceled()
    {
        // Arrange
        var queuedCommand = new mcp_nexus.Engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };

        // Act
        queuedCommand.CompletionSource.SetCanceled();

        // Assert
        queuedCommand.CompletionSource.Task.IsCompleted.Should().BeTrue();
        queuedCommand.CompletionSource.Task.IsCanceled.Should().BeTrue();
    }
}
