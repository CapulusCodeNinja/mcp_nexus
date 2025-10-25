using FluentAssertions;

using Xunit;

namespace Nexus.Engine.Share.Tests;

/// <summary>
/// Unit tests for SessionIdGenerator class.
/// Tests session ID generation, counter management, and thread safety.
/// </summary>
public class SessionIdGeneratorTests : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the SessionIdGeneratorTests class.
    /// </summary>
    public SessionIdGeneratorTests()
    {
        // Reset counter before each test
        SessionIdGenerator.Instance.Reset();
    }

    /// <summary>
    /// Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        // Clean up after each test
        SessionIdGenerator.Instance.Reset();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that GenerateSessionId generates ID with correct format.
    /// </summary>
    [Fact]
    public void GenerateSessionId_GeneratesCorrectFormat()
    {
        // Act
        var sessionId = SessionIdGenerator.Instance.GenerateSessionId();

        // Assert - Should be sess-{number}
        _ = sessionId.Should().MatchRegex(@"^sess-\d+$");
    }

    /// <summary>
    /// Verifies that GenerateSessionId starts counter at 1 for first call.
    /// </summary>
    [Fact]
    public void GenerateSessionId_StartsCounterAtOne()
    {
        // Act
        var sessionId = SessionIdGenerator.Instance.GenerateSessionId();

        // Assert
        _ = sessionId.Should().Be("sess-1");
    }

    /// <summary>
    /// Verifies that GenerateSessionId increments counter sequentially.
    /// </summary>
    [Fact]
    public void GenerateSessionId_IncrementsCounterSequentially()
    {
        // Act
        var sessionId1 = SessionIdGenerator.Instance.GenerateSessionId();
        var sessionId2 = SessionIdGenerator.Instance.GenerateSessionId();
        var sessionId3 = SessionIdGenerator.Instance.GenerateSessionId();

        // Assert
        _ = sessionId1.Should().Be("sess-1");
        _ = sessionId2.Should().Be("sess-2");
        _ = sessionId3.Should().Be("sess-3");
    }

    /// <summary>
    /// Verifies that GenerateSessionId generates unique IDs.
    /// </summary>
    [Fact]
    public void GenerateSessionId_GeneratesUniqueIds()
    {
        // Act
        var sessionIds = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            sessionIds.Add(SessionIdGenerator.Instance.GenerateSessionId());
        }

        // Assert
        _ = sessionIds.Should().OnlyHaveUniqueItems();
    }

    /// <summary>
    /// Verifies that GetCurrentCount returns correct counter value.
    /// </summary>
    [Fact]
    public void GetCurrentCount_ReturnsCorrectValue()
    {
        // Arrange
        _ = SessionIdGenerator.Instance.GenerateSessionId();
        _ = SessionIdGenerator.Instance.GenerateSessionId();
        _ = SessionIdGenerator.Instance.GenerateSessionId();

        // Act
        var count = SessionIdGenerator.Instance.GetCurrentCount();

        // Assert
        _ = count.Should().Be(3);
    }

    /// <summary>
    /// Verifies that GetCurrentCount returns zero after reset.
    /// </summary>
    [Fact]
    public void GetCurrentCount_AfterReset_ReturnsZero()
    {
        // Arrange
        _ = SessionIdGenerator.Instance.GenerateSessionId();
        _ = SessionIdGenerator.Instance.GenerateSessionId();

        // Act
        SessionIdGenerator.Instance.Reset();
        var count = SessionIdGenerator.Instance.GetCurrentCount();

        // Assert
        _ = count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that GenerateSessionId restarts counter after reset.
    /// </summary>
    [Fact]
    public void GenerateSessionId_AfterReset_RestartsCounterAtOne()
    {
        // Arrange
        _ = SessionIdGenerator.Instance.GenerateSessionId();
        _ = SessionIdGenerator.Instance.GenerateSessionId();
        SessionIdGenerator.Instance.Reset();

        // Act
        var sessionId = SessionIdGenerator.Instance.GenerateSessionId();

        // Assert
        _ = sessionId.Should().Be("sess-1");
    }

    /// <summary>
    /// Verifies that GenerateSessionId is thread-safe.
    /// </summary>
    [Fact]
    public void GenerateSessionId_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int sessionsPerThread = 100;
        var sessionIds = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Generate session IDs from multiple threads concurrently
        _ = Parallel.For(0, threadCount, _ =>
        {
            for (var i = 0; i < sessionsPerThread; i++)
            {
                var sessionId = SessionIdGenerator.Instance.GenerateSessionId();
                sessionIds.Add(sessionId);
            }
        });

        // Assert
        var expectedCount = threadCount * sessionsPerThread;
        _ = sessionIds.Should().HaveCount(expectedCount);
        _ = sessionIds.Should().OnlyHaveUniqueItems(); // All IDs should be unique
        _ = SessionIdGenerator.Instance.GetCurrentCount().Should().Be(expectedCount);
    }

    /// <summary>
    /// Verifies that Reset is thread-safe with concurrent generation.
    /// </summary>
    [Fact]
    public void Reset_WithConcurrentGeneration_IsThreadSafe()
    {
        // Arrange
        const int generationThreads = 5;
        const int sessionsPerThread = 50;
        var sessionIds = new System.Collections.Concurrent.ConcurrentBag<string>();
        var resetCalled = false;

        // Act - Generate session IDs from multiple threads, with one thread resetting mid-way
        _ = Parallel.For(0, generationThreads + 1, threadIndex =>
        {
            if (threadIndex == generationThreads)
            {
                // Reset thread - wait a bit then reset
                Thread.Sleep(10);
                SessionIdGenerator.Instance.Reset();
                resetCalled = true;
            }
            else
            {
                // Generation threads
                for (var i = 0; i < sessionsPerThread; i++)
                {
                    var sessionId = SessionIdGenerator.Instance.GenerateSessionId();
                    sessionIds.Add(sessionId);
                }
            }
        });

        // Assert
        _ = resetCalled.Should().BeTrue();
        _ = sessionIds.Should().HaveCountGreaterThan(0); // Some sessions should be generated
        _ = sessionIds.Should().OnlyHaveUniqueItems(); // All IDs should still be unique
    }

    /// <summary>
    /// Verifies that GetCurrentCount is thread-safe.
    /// </summary>
    [Fact]
    public void GetCurrentCount_IsThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int sessionsPerThread = 50;
        var counts = new System.Collections.Concurrent.ConcurrentBag<int>();

        // Act - Generate sessions and read count concurrently
        _ = Parallel.For(0, threadCount, threadIndex =>
        {
            if (threadIndex % 2 == 0)
            {
                // Generation threads
                for (var i = 0; i < sessionsPerThread; i++)
                {
                    _ = SessionIdGenerator.Instance.GenerateSessionId();
                }
            }
            else
            {
                // Count reading threads
                for (var i = 0; i < sessionsPerThread; i++)
                {
                    var count = SessionIdGenerator.Instance.GetCurrentCount();
                    counts.Add(count);
                }
            }
        });

        // Assert
        _ = counts.Should().HaveCountGreaterThan(0);
        _ = SessionIdGenerator.Instance.GetCurrentCount().Should().BeGreaterOrEqualTo(0);
    }

    /// <summary>
    /// Verifies that multiple resets work correctly.
    /// </summary>
    [Fact]
    public void Reset_MultipleTimes_WorksCorrectly()
    {
        // Act & Assert - Multiple reset cycles
        for (var cycle = 0; cycle < 5; cycle++)
        {
            // Generate some IDs
            var id1 = SessionIdGenerator.Instance.GenerateSessionId();
            var id2 = SessionIdGenerator.Instance.GenerateSessionId();

            _ = id1.Should().Be("sess-1");
            _ = id2.Should().Be("sess-2");
            _ = SessionIdGenerator.Instance.GetCurrentCount().Should().Be(2);

            // Reset
            SessionIdGenerator.Instance.Reset();
            _ = SessionIdGenerator.Instance.GetCurrentCount().Should().Be(0);
        }
    }

    /// <summary>
    /// Verifies that session IDs remain unique across many generations.
    /// </summary>
    [Fact]
    public void GenerateSessionId_ManyGenerations_RemainsUnique()
    {
        // Arrange
        const int sessionCount = 1000;
        var sessionIds = new HashSet<string>();

        // Act
        for (var i = 0; i < sessionCount; i++)
        {
            var sessionId = SessionIdGenerator.Instance.GenerateSessionId();
            _ = sessionIds.Add(sessionId).Should().BeTrue($"Session ID {sessionId} should be unique");
        }

        // Assert
        _ = sessionIds.Should().HaveCount(sessionCount);
    }

    /// <summary>
    /// Verifies that session ID format is consistent.
    /// </summary>
    [Fact]
    public void GenerateSessionId_FormatIsConsistent()
    {
        // Act
        var sessionIds = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            sessionIds.Add(SessionIdGenerator.Instance.GenerateSessionId());
        }

        // Assert - All should match the pattern
        foreach (var sessionId in sessionIds)
        {
            _ = sessionId.Should().MatchRegex(@"^sess-\d+$");
        }
    }
}

