using WinAiDbg.Engine.Share.Models;
using WinAiDbg.External.Apis.Native;

using NLog;

using Xunit;

namespace WinAiDbg.Engine.Share.Tests;

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
            CommandInfo.Completed(sessionId, "cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-2", "kL", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(3), "output2", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-3", "lm", openedAt, openedAt.AddSeconds(3), openedAt.AddSeconds(4), "output3", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-4", "!threads", openedAt, openedAt.AddSeconds(4), openedAt.AddSeconds(5), "output4", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-5", "!peb", openedAt, openedAt.AddSeconds(5), openedAt.AddSeconds(6), "output5", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-6", "dt", openedAt, openedAt.AddSeconds(6), openedAt.AddSeconds(7), "output6", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-7", "dv", openedAt, openedAt.AddSeconds(7), openedAt.AddSeconds(8), "output7", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-8", "u", openedAt, openedAt.AddSeconds(8), openedAt.AddSeconds(9), "output8", string.Empty, null),
            CommandInfo.Failed(sessionId, "cmd-9", "!error", openedAt, openedAt.AddSeconds(9), openedAt.AddSeconds(10), string.Empty, "Command failed", null),
            CommandInfo.Cancelled(sessionId, "cmd-10", "!runaway", openedAt, openedAt.AddSeconds(10), openedAt.AddSeconds(11), string.Empty, string.Empty, null),
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
            CommandInfo.Failed(sessionId, "cmd-1", "!invalid1", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), string.Empty, "Error 1", null),
            CommandInfo.Failed(sessionId, "cmd-2", "!invalid2", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(3), string.Empty, "Error 2", null),
            CommandInfo.Failed(sessionId, "cmd-3", "!invalid3", openedAt, openedAt.AddSeconds(3), openedAt.AddSeconds(4), string.Empty, "Error 3", null),
            CommandInfo.Failed(sessionId, "cmd-4", "!invalid4", openedAt, openedAt.AddSeconds(4), openedAt.AddSeconds(5), string.Empty, "Error 4", null),
            CommandInfo.Failed(sessionId, "cmd-5", "!invalid5", openedAt, openedAt.AddSeconds(5), openedAt.AddSeconds(6), string.Empty, "Error 5", null),
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
            CommandInfo.Enqueued(sessionId, "cmd-queued-1", "!pending", openedAt, null),
            CommandInfo.Completed(sessionId, "cmd-completed-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
            CommandInfo.TimedOut(sessionId, "cmd-timeout-1", "!slow", openedAt, openedAt.AddSeconds(3), openedAt.AddSeconds(300), string.Empty, "Timeout error", null),
            CommandInfo.Cancelled(sessionId, "cmd-cancelled-1", "!cancelled", openedAt, openedAt.AddSeconds(4), openedAt.AddSeconds(5), string.Empty, string.Empty, null),
            CommandInfo.Failed(sessionId, "cmd-failed-1", "!error", openedAt, openedAt.AddSeconds(6), openedAt.AddSeconds(7), string.Empty, "Command failed", null),
            CommandInfo.Executing(sessionId, "cmd-executing-1", "!running", openedAt, openedAt.AddSeconds(8), null),
            CommandInfo.Completed(sessionId, "cmd-completed-2", "kL", openedAt, openedAt.AddSeconds(9), openedAt.AddSeconds(10), "output2", string.Empty, null),
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

    /// <summary>
    /// Verifies that EmitCommandStats with batchCommandId and batch size greater than 1 renders correctly.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithBatchCommandIdAndSize_RendersCorrectly()
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
            TimeSpan.FromMilliseconds(600),
            5); // batchSize > 1
    }

    /// <summary>
    /// Verifies that EmitCommandStats with batchCommandId and batch size of 1 renders correctly.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithBatchCommandIdAndSizeOne_RendersCorrectly()
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
            TimeSpan.FromMilliseconds(600),
            1); // batchSize = 1
    }

    /// <summary>
    /// Verifies that EmitCommandStats with batchCommandId but null batch size renders correctly.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithBatchCommandIdButNullSize_RendersCorrectly()
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
            TimeSpan.FromMilliseconds(600),
            null); // batchSize = null
    }

    /// <summary>
    /// Verifies that EmitCommandStats with empty batchCommandId renders as single command.
    /// </summary>
    [Fact]
    public void EmitCommandStats_WithEmptyBatchCommandId_RendersSingleCommand()
    {
        // Arrange
        var sessionId = "session-123";
        var commandId = "cmd-456";
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
            string.Empty, // empty batchCommandId
            command,
            queuedAt,
            startedAt,
            completedAt,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(600));
    }

    /// <summary>
    /// Verifies that EmitSessionStats with close reason renders correctly.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithCloseReason_RendersCorrectly()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);
        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            1,
            1,
            0,
            0,
            0,
            commands,
            null,
            "IdleTimeout");
    }

    /// <summary>
    /// Verifies that EmitSessionStats with empty close reason does not render it.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithEmptyCloseReason_DoesNotRenderIt()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);
        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            1,
            1,
            0,
            0,
            0,
            commands,
            null,
            string.Empty);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with long command text truncates it.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithLongCommandText_TruncatesIt()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);
        var longCommand = "This is a very long command that exceeds 25 characters and should be truncated";
        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", longCommand, openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            1,
            1,
            0,
            0,
            0,
            commands);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with command text exactly 25 characters does not truncate.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithExactly25CharCommand_DoesNotTruncate()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);
        var exactCommand = "1234567890123456789012345"; // exactly 25 chars
        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", exactCommand, openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            1,
            1,
            0,
            0,
            0,
            commands);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with batch mapping renders batched and single commands correctly.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithBatchMapping_RendersBatchedAndSingle()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-2", "kL", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(3), "output2", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-3", "lm", openedAt, openedAt.AddSeconds(3), openedAt.AddSeconds(4), "output3", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-4", "!threads", openedAt, openedAt.AddSeconds(4), openedAt.AddSeconds(5), "output4", string.Empty, null),
        };

        var batchMapping = new Dictionary<string, string?>
        {
            { "cmd-1", "batch-1" },
            { "cmd-2", "batch-1" },
            { "cmd-3", null }, // single command
            { "cmd-4", "batch-2" },
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            4,
            4,
            0,
            0,
            0,
            commands,
            batchMapping);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with empty batch mapping renders all as single commands.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithEmptyBatchMapping_RendersAllAsSingle()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-2", "kL", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(3), "output2", string.Empty, null),
        };

        var emptyBatchMapping = new Dictionary<string, string?>();

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            2,
            2,
            0,
            0,
            0,
            commands,
            emptyBatchMapping);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with batch mapping containing empty batch IDs treats them as single.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithEmptyBatchIds_TreatsAsSingle()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-2", "kL", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(3), "output2", string.Empty, null),
        };

        var batchMapping = new Dictionary<string, string?>
        {
            { "cmd-1", string.Empty },
            { "cmd-2", "   " }, // whitespace
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            2,
            2,
            0,
            0,
            0,
            commands,
            batchMapping);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with multiple batches renders slowest batches correctly.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithMultipleBatches_RendersTopSlowest()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", "fast1", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(1.1), "output1", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-2", "fast2", openedAt, openedAt.AddSeconds(1.1), openedAt.AddSeconds(1.2), "output2", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-3", "slow1", openedAt, openedAt.AddSeconds(2), openedAt.AddSeconds(5), "output3", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-4", "slow2", openedAt, openedAt.AddSeconds(5), openedAt.AddSeconds(8), "output4", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-5", "medium1", openedAt, openedAt.AddSeconds(8), openedAt.AddSeconds(9), "output5", string.Empty, null),
            CommandInfo.Completed(sessionId, "cmd-6", "medium2", openedAt, openedAt.AddSeconds(9), openedAt.AddSeconds(10), "output6", string.Empty, null),
        };

        var batchMapping = new Dictionary<string, string?>
        {
            { "cmd-1", "batch-fast" },
            { "cmd-2", "batch-fast" },
            { "cmd-3", "batch-slow" },
            { "cmd-4", "batch-slow" },
            { "cmd-5", "batch-medium" },
            { "cmd-6", "batch-medium" },
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            6,
            6,
            0,
            0,
            0,
            commands,
            batchMapping);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with commands missing timing info renders N/A correctly.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithMissingTimingInfo_RendersNA()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        var commands = new List<CommandInfo>
        {
            CommandInfo.Enqueued(sessionId, "cmd-1", "!pending", openedAt, null),
            CommandInfo.Executing(sessionId, "cmd-2", "!running", openedAt, openedAt.AddSeconds(1), null),
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            2,
            0,
            0,
            0,
            0,
            commands);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with single command in batch mapping shows correct percentages.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithSingleCommandInBatch_ShowsCorrectPercentages()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        var commands = new List<CommandInfo>
        {
            CommandInfo.Completed(sessionId, "cmd-1", "!analyze -v", openedAt, openedAt.AddSeconds(1), openedAt.AddSeconds(2), "output1", string.Empty, null),
        };

        var batchMapping = new Dictionary<string, string?>
        {
            { "cmd-1", "batch-1" },
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            1,
            1,
            0,
            0,
            0,
            commands,
            batchMapping);
    }

    /// <summary>
    /// Verifies that EmitSessionStats with batch mapping but no execution times handles gracefully.
    /// </summary>
    [Fact]
    public void EmitSessionStats_WithBatchMappingButNoExecutionTimes_HandlesGracefully()
    {
        // Arrange
        var sessionId = "session-123";
        var openedAt = DateTime.Now;
        var closedAt = openedAt.AddMinutes(5);

        var commands = new List<CommandInfo>
        {
            CommandInfo.Enqueued(sessionId, "cmd-1", "!pending1", openedAt, null),
            CommandInfo.Enqueued(sessionId, "cmd-2", "!pending2", openedAt, null),
        };

        var batchMapping = new Dictionary<string, string?>
        {
            { "cmd-1", "batch-1" },
            { "cmd-2", "batch-1" },
        };

        // Act & Assert (should not throw)
        Statistics.EmitSessionStats(
            m_Logger,
            sessionId,
            openedAt,
            closedAt,
            TimeSpan.FromMilliseconds(300000),
            2,
            0,
            0,
            0,
            0,
            commands,
            batchMapping);
    }

    /// <summary>
    /// Verifies that EmitProcessStats with sample processes succeeds.
    /// </summary>
    [Fact]
    public void EmitProcessStats_WithProcesses_Succeeds()
    {
        // Arrange
        var processes = new List<TrackedProcessSnapshot>
        {
            new()
            {
                ProcessId = 1234,
                StartTime = DateTime.Now.AddMinutes(-2),
                ProcessName = "cdb",
                FileName = "cdb.exe",
                Arguments = "-z C:\\dumps\\test.dmp",
            },
            new()
            {
                ProcessId = 5678,
                StartTime = DateTime.Now.AddMinutes(-1),
                ProcessName = "cmd",
                FileName = "cmd.exe",
                Arguments = "/c echo hello",
            },
        };

        // Act & Assert (should not throw)
        Statistics.EmitProcessStats(m_Logger, processes);
    }
}
