using Nexus.Engine.Share.Models;

using NLog;

using Xunit;

namespace Nexus.Engine.Share.Tests;

/// <summary>
/// Unit tests for the <see cref="Statistics"/> class.
/// </summary>
public class StatisticsTests
{
    private readonly Logger m_Logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsTests"/> class.
    /// </summary>
    public StatisticsTests()
    {
        m_Logger = LogManager.GetCurrentClassLogger();
    }

    #region EmitCommandStats Tests

    /// <summary>
    /// Verifies that EmitCommandStats with completed status succeeds.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithCompletedStatus_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
        var command = "!analyze -v";
        var queuedAt = DateTime.Now;
        var startedAt = queuedAt.AddMilliseconds(100);
        var completedAt = startedAt.AddMilliseconds(500);

        // Act & Assert (should not throw)
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Completed,
            sessionId,
            commandId,
            command,
            queuedAt,
            startedAt,
            completedAt,
            100,
            500,
            600);
    }

    /// <summary>
    /// Verifies that EmitCommandStats with failed status succeeds.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithFailedStatus_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
        var command = "!analyze -v";
        var queuedAt = DateTime.Now;
        var startedAt = queuedAt.AddMilliseconds(100);
        var completedAt = startedAt.AddMilliseconds(500);

        // Act & Assert (should not throw)
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Failed,
            sessionId,
            commandId,
            command,
            queuedAt,
            startedAt,
            completedAt,
            100,
            500,
            600);
    }

    /// <summary>
    /// Verifies that EmitCommandStats with null commandId succeeds.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithNullCommandId_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var queuedAt = DateTime.Now;
        var startedAt = queuedAt.AddMilliseconds(100);
        var completedAt = startedAt.AddMilliseconds(500);

        // Act & Assert (should not throw)
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Completed,
            sessionId,
            null,
            "test command",
            queuedAt,
            startedAt,
            completedAt,
            100,
            500,
            600);
    }

    /// <summary>
    /// Verifies that EmitCommandStats with null command text succeeds.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithNullCommand_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
        var queuedAt = DateTime.Now;
        var startedAt = queuedAt.AddMilliseconds(100);
        var completedAt = startedAt.AddMilliseconds(500);

        // Act & Assert (should not throw)
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Completed,
            sessionId,
            commandId,
            null,
            queuedAt,
            startedAt,
            completedAt,
            100,
            500,
            600);
    }

    #endregion

    #region EmitSessionStats Tests

    /// <summary>
    /// Verifies that EmitSessionStats with valid data succeeds.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithValidData_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            300000,
            10,
            8,
            1,
            1,
            0);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with zero commands succeeds.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithZeroCommands_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(1);

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            60000,
            0,
            0,
            0,
            0,
            0);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with all failed commands succeeds.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithAllFailedCommands_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(2);

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            120000,
            5,
            0,
            5,
            0,
            0);
    }

    #endregion
}

