using FluentAssertions;
using Xunit;
using nexus.engine.Models;

namespace nexus.engine.unittests.Internal;

/// <summary>
/// Unit tests for the QueuedCommand class.
/// </summary>
public class QueuedCommandTests
{
    private readonly DateTime m_TestTime = new(2024, 1, 15, 10, 30, 0);
    private readonly string m_TestCommandId = "cmd-123";
    private readonly string m_TestCommand = "lm";

    /// <summary>
    /// Verifies that QueuedCommand can be constructed with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
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

    /// <summary>
    /// Verifies that the State property can be set and updated correctly.
    /// </summary>
    [Fact]
    public void State_WhenSet_ShouldUpdateCorrectly()
    {
        // Arrange
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
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

    /// <summary>
    /// Verifies that CompletionSource is initialized and not completed by default.
    /// </summary>
    [Fact]
    public void CompletionSource_ShouldBeInitialized()
    {
        // Arrange
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };

        // Act & Assert
        // CompletionSource is initialized by default
        queuedCommand.CompletionSource.Task.IsCompleted.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancellationTokenSource is initialized and not cancelled by default.
    /// </summary>
    [Fact]
    public void CancellationTokenSource_ShouldBeInitialized()
    {
        // Arrange
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
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

    /// <summary>
    /// Verifies that CancellationTokenSource reflects cancellation state when cancelled.
    /// </summary>
    [Fact]
    public void CancellationTokenSource_WhenCancelled_ShouldReflectCancellation()
    {
        // Arrange
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
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

    /// <summary>
    /// Verifies that CompletionSource task completes successfully when result is set.
    /// </summary>
    [Fact]
    public async Task CompletionSource_WhenSetResult_ShouldCompleteTask()
    {
        // Arrange
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
        {
            Id = m_TestCommandId,
            Command = m_TestCommand,
            QueuedTime = m_TestTime
        };
        var commandInfo = nexus.engine.unittests.TestHelpers.TestDataBuilder.CreateCompletedCommandInfo(m_TestCommandId, m_TestCommand);

        // Act
        queuedCommand.CompletionSource.SetResult(commandInfo);

        // Assert
        queuedCommand.CompletionSource.Task.IsCompleted.Should().BeTrue();
        var result = await queuedCommand.CompletionSource.Task;
        result.Should().Be(commandInfo);
    }

    /// <summary>
    /// Verifies that CompletionSource task completes with exception when exception is set.
    /// </summary>
    [Fact]
    public void CompletionSource_WhenSetException_ShouldCompleteTaskWithException()
    {
        // Arrange
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
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

    /// <summary>
    /// Verifies that CompletionSource task completes as canceled when SetCanceled is called.
    /// </summary>
    [Fact]
    public void CompletionSource_WhenSetCanceled_ShouldCompleteTaskAsCanceled()
    {
        // Arrange
        var queuedCommand = new nexus.engine.Internal.QueuedCommand
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
