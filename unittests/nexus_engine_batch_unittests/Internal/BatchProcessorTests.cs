using Microsoft.Extensions.Logging;
using Moq;
using nexus.engine.batch.Configuration;
using nexus.engine.batch.Internal;
using Xunit;

namespace nexus.engine.batch.tests;

/// <summary>
/// Tests for <see cref="BatchProcessor"/>.
/// </summary>
public class BatchProcessorTests
{
    private readonly Mock<ILogger<BatchProcessor>> m_MockLogger;
    private readonly Mock<ILogger<BatchCommandFilter>> m_MockFilterLogger;
    private readonly Mock<ILogger<BatchCommandBuilder>> m_MockBuilderLogger;
    private readonly Mock<ILogger<BatchResultParser>> m_MockParserLogger;
    private readonly BatchingConfiguration m_Configuration;
    private readonly BatchProcessor m_Processor;

    public BatchProcessorTests()
    {
        m_MockLogger = new Mock<ILogger<BatchProcessor>>();
        m_MockFilterLogger = new Mock<ILogger<BatchCommandFilter>>();
        m_MockBuilderLogger = new Mock<ILogger<BatchCommandBuilder>>();
        m_MockParserLogger = new Mock<ILogger<BatchResultParser>>();
        
        m_Configuration = new BatchingConfiguration
        {
            Enabled = true,
            MinBatchSize = 2,
            MaxBatchSize = 5,
            ExcludedCommands = new List<string> { "!analyze", "!dump" }
        };

        var filter = new BatchCommandFilter(m_Configuration, m_MockFilterLogger.Object);
        var builder = new BatchCommandBuilder(m_Configuration, m_MockBuilderLogger.Object);
        var parser = new BatchResultParser(m_MockParserLogger.Object);

        m_Processor = new BatchProcessor(m_Configuration, filter, builder, parser, m_MockLogger.Object);
    }

    [Fact]
    public void BatchCommands_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var commands = new List<Command>();

        // Act
        var result = m_Processor.BatchCommands(commands);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BatchCommands_WithNull_ReturnsEmptyList()
    {
        // Act
        var result = m_Processor.BatchCommands(null!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void BatchCommands_WithSingleCommand_ReturnsPassThrough()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" }
        };

        // Act
        var result = m_Processor.BatchCommands(commands);

        // Assert
        Assert.Single(result);
        Assert.Equal("cmd-1", result[0].CommandId);
        Assert.Equal("lm", result[0].CommandText);
    }

