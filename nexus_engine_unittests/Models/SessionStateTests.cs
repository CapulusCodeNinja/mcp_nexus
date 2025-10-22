using Xunit;
using FluentAssertions;
using nexus.engine.Models;

namespace nexus.engine.unittests.Models;

/// <summary>
/// Unit tests for the SessionState enum.
/// </summary>
public class SessionStateTests
{
    [Theory]
    [InlineData(SessionState.Initializing, 0)]
    [InlineData(SessionState.Active, 1)]
    [InlineData(SessionState.Closing, 2)]
    [InlineData(SessionState.Closed, 3)]
    [InlineData(SessionState.Faulted, 4)]
    public void EnumValues_ShouldHaveCorrectNumericValues(SessionState state, int expectedValue)
    {
        // Act & Assert
        ((int)state).Should().Be(expectedValue);
    }

    [Fact]
    public void AllEnumValues_ShouldBeDefined()
    {
        // Act
        var values = Enum.GetValues<SessionState>();

        // Assert
        values.Should().HaveCount(5);
        values.Should().Contain(SessionState.Initializing);
        values.Should().Contain(SessionState.Active);
        values.Should().Contain(SessionState.Closing);
        values.Should().Contain(SessionState.Closed);
        values.Should().Contain(SessionState.Faulted);
    }

    [Fact]
    public void EnumValues_ShouldBeUnique()
    {
        // Act
        var values = Enum.GetValues<SessionState>().Cast<int>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
