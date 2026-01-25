using FluentAssertions;

using WinAiDbg.Protocol.Utilities;

using Xunit;

namespace WinAiDbg.Protocol.Tests.Tools;

/// <summary>
/// Tests for output wrapping decision for extension vs non-extension commands.
/// </summary>
public class ReadDumpAnalyzeCommandResultToolTests
{
    /// <summary>
    /// Ensures non-extension command output is wrapped in a code block.
    /// </summary>
    [Fact]
    public void AppendOutputForCommand_NonExtension_WrapsInCodeBlock()
    {
        // Arrange
        var command = "!threads";
        var output = "line1\nline2";

        // Act
        var result = MarkdownFormatter.AppendOutputForCommand(command, output, "Output");

        // Assert
        _ = result.Should().Contain("```");
        _ = result.Should().Contain("line1");
        _ = result.Should().Contain("line2");
    }

    /// <summary>
    /// Ensures extension command output is returned verbatim.
    /// </summary>
    [Fact]
    public void AppendOutputForCommand_Extension_ReturnsVerbatim()
    {
        // Arrange
        var command = "Extension: basic_crash_analysis";
        var output = "## Some Markdown\n\n``\ncode\n``".Replace("``", "```");

        // Act
        var result = MarkdownFormatter.AppendOutputForCommand(command, output, "Output");

        // Assert
        _ = result.Should().Be(output);
    }
}