    [Fact]
    public void BatchCommands_WithTwoCommands_CreatesBatch()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" }
        };

        // Act
        var result = m_Processor.BatchCommands(commands);

        // Assert
        Assert.Single(result); // Two commands become one batch
        Assert.StartsWith("batch_", result[0].CommandId);
        Assert.Contains("MCP_NEXUS_COMMAND_SEPARATOR", result[0].CommandText);
    }

    [Fact]
    public void BatchCommands_WithBatchingDisabled_ReturnsPassThrough()
    {
        // Arrange
        m_Configuration.Enabled = false;
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" }
        };

        // Act
        var result = m_Processor.BatchCommands(commands);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("cmd-1", result[0].CommandId);
        Assert.Equal("cmd-2", result[1].CommandId);
    }

    [Fact]
    public void BatchCommands_WithExcludedCommand_ReturnsPassThrough()
    {
        // Arrange
        var commands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!analyze -v" }
        };

        // Act
        var result = m_Processor.BatchCommands(commands);

        // Assert
        Assert.Equal(2, result.Count); // Excluded command prevents batching
        Assert.Equal("cmd-1", result[0].CommandId);
        Assert.Equal("cmd-2", result[1].CommandId);
    }

    [Fact]
    public void BatchCommands_WithMoreThanMaxSize_CreatesMultipleBatches()
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
        var result = m_Processor.BatchCommands(commands);

        // Assert
        Assert.Equal(2, result.Count); // 6 commands -> 1 batch (5 commands) + 1 pass-through
        Assert.StartsWith("batch_", result[0].CommandId); // First 5 batched
        Assert.Equal("cmd-6", result[1].CommandId); // Last one below MinBatchSize, passed through
    }

    [Fact]
    public void UnbatchResults_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var results = new List<CommandResult>();

        // Act
        var unbatched = m_Processor.UnbatchResults(results);

        // Assert
        Assert.Empty(unbatched);
    }

    [Fact]
    public void UnbatchResults_WithNull_ReturnsEmptyList()
    {
        // Act
        var result = m_Processor.UnbatchResults(null!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void UnbatchResults_WithNonBatchResult_ReturnsPassThrough()
    {
        // Arrange
        var results = new List<CommandResult>
        {
            new CommandResult { CommandId = "cmd-1", ResultText = "Output 1" }
        };

        // Act
        var unbatched = m_Processor.UnbatchResults(results);

        // Assert
        Assert.Single(unbatched);
        Assert.Equal("cmd-1", unbatched[0].CommandId);
        Assert.Equal("Output 1", unbatched[0].ResultText);
    }

    [Fact]
    public void UnbatchResults_WithBatchResult_SplitsCorrectly()
    {
        // Arrange
        var batchOutput = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START
Output 1
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START
Output 2
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END
";
        var results = new List<CommandResult>
        {
            new CommandResult
            {
                CommandId = "batch_cmd-1_cmd-2",
                ResultText = batchOutput
            }
        };

        // Act
        var unbatched = m_Processor.UnbatchResults(results);

        // Assert
        Assert.Equal(2, unbatched.Count);
        Assert.Equal("cmd-1", unbatched[0].CommandId);
        Assert.Contains("Output 1", unbatched[0].ResultText);
        Assert.Equal("cmd-2", unbatched[1].CommandId);
        Assert.Contains("Output 2", unbatched[1].ResultText);
    }

    [Fact]
    public void UnbatchResults_WithMultipleBatches_UnbatchesAll()
    {
        // Arrange
        var batch1Output = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START
Output 1
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START
Output 2
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END
";
        var batch2Output = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-3_START
Output 3
MCP_NEXUS_COMMAND_SEPARATOR_cmd-3_END
";
        var results = new List<CommandResult>
        {
            new CommandResult { CommandId = "batch_cmd-1_cmd-2", ResultText = batch1Output },
            new CommandResult { CommandId = "batch_cmd-3", ResultText = batch2Output }
        };

        // Act
        var unbatched = m_Processor.UnbatchResults(results);

        // Assert
        Assert.Equal(3, unbatched.Count);
        Assert.Equal("cmd-1", unbatched[0].CommandId);
        Assert.Equal("cmd-2", unbatched[1].CommandId);
        Assert.Equal("cmd-3", unbatched[2].CommandId);
    }

    [Fact]
    public void RoundTrip_BatchAndUnbatch_PreservesData()
    {
        // Arrange
        var originalCommands = new List<Command>
        {
            new Command { CommandId = "cmd-1", CommandText = "lm" },
            new Command { CommandId = "cmd-2", CommandText = "!threads" }
        };

        // Act - Batch
        var batched = m_Processor.BatchCommands(originalCommands);

        // Simulate execution and create result
        var simulatedBatchResult = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START
Result for lm
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START
Result for !threads
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END
";
        var results = new List<CommandResult>
        {
            new CommandResult
            {
                CommandId = batched[0].CommandId,
                ResultText = simulatedBatchResult
            }
        };

        // Act - Unbatch
        var unbatched = m_Processor.UnbatchResults(results);

        // Assert
        Assert.Equal(2, unbatched.Count);
        Assert.Equal("cmd-1", unbatched[0].CommandId);
        Assert.Contains("Result for lm", unbatched[0].ResultText);
        Assert.Equal("cmd-2", unbatched[1].CommandId);
        Assert.Contains("Result for !threads", unbatched[1].ResultText);
    }
}

