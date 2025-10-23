using Microsoft.Extensions.Logging;
using Moq;
using nexus.engine.batch.Internal;
using Xunit;

namespace nexus.engine.batch.tests;

/// <summary>
/// Tests for <see cref="BatchResultParser"/>.
/// </summary>
public class BatchResultParserTests
{
    private readonly Mock<ILogger<BatchResultParser>> m_MockLogger;
    private readonly BatchResultParser m_Parser;

    public BatchResultParserTests()
    {
        m_MockLogger = new Mock<ILogger<BatchResultParser>>();
        m_Parser = new BatchResultParser(m_MockLogger.Object);
    }

    [Fact]
    public void ParseResult_WithNonBatchResult_ReturnsPassThrough()
    {
        // Arrange
        var result = new CommandResult
        {
            CommandId = "cmd-1",
            ResultText = "Some output"
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        Assert.Single(results);
        Assert.Equal("cmd-1", results[0].CommandId);
        Assert.Equal("Some output", results[0].ResultText);
    }

    [Fact]
    public void ParseResult_WithBatchResultAndSentinels_SplitsCorrectly()
    {
        // Arrange
        var batchOutput = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START
Result for command 1
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START
Result for command 2
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END
";
        var result = new CommandResult
        {
            CommandId = "batch_cmd-1_cmd-2",
            ResultText = batchOutput
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("cmd-1", results[0].CommandId);
        Assert.Contains("Result for command 1", results[0].ResultText);
        Assert.Equal("cmd-2", results[1].CommandId);
        Assert.Contains("Result for command 2", results[1].ResultText);
    }

    [Fact]
    public void ParseResult_WithThreeCommands_SplitsCorrectly()
    {
        // Arrange
        var batchOutput = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START
Output 1
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START
Output 2
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-3_START
Output 3
MCP_NEXUS_COMMAND_SEPARATOR_cmd-3_END
";
        var result = new CommandResult
        {
            CommandId = "batch_cmd-1_cmd-2_cmd-3",
            ResultText = batchOutput
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("cmd-1", results[0].CommandId);
        Assert.Equal("cmd-2", results[1].CommandId);
        Assert.Equal("cmd-3", results[2].CommandId);
    }

    [Fact]
    public void ParseResult_WithBatchResultButNoSentinels_ReturnsFallback()
    {
        // Arrange
        var result = new CommandResult
        {
            CommandId = "batch_cmd-1_cmd-2",
            ResultText = "Some output without sentinels"
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        Assert.Single(results);
        Assert.Equal("cmd-1", results[0].CommandId); // First command ID from batch
        Assert.Equal("Some output without sentinels", results[0].ResultText);
    }

    [Fact]
    public void ParseResult_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => m_Parser.ParseResult(null!));
    }

    [Fact]
    public void ParseResult_WithMultilineOutput_PreservesContent()
    {
        // Arrange
        var batchOutput = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START
Line 1
Line 2
Line 3
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START
Another line 1
Another line 2
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END
";
        var result = new CommandResult
        {
            CommandId = "batch_cmd-1_cmd-2",
            ResultText = batchOutput
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains("Line 1", results[0].ResultText);
        Assert.Contains("Line 2", results[0].ResultText);
        Assert.Contains("Line 3", results[0].ResultText);
        Assert.Contains("Another line 1", results[1].ResultText);
        Assert.Contains("Another line 2", results[1].ResultText);
    }

    [Fact]
    public void ParseResult_WithEmptyResult_HandlesGracefully()
    {
        // Arrange
        var batchOutput = @"
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_START
MCP_NEXUS_COMMAND_SEPARATOR_cmd-1_END
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_START
MCP_NEXUS_COMMAND_SEPARATOR_cmd-2_END
";
        var result = new CommandResult
        {
            CommandId = "batch_cmd-1_cmd-2",
            ResultText = batchOutput
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("cmd-1", results[0].CommandId);
        Assert.Equal("cmd-2", results[1].CommandId);
    }
}

