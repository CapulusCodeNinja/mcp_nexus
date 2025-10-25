using FluentAssertions;

using Xunit;

namespace Nexus.Engine.Share.Tests;

/// <summary>
/// Unit tests for CommandIdGenerator class.
/// Tests command ID generation, session counter management, and thread safety.
/// </summary>
public class CommandIdGeneratorTests : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the CommandIdGeneratorTests class.
    /// </summary>
    public CommandIdGeneratorTests()
    {
        // Clear any existing state before each test
        CommandIdGenerator.Instance.Clear();
    }

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        // Clean up after each test
        CommandIdGenerator.Instance.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that GenerateCommandId throws ArgumentException when sessionId is null.
    /// </summary>
    [Fact]
    public void GenerateCommandId_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => CommandIdGenerator.Instance.GenerateCommandId(null!));
    }

    /// <summary>
    /// Verifies that GenerateCommandId throws ArgumentException when sessionId is empty.
    /// </summary>
    [Fact]
    public void GenerateCommandId_WithEmptySessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => CommandIdGenerator.Instance.GenerateCommandId(string.Empty));
    }

    /// <summary>
    /// Verifies that GenerateCommandId throws ArgumentException when sessionId is whitespace.
    /// </summary>
    [Fact]
    public void GenerateCommandId_WithWhitespaceSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => CommandIdGenerator.Instance.GenerateCommandId("   "));
    }

    /// <summary>
    /// Verifies that GenerateCommandId generates ID with correct format.
    /// </summary>
    [Fact]
    public void GenerateCommandId_GeneratesCorrectFormat()
    {
        // Arrange
        const string sessionId = "test-session-1";

        // Act
        var commandId = CommandIdGenerator.Instance.GenerateCommandId(sessionId);

        // Assert - Should be cmd-{sessionId}-{number}
        _ = commandId.Should().MatchRegex(@"^cmd-test-session-1-\d+$");
    }

    /// <summary>
    /// Verifies that GenerateCommandId starts counter at 1 for new session.
    /// </summary>
    [Fact]
    public void GenerateCommandId_StartsCounterAtOne()
    {
        // Arrange
        const string sessionId = "new-session";

        // Act
        var commandId = CommandIdGenerator.Instance.GenerateCommandId(sessionId);

        // Assert
        _ = commandId.Should().Be("cmd-new-session-1");
    }

    /// <summary>
    /// Verifies that GenerateCommandId increments counter for subsequent calls.
    /// </summary>
    [Fact]
    public void GenerateCommandId_IncrementsCounterSequentially()
    {
        // Arrange
        const string sessionId = "test-session";

        // Act
        var commandId1 = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
        var commandId2 = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
        var commandId3 = CommandIdGenerator.Instance.GenerateCommandId(sessionId);

        // Assert
        _ = commandId1.Should().Be("cmd-test-session-1");
        _ = commandId2.Should().Be("cmd-test-session-2");
        _ = commandId3.Should().Be("cmd-test-session-3");
    }

    /// <summary>
    /// Verifies that GenerateCommandId maintains separate counters for different sessions.
    /// </summary>
    [Fact]
    public void GenerateCommandId_MaintainsSeparateCountersPerSession()
    {
        // Arrange
        const string sessionId1 = "session-1";
        const string sessionId2 = "session-2";

        // Act
        var cmd1_1 = CommandIdGenerator.Instance.GenerateCommandId(sessionId1);
        var cmd2_1 = CommandIdGenerator.Instance.GenerateCommandId(sessionId2);
        var cmd1_2 = CommandIdGenerator.Instance.GenerateCommandId(sessionId1);
        var cmd2_2 = CommandIdGenerator.Instance.GenerateCommandId(sessionId2);

        // Assert
        _ = cmd1_1.Should().Be("cmd-session-1-1");
        _ = cmd2_1.Should().Be("cmd-session-2-1");
        _ = cmd1_2.Should().Be("cmd-session-1-2");
        _ = cmd2_2.Should().Be("cmd-session-2-2");
    }

    /// <summary>
    /// Verifies that ResetSession removes session counter.
    /// </summary>
    [Fact]
    public void ResetSession_RemovesSessionCounter()
    {
        // Arrange
        const string sessionId = "test-session";
        _ = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
        _ = CommandIdGenerator.Instance.GenerateCommandId(sessionId);

        // Act
        var result = CommandIdGenerator.Instance.ResetSession(sessionId);

        // Assert
        _ = result.Should().BeTrue();
        _ = CommandIdGenerator.Instance.GetCurrentCount(sessionId).Should().Be(0);
    }

    /// <summary>
    /// Verifies that ResetSession returns false for non-existent session.
    /// </summary>
    [Fact]
    public void ResetSession_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = CommandIdGenerator.Instance.ResetSession("non-existent-session");

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ResetSession returns false for null sessionId.
    /// </summary>
    [Fact]
    public void ResetSession_WithNullSessionId_ReturnsFalse()
    {
        // Act
        var result = CommandIdGenerator.Instance.ResetSession(null!);

        // Assert
        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GenerateCommandId restarts counter after reset.
    /// </summary>
    [Fact]
    public void GenerateCommandId_AfterReset_RestartsCounterAtOne()
    {
        // Arrange
        const string sessionId = "test-session";
        _ = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
        _ = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
        _ = CommandIdGenerator.Instance.ResetSession(sessionId);

        // Act
        var commandId = CommandIdGenerator.Instance.GenerateCommandId(sessionId);

        // Assert
        _ = commandId.Should().Be("cmd-test-session-1");
    }

    /// <summary>
    /// Verifies that GetCurrentCount returns correct counter value.
    /// </summary>
    [Fact]
    public void GetCurrentCount_ReturnsCorrectValue()
    {
        // Arrange
        const string sessionId = "test-session";
        _ = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
        _ = CommandIdGenerator.Instance.GenerateCommandId(sessionId);

        // Act
        var count = CommandIdGenerator.Instance.GetCurrentCount(sessionId);

        // Assert
        _ = count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that GetCurrentCount returns zero for non-existent session.
    /// </summary>
    [Fact]
    public void GetCurrentCount_WithNonExistentSession_ReturnsZero()
    {
        // Act
        var count = CommandIdGenerator.Instance.GetCurrentCount("non-existent-session");

        // Assert
        _ = count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that GetCurrentCount returns zero for null sessionId.
    /// </summary>
    [Fact]
    public void GetCurrentCount_WithNullSessionId_ReturnsZero()
    {
        // Act
        var count = CommandIdGenerator.Instance.GetCurrentCount(null!);

        // Assert
        _ = count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that GetActiveSessionCount returns correct number of sessions.
    /// </summary>
    [Fact]
    public void GetActiveSessionCount_ReturnsCorrectCount()
    {
        // Arrange
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-1");
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-2");
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-3");

        // Act
        var count = CommandIdGenerator.Instance.GetActiveSessionCount();

        // Assert
        _ = count.Should().Be(3);
    }

    /// <summary>
    /// Verifies that GetActiveSessionCount decreases after ResetSession.
    /// </summary>
    [Fact]
    public void GetActiveSessionCount_DecreasesAfterReset()
    {
        // Arrange
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-1");
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-2");
        _ = CommandIdGenerator.Instance.ResetSession("session-1");

        // Act
        var count = CommandIdGenerator.Instance.GetActiveSessionCount();

        // Assert
        _ = count.Should().Be(1);
    }

    /// <summary>
    /// Verifies that GenerateCommandId is thread-safe.
    /// </summary>
    [Fact]
    public void GenerateCommandId_IsThreadSafe()
    {
        // Arrange
        const string sessionId = "concurrent-session";
        const int threadCount = 10;
        const int commandsPerThread = 100;
        var commandIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Generate command IDs from multiple threads concurrently
        _ = Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < commandsPerThread; i++)
            {
                var commandId = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
                commandIds.Add(commandId);
            }
        });

        // Assert
        var expectedCount = threadCount * commandsPerThread;
        _ = commandIds.Should().HaveCount(expectedCount);
        _ = commandIds.Should().OnlyHaveUniqueItems(); // All IDs should be unique
        _ = CommandIdGenerator.Instance.GetCurrentCount(sessionId).Should().Be(expectedCount);
    }

    /// <summary>
    /// Verifies that multiple sessions can be used concurrently without conflicts.
    /// </summary>
    [Fact]
    public void GenerateCommandId_WithMultipleSessions_IsThreadSafe()
    {
        // Arrange
        const int sessionCount = 5;
        const int commandsPerSession = 100;
        var commandIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Generate command IDs for multiple sessions concurrently
        _ = Parallel.For(0, sessionCount, sessionIndex =>
        {
            var sessionId = $"session-{sessionIndex}";
            for (var i = 0; i < commandsPerSession; i++)
            {
                var commandId = CommandIdGenerator.Instance.GenerateCommandId(sessionId);
                commandIds.Add(commandId);
            }
        });

        // Assert
        var expectedCount = sessionCount * commandsPerSession;
        _ = commandIds.Should().HaveCount(expectedCount);
        _ = commandIds.Should().OnlyHaveUniqueItems(); // All IDs should be unique across all sessions
        _ = CommandIdGenerator.Instance.GetActiveSessionCount().Should().Be(sessionCount);

        // Verify each session has the correct count
        for (var i = 0; i < sessionCount; i++)
        {
            var sessionId = $"session-{i}";
            _ = CommandIdGenerator.Instance.GetCurrentCount(sessionId).Should().Be(commandsPerSession);
        }
    }

    /// <summary>
    /// Verifies that Clear removes all session counters.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllSessionCounters()
    {
        // Arrange
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-1");
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-2");
        _ = CommandIdGenerator.Instance.GenerateCommandId("session-3");

        // Act
        CommandIdGenerator.Instance.Clear();

        // Assert
        _ = CommandIdGenerator.Instance.GetActiveSessionCount().Should().Be(0);
        _ = CommandIdGenerator.Instance.GetCurrentCount("session-1").Should().Be(0);
        _ = CommandIdGenerator.Instance.GetCurrentCount("session-2").Should().Be(0);
        _ = CommandIdGenerator.Instance.GetCurrentCount("session-3").Should().Be(0);
    }
}

