using FluentAssertions;

using nexus.engine.Models;

using Xunit;

namespace nexus.engine.unittests.Models;

/// <summary>
/// Unit tests for the CommandState enum.
/// </summary>
public class CommandStateTests
{
    /// <summary>
    /// Verifies that CommandState enum values have correct numeric values.
    /// </summary>
    [Theory]
    [InlineData(CommandState.Queued, 0)]
    [InlineData(CommandState.Executing, 1)]
    [InlineData(CommandState.Completed, 2)]
    [InlineData(CommandState.Failed, 3)]
    [InlineData(CommandState.Cancelled, 4)]
    [InlineData(CommandState.Timeout, 5)]
    public void EnumValues_ShouldHaveCorrectNumericValues(CommandState state, int expectedValue)
    {
        // Act & Assert
        ((int)state).Should().Be(expectedValue);
    }

    /// <summary>
    /// Verifies that all CommandState enum values are defined and accessible.
    /// </summary>
    [Fact]
    public void AllEnumValues_ShouldBeDefined()
    {
        // Act
        var values = Enum.GetValues<CommandState>();

        // Assert
        values.Should().HaveCount(6);
        values.Should().Contain(CommandState.Queued);
        values.Should().Contain(CommandState.Executing);
        values.Should().Contain(CommandState.Completed);
        values.Should().Contain(CommandState.Failed);
        values.Should().Contain(CommandState.Cancelled);
        values.Should().Contain(CommandState.Timeout);
    }

    /// <summary>
    /// Verifies that all CommandState enum values are unique.
    /// </summary>
    [Fact]
    public void EnumValues_ShouldBeUnique()
    {
        // Act
        var values = Enum.GetValues<CommandState>().Cast<int>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
