using Xunit;
using FluentAssertions;
using nexus.engine.Events;
using nexus.engine.Models;

namespace nexus.engine.unittests.Events;

/// <summary>
/// Unit tests for the CommandStateChangedEventArgs class.
/// </summary>
public class CommandStateChangedEventArgsTests
{
    private readonly DateTime m_TestTime = new(2024, 1, 15, 10, 30, 0);
    private readonly string m_SessionId = "sess-123";
    private readonly string m_CommandId = "cmd-456";
    private readonly string m_Command = "lm";

    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Act
        var args = new CommandStateChangedEventArgs
        {
            SessionId = m_SessionId,
            CommandId = m_CommandId,
            OldState = CommandState.Queued,
            NewState = CommandState.Executing,
            Timestamp = m_TestTime,
            Command = m_Command
        };

        // Assert
        args.SessionId.Should().Be(m_SessionId);
        args.CommandId.Should().Be(m_CommandId);
        args.OldState.Should().Be(CommandState.Queued);
        args.NewState.Should().Be(CommandState.Executing);
        args.Timestamp.Should().Be(m_TestTime);
        args.Command.Should().Be(m_Command);
    }

    [Fact]
    public void Constructor_WithNullCommand_ShouldAllowNull()
    {
        // Act
        var args = new CommandStateChangedEventArgs
        {
            SessionId = m_SessionId,
            CommandId = m_CommandId,
            OldState = CommandState.Queued,
            NewState = CommandState.Executing,
            Timestamp = m_TestTime,
            Command = null
        };

        // Assert
        args.Command.Should().BeNull();
    }

    [Theory]
    [InlineData(CommandState.Queued, CommandState.Executing)]
    [InlineData(CommandState.Executing, CommandState.Completed)]
    [InlineData(CommandState.Executing, CommandState.Failed)]
    [InlineData(CommandState.Executing, CommandState.Cancelled)]
    [InlineData(CommandState.Executing, CommandState.Timeout)]
    public void StateTransitions_ShouldBeValid(CommandState oldState, CommandState newState)
    {
        // Act
        var args = new CommandStateChangedEventArgs
        {
            SessionId = m_SessionId,
            CommandId = m_CommandId,
            OldState = oldState,
            NewState = newState,
            Timestamp = m_TestTime,
            Command = m_Command
        };

        // Assert
        args.OldState.Should().Be(oldState);
        args.NewState.Should().Be(newState);
    }
}
