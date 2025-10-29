using FluentAssertions;

using Nexus.Engine.Batch.Internal;

using Xunit;

namespace Nexus.Engine.Batch.Tests.Internal;

/// <summary>
/// Unit tests for BatchCommandFilter class.
/// Tests command filtering logic for batching eligibility.
/// </summary>
public class BatchCommandFilterTests
{
    private readonly BatchCommandFilter m_Filter;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCommandFilterTests"/> class.
    /// </summary>
    public BatchCommandFilterTests()
    {
        m_Filter = new BatchCommandFilter();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded returns true for analyze command.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_WithAnalyzeCommand_ReturnsTrue()
    {
        var result = m_Filter.IsCommandExcluded("!analyze -v");

        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded returns true for dump command.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_WithDumpCommand_ReturnsTrue()
    {
        var result = m_Filter.IsCommandExcluded("!dump /ma");

        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded returns true for heap command.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_WithHeapCommand_ReturnsTrue()
    {
        var result = m_Filter.IsCommandExcluded("!heap");

        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded returns false for simple command.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_WithSimpleCommand_ReturnsFalse()
    {
        var result = m_Filter.IsCommandExcluded("k");

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded returns false for lm command.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_WithLmCommand_ReturnsFalse()
    {
        var result = m_Filter.IsCommandExcluded("lm");

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded returns false for null command.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_WithNullCommand_ReturnsFalse()
    {
        var result = m_Filter.IsCommandExcluded(null!);

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded returns false for empty command.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_WithEmptyCommand_ReturnsFalse()
    {
        var result = m_Filter.IsCommandExcluded(string.Empty);

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsCommandExcluded is case insensitive.
    /// </summary>
    [Fact]
    public void IsCommandExcluded_IsCaseInsensitive()
    {
        var result1 = m_Filter.IsCommandExcluded("!ANALYZE");
        var result2 = m_Filter.IsCommandExcluded("!Analyze");
        var result3 = m_Filter.IsCommandExcluded("!analyze");

        _ = result1.Should().BeTrue();
        _ = result2.Should().BeTrue();
        _ = result3.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ShouldBatch returns false for null list.
    /// </summary>
    [Fact]
    public void ShouldBatch_WithNullList_ReturnsFalse()
    {
        var result = m_Filter.ShouldBatch(null!);

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ShouldBatch returns false for empty list.
    /// </summary>
    [Fact]
    public void ShouldBatch_WithEmptyList_ReturnsFalse()
    {
        var result = m_Filter.ShouldBatch(new List<Command>());

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ShouldBatch returns false for single command.
    /// </summary>
    [Fact]
    public void ShouldBatch_WithSingleCommand_ReturnsFalse()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
        };

        var result = m_Filter.ShouldBatch(commands);

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ShouldBatch returns true for two batchable commands.
    /// </summary>
    [Fact]
    public void ShouldBatch_WithTwoBatchableCommands_ReturnsTrue()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
            new Command { CommandId = "cmd-2", CommandText = "lm" },
        };

        var result = m_Filter.ShouldBatch(commands);

        _ = result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ShouldBatch returns false when all commands are excluded.
    /// </summary>
    [Fact]
    public void ShouldBatch_WithAllExcludedCommands_ReturnsFalse()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "!analyze -v" },
            new Command { CommandId = "cmd-2", CommandText = "!dump" },
        };

        var result = m_Filter.ShouldBatch(commands);

        _ = result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ShouldBatch returns false when any command is excluded.
    /// </summary>
    [Fact]
    public void ShouldBatch_WithMixedCommands_ReturnsFalse()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "!analyze -v" },
            new Command { CommandId = "cmd-2", CommandText = "k" },
            new Command { CommandId = "cmd-3", CommandText = "lm" },
        };

        var result = m_Filter.ShouldBatch(commands);

        _ = result.Should().BeFalse();
    }
}
