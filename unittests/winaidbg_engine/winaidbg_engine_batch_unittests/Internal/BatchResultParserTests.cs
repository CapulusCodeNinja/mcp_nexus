using FluentAssertions;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.Engine.Batch.Internal;

using Xunit;

namespace WinAiDbg.Engine.Batch.Unittests.Internal;

/// <summary>
/// Unit tests for BatchResultParser class.
/// Tests result parsing and unbatching logic.
/// </summary>
public class BatchResultParserTests
{
    private readonly BatchProcessor m_Processor;
    private readonly BatchResultParser m_Parser;
    private readonly Mock<ISettings> m_Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchResultParserTests"/> class.
    /// </summary>
    public BatchResultParserTests()
    {
        m_Settings = new Mock<ISettings>();
        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Batching = new BatchingSettings
                {
                    Enabled = true,
                    MaxBatchSize = 5,
                    MinBatchSize = 2,
                    ExcludedCommands = new List<string> { "!analyze", "!dump", "!heap" },
                },
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
        m_Processor = new BatchProcessor(m_Settings.Object);
        m_Parser = new BatchResultParser(m_Processor);
    }

    /// <summary>
    /// Verifies that ParseResult throws ArgumentNullException for null result.
    /// </summary>
    [Fact]
    public void ParseResult_WithNullResult_ThrowsArgumentNullException()
    {
        var action = () => m_Parser.ParseResult(null!);

        _ = action.Should().Throw<ArgumentNullException>()
            .WithParameterName("result");
    }

    /// <summary>
    /// Verifies that ParseResult returns single result for non-batch command.
    /// </summary>
    [Fact]
    public void ParseResult_WithNonBatchResult_ReturnsSingleResult()
    {
        var result = new CommandResult
        {
            CommandId = "cmd-1",
            SessionId = "test-session",
            ResultText = "Stack trace output",
        };

        var results = m_Parser.ParseResult(result);

        _ = results.Should().HaveCount(1);
        _ = results[0].Should().BeSameAs(result);
    }

    /// <summary>
    /// Verifies that ParseResult extracts individual results from batch result.
    /// </summary>
    [Fact]
    public void ParseResult_WithBatchResult_ExtractsIndividualResults()
    {
        // Arrange - First batch the commands to register the mapping
        var sessionId = "test-session-parse-11";
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "k" },
            new() { CommandId = "cmd-2", CommandText = "lm" },
        };

        var batchedCommands = m_Processor.BatchCommands(sessionId, commands);
        var batchId = batchedCommands[0].CommandId;

        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}
Stack output for command 1
{BatchSentinels.GetEndMarker("cmd-1")}
{BatchSentinels.GetStartMarker("cmd-2")}
Module output for command 2
{BatchSentinels.GetEndMarker("cmd-2")}
";

        var result = new CommandResult
        {
            CommandId = batchId,
            SessionId = sessionId,
            ResultText = resultText,
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        _ = results.Should().HaveCount(2);
        _ = results[0].CommandId.Should().Be("cmd-1");
        _ = results[0].ResultText.Should().Contain("Stack output for command 1");
        _ = results[1].CommandId.Should().Be("cmd-2");
        _ = results[1].ResultText.Should().Contain("Module output for command 2");
    }

    /// <summary>
    /// Verifies that ParseResult extracts batch results even when outer WinAiDbg sentinels are present.
    /// </summary>
    [Fact]
    public void ParseResult_WithBatchResultAndOuterSentinels_ExtractsIndividualResults()
    {
        // Arrange - First batch the commands to register the mapping
        var sessionId = "test-session-parse-outer-1";
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "k" },
            new() { CommandId = "cmd-2", CommandText = "lm" },
        };

        var batchedCommands = m_Processor.BatchCommands(sessionId, commands);
        var batchId = batchedCommands[0].CommandId;

        // Simulate the text captured between CdbSentinels.StartMarker and CdbSentinels.EndMarker
        // while still containing the batch-internal separators.
        var resultText = $@"
