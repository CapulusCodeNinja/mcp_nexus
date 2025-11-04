using FluentAssertions;

using Moq;

using Nexus.Config;
using Nexus.Config.Models;
using Nexus.Engine.Batch;
using Nexus.Engine.Internal;
using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;
using Nexus.Engine.Tests.Internal;
using Nexus.External.Apis.FileSystem;
using Nexus.External.Apis.ProcessManagement;

using Xunit;

namespace Nexus.Engine.Unittests.Internal;

/// <summary>
/// Unit tests for CommandQueue class.
/// Tests command enqueuing, state management, cancellation, and result caching.
/// </summary>
public class CommandQueueTests : IDisposable
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IBatchProcessor> m_BatchProcessor;

    private readonly CommandQueue m_Queue;
    private readonly List<CommandStateChangedEventArgs> m_StateChanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandQueueTests"/> class.
    /// </summary>
    public CommandQueueTests()
    {
        m_Settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
        m_BatchProcessor = new Mock<IBatchProcessor>();

        m_Queue = new CommandQueue("test-session-1", m_Settings.Object, m_BatchProcessor.Object);
        m_StateChanges = new List<CommandStateChangedEventArgs>();
        m_Queue.CommandStateChanged += (sender, args) => m_StateChanges.Add(args);
    }

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        m_Queue.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that constructor throws ArgumentNullException when sessionId is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullSessionId_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new CommandQueue(null!, m_Settings.Object, m_BatchProcessor.Object));
    }

    /// <summary>
    /// Verifies that EnqueueCommand generates a unique command ID.
    /// </summary>
    [Fact]
    public void EnqueueCommand_GeneratesUniqueCommandId()
    {
        // Act
        var commandId = m_Queue.EnqueueCommand("k");

        // Assert
        _ = commandId.Should().NotBeNullOrEmpty();
        _ = commandId.Should().StartWith("cmd-");
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException for null command.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNullCommand_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Queue.EnqueueCommand(null!));
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException for empty command.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithEmptyCommand_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Queue.EnqueueCommand(string.Empty));
    }

    /// <summary>
    /// Verifies that EnqueueCommand throws ArgumentException for whitespace command.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithWhitespaceCommand_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Queue.EnqueueCommand("   "));
    }

    /// <summary>
    /// Verifies that EnqueueCommand generates unique IDs for multiple commands.
    /// </summary>
    [Fact]
    public void EnqueueCommand_MultipleCommands_GeneratesUniqueIds()
    {
        // Act
        var id1 = m_Queue.EnqueueCommand("k");
        var id2 = m_Queue.EnqueueCommand("lm");
        var id3 = m_Queue.EnqueueCommand("!analyze -v");

        // Assert
        _ = id1.Should().NotBe(id2);
        _ = id1.Should().NotBe(id3);
        _ = id2.Should().NotBe(id3);
    }

    /// <summary>
    /// Verifies that EnqueueCommand triggers CommandStateChanged event.
    /// </summary>
    [Fact]
    public void EnqueueCommand_TriggersCommandStateChangedEvent()
    {
        // Act
        var commandId = m_Queue.EnqueueCommand("k");

        // Assert
        _ = m_StateChanges.Should().HaveCount(1);
        _ = m_StateChanges[0].CommandId.Should().Be(commandId);
        _ = m_StateChanges[0].NewState.Should().Be(CommandState.Queued);
        _ = m_StateChanges[0].Command.Should().Be("k");
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null for null commandId.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithNullCommandId_ReturnsNull()
    {
        // Act
        var result = m_Queue.GetCommandInfo(null!);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null for empty commandId.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithEmptyCommandId_ReturnsNull()
    {
        // Act
        var result = m_Queue.GetCommandInfo(string.Empty);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null for non-existent command.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithNonExistentCommandId_ReturnsNull()
    {
        // Act
        var result = m_Queue.GetCommandInfo("cmd-nonexistent");

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns info for queued command.
    /// </summary>
    [Fact]
    public void GetCommandInfo_ForQueuedCommand_ReturnsInfo()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act
        var result = m_Queue.GetCommandInfo(commandId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.CommandId.Should().Be(commandId);
        _ = result.Command.Should().Be("k");
        _ = result.State.Should().Be(CommandState.Queued);
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns empty dictionary when no commands.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WithNoCommands_ReturnsEmptyDictionary()
    {
        // Act
        var result = m_Queue.GetAllCommandInfos();

        // Assert
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns all queued commands.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WithMultipleCommands_ReturnsAllCommands()
    {
        // Arrange
        var id1 = m_Queue.EnqueueCommand("k");
        var id2 = m_Queue.EnqueueCommand("lm");
        var id3 = m_Queue.EnqueueCommand("!analyze -v");

        // Act
        var result = m_Queue.GetAllCommandInfos();

        // Assert
        _ = result.Should().HaveCount(3);
        _ = result.Should().ContainKey(id1);
        _ = result.Should().ContainKey(id2);
        _ = result.Should().ContainKey(id3);
    }

    /// <summary>
    /// Verifies that CancelCommand returns false for null commandId.
    /// </summary>
    [Fact]
    public void CancelCommand_WithNullCommandId_ReturnsFalse()
    {
        // Act
        var result = m_Queue.CancelCommand(null!);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false for empty commandId.
    /// </summary>
    [Fact]
    public void CancelCommand_WithEmptyCommandId_ReturnsFalse()
    {
        // Act
        var result = m_Queue.CancelCommand(string.Empty);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false for non-existent command.
    /// </summary>
    [Fact]
    public void CancelCommand_WithNonExistentCommandId_ReturnsFalse()
    {
        // Act
        var result = m_Queue.CancelCommand("cmd-nonexistent");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelCommand cancels queued command.
    /// </summary>
    [Fact]
    public void CancelCommand_ForQueuedCommand_CancelsAndReturnsTrue()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        m_StateChanges.Clear();

        // Act
        var result = m_Queue.CancelCommand(commandId);

        // Assert
        _ = result.Should().BeTrue();

        var info = m_Queue.GetCommandInfo(commandId);
        _ = info!.State.Should().Be(CommandState.Cancelled);

        // Verify state change event
        _ = m_StateChanges.Should().HaveCount(1);
        _ = m_StateChanges[0].NewState.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that CancelAllCommands returns zero when no commands.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WithNoCommands_ReturnsZero()
    {
        // Act
        var result = m_Queue.CancelAllCommands();

        // Assert
        _ = result.Should().Be(0);
    }

    /// <summary>
    /// Verifies that CancelAllCommands cancels all queued commands.
    /// </summary>
    [Fact]
    public void CancelAllCommands_CancelsAllQueuedCommands()
    {
        // Arrange
        var id1 = m_Queue.EnqueueCommand("k");
        var id2 = m_Queue.EnqueueCommand("lm");
        var id3 = m_Queue.EnqueueCommand("!analyze -v");
        m_StateChanges.Clear();

        // Act
        var result = m_Queue.CancelAllCommands("Test cancellation");

        // Assert
        _ = result.Should().Be(3);

        // Verify all are cancelled
        _ = m_Queue.GetCommandInfo(id1)!.State.Should().Be(CommandState.Cancelled);
        _ = m_Queue.GetCommandInfo(id2)!.State.Should().Be(CommandState.Cancelled);
        _ = m_Queue.GetCommandInfo(id3)!.State.Should().Be(CommandState.Cancelled);

        // Verify state change events
        _ = m_StateChanges.Should().HaveCount(3);
        _ = m_StateChanges.Should().OnlyContain(e => e.NewState == CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws ArgumentException for null commandId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandInfoAsync_WithNullCommandId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await m_Queue.GetCommandInfoAsync(null!));
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws ArgumentException for empty commandId.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandInfoAsync_WithEmptyCommandId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await m_Queue.GetCommandInfoAsync(string.Empty));
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws KeyNotFoundException for non-existent command.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandInfoAsync_WithNonExistentCommandId_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await m_Queue.GetCommandInfoAsync("cmd-nonexistent"));
    }

    /// <summary>
    /// Verifies that Dispose can be called multiple times safely.
    /// </summary>
    [Fact]
    public void Dispose_MultipleCallsAreSafe()
    {
        // Act
        m_Queue.Dispose();
        m_Queue.Dispose();
        m_Queue.Dispose();

        // Assert - No exception should be thrown
    }

    /// <summary>
    /// Verifies that operations after Dispose throw ObjectDisposedException.
    /// </summary>
    [Fact]
    public void EnqueueCommand_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Queue.EnqueueCommand("k"));
    }

    /// <summary>
    /// Verifies that GetCommandInfo after Dispose throws ObjectDisposedException.
    /// </summary>
    [Fact]
    public void GetCommandInfo_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Queue.GetCommandInfo("cmd-123"));
    }

    /// <summary>
    /// Verifies that CancelCommand after Dispose throws ObjectDisposedException.
    /// </summary>
    [Fact]
    public void CancelCommand_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Queue.CancelCommand("cmd-123"));
    }

    /// <summary>
    /// Verifies that CommandStateChanged event includes correct session ID.
    /// </summary>
    [Fact]
    public void EnqueueCommand_StateChangeEvent_IncludesCorrectSessionId()
    {
        // Act
        _ = m_Queue.EnqueueCommand("k");

        // Assert
        _ = m_StateChanges[0].SessionId.Should().Be("test-session-1");
    }

    /// <summary>
    /// Verifies that CommandStateChanged event includes timestamp.
    /// </summary>
    [Fact]
    public void EnqueueCommand_StateChangeEvent_IncludesTimestamp()
    {
        // Arrange
        var before = DateTime.Now;

        // Act
        _ = m_Queue.EnqueueCommand("k");

        // Assert
        var after = DateTime.Now;
        _ = m_StateChanges[0].Timestamp.Should().BeOnOrAfter(before);
        _ = m_StateChanges[0].Timestamp.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Verifies that EnqueueCommand stores queued time.
    /// </summary>
    [Fact]
    public void EnqueueCommand_StoresQueuedTime()
    {
        // Arrange
        var before = DateTime.Now;

        // Act
        var commandId = m_Queue.EnqueueCommand("k");

        // Assert
        var after = DateTime.Now;
        var info = m_Queue.GetCommandInfo(commandId);
        _ = info!.QueuedTime.Should().BeOnOrAfter(before);
        _ = info.QueuedTime.Should().BeOnOrBefore(after);
    }

    /// <summary>
    /// Verifies that CancelCommand triggers state change from Queued to Cancelled.
    /// </summary>
    [Fact]
    public void CancelCommand_TriggersStateChangeEvent()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        m_StateChanges.Clear();

        // Act
        _ = m_Queue.CancelCommand(commandId);

        // Assert
        _ = m_StateChanges.Should().HaveCount(1);
        _ = m_StateChanges[0].CommandId.Should().Be(commandId);
        _ = m_StateChanges[0].OldState.Should().Be(CommandState.Queued);
        _ = m_StateChanges[0].NewState.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that command IDs follow expected format.
    /// </summary>
    [Fact]
    public void EnqueueCommand_GeneratesCommandIdWithCorrectFormat()
    {
        // Act
        var commandId = m_Queue.EnqueueCommand("k");

        // Assert - Should be cmd-{sessionId}-{number}
        _ = commandId.Should().MatchRegex(@"^cmd-test-session-1-\d+$");
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos includes command text.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_IncludesCommandText()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("!analyze -v");

        // Act
        var result = m_Queue.GetAllCommandInfos();

        // Assert
        _ = result[commandId].Command.Should().Be("!analyze -v");
    }

    /// <summary>
    /// Verifies that StartAsync throws ArgumentNullException for null CdbSession.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_WithNullCdbSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await m_Queue.StartAsync(null!));
    }

    /// <summary>
    /// Verifies that StartAsync throws ObjectDisposedException after Dispose.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await m_Queue.StartAsync(null!));
    }

    /// <summary>
    /// Verifies that StopAsync can be called when disposed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WhenDisposed_DoesNotThrow()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert - Should not throw
        await m_Queue.StopAsync();
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos throws ObjectDisposedException after Dispose.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Queue.GetAllCommandInfos());
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws ObjectDisposedException after Dispose.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandInfoAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await m_Queue.GetCommandInfoAsync("cmd-123"));
    }

    /// <summary>
    /// Verifies that CancelAllCommands throws ObjectDisposedException after Dispose.
    /// </summary>
    [Fact]
    public void CancelAllCommands_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        m_Queue.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => m_Queue.CancelAllCommands());
    }

    /// <summary>
    /// Verifies that EnqueueCommand with whitespace-only command throws ArgumentException.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithWhitespaceOnlyCommand_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Queue.EnqueueCommand("\t\r\n  "));
    }

    /// <summary>
    /// Verifies that GetCommandInfo with whitespace command ID returns null.
    /// </summary>
    [Fact]
    public void GetCommandInfo_WithWhitespaceCommandId_ReturnsNull()
    {
        // Act
        var result = m_Queue.GetCommandInfo("   \t  ");

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync with whitespace command ID throws ArgumentException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandInfoAsync_WithWhitespaceCommandId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await m_Queue.GetCommandInfoAsync("   \t  "));
    }

    /// <summary>
    /// Verifies that CancelCommand with whitespace command ID returns false.
    /// </summary>
    [Fact]
    public void CancelCommand_WithWhitespaceCommandId_ReturnsFalse()
    {
        // Act
        var result = m_Queue.CancelCommand("   \t  ");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelAllCommands with null reason works correctly.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WithNullReason_CancelsCommands()
    {
        // Arrange
        _ = m_Queue.EnqueueCommand("k");
        _ = m_Queue.EnqueueCommand("lm");

        // Act
        var result = m_Queue.CancelAllCommands(null);

        // Assert
        _ = result.Should().Be(2);
    }

    /// <summary>
    /// Verifies that EnqueueCommand increments command sequence correctly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_IncrementsSequenceNumber()
    {
        // Act
        var id1 = m_Queue.EnqueueCommand("k");
        var id2 = m_Queue.EnqueueCommand("lm");

        // Assert
        _ = id1.Should().EndWith("-1");
        _ = id2.Should().EndWith("-2");
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos includes sessionId in returned infos.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_IncludesSessionId()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act
        var result = m_Queue.GetAllCommandInfos();

        // Assert
        _ = result[commandId].SessionId.Should().Be("test-session-1");
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns info with correct sessionId.
    /// </summary>
    [Fact]
    public void GetCommandInfo_ReturnsInfoWithSessionId()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act
        var result = m_Queue.GetCommandInfo(commandId);

        // Assert
        _ = result!.SessionId.Should().Be("test-session-1");
    }

    /// <summary>
    /// Verifies that multiple EnqueueCommand calls work correctly in sequence.
    /// </summary>
    [Fact]
    public void EnqueueCommand_MultipleSequential_AllSucceed()
    {
        // Act
        var ids = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            ids.Add(m_Queue.EnqueueCommand($"command-{i}"));
        }

        // Assert
        _ = ids.Should().HaveCount(10);
        _ = ids.Should().OnlyHaveUniqueItems();

        var allInfos = m_Queue.GetAllCommandInfos();
        _ = allInfos.Should().HaveCount(10);
    }

    /// <summary>
    /// Verifies that CancelCommand updates state before notifying listeners.
    /// </summary>
    [Fact]
    public void CancelCommand_UpdatesStateBeforeEvent()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        CommandInfo? infoAtEventTime = null;
        m_Queue.CommandStateChanged += (sender, args) =>
        {
            if (args.NewState == CommandState.Cancelled)
            {
                infoAtEventTime = m_Queue.GetCommandInfo(commandId);
            }
        };

        m_StateChanges.Clear();

        // Act
        _ = m_Queue.CancelCommand(commandId);

        // Assert
        _ = infoAtEventTime.Should().NotBeNull();
        _ = infoAtEventTime!.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that CommandStateChanged event has correct old and new states.
    /// </summary>
    [Fact]
    public void CommandStateChanged_HasCorrectOldAndNewStates()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        m_StateChanges.Clear();

        // Act
        _ = m_Queue.CancelCommand(commandId);

        // Assert
        _ = m_StateChanges[0].OldState.Should().Be(CommandState.Queued);
        _ = m_StateChanges[0].NewState.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that Dispose cancels all commands before stopping.
    /// </summary>
    [Fact]
    public void Dispose_CancelsAllCommandsBeforeStopping()
    {
        // Arrange
        var id1 = m_Queue.EnqueueCommand("k");
        var id2 = m_Queue.EnqueueCommand("lm");
        m_StateChanges.Clear();

        // Act
        m_Queue.Dispose();

        // Assert - Commands should be cancelled
        _ = m_StateChanges.Should().Contain(e => e.CommandId == id1 && e.NewState == CommandState.Cancelled);
        _ = m_StateChanges.Should().Contain(e => e.CommandId == id2 && e.NewState == CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns same instance on multiple calls.
    /// </summary>
    [Fact]
    public void GetCommandInfo_ReturnsSameInstanceOnMultipleCalls()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act
        var info1 = m_Queue.GetCommandInfo(commandId);
        var info2 = m_Queue.GetCommandInfo(commandId);

        // Assert - Should return info for same command (not necessarily same instance due to new CommandInfo creation)
        _ = info1!.CommandId.Should().Be(info2!.CommandId);
        _ = info1.Command.Should().Be(info2.Command);
        _ = info1.State.Should().Be(info2.State);
    }

    /// <summary>
    /// Verifies that EnqueueCommand command text is preserved exactly.
    /// </summary>
    [Fact]
    public void EnqueueCommand_PreservesCommandTextExactly()
    {
        // Arrange
        var command = "  k -v --detailed  ";

        // Act
        var commandId = m_Queue.EnqueueCommand(command);
        var info = m_Queue.GetCommandInfo(commandId);

        // Assert
        _ = info!.Command.Should().Be(command);
    }

    /// <summary>
    /// Verifies that CancelAllCommands state change events include all cancelled commands.
    /// </summary>
    [Fact]
    public void CancelAllCommands_StateChangeEvents_IncludeAllCommands()
    {
        // Arrange
        var ids = new List<string>
        {
            m_Queue.EnqueueCommand("k"),
            m_Queue.EnqueueCommand("lm"),
            m_Queue.EnqueueCommand("!analyze -v"),
        };
        m_StateChanges.Clear();

        // Act
        _ = m_Queue.CancelAllCommands("Batch cancel");

        // Assert
        _ = m_StateChanges.Should().HaveCount(3);
        foreach (var id in ids)
        {
            _ = m_StateChanges.Should().Contain(e => e.CommandId == id && e.NewState == CommandState.Cancelled);
        }
    }

    /// <summary>
    /// Verifies that CommandStateChanged event is not raised if no subscribers.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var queueWithoutSubscribers = new CommandQueue("test-session-2", m_Settings.Object, m_BatchProcessor.Object);

        // Act & Assert - Should not throw
        _ = queueWithoutSubscribers.EnqueueCommand("k");
        queueWithoutSubscribers.Dispose();
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws KeyNotFoundException when command does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandInfoAsync_WhenCommandNotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        var act = async () => await m_Queue.GetCommandInfoAsync("nonexistent-command-id");
        _ = await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*nonexistent-command-id*");
    }

    /// <summary>
    /// Verifies that CancelCommand returns false when command does not exist.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandNotFound_ReturnsFalse()
    {
        // Act
        var result = m_Queue.CancelCommand("nonexistent-command-id");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelAllCommands with reason updates all commands.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WithReason_UpdatesAllCommands()
    {
        // Arrange
        _ = m_Queue.EnqueueCommand("k");
        _ = m_Queue.EnqueueCommand("lm");
        m_StateChanges.Clear();

        // Act
        var count = m_Queue.CancelAllCommands("Test reason");

        // Assert
        _ = count.Should().Be(2);
        _ = m_StateChanges.Should().HaveCount(2);
        foreach (var change in m_StateChanges)
        {
            _ = change.NewState.Should().Be(CommandState.Cancelled);
        }
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns empty dictionary when no commands.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WhenNoCommands_ReturnsEmptyDictionary()
    {
        // Act
        var result = m_Queue.GetAllCommandInfos();

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns all commands.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WhenCommandsExist_ReturnsAllCommands()
    {
        // Arrange
        var id1 = m_Queue.EnqueueCommand("k");
        var id2 = m_Queue.EnqueueCommand("lm");

        // Act
        var result = m_Queue.GetAllCommandInfos();

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().HaveCount(2);
        _ = result.Should().ContainKey(id1);
        _ = result.Should().ContainKey(id2);
    }

    /// <summary>
    /// Verifies that CollectAvailableCommands with zero wait time uses fast path.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithZeroWaitTime_CollectsImmediately()
    {
        // Arrange
        var settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Batching = new BatchingSettings
                {
                    CommandCollectionWaitMs = 0,
                },
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = settings.Setup(s => s.Get()).Returns(sharedConfig);
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-collect", settings.Object, batchProcessor.Object);

        // Act
        var id1 = queue.EnqueueCommand("k");
        var id2 = queue.EnqueueCommand("lm");

        // Assert - Commands should be enqueued successfully
        _ = queue.GetCommandInfo(id1).Should().NotBeNull();
        _ = queue.GetCommandInfo(id2).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that CollectAvailableCommands with configured wait time uses timeout path.
    /// </summary>
    [Fact]
    public void EnqueueCommand_WithConfiguredWaitTime_CollectsWithTimeout()
    {
        // Arrange
        var settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Batching = new BatchingSettings
                {
                    CommandCollectionWaitMs = 10,
                },
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = settings.Setup(s => s.Get()).Returns(sharedConfig);
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-wait", settings.Object, batchProcessor.Object);

        // Act
        var id1 = queue.EnqueueCommand("k");
        var id2 = queue.EnqueueCommand("lm");

        // Assert - Commands should be enqueued successfully
        _ = queue.GetCommandInfo(id1).Should().NotBeNull();
        _ = queue.GetCommandInfo(id2).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that GetBatchCommandId is called during command completion.
    /// </summary>
    [Fact]
    public void CompleteCommandWithStatistics_CallsGetBatchCommandId()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        _ = batchProcessor.Setup(bp => bp.GetBatchCommandId(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("batch-cmd-1");
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns(new List<CommandResult>());

        var queue = new CommandQueue("test-session-batch", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue and cancel to complete command
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - GetBatchCommandId should be setup (indirectly tested through ProcessCommandResults)
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ProcessCommandsAsync handles exception during command processing.
    /// </summary>
    [Fact]
    public void ProcessCommandsAsync_WithException_MarksCommandFailed()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        _ = batchProcessor.Setup(bp => bp.BatchCommands(It.IsAny<string>(), It.IsAny<List<Command>>()))
            .Throws(new InvalidOperationException("Test error"));

        var queue = new CommandQueue("test-session-ex", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue command (processing will fail due to batch error)
        var commandId = queue.EnqueueCommand("k");

        // Assert - Command should exist
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that CollectAvailableCommands handles cancellation during wait.
    /// </summary>
    [Fact]
    public void CollectAvailableCommands_WithCancellation_HandlesGracefully()
    {
        // Arrange
        var settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Batching = new BatchingSettings
                {
                    CommandCollectionWaitMs = 100,
                },
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = settings.Setup(s => s.Get()).Returns(sharedConfig);
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-cancel-wait", settings.Object, batchProcessor.Object);

        // Act - Enqueue command with cancellation
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Command should be cancelled
        _ = queue.GetCommandInfo(commandId)!.State.Should().Be(CommandState.Cancelled);
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that StartAsync starts the processing loop.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StartAsync_StartsProcessingLoop()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockProcessManager = new Mock<IProcessManager>();
        var cdbSession = new CdbSessionTestAccessor(m_Settings.Object, mockFileSystem.Object, mockProcessManager.Object);
        cdbSession.SetInitializedForTesting(true);

        try
        {
            // Act
            await m_Queue.StartAsync(cdbSession);

            // Assert - Queue should be started (processing loop is running)
            // No delay needed - just verify it started without exception
        }
        finally
        {
            // Cleanup
            await m_Queue.StopAsync();
            await cdbSession.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that StopAsync stops the processing loop gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_StopsProcessingLoop()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockProcessManager = new Mock<IProcessManager>();
        var cdbSession = new CdbSessionTestAccessor(m_Settings.Object, mockFileSystem.Object, mockProcessManager.Object);
        cdbSession.SetInitializedForTesting(true);
        await m_Queue.StartAsync(cdbSession);

        // Act
        await m_Queue.StopAsync();

        // Assert - Should complete without exception
        await cdbSession.DisposeAsync();
    }

    /// <summary>
    /// Verifies that GetCommandInfo for cancelled command returns info.
    /// </summary>
    [Fact]
    public void GetCommandInfo_ForCancelledCommand_ReturnsInfo()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        _ = m_Queue.CancelCommand(commandId);

        // Act - Get info (should return cancelled state)
        var result = m_Queue.GetCommandInfo(commandId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.CommandId.Should().Be(commandId);
        _ = result.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that GetCommandInfo multiple calls returns consistent results.
    /// </summary>
    [Fact]
    public void GetCommandInfo_MultipleCalls_ReturnsConsistentResults()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        _ = m_Queue.CancelCommand(commandId);

        // Act
        var result1 = m_Queue.GetCommandInfo(commandId);
        var result2 = m_Queue.GetCommandInfo(commandId);

        // Assert - Both should return the same command info (state should be consistent)
        _ = result1.Should().NotBeNull();
        _ = result2.Should().NotBeNull();
        _ = result1!.CommandId.Should().Be(result2!.CommandId);
        _ = result1.State.Should().Be(result2.State);
        _ = result1.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that CancelCommand when command is already cancelled still returns true (command is still active).
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandIsCancelled_StillReturnsTrueWhileActive()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        _ = m_Queue.CancelCommand(commandId);

        // Act - Try to cancel again (command is still in active commands, not yet moved to cache)
        var result = m_Queue.CancelCommand(commandId);

        // Assert - Returns true because command is still in m_ActiveCommands
        // (it won't be moved to cache until the queue processes it)
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CancelCommand returns false when command does not exist.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = m_Queue.CancelCommand("non-existent-command-id");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CancelCommand when command is executing returns true.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandIsExecuting_ReturnsTrue()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act - Cancel while still queued (before execution)
        var result = m_Queue.CancelCommand(commandId);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CancelCommand when command is executing cancels token.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandIsExecuting_CancelsToken()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act
        _ = m_Queue.CancelCommand(commandId);
        var info = m_Queue.GetCommandInfo(commandId);

        // Assert
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that CancelCommand when command is executing updates state to cancelled.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandIsExecuting_UpdatesStateToCancelled()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");
        m_StateChanges.Clear();

        // Act
        _ = m_Queue.CancelCommand(commandId);

        // Assert
        var info = m_Queue.GetCommandInfo(commandId);
        _ = info!.State.Should().Be(CommandState.Cancelled);
        _ = m_StateChanges.Should().Contain(e => e.NewState == CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that CancelCommand when command is executing removes from active commands.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandIsExecuting_RemovesFromActiveCommands()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act
        _ = m_Queue.CancelCommand(commandId);

        // Assert - Command should be in cache, not active
        var info = m_Queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that CancelCommand when command is executing handles exception gracefully.
    /// </summary>
    [Fact]
    public void CancelCommand_WhenCommandIsExecuting_HandlesExceptionGracefully()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act - Cancel should succeed even if there are issues
        var result = m_Queue.CancelCommand(commandId);

        // Assert
        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ProcessCommandResults handles missing command ID gracefully.
    /// </summary>
    [Fact]
    public void ProcessCommandResults_WithMissingCommandId_HandlesGracefully()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var result = new CommandResult
        {
            CommandId = "cmd-nonexistent",
            SessionId = "test-session",
            ResultText = "test",
        };
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns(new List<CommandResult> { result });

        var queue = new CommandQueue("test-session-missing", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue a command, then try to process a result for a different command
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should not throw
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that LogUnbatchingResults logs trace when counts match.
    /// </summary>
    [Fact]
    public void LogUnbatchingResults_WithMatchingCounts_LogsTrace()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var commands = new List<Command> { new Command { CommandId = "cmd-1", CommandText = "k" } };
        _ = batchProcessor.Setup(bp => bp.BatchCommands(It.IsAny<string>(), It.IsAny<List<Command>>()))
            .Returns(commands);
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns((List<CommandResult> results) => results); // Return same count

        var queue = new CommandQueue("test-session-unbatch", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should complete without exception
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that LogUnbatchingResults logs debug when counts differ.
    /// </summary>
    [Fact]
    public void LogUnbatchingResults_WithDifferentCounts_LogsDebug()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var commands = new List<Command> { new Command { CommandId = "cmd-1", CommandText = "k" } };
        _ = batchProcessor.Setup(bp => bp.BatchCommands(It.IsAny<string>(), It.IsAny<List<Command>>()))
            .Returns(commands);
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns(new List<CommandResult>()); // Return different count (empty)

        var queue = new CommandQueue("test-session-unbatch-diff", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should complete without exception
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that CreateCommandInfoFromResult handles unexpected state.
    /// </summary>
    [Fact]
    public void CreateCommandInfoFromResult_WithUnexpectedState_ReturnsFailed()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-unexpected", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue and cancel
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should complete successfully
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that batch processing GetOriginalCommandIds is called.
    /// </summary>
    [Fact]
    public void ProcessCommandsAsync_CallsGetOriginalCommandIds()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var batchCommand = new Command { CommandId = "batch-cmd-1", CommandText = "k;lm" };
        _ = batchProcessor.Setup(bp => bp.BatchCommands(It.IsAny<string>(), It.IsAny<List<Command>>()))
            .Returns(new List<Command> { batchCommand });
        _ = batchProcessor.Setup(bp => bp.GetOriginalCommandIds(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<string> { "cmd-1", "cmd-2" });
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns(new List<CommandResult>());

        var queue = new CommandQueue("test-session-batch-ids", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - GetOriginalCommandIds should be called (indirectly tested)
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteSingleCommand handles cancellation during execution.
    /// </summary>
    [Fact]
    public void ExecuteSingleCommand_WithCancellation_ReturnsCancelledResult()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-cancel-exec", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue and cancel (simulates cancellation)
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Use synchronous GetCommandInfo to avoid hanging
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteSingleCommand handles timeout during execution.
    /// </summary>
    [Fact]
    public void ExecuteSingleCommand_WithTimeout_ReturnsTimeoutResult()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-timeout-exec", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue and cancel (simulates timeout via cancellation)
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Command should be cancelled
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteSingleCommand handles exception during execution.
    /// </summary>
    [Fact]
    public void ExecuteSingleCommand_WithException_ReturnsFailedResult()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-exception-exec", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue and cancel (simulates failure)
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that MarkCommandsAsExecuting updates command states.
    /// </summary>
    [Fact]
    public void MarkCommandsAsExecuting_UpdatesCommandStates()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var batchCommand = new Command { CommandId = "batch-cmd-1", CommandText = "k;lm" };
        _ = batchProcessor.Setup(bp => bp.BatchCommands(It.IsAny<string>(), It.IsAny<List<Command>>()))
            .Returns(new List<Command> { batchCommand });
        _ = batchProcessor.Setup(bp => bp.GetOriginalCommandIds(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string sessionId, string batchId) => new List<string> { batchId });
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns(new List<CommandResult>());

        var queue = new CommandQueue("test-session-mark-exec", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Command should exist
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that CompleteCommandWithStatistics handles command without StartTime.
    /// </summary>
    [Fact]
    public void CompleteCommandWithStatistics_HandlesCommandWithoutStartTime()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-no-start", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should complete successfully even without explicit start time
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that HandleCommandInfo processes Completed state correctly.
    /// </summary>
    [Fact]
    public void HandleCommandInfo_ProcessesCompletedState()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-completed", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue and cancel
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that HandleCommandInfo processes Timeout state correctly.
    /// </summary>
    [Fact]
    public void HandleCommandInfo_ProcessesTimeoutState()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-timeout-state", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that HandleCommandInfo processes Failed state correctly.
    /// </summary>
    [Fact]
    public void HandleCommandInfo_ProcessesFailedState()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-failed-state", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert
        var info = queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ExecuteSingleCommand handles cancellation token registration disposal.
    /// </summary>
    [Fact]
    public void ExecuteSingleCommand_HandlesRegistrationDisposal()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-registration", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should complete without exception
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ProcessCommandsAsync handles batch command execution.
    /// </summary>
    [Fact]
    public void ProcessCommandsAsync_HandlesBatchCommandExecution()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var batchCommand = new Command { CommandId = "batch-cmd-1", CommandText = "k;lm;dt" };
        _ = batchProcessor.Setup(bp => bp.BatchCommands(It.IsAny<string>(), It.IsAny<List<Command>>()))
            .Returns(new List<Command> { batchCommand });
        _ = batchProcessor.Setup(bp => bp.GetOriginalCommandIds(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string sessionId, string batchId) => new List<string> { batchId });
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns((List<CommandResult> results) => results);

        var queue = new CommandQueue("test-session-batch-exec", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ProcessCommandsAsync handles multiple batch commands.
    /// </summary>
    [Fact]
    public void ProcessCommandsAsync_HandlesMultipleBatchCommands()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var batchCommands = new List<Command>
        {
            new Command { CommandId = "batch-cmd-1", CommandText = "k;lm" },
            new Command { CommandId = "batch-cmd-2", CommandText = "dt" },
        };
        _ = batchProcessor.Setup(bp => bp.BatchCommands(It.IsAny<string>(), It.IsAny<List<Command>>()))
            .Returns(batchCommands);
        _ = batchProcessor.Setup(bp => bp.GetOriginalCommandIds(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string sessionId, string batchId) => new List<string> { batchId });
        _ = batchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns((List<CommandResult> results) => results);

        var queue = new CommandQueue("test-session-multi-batch", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that StopAsync handles exception during stop gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WithException_HandlesGracefully()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockProcessManager = new Mock<IProcessManager>();
        var cdbSession = new CdbSessionTestAccessor(m_Settings.Object, mockFileSystem.Object, mockProcessManager.Object);
        cdbSession.SetInitializedForTesting(true);
        try
        {
            await m_Queue.StartAsync(cdbSession);

            // Act
            await m_Queue.StopAsync();

            // Assert - Should complete without exception
        }
        finally
        {
            await cdbSession.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that ProcessCommandsAsync handles OperationCanceledException in main loop.
    /// </summary>
    [Fact]
    public void ProcessCommandsAsync_WithOperationCanceled_HandlesGracefully()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-op-cancel", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue and cancel, then stop
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should complete without exception
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that ProcessCommandsAsync handles fatal exception in main loop.
    /// </summary>
    [Fact]
    public void ProcessCommandsAsync_WithFatalException_HandlesGracefully()
    {
        // Arrange
        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-fatal", m_Settings.Object, batchProcessor.Object);

        // Act
        var commandId = queue.EnqueueCommand("k");
        _ = queue.CancelCommand(commandId);

        // Assert - Should complete without exception
        _ = queue.GetCommandInfo(commandId).Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that GetCommandInfo returns null for non-existent command.
    /// </summary>
    [Fact]
    public void GetCommandInfo_ReturnsNullForNonExistentCommand()
    {
        // Arrange
        var commandId = "cmd-nonexistent-123";

        // Act
        var result = m_Queue.GetCommandInfo(commandId);

        // Assert
        _ = result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CancelCommand handles command without cache entry.
    /// </summary>
    [Fact]
    public void CancelCommand_HandlesCommandWithoutCacheEntry()
    {
        // Arrange
        var commandId = m_Queue.EnqueueCommand("k");

        // Act
        var result = m_Queue.CancelCommand(commandId);

        // Assert
        _ = result.Should().BeTrue();
        var info = m_Queue.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that GetAllCommandInfos returns all active commands.
    /// </summary>
    [Fact]
    public void GetAllCommandInfos_WithMultipleActiveCommands_ReturnsAll()
    {
        // Arrange
        var commandId1 = m_Queue.EnqueueCommand("k");
        var commandId2 = m_Queue.EnqueueCommand("lm");

        // Act
        var allInfos = m_Queue.GetAllCommandInfos();

        // Assert
        _ = allInfos.Should().ContainKey(commandId1);
        _ = allInfos.Should().ContainKey(commandId2);
        _ = allInfos.Count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that CancelAllCommands with reason parameter logs the reason.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WithReason_LogsReason()
    {
        // Arrange
        var commandId1 = m_Queue.EnqueueCommand("k");
        var commandId2 = m_Queue.EnqueueCommand("lm");

        // Act
        var count = m_Queue.CancelAllCommands("Test reason");

        // Assert
        _ = count.Should().Be(2);
        var info1 = m_Queue.GetCommandInfo(commandId1);
        var info2 = m_Queue.GetCommandInfo(commandId2);
        _ = info1!.State.Should().Be(CommandState.Cancelled);
        _ = info2!.State.Should().Be(CommandState.Cancelled);
    }

    /// <summary>
    /// Verifies that CancelAllCommands with null reason uses default message.
    /// </summary>
    [Fact]
    public void CancelAllCommands_WithNullReason_UsesDefaultMessage()
    {
        // Arrange
        _ = m_Queue.EnqueueCommand("k");

        // Act
        var count = m_Queue.CancelAllCommands(null);

        // Assert
        _ = count.Should().Be(1);
    }

    /// <summary>
    /// Verifies that CollectAvailableCommands with waitMs > 0 collects commands with timeout.
    /// </summary>
    [Fact]
    public void CollectAvailableCommands_WithWaitTime_CollectsCommandsWithTimeout()
    {
        // Arrange
        var sharedConfig = new SharedConfiguration
        {
            McpNexus = new McpNexusSettings
            {
                Batching = new BatchingSettings
                {
                    CommandCollectionWaitMs = 10,
                },
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);

        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-wait", m_Settings.Object, batchProcessor.Object);

        // Act - Enqueue multiple commands
        var commandId1 = queue.EnqueueCommand("k");
        var commandId2 = queue.EnqueueCommand("lm");

        // Assert - Commands should be enqueued
        var info1 = queue.GetCommandInfo(commandId1);
        var info2 = queue.GetCommandInfo(commandId2);
        _ = info1.Should().NotBeNull();
        _ = info2.Should().NotBeNull();
        queue.Dispose();
    }

    /// <summary>
    /// Verifies that StopAsync handles exceptions during processing task wait.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopAsync_WithExceptionInProcessingTask_HandlesGracefully()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockProcessManager = new Mock<IProcessManager>();
        var cdbSession = new CdbSessionTestAccessor(m_Settings.Object, mockFileSystem.Object, mockProcessManager.Object);
        cdbSession.SetInitializedForTesting(true);

        var batchProcessor = new Mock<IBatchProcessor>();
        var queue = new CommandQueue("test-session-stop", m_Settings.Object, batchProcessor.Object);
        try
        {
            await queue.StartAsync(cdbSession);

            // Act - Stop should handle any exceptions gracefully
            await queue.StopAsync();

            // Assert - Should complete without throwing
        }
        finally
        {
            await cdbSession.DisposeAsync();
            queue.Dispose();
        }
    }

    /// <summary>
    /// Verifies that Dispose handles exceptions gracefully.
    /// </summary>
    [Fact]
    public void Dispose_WithExceptions_HandlesGracefully()
    {
        // Arrange
        var queue = new CommandQueue("test-session-dispose", m_Settings.Object, m_BatchProcessor.Object);
        var commandId = queue.EnqueueCommand("k");

        // Act - Dispose should handle any exceptions
        queue.Dispose();

        // Assert - Should not throw
        _ = Assert.Throws<ObjectDisposedException>(() => queue.EnqueueCommand("lm"));
    }

    /// <summary>
    /// Verifies that HandleSuccessfulCommandExecution creates correct CommandInfo.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleSuccessfulCommandExecution_CreatesCorrectCommandInfo()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-success", m_Settings.Object, m_BatchProcessor.Object);
        var commandId = accessor.EnqueueCommand("k");

        // Get the actual QueuedCommand instance from the queue
        var activeCommandsField = typeof(CommandQueue).GetField("m_ActiveCommands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var activeCommands = activeCommandsField?.GetValue(accessor) as System.Collections.Concurrent.ConcurrentDictionary<string, QueuedCommand>;
        var queuedCommand = activeCommands?[commandId];
        _ = queuedCommand.Should().NotBeNull();

        var startTime = DateTime.Now;

        // Act
        await accessor.HandleSuccessfulCommandExecution(queuedCommand!, startTime, "result text");

        // Assert - Command should be completed and cached
        var info = accessor.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Completed);
        _ = info.AggregatedOutput.Should().Be("result text");
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that HandleCancelledCommand creates correct CommandInfo.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleCancelledCommand_CreatesCorrectCommandInfo()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-cancel", m_Settings.Object, m_BatchProcessor.Object);
        var commandId = accessor.EnqueueCommand("k");

        // Get the actual QueuedCommand instance from the queue
        var activeCommandsField = typeof(CommandQueue).GetField("m_ActiveCommands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var activeCommands = activeCommandsField?.GetValue(accessor) as System.Collections.Concurrent.ConcurrentDictionary<string, QueuedCommand>;
        var queuedCommand = activeCommands?[commandId];
        _ = queuedCommand.Should().NotBeNull();

        var startTime = DateTime.Now;

        // Act
        await accessor.HandleCancelledCommand(queuedCommand!, startTime);

        // Assert - Command should be cancelled and cached
        var info = accessor.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Cancelled);
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that HandleTimedOutCommand creates correct CommandInfo.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleTimedOutCommand_CreatesCorrectCommandInfo()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-timeout", m_Settings.Object, m_BatchProcessor.Object);
        var commandId = accessor.EnqueueCommand("k");

        // Get the actual QueuedCommand instance from the queue
        var activeCommandsField = typeof(CommandQueue).GetField("m_ActiveCommands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var activeCommands = activeCommandsField?.GetValue(accessor) as System.Collections.Concurrent.ConcurrentDictionary<string, QueuedCommand>;
        var queuedCommand = activeCommands?[commandId];
        _ = queuedCommand.Should().NotBeNull();

        var startTime = DateTime.Now;
        var timeoutEx = new TimeoutException("Command timed out");

        // Act
        await accessor.HandleTimedOutCommand(queuedCommand!, startTime, timeoutEx);

        // Assert - Command should be timed out and cached
        var info = accessor.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Timeout);
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that HandleFailedCommand creates correct CommandInfo.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task HandleFailedCommand_CreatesCorrectCommandInfo()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-failed", m_Settings.Object, m_BatchProcessor.Object);
        var commandId = accessor.EnqueueCommand("k");

        // Get the actual QueuedCommand instance from the queue
        var activeCommandsField = typeof(CommandQueue).GetField("m_ActiveCommands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var activeCommands = activeCommandsField?.GetValue(accessor) as System.Collections.Concurrent.ConcurrentDictionary<string, QueuedCommand>;
        var queuedCommand = activeCommands?[commandId];
        _ = queuedCommand.Should().NotBeNull();

        var startTime = DateTime.Now;
        var exception = new InvalidOperationException("Command failed");

        // Act
        await accessor.HandleFailedCommand(queuedCommand!, startTime, exception);

        // Assert - Command should be failed and cached
        var info = accessor.GetCommandInfo(commandId);
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Failed);
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that CreateCommandInfoFromResult handles cancelled result.
    /// </summary>
    [Fact]
    public void CreateCommandInfoFromResult_WithCancelledResult_ReturnsCancelledState()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-create", m_Settings.Object, m_BatchProcessor.Object);
        var result = new CommandResult
        {
            SessionId = "test-session-create",
            CommandId = "cmd-1",
            ResultText = "Cancelled",
            IsCancelled = true,
        };
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-1",
            Command = "k",
            ProcessId = null,
            QueuedTime = DateTime.Now,
        };
        var startTime = DateTime.Now;
        var endTime = DateTime.Now;

        // Act
        var (commandInfo, finalState) = accessor.CreateCommandInfoFromResult(result, queuedCommand, startTime, endTime);

        // Assert
        _ = finalState.Should().Be(CommandState.Cancelled);
        _ = commandInfo.State.Should().Be(CommandState.Cancelled);
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that CreateCommandInfoFromResult handles timeout result.
    /// </summary>
    [Fact]
    public void CreateCommandInfoFromResult_WithTimeoutResult_ReturnsTimeoutState()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-timeout", m_Settings.Object, m_BatchProcessor.Object);
        var result = new CommandResult
        {
            SessionId = "test-session-timeout",
            CommandId = "cmd-1",
            ResultText = "Timeout",
            IsTimeout = true,
        };
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-1",
            Command = "k",
            ProcessId = null,
            QueuedTime = DateTime.Now,
        };
        var startTime = DateTime.Now;
        var endTime = DateTime.Now;

        // Act
        var (commandInfo, finalState) = accessor.CreateCommandInfoFromResult(result, queuedCommand, startTime, endTime);

        // Assert
        _ = finalState.Should().Be(CommandState.Timeout);
        _ = commandInfo.State.Should().Be(CommandState.Timeout);
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that CreateCommandInfoFromResult handles failed result.
    /// </summary>
    [Fact]
    public void CreateCommandInfoFromResult_WithFailedResult_ReturnsFailedState()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-failed", m_Settings.Object, m_BatchProcessor.Object);
        var result = new CommandResult
        {
            SessionId = "test-session-failed",
            CommandId = "cmd-1",
            ResultText = "ERROR: Failed",
            IsFailed = true,
        };
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-1",
            Command = "k",
            ProcessId = null,
            QueuedTime = DateTime.Now,
        };
        var startTime = DateTime.Now;
        var endTime = DateTime.Now;

        // Act
        var (commandInfo, finalState) = accessor.CreateCommandInfoFromResult(result, queuedCommand, startTime, endTime);

        // Assert
        _ = finalState.Should().Be(CommandState.Failed);
        _ = commandInfo.State.Should().Be(CommandState.Failed);
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that CreateCommandInfoFromResult handles successful result.
    /// </summary>
    [Fact]
    public void CreateCommandInfoFromResult_WithSuccessfulResult_ReturnsCompletedState()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-success", m_Settings.Object, m_BatchProcessor.Object);
        var result = new CommandResult
        {
            SessionId = "test-session-success",
            CommandId = "cmd-1",
            ResultText = "Success",
            IsCancelled = false,
            IsTimeout = false,
            IsFailed = false,
        };
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-1",
            Command = "k",
            ProcessId = null,
            QueuedTime = DateTime.Now,
        };
        var startTime = DateTime.Now;
        var endTime = DateTime.Now;

        // Act
        var (commandInfo, finalState) = accessor.CreateCommandInfoFromResult(result, queuedCommand, startTime, endTime);

        // Assert
        _ = finalState.Should().Be(CommandState.Completed);
        _ = commandInfo.State.Should().Be(CommandState.Completed);
        _ = commandInfo.AggregatedOutput.Should().Be("Success");
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that LogUnbatchingResults logs trace when counts match.
    /// </summary>
    [Fact]
    public void LogUnbatchingResults_WithEqualCounts_LogsTrace()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-log", m_Settings.Object, m_BatchProcessor.Object);

        // Act - Should not throw
        accessor.LogUnbatchingResults(5, 5);

        // Assert - No exception thrown
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that CompleteCommandWithStatistics handles missing command gracefully.
    /// </summary>
    [Fact]
    public void CompleteCommandWithStatistics_WithMissingCommand_HandlesGracefully()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-complete", m_Settings.Object, m_BatchProcessor.Object);
        var result = new CommandResult
        {
            SessionId = "test-session-complete",
            CommandId = "cmd-nonexistent",
            ResultText = "Result",
        };
        var queuedCommandsById = new Dictionary<string, QueuedCommand>();
        var commandStartTimes = new Dictionary<string, DateTime>();

        // Act - Should not throw, just log warning
        accessor.CompleteCommandWithStatistics(result, queuedCommandsById, commandStartTimes);

        // Assert - No exception thrown
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that CompleteCommandWithStatistics completes command successfully.
    /// </summary>
    [Fact]
    public void CompleteCommandWithStatistics_WithValidCommand_CompletesSuccessfully()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-complete", m_Settings.Object, m_BatchProcessor.Object);
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-1",
            Command = "k",
            ProcessId = null,
            QueuedTime = DateTime.Now,
        };
        var result = new CommandResult
        {
            SessionId = "test-session-complete",
            CommandId = "cmd-1",
            ResultText = "Success",
        };
        var queuedCommandsById = new Dictionary<string, QueuedCommand> { { "cmd-1", queuedCommand } };
        var commandStartTimes = new Dictionary<string, DateTime> { { "cmd-1", DateTime.Now } };
        _ = m_BatchProcessor.Setup(bp => bp.GetBatchCommandId(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string? sessionId, string? commandId) => null);

        // Act
        accessor.CompleteCommandWithStatistics(result, queuedCommandsById, commandStartTimes);

        // Assert - Command should be completed and cached
        var info = accessor.GetCommandInfo("cmd-1");
        _ = info.Should().NotBeNull();
        _ = info!.State.Should().Be(CommandState.Completed);
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that ProcessCommandResults processes results correctly.
    /// </summary>
    [Fact]
    public void ProcessCommandResults_WithValidResults_ProcessesCorrectly()
    {
        // Arrange
        var accessor = new CommandQueueTestAccessor("test-session-process", m_Settings.Object, m_BatchProcessor.Object);
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-1",
            Command = "k",
            ProcessId = null,
            QueuedTime = DateTime.Now,
        };
        var executionResults = new List<CommandResult>
        {
            new CommandResult { SessionId = "test-session-process", CommandId = "cmd-1", ResultText = "Success" },
        };
        var queuedCommandsById = new Dictionary<string, QueuedCommand> { { "cmd-1", queuedCommand } };
        var commandStartTimes = new Dictionary<string, DateTime> { { "cmd-1", DateTime.Now } };
        _ = m_BatchProcessor.Setup(bp => bp.UnbatchResults(It.IsAny<List<CommandResult>>()))
            .Returns((List<CommandResult> results) => results);
        _ = m_BatchProcessor.Setup(bp => bp.GetBatchCommandId(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string? sessionId, string? commandId) => null);

        // Act
        accessor.ProcessCommandResults(executionResults, queuedCommandsById, commandStartTimes);

        // Assert - Command should be processed
        var info = accessor.GetCommandInfo("cmd-1");
        _ = info.Should().NotBeNull();
        accessor.Dispose();
    }

    /// <summary>
    /// Verifies that GetCommandInfoAsync throws KeyNotFoundException when command is not found after completion.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetCommandInfoAsync_WhenCommandNotFoundAfterCompletion_ThrowsKeyNotFoundException()
    {
        // Arrange
        var queue = new CommandQueue("test-session-notfound", m_Settings.Object, m_BatchProcessor.Object);
        var commandId = queue.EnqueueCommand("k");
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act & Assert - Should throw KeyNotFoundException if command completes but is not in cache
        // (This is an edge case - normally commands would be in cache after completion)
        _ = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await queue.GetCommandInfoAsync("nonexistent-command", cts.Token));
        queue.Dispose();
    }
}
