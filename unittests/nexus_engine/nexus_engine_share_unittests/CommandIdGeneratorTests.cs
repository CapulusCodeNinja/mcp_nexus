using FluentAssertions;

using Xunit;

namespace Nexus.Engine.Share.Tests;

/// <summary>
/// Unit tests for CommandIdGenerator class.
/// Tests command ID generation, session counter management, and thread safety.
/// </summary>
public class CommandIdGeneratorTests
{
    private readonly CommandIdGeneratorAccessor m_Generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandIdGeneratorTests"/> class.
    /// </summary>
    public CommandIdGeneratorTests()
    {
        m_Generator = new CommandIdGeneratorAccessor();
    }

    /// <summary>
    /// Verifies that GenerateCommandId throws ArgumentException when sessionId is null.
    /// </summary>
    [Fact]
    public void GenerateCommandId_WithNullSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Generator.GenerateCommandId(null!));
    }

    /// <summary>
    /// Verifies that GenerateCommandId throws ArgumentException when sessionId is empty.
    /// </summary>
    [Fact]
    public void GenerateCommandId_WithEmptySessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Generator.GenerateCommandId(string.Empty));
    }

    /// <summary>
    /// Verifies that GenerateCommandId throws ArgumentException when sessionId is whitespace.
    /// </summary>
    [Fact]
    public void GenerateCommandId_WithWhitespaceSessionId_ThrowsArgumentException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => m_Generator.GenerateCommandId("   "));
    }

    /// <summary>
    /// Verifies that GenerateCommandId generates ID with correct format.
    /// </summary>
    [Fact]
    public void GenerateCommandId_GeneratesCorrectFormat()
    {
        // Arrange
        const string sessionId = "test-session-format";

        // Act
        var commandId = m_Generator.GenerateCommandId(sessionId);

        // Assert - Should be cmd-{sessionId}-{number}
        _ = commandId.Should().MatchRegex(@"^cmd-test-session-format-\d+$");
    }

    /// <summary>
    /// Verifies that GenerateCommandId starts counter at 1 for new session.
    /// </summary>
    [Fact]
    public void GenerateCommandId_StartsCounterAtOne()
    {
        // Arrange
        const string sessionId = "new-session-start";

        // Act
        var commandId = m_Generator.GenerateCommandId(sessionId);

        // Assert
        _ = commandId.Should().Be("cmd-new-session-start-1");
    }

    /// <summary>
    /// Verifies that GenerateCommandId increments counter for subsequent calls.
    /// </summary>
    [Fact]
    public void GenerateCommandId_IncrementsCounterSequentially()
    {
        // Arrange
        const string sessionId = "test-session-seq";

        // Act
        var commandId1 = m_Generator.GenerateCommandId(sessionId);
        var commandId2 = m_Generator.GenerateCommandId(sessionId);
        var commandId3 = m_Generator.GenerateCommandId(sessionId);

        // Assert
        _ = commandId1.Should().Be("cmd-test-session-seq-1");
        _ = commandId2.Should().Be("cmd-test-session-seq-2");
        _ = commandId3.Should().Be("cmd-test-session-seq-3");
    }

    /// <summary>
    /// Verifies that GenerateCommandId maintains separate counters for different sessions.
    /// </summary>
    [Fact]
    public void GenerateCommandId_MaintainsSeparateCountersPerSession()
    {
        // Arrange
        const string sessionId1 = "session-separate-1";
        const string sessionId2 = "session-separate-2";

        // Act
        var cmd1_1 = m_Generator.GenerateCommandId(sessionId1);
        var cmd2_1 = m_Generator.GenerateCommandId(sessionId2);
        var cmd1_2 = m_Generator.GenerateCommandId(sessionId1);
        var cmd2_2 = m_Generator.GenerateCommandId(sessionId2);

        // Assert
        _ = cmd1_1.Should().Be("cmd-session-separate-1-1");
        _ = cmd2_1.Should().Be("cmd-session-separate-2-1");
        _ = cmd1_2.Should().Be("cmd-session-separate-1-2");
        _ = cmd2_2.Should().Be("cmd-session-separate-2-2");
    }

    /// <summary>
    /// Verifies that ResetSession removes session counter.
    /// </summary>
    [Fact]
    public void ResetSession_RemovesSessionCounter()
    {
        // Arrange
        const string sessionId = "test-session-reset";
        _ = m_Generator.GenerateCommandId(sessionId);
        _ = m_Generator.GenerateCommandId(sessionId);

        // Act
        var result = m_Generator.ResetSession(sessionId);

        // Assert
        _ = result.Should().BeTrue();
        _ = m_Generator.GetCurrentCount(sessionId).Should().Be(0);
    }

    /// <summary>
    /// Verifies that ResetSession returns false for non-existent session.
    /// </summary>
    [Fact]
    public void ResetSession_WithNonExistentSession_ReturnsFalse()
    {
        // Act
        var result = m_Generator.ResetSession("non-existent-session-reset");

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
        var result = m_Generator.ResetSession(null!);

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
        const string sessionId = "test-session-after-reset";
        _ = m_Generator.GenerateCommandId(sessionId);
        _ = m_Generator.GenerateCommandId(sessionId);
        _ = m_Generator.ResetSession(sessionId);

        // Act
        var commandId = m_Generator.GenerateCommandId(sessionId);

        // Assert
        _ = commandId.Should().Be("cmd-test-session-after-reset-1");
    }

    /// <summary>
    /// Verifies that GetCurrentCount returns correct counter value.
    /// </summary>
    [Fact]
    public void GetCurrentCount_ReturnsCorrectValue()
    {
        // Arrange
        const string sessionId = "test-session-count";
        _ = m_Generator.GenerateCommandId(sessionId);
        _ = m_Generator.GenerateCommandId(sessionId);

        // Act
        var count = m_Generator.GetCurrentCount(sessionId);

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
        var count = m_Generator.GetCurrentCount("non-existent-session-count");

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
        var count = m_Generator.GetCurrentCount(null!);

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
        _ = m_Generator.GenerateCommandId("session-active-1");
        _ = m_Generator.GenerateCommandId("session-active-2");
        _ = m_Generator.GenerateCommandId("session-active-3");

        // Act
        var count = m_Generator.GetActiveSessionCount();

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
        _ = m_Generator.GenerateCommandId("session-decrease-1");
        _ = m_Generator.GenerateCommandId("session-decrease-2");
        _ = m_Generator.ResetSession("session-decrease-1");

        // Act
        var count = m_Generator.GetActiveSessionCount();

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
        const string sessionId = "concurrent-session-thread";
        const int threadCount = 10;
        const int commandsPerThread = 100;
        var commandIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Generate command IDs from multiple threads concurrently
        _ = Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < commandsPerThread; i++)
            {
                var commandId = m_Generator.GenerateCommandId(sessionId);
                commandIds.Add(commandId);
            }
        });

        // Assert
        var expectedCount = threadCount * commandsPerThread;
        _ = commandIds.Should().HaveCount(expectedCount);
        _ = commandIds.Should().OnlyHaveUniqueItems(); // All IDs should be unique
        _ = m_Generator.GetCurrentCount(sessionId).Should().Be(expectedCount);
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
            var sessionId = $"session-multi-{sessionIndex}";
            for (var i = 0; i < commandsPerSession; i++)
            {
                var commandId = m_Generator.GenerateCommandId(sessionId);
                commandIds.Add(commandId);
            }
        });

        // Assert
        var expectedCount = sessionCount * commandsPerSession;
        _ = commandIds.Should().HaveCount(expectedCount);
        _ = commandIds.Should().OnlyHaveUniqueItems(); // All IDs should be unique across all sessions
        _ = m_Generator.GetActiveSessionCount().Should().Be(sessionCount);

        // Verify each session has the correct count
        for (var i = 0; i < sessionCount; i++)
        {
            var sessionId = $"session-multi-{i}";
            _ = m_Generator.GetCurrentCount(sessionId).Should().Be(commandsPerSession);
        }
    }

    /// <summary>
    /// Verifies that Clear removes all session counters.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllSessionCounters()
    {
        // Arrange
        _ = m_Generator.GenerateCommandId("session-clear-1");
        _ = m_Generator.GenerateCommandId("session-clear-2");
        _ = m_Generator.GenerateCommandId("session-clear-3");

        // Act
        m_Generator.Clear();

        // Assert
        _ = m_Generator.GetActiveSessionCount().Should().Be(0);
        _ = m_Generator.GetCurrentCount("session-clear-1").Should().Be(0);
        _ = m_Generator.GetCurrentCount("session-clear-2").Should().Be(0);
        _ = m_Generator.GetCurrentCount("session-clear-3").Should().Be(0);
    }
}

