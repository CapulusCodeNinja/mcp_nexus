using FluentAssertions;

using WinAiDbg.Engine.Share.Events;
using WinAiDbg.Engine.Share.Models;

using Xunit;

namespace WinAiDbg.Engine.Unittests.Events;

/// <summary>
/// Unit tests for the SessionStateChangedEventArgs class.
/// </summary>
public class SessionStateChangedEventArgsTests
{
    private readonly DateTime m_TestTime = new(2024, 1, 15, 10, 30, 0);
    private readonly string m_SessionId = "sess-123";

    /// <summary>
    /// Verifies that the constructor correctly sets all properties when provided with valid parameters.
    /// </summary>
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
            Message = "Session initialized successfully",
        };

        // Assert
        _ = args.SessionId.Should().Be(m_SessionId);
        _ = args.OldState.Should().Be(SessionState.Initializing);
        _ = args.NewState.Should().Be(SessionState.Active);
        _ = args.Timestamp.Should().Be(m_TestTime);
        _ = args.Message.Should().Be("Session initialized successfully");
    }

    /// <summary>
    /// Verifies that the constructor allows the Message property to be set to null.
    /// </summary>
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
            Message = null,
        };

        // Assert
        _ = args.Message.Should().BeNull();
    }

    /// <summary>
    /// Verifies that various state transitions are correctly represented in the event args.
    /// </summary>
    /// <param name="oldState">The previous session state.</param>
    /// <param name="newState">The new session state.</param>
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
            Message = $"Transition from {oldState} to {newState}",
        };

        // Assert
        _ = args.OldState.Should().Be(oldState);
        _ = args.NewState.Should().Be(newState);
    }
}
