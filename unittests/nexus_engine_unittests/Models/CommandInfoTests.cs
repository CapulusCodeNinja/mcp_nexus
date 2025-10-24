using FluentAssertions;

using Nexus.Engine.Models;

using Xunit;

namespace Nexus.Engine.Unittests.Models;

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
        _ = commandInfo.CommandId.Should().Be(m_TestCommandId);
        _ = commandInfo.Command.Should().Be(m_TestCommand);
        _ = commandInfo.State.Should().Be(CommandState.Queued);
        _ = commandInfo.QueuedTime.Should().Be(m_TestTime);
        _ = commandInfo.StartTime.Should().BeNull();
        _ = commandInfo.EndTime.Should().BeNull();
        _ = commandInfo.Output.Should().BeNull();
        _ = commandInfo.IsSuccess.Should().BeNull();
        _ = commandInfo.ErrorMessage.Should().BeNull();
        _ = commandInfo.ExecutionTime.Should().BeNull();
        _ = commandInfo.TotalTime.Should().BeNull();
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
        _ = commandInfo.CommandId.Should().Be(m_TestCommandId);
        _ = commandInfo.Command.Should().Be(m_TestCommand);
        _ = commandInfo.State.Should().Be(CommandState.Executing);
        _ = commandInfo.QueuedTime.Should().Be(m_TestTime);
        _ = commandInfo.StartTime.Should().Be(startTime);
        _ = commandInfo.EndTime.Should().BeNull();
        _ = commandInfo.Output.Should().BeNull();
        _ = commandInfo.IsSuccess.Should().BeNull();
        _ = commandInfo.ErrorMessage.Should().BeNull();
        _ = commandInfo.ExecutionTime.Should().BeNull();
        _ = commandInfo.TotalTime.Should().BeNull();
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
        _ = commandInfo.CommandId.Should().Be(m_TestCommandId);
        _ = commandInfo.Command.Should().Be(m_TestCommand);
        _ = commandInfo.State.Should().Be(CommandState.Completed);
        _ = commandInfo.QueuedTime.Should().Be(m_TestTime);
        _ = commandInfo.StartTime.Should().Be(startTime);
        _ = commandInfo.EndTime.Should().Be(endTime);
        _ = commandInfo.Output.Should().Be(output);
        _ = commandInfo.IsSuccess.Should().Be(isSuccess);
        _ = commandInfo.ErrorMessage.Should().BeNull();
        _ = commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(4));
        _ = commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(5));
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
        _ = commandInfo.CommandId.Should().Be(m_TestCommandId);
        _ = commandInfo.Command.Should().Be(m_TestCommand);
        _ = commandInfo.State.Should().Be(CommandState.Failed);
        _ = commandInfo.QueuedTime.Should().Be(m_TestTime);
        _ = commandInfo.StartTime.Should().Be(startTime);
        _ = commandInfo.EndTime.Should().Be(endTime);
        _ = commandInfo.Output.Should().Be(output);
        _ = commandInfo.IsSuccess.Should().Be(isSuccess);
        _ = commandInfo.ErrorMessage.Should().Be(errorMessage);
        _ = commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(4));
        _ = commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(5));
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
        _ = commandInfo.CommandId.Should().Be(m_TestCommandId);
        _ = commandInfo.Command.Should().Be(m_TestCommand);
        _ = commandInfo.State.Should().Be(CommandState.Cancelled);
        _ = commandInfo.QueuedTime.Should().Be(m_TestTime);
        _ = commandInfo.StartTime.Should().Be(startTime);
        _ = commandInfo.EndTime.Should().Be(endTime);
        _ = commandInfo.Output.Should().BeNull();
        _ = commandInfo.IsSuccess.Should().Be(false);
        _ = commandInfo.ErrorMessage.Should().Be("Command was cancelled");
        _ = commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(2));
        _ = commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(3));
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
        _ = commandInfo.CommandId.Should().Be(m_TestCommandId);
        _ = commandInfo.Command.Should().Be(m_TestCommand);
        _ = commandInfo.State.Should().Be(CommandState.Timeout);
        _ = commandInfo.QueuedTime.Should().Be(m_TestTime);
        _ = commandInfo.StartTime.Should().Be(startTime);
        _ = commandInfo.EndTime.Should().Be(endTime);
        _ = commandInfo.Output.Should().BeNull();
        _ = commandInfo.IsSuccess.Should().Be(false);
        _ = commandInfo.ErrorMessage.Should().Be(errorMessage);
        _ = commandInfo.ExecutionTime.Should().Be(TimeSpan.FromSeconds(9));
        _ = commandInfo.TotalTime.Should().Be(TimeSpan.FromSeconds(10));
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
        _ = commandInfo.ExecutionTime.Should().BeNull();
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
        _ = commandInfo.ExecutionTime.Should().BeNull();
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
        _ = commandInfo.TotalTime.Should().BeNull();
    }
}
