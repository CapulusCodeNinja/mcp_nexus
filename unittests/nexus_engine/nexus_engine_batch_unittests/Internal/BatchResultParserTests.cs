using FluentAssertions;

using Nexus.Engine.Batch.Internal;

using Xunit;

namespace Nexus.Engine.Batch.Tests.Internal;

/// <summary>
/// Unit tests for BatchResultParser class.
/// Tests result parsing and unbatching logic.
/// </summary>
public class BatchResultParserTests
{
    private readonly BatchResultParser m_Parser;

    /// <summary>
    /// Initializes a new instance of the BatchResultParserTests class.
    /// </summary>
    public BatchResultParserTests()
    {
        m_Parser = new BatchResultParser();
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
            ResultText = "Stack trace output"
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
            CommandId = "batch_cmd-1_cmd-2",
            ResultText = resultText
        };

        var results = m_Parser.ParseResult(result);

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
            CommandId = "batch_cmd-1_cmd-2_cmd-3",
            ResultText = resultText
        };

        var results = m_Parser.ParseResult(result);

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
        var result = new CommandResult
        {
            CommandId = "batch_cmd-1_cmd-2",
            ResultText = "Output without sentinels"
        };

        var results = m_Parser.ParseResult(result);

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
        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}

{BatchSentinels.GetEndMarker("cmd-1")}
{BatchSentinels.GetStartMarker("cmd-2")}

{BatchSentinels.GetEndMarker("cmd-2")}
";

        var result = new CommandResult
        {
            CommandId = "batch_cmd-1_cmd-2",
            ResultText = resultText
        };

        var results = m_Parser.ParseResult(result);

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
        var resultText = $@"
{BatchSentinels.GetStartMarker("cmd-1")}
   
   Output with whitespace   
   
{BatchSentinels.GetEndMarker("cmd-1")}
";

        var result = new CommandResult
        {
            CommandId = "batch_cmd-1",
            ResultText = resultText
        };

        var results = m_Parser.ParseResult(result);

        _ = results.Should().HaveCount(1);
        _ = results[0].ResultText.Should().Be("Output with whitespace");
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
            ResultText = resultText
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
            ResultText = resultText
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
            ResultText = resultText
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
            CommandId = "batch_cmd-A_cmd-B_cmd-C",
            ResultText = resultText
        };

        var results = m_Parser.ParseResult(result);

        _ = results.Should().HaveCount(3);
        _ = results[0].CommandId.Should().Be("cmd-A");
        _ = results[0].ResultText.Should().Be("First");
        _ = results[1].CommandId.Should().Be("cmd-B");
        _ = results[1].ResultText.Should().Be("Second");
        _ = results[2].CommandId.Should().Be("cmd-C");
        _ = results[2].ResultText.Should().Be("Third");
    }
}

