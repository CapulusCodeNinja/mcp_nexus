using FluentAssertions;

using Nexus.Engine.Share.Models;

using Xunit;

namespace Nexus.Engine.Unittests.Models;

/// <summary>
/// Unit tests for the SessionState enum.
/// </summary>
public class SessionStateTests
{
    /// <summary>
    /// Verifies that SessionState enum values have correct numeric values.
    /// </summary>
    /// <param name="state">The enum value under test.</param>
    /// <param name="expectedValue">The expected integer representation.</param>
    [Theory]
    [InlineData(SessionState.Initializing, 0)]
    [InlineData(SessionState.Active, 1)]
    [InlineData(SessionState.Closing, 2)]
    [InlineData(SessionState.Closed, 3)]
    [InlineData(SessionState.Faulted, 4)]
    public void EnumValues_ShouldHaveCorrectNumericValues(SessionState state, int expectedValue)
    {
        // Act & Assert
        _ = ((int)state).Should().Be(expectedValue);
    }

    /// <summary>
    /// Verifies that all SessionState enum values are defined and accessible.
    /// </summary>
    [Fact]
    public void AllEnumValues_ShouldBeDefined()
    {
        // Act
        var values = Enum.GetValues<SessionState>();

        // Assert
        _ = values.Should().HaveCount(5);
        _ = values.Should().Contain(SessionState.Initializing);
        _ = values.Should().Contain(SessionState.Active);
        _ = values.Should().Contain(SessionState.Closing);
        _ = values.Should().Contain(SessionState.Closed);
        _ = values.Should().Contain(SessionState.Faulted);
    }

    /// <summary>
    /// Verifies that all SessionState enum values are unique.
    /// </summary>
    [Fact]
    public void EnumValues_ShouldBeUnique()
    {
        // Act
        var values = Enum.GetValues<SessionState>().Cast<int>();

        // Assert
        _ = values.Should().OnlyHaveUniqueItems();
    }
}
