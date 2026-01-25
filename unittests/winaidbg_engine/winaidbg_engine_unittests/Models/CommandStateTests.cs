using FluentAssertions;

using WinAiDbg.Engine.Share.Models;

using Xunit;

namespace WinAiDbg.Engine.Unittests.Models;

/// <summary>
/// Unit tests for the CommandState enum.
/// </summary>
public class CommandStateTests
{
    /// <summary>
    /// Verifies that CommandState enum values have correct numeric values.
    /// </summary>
    /// <param name="state">The enum value under test.</param>
    /// <param name="expectedValue">The expected integer representation.</param>
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
        _ = ((int)state).Should().Be(expectedValue);
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
        _ = values.Should().HaveCount(6);
        _ = values.Should().Contain(CommandState.Queued);
        _ = values.Should().Contain(CommandState.Executing);
        _ = values.Should().Contain(CommandState.Completed);
        _ = values.Should().Contain(CommandState.Failed);
        _ = values.Should().Contain(CommandState.Cancelled);
        _ = values.Should().Contain(CommandState.Timeout);
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
        _ = values.Should().OnlyHaveUniqueItems();
    }
}
