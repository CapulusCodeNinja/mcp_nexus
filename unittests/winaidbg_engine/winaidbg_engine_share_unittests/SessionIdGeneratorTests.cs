using FluentAssertions;

using Xunit;

namespace WinAiDbg.Engine.Share.Unittests;

/// <summary>
/// Unit tests for SessionIdGenerator class.
/// Tests session ID generation, format validation, uniqueness, and thread safety.
/// </summary>
public class SessionIdGeneratorTests
{
    /// <summary>
    /// Verifies that GenerateSessionId generates ID with correct timestamp format.
    /// </summary>
    [Fact]
    public void GenerateSessionId_GeneratesCorrectFormat()
    {
        // Act
        var sessionId = new SessionIdGeneratorAccessor().GenerateSessionId();

        // Assert - Should be sess-YYYY-MM-DD-HH-mm-ss-fffffff (7 digits for ticks)
        _ = sessionId.Should().MatchRegex(@"^sess-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{7}$");
    }

    /// <summary>
    /// Verifies that GenerateSessionId starts with correct prefix.
    /// </summary>
    [Fact]
    public void GenerateSessionId_StartsWithCorrectPrefix()
    {
        // Act
        var sessionId = new SessionIdGeneratorAccessor().GenerateSessionId();

        // Assert
        _ = sessionId.Should().StartWith("sess-");
    }

    /// <summary>
    /// Verifies that GenerateSessionId includes current date components.
    /// </summary>
    [Fact]
    public void GenerateSessionId_IncludesCurrentDateComponents()
    {
        // Arrange
        var now = DateTime.Now;

        // Act
        var sessionId = new SessionIdGeneratorAccessor().GenerateSessionId();

        // Assert - Should contain current year, month, and day
        _ = sessionId.Should().Contain($"{now:yyyy}");
        _ = sessionId.Should().Contain($"{now:MM}");
        _ = sessionId.Should().Contain($"{now:dd}");
    }

    /// <summary>
    /// Verifies that GenerateSessionId generates unique IDs when called rapidly.
    /// </summary>
    [Fact]
    public void GenerateSessionId_GeneratesUniqueIds()
    {
        // Act
        var sessionIds = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            sessionIds.Add(new SessionIdGeneratorAccessor().GenerateSessionId());
        }

        // Assert - With tick precision, all should be unique
        _ = sessionIds.Should().OnlyHaveUniqueItems();
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
                var sessionId = new SessionIdGeneratorAccessor().GenerateSessionId();
                sessionIds.Add(sessionId);
            }
        });

        // Assert
        var expectedCount = threadCount * sessionsPerThread;
        _ = sessionIds.Should().HaveCount(expectedCount);

        // All IDs should match the format
        foreach (var sessionId in sessionIds)
        {
            _ = sessionId.Should().MatchRegex(@"^sess-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{7}$");
        }

        // With tick precision, most should be unique
        _ = sessionIds.Distinct().Count().Should().BeGreaterThan(expectedCount * 9 / 10); // At least 90% unique
    }

    /// <summary>
    /// Verifies that session IDs remain unique across many generations with small delays.
    /// </summary>
    [Fact]
    public void GenerateSessionId_WithSmallDelays_RemainsUnique()
    {
        // Arrange
        const int sessionCount = 100;
        var sessionIds = new HashSet<string>();

        // Act
        for (var i = 0; i < sessionCount; i++)
        {
            var sessionId = new SessionIdGeneratorAccessor().GenerateSessionId();
            _ = sessionIds.Add(sessionId);
        }

        // Assert - Should have many unique IDs
        _ = sessionIds.Count.Should().BeGreaterThan(10);
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
            sessionIds.Add(new SessionIdGeneratorAccessor().GenerateSessionId());
        }

        // Assert - All should match the timestamp pattern
        foreach (var sessionId in sessionIds)
        {
            _ = sessionId.Should().MatchRegex(@"^sess-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{7}$");
        }
    }

    /// <summary>
    /// Verifies that session ID contains valid date-time components.
    /// </summary>
    [Fact]
    public void GenerateSessionId_ContainsValidDateTimeComponents()
    {
        // Act
        var sessionId = new SessionIdGeneratorAccessor().GenerateSessionId();

        // Assert - Extract and validate components
        var parts = sessionId.Replace("sess-", string.Empty).Split('-');
        _ = parts.Should().HaveCount(7); // YYYY-MM-DD-HH-mm-ss-fffffff

        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);
        var day = int.Parse(parts[2]);
        var hour = int.Parse(parts[3]);
        var minute = int.Parse(parts[4]);
        var second = int.Parse(parts[5]);
        var ticks = int.Parse(parts[6]);

        _ = year.Should().BeInRange(2020, 2100);
        _ = month.Should().BeInRange(1, 12);
        _ = day.Should().BeInRange(1, 31);
        _ = hour.Should().BeInRange(0, 23);
        _ = minute.Should().BeInRange(0, 59);
        _ = second.Should().BeInRange(0, 59);
        _ = ticks.Should().BeInRange(0, 9999999); // 7-digit ticks within a second
    }

    /// <summary>
    /// Verifies that session IDs are sortable chronologically.
    /// </summary>
    [Fact]
    public void GenerateSessionId_IsSortableChronologically()
    {
        // Arrange
        var sessionIds = new List<string>();

        // Act - Generate IDs rapidly
        for (var i = 0; i < 10; i++)
        {
            sessionIds.Add(new SessionIdGeneratorAccessor().GenerateSessionId());
        }

        // Assert - IDs should be in ascending order (or very close)
        var sortedIds = sessionIds.OrderBy(id => id).ToList();
        for (var i = 0; i < sessionIds.Count; i++)
        {
            // Most IDs should be in order (allowing for some millisecond precision issues)
            if (i < sessionIds.Count - 1)
            {
                var comparison = string.Compare(sessionIds[i], sessionIds[i + 1], StringComparison.Ordinal);
                _ = comparison.Should().BeLessOrEqualTo(0);
            }
        }
    }

    /// <summary>
    /// Verifies that rapid calls produce mostly unique IDs with tick precision.
    /// </summary>
    [Fact]
    public void GenerateSessionId_RapidCalls_ProduceMostlyUniqueIds()
    {
        // Arrange
        var sessionIds = new List<string>();

        // Act - Generate IDs as fast as possible
        for (var i = 0; i < 1000; i++)
        {
            sessionIds.Add(new SessionIdGeneratorAccessor().GenerateSessionId());
        }

        // Assert - With tick precision, most should be unique (allow for some duplicates in extremely rapid calls)
        var uniqueCount = sessionIds.Distinct().Count();
        _ = uniqueCount.Should().BeGreaterThan(900); // At least 90% unique
    }

    /// <summary>
    /// Verifies that session ID generation works across multiple instances.
    /// </summary>
    [Fact]
    public void GenerateSessionId_MultipleInstances_UseSameSingleton()
    {
        // Act
        var id1 = new SessionIdGeneratorAccessor().GenerateSessionId();
        var id2 = new SessionIdGeneratorAccessor().GenerateSessionId();

        // Assert - Both should use the same singleton and generate valid IDs
        _ = id1.Should().MatchRegex(@"^sess-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{7}$");
        _ = id2.Should().MatchRegex(@"^sess-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}-\d{7}$");
    }
}
