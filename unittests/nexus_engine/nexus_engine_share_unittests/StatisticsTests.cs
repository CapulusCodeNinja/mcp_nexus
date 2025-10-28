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
            null, // batchCommandId
            command,
            queuedAt,
            startedAt,
            completedAt,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(600));
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
            null, // batchCommandId
            command,
            queuedAt,
            startedAt,
            completedAt,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(600));
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
            null, // batchCommandId
            "test command",
            queuedAt,
            startedAt,
            completedAt,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(600));
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
            null, // batchCommandId
            null,
            queuedAt,
            startedAt,
            completedAt,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(600));
    }

    /// <summary>
    /// Verifies that EmitCommandStats with batch command ID succeeds.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithBatchCommandId_Succeeds()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
        var batchCommandId = "batch-789";
        var command = "lsa 0x123";
        var queuedAt = DateTime.Now;
        var startedAt = queuedAt.AddMilliseconds(100);
        var completedAt = startedAt.AddMilliseconds(500);

        // Act & Assert (should not throw)
        Statistics.EmitCommandStats(
            m_Logger,
            CommandState.Completed,
            sessionId,
            commandId,
            batchCommandId,
            command,
            queuedAt,
            startedAt,
            completedAt,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(600));
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

        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed("cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", true),
            CommandInfo.Completed("cmd-2", "kL", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(3), "output2", true),
            CommandInfo.Completed("cmd-3", "lm", openedAt, openedAt.AddSeconds(3), openedAt.AddSeconds(4), "output3", true),
            CommandInfo.Completed("cmd-4", "!threads", openedAt, openedAt.AddSeconds(4), openedAt.AddSeconds(5), "output4", true),
            CommandInfo.Completed("cmd-5", "!peb", openedAt, openedAt.AddSeconds(5), openedAt.AddSeconds(6), "output5", true),
            CommandInfo.Completed("cmd-6", "dt", openedAt, openedAt.AddSeconds(6), openedAt.AddSeconds(7), "output6", true),
            CommandInfo.Completed("cmd-7", "dv", openedAt, openedAt.AddSeconds(7), openedAt.AddSeconds(8), "output7", true),
            CommandInfo.Completed("cmd-8", "u", openedAt, openedAt.AddSeconds(8), openedAt.AddSeconds(9), "output8", true),
            CommandInfo.Completed("cmd-9", "!error", openedAt, openedAt.AddSeconds(9), openedAt.AddSeconds(10), "", false, "Command failed"),
            CommandInfo.Cancelled("cmd-10", "!runaway", openedAt, openedAt.AddSeconds(10), openedAt.AddSeconds(11))
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            10,
            8,
            1,
            1,
            0,
            commands);
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
        var commands = new List<CommandInfo>();

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(60000),
            0,
            0,
            0,
            0,
            0,
            commands);
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

        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed("cmd-1", "!invalid1", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "", false, "Error 1"),
            CommandInfo.Completed("cmd-2", "!invalid2", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(3), "", false, "Error 2"),
            CommandInfo.Completed("cmd-3", "!invalid3", openedAt, openedAt.AddSeconds(3), openedAt.AddSeconds(4), "", false, "Error 3"),
            CommandInfo.Completed("cmd-4", "!invalid4", openedAt, openedAt.AddSeconds(4), openedAt.AddSeconds(5), "", false, "Error 4"),
            CommandInfo.Completed("cmd-5", "!invalid5", openedAt, openedAt.AddSeconds(5), openedAt.AddSeconds(6), "", false, "Error 5")
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(120000),
            5,
            0,
            5,
            0,
            0,
            commands);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with mixed command states renders table correctly.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithMixedCommandStates_RendersTableCorrectly()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        // Create commands in mixed order to verify sorting by status
        var commands = new List<CommandInfo>
        {
            CommandInfo.Queued("cmd-queued-1", "!pending", openedAt),
            CommandInfo.Completed("cmd-completed-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", true),
            CommandInfo.TimedOut("cmd-timeout-1", "!slow", openedAt, openedAt.AddSeconds(3), openedAt.AddSeconds(300), "Timeout error"),
            CommandInfo.Cancelled("cmd-cancelled-1", "!cancelled", openedAt, openedAt.AddSeconds(4), openedAt.AddSeconds(5)),
            CommandInfo.Completed("cmd-failed-1", "!error", openedAt, openedAt.AddSeconds(6), openedAt.AddSeconds(7), "", false, "Command failed"),
            CommandInfo.Executing("cmd-executing-1", "!running", openedAt, openedAt.AddSeconds(8)),
            CommandInfo.Completed("cmd-completed-2", "kL", openedAt, openedAt.AddSeconds(9), openedAt.AddSeconds(10), "output2", true)
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            7,
            2,
            1,
            1,
            1,
            commands);
    }

    #endregion
}

