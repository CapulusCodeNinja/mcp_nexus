using Xunit;
using FluentAssertions;
using nexus.engine.Events;
using nexus.engine.Models;

namespace nexus.engine.unittests.Events;

/// <summary>
/// Unit tests for the SessionStateChangedEventArgs class.
/// </summary>
public class SessionStateChangedEventArgsTests
{
    private readonly DateTime m_TestTime = new(2024, 1, 15, 10, 30, 0);
    private readonly string m_SessionId = "sess-123";

    [Fact]
    public void Constructor_WithValidParameters_ShouldSetProperties()
    {
        // Act
        var args = new SessionStateChangedEventArgs
        {
            SessionId = m_SessionId,
            OldState = SessionState.Initializing,
            NewState = SessionState.Active,
            Timestamp = m_TestTime,
            Message = "Session initialized successfully"
        };

        // Assert
        args.SessionId.Should().Be(m_SessionId);
        args.OldState.Should().Be(SessionState.Initializing);
        args.NewState.Should().Be(SessionState.Active);
        args.Timestamp.Should().Be(m_TestTime);
        args.Message.Should().Be("Session initialized successfully");
    }

    [Fact]
    public void Constructor_WithNullMessage_ShouldAllowNull()
    {
        // Act
        var args = new SessionStateChangedEventArgs
        {
            SessionId = m_SessionId,
            OldState = SessionState.Active,
            NewState = SessionState.Closing,
            Timestamp = m_TestTime,
            Message = null
        };

        // Assert
        args.Message.Should().BeNull();
    }

    [Theory]
    [InlineData(SessionState.Initializing, SessionState.Active)]
    [InlineData(SessionState.Active, SessionState.Closing)]
    [InlineData(SessionState.Closing, SessionState.Closed)]
    [InlineData(SessionState.Active, SessionState.Faulted)]
    public void StateTransitions_ShouldBeValid(SessionState oldState, SessionState newState)
    {
        // Act
        var args = new SessionStateChangedEventArgs
        {
            SessionId = m_SessionId,
            OldState = oldState,
            NewState = newState,
            Timestamp = m_TestTime,
            Message = $"Transition from {oldState} to {newState}"
        };

        // Assert
        args.OldState.Should().Be(oldState);
        args.NewState.Should().Be(newState);
    }
}
