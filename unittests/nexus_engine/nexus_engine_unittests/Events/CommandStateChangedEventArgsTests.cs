using FluentAssertions;

using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;

using Xunit;

namespace Nexus.Engine.Unittests.Events;

/// <summary>
/// Unit tests for the CommandStateChangedEventArgs class.
/// </summary>
public class CommandStateChangedEventArgsTests
{
    private readonly DateTime m_TestTime = new(2024, 1, 15, 10, 30, 0);
    private readonly string m_SessionId = "sess-123";
    private readonly string m_CommandId = "cmd-456";
    private readonly string m_Command = "lm";

    /// <summary>
    /// Verifies that the constructor correctly sets all properties when provided with valid parameters.
    /// </summary>
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
            Command = m_Command,
        };

        // Assert
        _ = args.SessionId.Should().Be(m_SessionId);
        _ = args.CommandId.Should().Be(m_CommandId);
        _ = args.OldState.Should().Be(CommandState.Queued);
        _ = args.NewState.Should().Be(CommandState.Executing);
        _ = args.Timestamp.Should().Be(m_TestTime);
        _ = args.Command.Should().Be(m_Command);
    }

    /// <summary>
    /// Verifies that the constructor allows the Command property to be set to null.
    /// </summary>
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
            Command = null,
        };

        // Assert
        _ = args.Command.Should().BeNull();
    }

    /// <summary>
    /// Verifies that various state transitions are correctly represented in the event args.
    /// </summary>
    /// <param name="oldState">The previous command state.</param>
    /// <param name="newState">The new command state.</param>
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
            Command = m_Command,
        };

        // Assert
        _ = args.OldState.Should().Be(oldState);
        _ = args.NewState.Should().Be(newState);
    }
}
