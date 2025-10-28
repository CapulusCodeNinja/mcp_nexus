using FluentAssertions;

using Nexus.Engine.Internal;
using Nexus.Engine.Share.Models;

using Xunit;

namespace Nexus.Engine.Tests.Internal;

/// <summary>
/// Unit tests for the <see cref="QueuedCommand"/> class.
/// </summary>
public class QueuedCommandTests
{
    #region Constructor and Properties Tests

    /// <summary>
    /// Verifies that constructor sets all required properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithRequiredProperties_Succeeds()
    {
        // Arrange
        var id = "cmd-123";
        var command = "!analyze -v";
        var queuedTime = DateTime.Now;

        // Act
        var queuedCommand = new QueuedCommand
        {
            Id = id,
            Command = command,
            QueuedTime = queuedTime,
            ProcessId = null
        };

        // Assert
        _ = queuedCommand.Should().NotBeNull();
        _ = queuedCommand.Id.Should().Be(id);
        _ = queuedCommand.Command.Should().Be(command);
        _ = queuedCommand.QueuedTime.Should().Be(queuedTime);
        _ = queuedCommand.State.Should().Be(CommandState.Queued);
        _ = (queuedCommand.CompletionSource != null).Should().BeTrue();
        _ = (queuedCommand.CancellationTokenSource != null).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that State property can be set and retrieved.
    /// </summary>
    [Fact]
    public void State_CanBeSetAndRetrieved()
    {
        // Arrange
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test",
            QueuedTime = DateTime.Now,
            ProcessId = null,
            State = CommandState.Executing
        };

        // Assert
        _ = queuedCommand.State.Should().Be(CommandState.Executing);
    }

    /// <summary>
    /// Verifies that CompletionSource property can be set.
    /// </summary>
    [Fact]
    public void CompletionSource_CanBeSet()
    {
        // Arrange
        var newCompletionSource = new TaskCompletionSource<CommandInfo>();
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test",
            QueuedTime = DateTime.Now,
            ProcessId = null,
            CompletionSource = newCompletionSource
        };

        // Assert
        _ = ReferenceEquals(queuedCommand.CompletionSource, newCompletionSource).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CancellationTokenSource property can be set.
    /// </summary>
    [Fact]
    public void CancellationTokenSource_CanBeSet()
    {
        // Arrange
        var newCancellationTokenSource = new CancellationTokenSource();
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test",
            QueuedTime = DateTime.Now,
            ProcessId = null,
            CancellationTokenSource = newCancellationTokenSource
        };

        // Assert
        _ = ReferenceEquals(queuedCommand.CancellationTokenSource, newCancellationTokenSource).Should().BeTrue();
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies that Dispose releases resources correctly.
    /// </summary>
    [Fact]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test",
            QueuedTime = DateTime.Now,
            ProcessId = null
        };

        // Act & Assert (should not throw)
        queuedCommand.Dispose();
    }

    /// <summary>
    /// Verifies that multiple Dispose calls are safe.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test",
            QueuedTime = DateTime.Now,
            ProcessId = null
        };

        // Act & Assert (should not throw)
        queuedCommand.Dispose();
        queuedCommand.Dispose();
        queuedCommand.Dispose();
    }

    /// <summary>
    /// Verifies that Dispose can be called with null CancellationTokenSource.
    /// </summary>
    [Fact]
    public void Dispose_WithNullCancellationTokenSource_Succeeds()
    {
        // Arrange
        var queuedCommand = new QueuedCommand
        {
            Id = "cmd-123",
            Command = "test",
            QueuedTime = DateTime.Now,
            ProcessId = null,
            CancellationTokenSource = null!
        };

        // Act & Assert (should not throw)
        queuedCommand.Dispose();
    }

    #endregion
}