WINAIDBG_SENTINEL_COMMAND_START
{BatchSentinels.GetStartMarker("cmd-1")}
Stack output for command 1
{BatchSentinels.GetEndMarker("cmd-1")}
{BatchSentinels.GetStartMarker("cmd-2")}
Module output for command 2
{BatchSentinels.GetEndMarker("cmd-2")}
WINAIDBG_SENTINEL_COMMAND_END
";

        var result = new CommandResult
        {
            CommandId = batchId,
            SessionId = sessionId,
            ResultText = resultText,
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        _ = results.Should().HaveCount(2);
        _ = results[0].CommandId.Should().Be("cmd-1");
        _ = results[0].ResultText.Should().Contain("Stack output for command 1");
        _ = results[1].CommandId.Should().Be("cmd-2");
        _ = results[1].ResultText.Should().Contain("Module output for command 2");
    }

    /// <summary>
    /// Verifies that ParseResult handles batch with three commands.
    /// </summary>
    [Fact]
    public void ParseResult_WithThreeCommandBatch_ExtractsAllThree()
    {
        // Arrange - First batch the commands to register the mapping
        var sessionId = "test-session-parse-6";
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "k" },
            new() { CommandId = "cmd-2", CommandText = "lm" },
            new() { CommandId = "cmd-3", CommandText = "!threads" },
        };

        var batchedCommands = m_Processor.BatchCommands(sessionId, commands);
        var batchId = batchedCommands[0].CommandId;

        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}
Output 1
{BatchSentinels.GetEndMarker("cmd-1")}
{BatchSentinels.GetStartMarker("cmd-2")}
Output 2
{BatchSentinels.GetEndMarker("cmd-2")}
{BatchSentinels.GetStartMarker("cmd-3")}
Output 3
{BatchSentinels.GetEndMarker("cmd-3")}
";

        var result = new CommandResult
        {
            CommandId = batchId,
            SessionId = sessionId,
            ResultText = resultText,
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        _ = results.Should().HaveCount(3);
        _ = results[0].CommandId.Should().Be("cmd-1");
        _ = results[1].CommandId.Should().Be("cmd-2");
        _ = results[2].CommandId.Should().Be("cmd-3");
    }

    /// <summary>
    /// Verifies that ParseResult handles batch result without sentinels gracefully.
    /// </summary>
    [Fact]
    public void ParseResult_WithBatchResultWithoutSentinels_ReturnsSingleResult()
    {
        // Arrange - First batch the commands to register the mapping
        var sessionId = "test-session-parse-12";
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "k" },
            new() { CommandId = "cmd-2", CommandText = "lm" },
        };

        var batchedCommands = m_Processor.BatchCommands(sessionId, commands);
        var batchId = batchedCommands[0].CommandId;

        var result = new CommandResult
        {
            CommandId = batchId,
            SessionId = sessionId,
            ResultText = "Output without sentinels",
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert - When batch result has no sentinels, fallback returns first command with full output
        _ = results.Should().HaveCount(1);
        _ = results[0].CommandId.Should().Be("cmd-1");
        _ = results[0].ResultText.Should().Be("Output without sentinels");
    }

    /// <summary>
    /// Verifies that ParseResult handles empty result text.
    /// </summary>
    [Fact]
    public void ParseResult_WithEmptyResultText_ExtractsEmptyResults()
    {
        // Arrange - First batch the commands to register the mapping
        var sessionId = "test-session-parse-10";
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "k" },
            new() { CommandId = "cmd-2", CommandText = "lm" },
        };

        var batchedCommands = m_Processor.BatchCommands(sessionId, commands);
        var batchId = batchedCommands[0].CommandId;

        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}

{BatchSentinels.GetEndMarker("cmd-1")}
{BatchSentinels.GetStartMarker("cmd-2")}

{BatchSentinels.GetEndMarker("cmd-2")}
";

        var result = new CommandResult
        {
            CommandId = batchId,
            SessionId = sessionId,
            ResultText = resultText,
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        _ = results.Should().HaveCount(2);
        _ = results[0].ResultText.Should().BeEmpty();
        _ = results[1].ResultText.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ParseResult trims whitespace from extracted results.
    /// </summary>
    [Fact]
    public void ParseResult_ExtractsResults_TrimsWhitespace()
    {
        // Arrange - First batch the commands to register the mapping
        var sessionId = "test-session-trim-1";
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-1", CommandText = "k" },
            new() { CommandId = "cmd-2", CommandText = "lm" },
        };

        var batchedCommands = m_Processor.BatchCommands(sessionId, commands);
        var batchId = batchedCommands[0].CommandId;

        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}
   
   Output with whitespace   
   
