using Microsoft.Extensions.Logging;
using Moq;
using nexus.engine.batch.Configuration;
using nexus.engine.batch.Internal;
using Xunit;

namespace nexus.engine.batch.tests;

/// <summary>
/// Tests for <see cref="BatchCommandBuilder"/>.
/// </summary>
public class BatchCommandBuilderTests
{
    private readonly Mock<ILogger<BatchCommandBuilder>> m_MockLogger;
    private readonly BatchingConfiguration m_Configuration;
    private readonly BatchCommandBuilder m_Builder;

    public BatchCommandBuilderTests()
    {
        m_MockLogger = new Mock<ILogger<BatchCommandBuilder>>();
        m_Configuration = new BatchingConfiguration
        {
            Enabled = true,
            MinBatchSize = 2,
            MaxBatchSize = 5
        };
        m_Builder = new BatchCommandBuilder(m_Configuration, m_MockLogger.Object);
    }

    [Fact]
    public void BuildBatch_WithSingleCommand_ReturnsSameCommand()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" }
        };

        // Act
        var result = m_Builder.BuildBatch(commands);

        // Assert
        Assert.Equal("cmd-1", result.CommandId);
        Assert.Equal("lm", result.CommandText);
    }

    [Fact]
    public void BuildBatch_WithMultipleCommands_CreatesBatchedCommand()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" }
        };

        // Act
        var result = m_Builder.BuildBatch(commands);

        // Assert
        Assert.StartsWith("batch_", result.CommandId);
        Assert.Contains("cmd-1", result.CommandId);
        Assert.Contains("cmd-2", result.CommandId);
        Assert.Contains("MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START", result.CommandText);
        Assert.Contains("MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END", result.CommandText);
        Assert.Contains("MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START", result.CommandText);
        Assert.Contains("MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END", result.CommandText);
        Assert.Contains("lm", result.CommandText);
        Assert.Contains("!threads", result.CommandText);
    }

    [Fact]
    public void BuildBatch_WithThreeCommands_CreatesCorrectBatchId()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" },
            new Command { CommandId = "cmd-3", CommandText = "!peb" }
        };

        // Act
        var result = m_Builder.BuildBatch(commands);

        // Assert
        Assert.Equal("batch_cmd-1_cmd-2_cmd-3", result.CommandId);
    }

    [Fact]
    public void BuildBatch_WithEmptyList_ThrowsArgumentException()
    {
        // Arrange
        var commands = new List<Command>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => m_Builder.BuildBatch(commands));
    }

    [Fact]
    public void BuildBatch_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => m_Builder.BuildBatch(null!));
    }

    [Fact]
    public void GroupIntoBatches_WithFewCommands_ReturnsOneBatch()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" }
        };

        // Act
        var result = m_Builder.GroupIntoBatches(commands);

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].Count);
    }

    [Fact]
    public void GroupIntoBatches_WithMaxSizeCommands_ReturnsTwoBatches()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" },
            new Command { CommandId = "cmd-3", CommandText = "!peb" },
            new Command { CommandId = "cmd-4", CommandText = "lsa 1" },
            new Command { CommandId = "cmd-5", CommandText = "lsa 2" },
            new Command { CommandId = "cmd-6", CommandText = "lsa 3" }
        };

        // Act
        var result = m_Builder.GroupIntoBatches(commands);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(5, result[0].Count); // First batch has MaxBatchSize (5)
        Assert.Single(result[1]); // Second batch has remainder (1)
    }

    [Fact]
    public void GroupIntoBatches_WithExactlyMaxSize_ReturnsOneBatch()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" },
            new Command { CommandId = "cmd-3", CommandText = "!peb" },
            new Command { CommandId = "cmd-4", CommandText = "lsa 1" },
            new Command { CommandId = "cmd-5", CommandText = "lsa 2" }
        };

        // Act
        var result = m_Builder.GroupIntoBatches(commands);

        // Assert
        Assert.Single(result);
        Assert.Equal(5, result[0].Count);
    }

    [Fact]
    public void GroupIntoBatches_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var commands = new List<Command>();

        // Act
        var result = m_Builder.GroupIntoBatches(commands);

        // Assert
        Assert.Empty(result);
    }
}

