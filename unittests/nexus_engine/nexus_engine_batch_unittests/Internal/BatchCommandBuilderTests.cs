using FluentAssertions;

using Nexus.Config;
using Nexus.Engine.Batch.Internal;

using Xunit;

namespace Nexus.Engine.Batch.Tests.Internal;

/// <summary>
/// Unit tests for BatchCommandBuilder class.
/// Tests command batching and grouping logic.
/// </summary>
public class BatchCommandBuilderTests
{
    private readonly ISettings m_Settings;
    private readonly BatchCommandBuilder m_Builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCommandBuilderTests"/> class.
    /// </summary>
    public BatchCommandBuilderTests()
    {
        m_Settings = new Settings();
        m_Builder = new BatchCommandBuilder(m_Settings);
    }

    /// <summary>
    /// Verifies that BuildBatch throws ArgumentException for null commands.
    /// </summary>
    [Fact]
    public void BuildBatch_WithNullCommands_ThrowsArgumentException()
    {
        var action = () => m_Builder.BuildBatch("test-session", null!);

        _ = action.Should().Throw<ArgumentException>()
            .WithMessage("*Commands list cannot be null or empty*")
            .WithParameterName("commands");
    }

    /// <summary>
    /// Verifies that BuildBatch throws ArgumentException for empty commands list.
    /// </summary>
    [Fact]
    public void BuildBatch_WithEmptyCommands_ThrowsArgumentException()
    {
        var action = () => m_Builder.BuildBatch("test-session", new List<Command>());

        _ = action.Should().Throw<ArgumentException>()
            .WithMessage("*Commands list cannot be null or empty*")
            .WithParameterName("commands");
    }

    /// <summary>
    /// Verifies that BuildBatch returns single command unchanged when list has only one command.
    /// </summary>
    [Fact]
    public void BuildBatch_WithSingleCommand_ReturnsSameCommand()
    {
        var command = new Command
        {
            CommandId = "cmd-1",
            CommandText = "k",
        };

        var result = m_Builder.BuildBatch("test-session", new List<Command> { command });

        _ = result.Should().BeSameAs(command);
        _ = result.CommandId.Should().Be("cmd-1");
        _ = result.CommandText.Should().Be("k");
    }

    /// <summary>
    /// Verifies that BuildBatch creates batch ID with embedded command IDs.
    /// </summary>
    [Fact]
    public void BuildBatch_WithMultipleCommands_CreatesBatchId()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
            new Command { CommandId = "cmd-2", CommandText = "lm" },
        };

        var result = m_Builder.BuildBatch("test-session", commands);

        _ = result.CommandId.Should().StartWith("cmd-test-session-");
    }

    /// <summary>
    /// Verifies that BuildBatch creates command text with sentinel markers.
    /// </summary>
    [Fact]
    public void BuildBatch_WithMultipleCommands_AddsSentinelMarkers()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
            new Command { CommandId = "cmd-2", CommandText = "lm" },
        };

        var result = m_Builder.BuildBatch("test-session", commands);

        _ = result.CommandText.Should().Contain("MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START");
        _ = result.CommandText.Should().Contain("MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END");
        _ = result.CommandText.Should().Contain("MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START");
        _ = result.CommandText.Should().Contain("MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END");
    }

    /// <summary>
    /// Verifies that BuildBatch includes all command texts in order.
    /// </summary>
    [Fact]
    public void BuildBatch_WithMultipleCommands_IncludesAllCommandTexts()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
            new Command { CommandId = "cmd-2", CommandText = "lm" },
            new Command { CommandId = "cmd-3", CommandText = "!threads" },
        };

        var result = m_Builder.BuildBatch("test-session", commands);

        _ = result.CommandText.Should().Contain("k");
        _ = result.CommandText.Should().Contain("lm");
        _ = result.CommandText.Should().Contain("!threads");

        // Verify order
        var indexK = result.CommandText.IndexOf("k", StringComparison.Ordinal);
        var indexLm = result.CommandText.IndexOf("lm", StringComparison.Ordinal);
        var indexThreads = result.CommandText.IndexOf("!threads", StringComparison.Ordinal);

        _ = indexK.Should().BeLessThan(indexLm);
        _ = indexLm.Should().BeLessThan(indexThreads);
    }

    /// <summary>
    /// Verifies that BuildBatch uses .echo commands for sentinels.
    /// </summary>
    [Fact]
    public void BuildBatch_WithMultipleCommands_UsesEchoCommandsForSentinels()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
            new Command { CommandId = "cmd-2", CommandText = "lm" },
        };

        var result = m_Builder.BuildBatch("test-session", commands);

        _ = result.CommandText.Should().Contain(".echo");
    }

    /// <summary>
    /// Verifies that BuildBatch separates commands with semicolons.
    /// </summary>
    [Fact]
    public void BuildBatch_WithMultipleCommands_SeparatesWithSemicolons()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
            new Command { CommandId = "cmd-2", CommandText = "lm" },
        };

        var result = m_Builder.BuildBatch("test-session", commands);

        _ = result.CommandText.Should().Contain(";");
    }

    /// <summary>
    /// Verifies that BuildBatch handles commands with special characters.
    /// </summary>
    [Fact]
    public void BuildBatch_WithSpecialCharacters_BuildsCorrectly()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "!analyze -v" },
            new Command { CommandId = "cmd-2", CommandText = "!heap -p -a" },
        };

        var result = m_Builder.BuildBatch("test-session", commands);

        _ = result.CommandText.Should().Contain("!analyze -v");
        _ = result.CommandText.Should().Contain("!heap -p -a");
        _ = result.CommandId.Should().StartWith("cmd-test-session-");
    }

    /// <summary>
    /// Verifies that BuildBatch handles three commands correctly.
    /// </summary>
    [Fact]
    public void BuildBatch_WithThreeCommands_CreatesBatchWithAllThree()
    {
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "k" },
            new Command { CommandId = "cmd-2", CommandText = "lm" },
            new Command { CommandId = "cmd-3", CommandText = "!threads" },
        };

        var result = m_Builder.BuildBatch("test-session", commands);

        _ = result.CommandId.Should().StartWith("cmd-test-session-");
        _ = result.CommandText.Should().Contain("MCP_NEXUS_COMMAND_SEPARATOR_cmd-1");
        _ = result.CommandText.Should().Contain("MCP_NEXUS_COMMAND_SEPARATOR_cmd-2");
        _ = result.CommandText.Should().Contain("MCP_NEXUS_COMMAND_SEPARATOR_cmd-3");
    }
}