{BatchSentinels.GetEndMarker("cmd-1")}
{BatchSentinels.GetStartMarker("cmd-2")}
More output
{BatchSentinels.GetEndMarker("cmd-2")}
";

        var result = new CommandResult
        {
            CommandId = batchId,
            SessionId = sessionId,
            ResultText = resultText,
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        _ = results.Should().HaveCount(2);
        _ = results[0].CommandId.Should().Be("cmd-1");
        _ = results[0].ResultText.Should().Be("Output with whitespace");
        _ = results[1].CommandId.Should().Be("cmd-2");
    }

    /// <summary>
    /// Verifies that ParseResult handles multiline output correctly.
    /// </summary>
    [Fact]
    public void ParseResult_WithMultilineOutput_PreservesNewlines()
    {
        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}
Line 1
Line 2
Line 3
{BatchSentinels.GetEndMarker("cmd-1")}
";

        var result = new CommandResult
        {
            CommandId = "batch_cmd-1",
            SessionId = "test-session",
            ResultText = resultText,
        };

        var results = m_Parser.ParseResult(result);

        _ = results.Should().HaveCount(1);
        _ = results[0].ResultText.Should().Contain("Line 1");
        _ = results[0].ResultText.Should().Contain("Line 2");
        _ = results[0].ResultText.Should().Contain("Line 3");
    }

    /// <summary>
    /// Verifies that ParseResult handles missing end marker.
    /// </summary>
    [Fact]
    public void ParseResult_WithMissingEndMarker_TakesRestOfOutput()
    {
        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}
Output continues without end marker
";

        var result = new CommandResult
        {
            CommandId = "batch_cmd-1",
            SessionId = "test-session",
            ResultText = resultText,
        };

        var results = m_Parser.ParseResult(result);

        _ = results.Should().HaveCount(1);
        _ = results[0].ResultText.Should().Contain("Output continues without end marker");
    }

    /// <summary>
    /// Verifies that ParseResult handles special characters in output.
    /// </summary>
    [Fact]
    public void ParseResult_WithSpecialCharacters_PreservesCharacters()
    {
        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}
Output with special chars: !@#$%^&*()
{BatchSentinels.GetEndMarker("cmd-1")}
";

        var result = new CommandResult
        {
            CommandId = "batch_cmd-1",
            SessionId = "test-session",
            ResultText = resultText,
        };

        var results = m_Parser.ParseResult(result);

        _ = results.Should().HaveCount(1);
        _ = results[0].ResultText.Should().Contain("!@#$%^&*()");
    }

    /// <summary>
    /// Verifies that ParseResult maintains command order.
    /// </summary>
    [Fact]
    public void ParseResult_MaintainsCommandOrder()
    {
        // Arrange - First batch the commands to register the mapping
        var sessionId = "test-session-order-1";
        var commands = new List<Command>
        {
            new() { CommandId = "cmd-A", CommandText = "first" },
            new() { CommandId = "cmd-B", CommandText = "second" },
            new() { CommandId = "cmd-C", CommandText = "third" },
        };

        var batchedCommands = m_Processor.BatchCommands(sessionId, commands);
        var batchId = batchedCommands[0].CommandId;

        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-A")}
First
{BatchSentinels.GetEndMarker("cmd-A")}
{BatchSentinels.GetStartMarker("cmd-B")}
Second
{BatchSentinels.GetEndMarker("cmd-B")}
{BatchSentinels.GetStartMarker("cmd-C")}
Third
{BatchSentinels.GetEndMarker("cmd-C")}
";

        var result = new CommandResult
        {
            CommandId = batchId,
            SessionId = sessionId,
            ResultText = resultText,
        };

        // Act
        var results = m_Parser.ParseResult(result);

        // Assert
        _ = results.Should().HaveCount(3);
        _ = results[0].CommandId.Should().Be("cmd-A");
        _ = results[0].ResultText.Should().Be("First");
        _ = results[1].CommandId.Should().Be("cmd-B");
        _ = results[1].ResultText.Should().Be("Second");
        _ = results[2].CommandId.Should().Be("cmd-C");
        _ = results[2].ResultText.Should().Be("Third");
    }
}
