using FluentAssertions;
using Xunit;
using nexus.engine.Models;

namespace nexus.engine.unittests.Models;

/// <summary>
/// Unit tests for the CommandInfo class.
/// </summary>
public class CommandInfoTests
{
    private readonly DateTime m_TestTime = new(2024, 1, 15, 10, 30, 0);
    private readonly string m_TestCommandId = "cmd-123";
    private readonly string m_TestCommand = "lm";

    /// <summary>
    /// Verifies that Queued factory method creates a queued CommandInfo correctly.
    /// </summary>
    [Fact]
    public void Queued_WithValidParameters_ShouldCreateQueuedCommandInfo()
    {
        // Act
        var commandInfo = CommandInfo.Queued(m_TestCommandId, m_TestCommand, m_TestTime);

        // Assert
        commandInfo.CommandId.Should().Be(m_TestCommandId);
        commandInfo.Command.Should().Be(m_TestCommand);
        commandInfo.State.Should().Be(CommandState.Queued);
        commandInfo.QueuedTime.Should().Be(m_TestTime);
        commandInfo.StartTime.Should().BeNull();
        commandInfo.EndTime.Should().BeNull();
        commandInfo.Output.Should().BeNull();
        commandInfo.IsSuccess.Should().BeNull();
        commandInfo.ErrorMessage.Should().BeNull();
        commandInfo.ExecutionTime.Should().BeNull();
        commandInfo.TotalTime.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Executing factory method creates an executing CommandInfo correctly.
    /// </summary>
    [Fact]
    public void Executing_WithValidParameters_ShouldCreateExecutingCommandInfo()
    {
        // Arrange
        var startTime = m_TestTime.AddSeconds(1);

        // Act
        var commandInfo = CommandInfo.Executing(m_TestCommandId, m_TestCommand, m_TestTime, startTime);

        // Assert
        commandInfo.CommandId.Should().Be(m_TestCommandId);
        commandInfo.Command.Should().Be(m_TestCommand);
        commandInfo.State.Should().Be(CommandState.Executing);
        commandInfo.QueuedTime.Should().Be(m_TestTime);
        commandInfo.StartTime.Should().Be(startTime);
        commandInfo.EndTime.Should().BeNull();
        commandInfo.Output.Should().BeNull();
        commandInfo.IsSuccess.Should().BeNull();
        commandInfo.ErrorMessage.Should().BeNull();
        commandInfo.ExecutionTime.Should().BeNull();
        commandInfo.TotalTime.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Completed factory method creates a successful completed CommandInfo correctly.
    /// </summary>
    [Fact]
    public void Completed_WithSuccessfulCommand_ShouldCreateCompletedCommandInfo()
    {
        // Arrange
        var startTime = m_TestTime.AddSeconds(1);
        var endTime = m_TestTime.AddSeconds(5);
        var output = "Module list output";
        var isSuccess = true;

        // Act
        var commandInfo = CommandInfo.Completed(m_TestCommandId, m_TestCommand, m_TestTime, startTime, endTime, output, isSuccess);

        // Assert
        commandInfo.CommandId.Should().Be(m_TestCommandId);
        commandInfo.Command.Should().Be(m_TestCommand);
        commandInfo.State.Should().Be(CommandState.Completed);
        commandInfo.QueuedTime.Should().Be(m_TestTime);
        commandInfo.StartTime.Should().Be(startTime);
        commandInfo.EndTime.Should().Be(endTime);
        commandInfo.Output.Should().Be(output);
        commandInfo.IsSuccess.Should().Be(isSuccess);
        commandInfo.ErrorMessage.Should().BeNull();
        commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(4));
        commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that Completed factory method creates a failed CommandInfo correctly.
    /// </summary>
    [Fact]
    public void Completed_WithFailedCommand_ShouldCreateFailedCommandInfo()
    {
        // Arrange
        var startTime = m_TestTime.AddSeconds(1);
        var endTime = m_TestTime.AddSeconds(5);
        var output = string.Empty;
        var isSuccess = false;
        var errorMessage = "Command failed";

        // Act
        var commandInfo = CommandInfo.Completed(m_TestCommandId, m_TestCommand, m_TestTime, startTime, endTime, output, isSuccess, errorMessage);

        // Assert
        commandInfo.CommandId.Should().Be(m_TestCommandId);
        commandInfo.Command.Should().Be(m_TestCommand);
        commandInfo.State.Should().Be(CommandState.Failed);
        commandInfo.QueuedTime.Should().Be(m_TestTime);
        commandInfo.StartTime.Should().Be(startTime);
        commandInfo.EndTime.Should().Be(endTime);
        commandInfo.Output.Should().Be(output);
        commandInfo.IsSuccess.Should().Be(isSuccess);
        commandInfo.ErrorMessage.Should().Be(errorMessage);
        commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(4));
        commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that Cancelled factory method creates a cancelled CommandInfo correctly.
    /// </summary>
    [Fact]
    public void Cancelled_WithValidParameters_ShouldCreateCancelledCommandInfo()
    {
        // Arrange
        var startTime = m_TestTime.AddSeconds(1);
        var endTime = m_TestTime.AddSeconds(3);

        // Act
        var commandInfo = CommandInfo.Cancelled(m_TestCommandId, m_TestCommand, m_TestTime, startTime, endTime);

        // Assert
        commandInfo.CommandId.Should().Be(m_TestCommandId);
        commandInfo.Command.Should().Be(m_TestCommand);
        commandInfo.State.Should().Be(CommandState.Cancelled);
        commandInfo.QueuedTime.Should().Be(m_TestTime);
        commandInfo.StartTime.Should().Be(startTime);
        commandInfo.EndTime.Should().Be(endTime);
        commandInfo.Output.Should().BeNull();
        commandInfo.IsSuccess.Should().Be(false);
        commandInfo.ErrorMessage.Should().Be("Command was cancelled");
        commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(2));
        commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Verifies that TimedOut factory method creates a timed out CommandInfo correctly.
    /// </summary>
    [Fact]
    public void TimedOut_WithValidParameters_ShouldCreateTimedOutCommandInfo()
    {
        // Arrange
        var startTime = m_TestTime.AddSeconds(1);
        var endTime = m_TestTime.AddSeconds(10);
        var errorMessage = "Command timed out after 5 minutes";

        // Act
        var commandInfo = CommandInfo.TimedOut(m_TestCommandId, m_TestCommand, m_TestTime, startTime, endTime, errorMessage);

        // Assert
        commandInfo.CommandId.Should().Be(m_TestCommandId);
        commandInfo.Command.Should().Be(m_TestCommand);
        commandInfo.State.Should().Be(CommandState.Timeout);
        commandInfo.QueuedTime.Should().Be(m_TestTime);
        commandInfo.StartTime.Should().Be(startTime);
        commandInfo.EndTime.Should().Be(endTime);
        commandInfo.Output.Should().BeNull();
        commandInfo.IsSuccess.Should().Be(false);
        commandInfo.ErrorMessage.Should().Be(errorMessage);
        commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(9));
        commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Verifies that ExecutionTime returns null when StartTime is null.
    /// </summary>
    [Fact]
    public void ExecutionTime_WhenStartTimeIsNull_ShouldReturnNull()
    {
        // Arrange
        var commandInfo = CommandInfo.Queued(m_TestCommandId, m_TestCommand, m_TestTime);

        // Act & Assert
        commandInfo.ExecutionTime.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ExecutionTime returns null when EndTime is null.
    /// </summary>
    [Fact]
    public void ExecutionTime_WhenEndTimeIsNull_ShouldReturnNull()
    {
        // Arrange
        var startTime = m_TestTime.AddSeconds(1);
        var commandInfo = CommandInfo.Executing(m_TestCommandId, m_TestCommand, m_TestTime, startTime);

        // Act & Assert
        commandInfo.ExecutionTime.Should().BeNull();
    }

    /// <summary>
    /// Verifies that TotalTime returns null when EndTime is null.
    /// </summary>
    [Fact]
    public void TotalTime_WhenEndTimeIsNull_ShouldReturnNull()
    {
        // Arrange
        var startTime = m_TestTime.AddSeconds(1);
        var commandInfo = CommandInfo.Executing(m_TestCommandId, m_TestCommand, m_TestTime, startTime);

        // Act & Assert
        commandInfo.TotalTime.Should().BeNull();
    }
}
