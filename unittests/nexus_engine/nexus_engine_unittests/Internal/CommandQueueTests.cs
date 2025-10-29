using FluentAssertions;

using Nexus.Engine.Internal;
using Nexus.Engine.Share.Events;
using Nexus.Engine.Share.Models;

using Xunit;

namespace Nexus.Engine.Unittests.Internal;

/// <summary>
/// Unit tests for CommandQueue class.
/// Tests command enqueuing, state management, cancellation, and result caching.
/// </summary>
public class CommandQueueTests : IDisposable
{
    private readonly CommandQueue m_Queue;
    private readonly List<CommandStateChangedEventArgs> m_StateChanges;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandQueueTests"/> class.
    /// </summary>
    public CommandQueueTests()
    {
        m_Queue = new CommandQueue("test-session-1");
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
        _ = Assert.Throws<ArgumentNullException>(() => new CommandQueue(null!));
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
}
