using FluentAssertions;

using Nexus.Engine.Share.Models;

using Xunit;

namespace Nexus.Engine.Unittests.Models;

/// <summary>
/// Unit tests for CommandInfo class.
/// Tests factory methods, computed properties, and state transitions.
/// </summary>
public class CommandInfoTests
{
    /// <summary>
    /// Verifies that Enqueued factory method creates correct state.
    /// </summary>
    [Fact]
    public void Enqueued_CreatesCommandInfoWithQueuedState()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-123";
        var command = "k";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);

        // Act
        var result = CommandInfo.Enqueued(sessionId, commandId, command, queuedTime, null);

        // Assert
        _ = result.SessionId.Should().Be(sessionId);
        _ = result.CommandId.Should().Be(commandId);
        _ = result.Command.Should().Be(command);
        _ = result.State.Should().Be(CommandState.Queued);
        _ = result.QueuedTime.Should().Be(queuedTime);
        _ = result.StartTime.Should().BeNull();
        _ = result.EndTime.Should().BeNull();
        _ = result.AggregatedOutput.Should().Be(string.Empty);
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.ErrorMessage.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that Executing factory method creates correct state.
    /// </summary>
    [Fact]
    public void Executing_CreatesCommandInfoWithExecutingState()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-123";
        var command = "lm";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);

        // Act
        var result = CommandInfo.Executing(sessionId, commandId, command, queuedTime, startTime, null);

        // Assert
        _ = result.SessionId.Should().Be(sessionId);
        _ = result.CommandId.Should().Be(commandId);
        _ = result.Command.Should().Be(command);
        _ = result.State.Should().Be(CommandState.Executing);
        _ = result.QueuedTime.Should().Be(queuedTime);
        _ = result.StartTime.Should().Be(startTime);
        _ = result.EndTime.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Completed factory method with success creates correct state.
    /// </summary>
    [Fact]
    public void Completed_WithSuccess_CreatesCommandInfoWithCompletedState()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-123";
        var command = "!analyze -v";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 15);
        var output = "Analysis output";

        // Act
        var result = CommandInfo.Completed(sessionId, commandId, command, queuedTime, startTime, endTime, output, string.Empty, null);

        // Assert
        _ = result.SessionId.Should().Be(sessionId);
        _ = result.CommandId.Should().Be(commandId);
        _ = result.State.Should().Be(CommandState.Completed);
        _ = result.QueuedTime.Should().Be(queuedTime);
        _ = result.StartTime.Should().Be(startTime);
        _ = result.EndTime.Should().Be(endTime);
        _ = result.AggregatedOutput.Should().Be(output);
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.ErrorMessage.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that Failed factory method with failure creates correct state.
    /// </summary>
    [Fact]
    public void Failed_WithFailure_CreatesCommandInfoWithFailedState()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-123";
        var command = "invalid";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 6);
        var output = "";
        var errorMessage = "Command not recognized";

        // Act
        var result = CommandInfo.Failed(sessionId, commandId, command, queuedTime, startTime, endTime, output, errorMessage, null);

        // Assert
        _ = result.SessionId.Should().Be(sessionId);
        _ = result.CommandId.Should().Be(commandId);
        _ = result.State.Should().Be(CommandState.Failed);
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.ErrorMessage.Should().Be(errorMessage);
    }

    /// <summary>
    /// Verifies that Cancelled factory method creates correct state.
    /// </summary>
    [Fact]
    public void Cancelled_CreatesCommandInfoWithCancelledState()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-123";
        var command = "k";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 7);

        // Act
        var result = CommandInfo.Cancelled(sessionId, commandId, command, queuedTime, startTime, endTime, string.Empty, string.Empty, null);

        // Assert
        _ = result.SessionId.Should().Be(sessionId);
        _ = result.CommandId.Should().Be(commandId);
        _ = result.State.Should().Be(CommandState.Cancelled);
        _ = result.QueuedTime.Should().Be(queuedTime);
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain("Command was cancelled");
    }

    /// <summary>
    /// Verifies that Cancelled factory method with start and end times.
    /// </summary>
    [Fact]
    public void Cancelled_WithStartAndEndTimes_SetsTimesCorrectly()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-123";
        var command = "k";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 7);

        // Act
        var result = CommandInfo.Cancelled(sessionId, commandId, command, queuedTime, startTime, endTime, string.Empty, string.Empty, null);

        // Assert
        _ = result.StartTime.Should().Be(startTime);
        _ = result.EndTime.Should().Be(endTime);
    }

    /// <summary>
    /// Verifies that TimedOut factory method creates correct state.
    /// </summary>
    [Fact]
    public void TimedOut_CreatesCommandInfoWithTimeoutState()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-123";
        var command = "!analyze -v";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);
        var endTime = new DateTime(2025, 1, 15, 10, 35, 5);
        var errorMessage = "Command timed out after 300 seconds";

        // Act
        var result = CommandInfo.TimedOut(sessionId, commandId, command, queuedTime, startTime, endTime, string.Empty, errorMessage, null);

        // Assert
        _ = result.SessionId.Should().Be(sessionId);
        _ = result.CommandId.Should().Be(commandId);
        _ = result.State.Should().Be(CommandState.Timeout);
        _ = result.QueuedTime.Should().Be(queuedTime);
        _ = result.StartTime.Should().Be(startTime);
        _ = result.EndTime.Should().Be(endTime);
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.ErrorMessage.Should().Contain(errorMessage);
    }

    /// <summary>
    /// Verifies that ExecutionTime is null when not started.
    /// </summary>
    [Fact]
    public void ExecutionTime_WhenNotStarted_ReturnsNull()
    {
        // Arrange
        var info = CommandInfo.Enqueued("session-123", "cmd-123", "k", DateTime.Now, null);

        // Act
        var result = info.ExecutionTime;

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ExecutionTime is null when started but not ended.
    /// </summary>
    [Fact]
    public void ExecutionTime_WhenStartedButNotEnded_ReturnsNull()
    {
        // Arrange
        var info = CommandInfo.Executing("session-123", "cmd-123", "k", DateTime.Now, DateTime.Now.AddSeconds(5), null);

        // Act
        var result = info.ExecutionTime;

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ExecutionTime calculates correctly when completed.
    /// </summary>
    [Fact]
    public void ExecutionTime_WhenCompleted_CalculatesCorrectDuration()
    {
        // Arrange
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 15);
        var info = CommandInfo.Completed("session-123", "cmd-123", "k", queuedTime, startTime, endTime, "output", string.Empty, null);

        // Act
        var result = info.ExecutionTime;

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Value.TotalSeconds.Should().Be(10);
    }

    /// <summary>
    /// Verifies that TotalTime is null when not completed.
    /// </summary>
    [Fact]
    public void TotalTime_WhenNotCompleted_ReturnsNull()
    {
        // Arrange
        var info = CommandInfo.Enqueued("session-123", "cmd-123", "k", DateTime.Now, null);

        // Act
        var result = info.TotalTime;

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that TotalTime calculates correctly from queue to completion.
    /// </summary>
    [Fact]
    public void TotalTime_WhenCompleted_CalculatesFromQueueToEnd()
    {
        // Arrange
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 5);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 15);
        var info = CommandInfo.Completed("session-123", "cmd-123", "k", queuedTime, startTime, endTime, "output", string.Empty, null);

        // Act
        var result = info.TotalTime;

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Value.TotalSeconds.Should().Be(15);
    }

    /// <summary>
    /// Verifies that TotalTime includes queue wait time.
    /// </summary>
    [Fact]
    public void TotalTime_IncludesQueueWaitTime()
    {
        // Arrange - Command queued, waited 10s, executed for 5s
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 10);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 15);
        var info = CommandInfo.Completed("session-123", "cmd-123", "k", queuedTime, startTime, endTime, "output", string.Empty, null);

        // Act
        var executionTime = info.ExecutionTime;
        var totalTime = info.TotalTime;

        // Assert
        _ = executionTime!.Value.TotalSeconds.Should().Be(5);
        _ = totalTime!.Value.TotalSeconds.Should().Be(15);
    }

    /// <summary>
    /// Verifies that Completed without error message sets empty string.
    /// </summary>
    [Fact]
    public void Completed_WithoutErrorMessage_SetsErrorMessageToEmptyString()
    {
        // Arrange & Act
        var result = CommandInfo.Completed("session-123", "cmd-123", "k", DateTime.Now, DateTime.Now, DateTime.Now, "output", string.Empty, null);

        // Assert
        _ = result.ErrorMessage.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that Cancelled sets error message correctly.
    /// </summary>
    [Fact]
    public void Cancelled_SetsErrorMessageCorrectly()
    {
        // Arrange & Act
        var result = CommandInfo.Cancelled("session-123", "cmd-123", "k", DateTime.Now, DateTime.Now, DateTime.Now, string.Empty, string.Empty, null);

        // Assert
        _ = result.StartTime.Should().NotBeNull();
        _ = result.EndTime.Should().NotBeNull();
        _ = result.ErrorMessage.Should().Contain("Command was cancelled");
    }

    /// <summary>
    /// Verifies ExecutionTime with exact second boundaries.
    /// </summary>
    [Fact]
    public void ExecutionTime_WithExactSecondBoundaries_CalculatesCorrectly()
    {
        // Arrange - Exactly 60 seconds
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 0);
        var endTime = new DateTime(2025, 1, 15, 10, 31, 0);
        var info = CommandInfo.Completed("session-123", "cmd-123", "k", queuedTime, startTime, endTime, "output", string.Empty, null);

        // Act
        var result = info.ExecutionTime;

        // Assert
        _ = result!.Value.TotalSeconds.Should().Be(60);
        _ = result.Value.TotalMinutes.Should().Be(1);
    }

    /// <summary>
    /// Verifies TotalTime with millisecond precision.
    /// </summary>
    [Fact]
    public void TotalTime_WithMillisecondPrecision_CalculatesCorrectly()
    {
        // Arrange
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0, 0);
        var startTime = new DateTime(2025, 1, 15, 10, 30, 0, 100);
        var endTime = new DateTime(2025, 1, 15, 10, 30, 0, 500);
        var info = CommandInfo.Completed("session-123", "cmd-123", "k", queuedTime, startTime, endTime, "output", string.Empty, null);

        // Act
        var result = info.TotalTime;

        // Assert
        _ = result!.Value.TotalMilliseconds.Should().Be(500);
    }

    /// <summary>
    /// Verifies that factory methods preserve all parameters.
    /// </summary>
    [Fact]
    public void FactoryMethods_PreserveAllParameters()
    {
        // Arrange
        var sessionId = "session-very-long-id-12345";
        var commandId = "cmd-very-long-id-12345";
        var command = "!analyze -v -hang -f";
        var queuedTime = new DateTime(2025, 1, 15, 10, 30, 0);

        // Act
        var result = CommandInfo.Enqueued(sessionId, commandId, command, queuedTime, null);

        // Assert - Verify no truncation or modification
        _ = result.SessionId.Should().Be(sessionId);
        _ = result.CommandId.Should().Be(commandId);
        _ = result.Command.Should().Be(command);
    }
}
