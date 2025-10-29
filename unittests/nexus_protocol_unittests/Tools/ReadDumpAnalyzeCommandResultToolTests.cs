using FluentAssertions;

using Nexus.Protocol.Utilities;

using Xunit;

namespace Nexus.Protocol.Tests.Tools;

/// <summary>
/// Tests for output wrapping decision for extension vs non-extension commands.
/// </summary>
public class ReadDumpAnalyzeCommandResultToolTests
{
    /// <inheritdoc/>
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

    /// <inheritdoc/>
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


