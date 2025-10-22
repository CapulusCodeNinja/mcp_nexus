using Xunit;
using FluentAssertions;
using mcp_nexus.Engine.Models;

namespace mcp_nexus.Engine.UnitTests.Models;

/// <summary>
/// Unit tests for the CommandState enum.
/// </summary>
public class CommandStateTests
{
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

    [Fact]
    public void EnumValues_ShouldBeUnique()
    {
        // Act
        var values = Enum.GetValues<CommandState>().Cast<int>();

        // Assert
        values.Should().OnlyHaveUniqueItems();
    }
}
