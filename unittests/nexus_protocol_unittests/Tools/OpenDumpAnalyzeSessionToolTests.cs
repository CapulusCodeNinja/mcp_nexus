using System.IO;

using FluentAssertions;

using Nexus.Protocol.Tools;

using Xunit;

namespace Nexus.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for the <see cref="OpenDumpAnalyzeSessionTool"/> class.
/// </summary>
public class OpenDumpAnalyzeSessionToolTests
{
    /// <summary>
    /// Verifies that the tool returns a clear error when the dump file does not exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NexusOpenDumpAnalyzeSession_WhenFileDoesNotExist_ReturnsNotFoundMessage()
    {
        // Act
        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(@"Z:\this\path\does\not\exist.dmp", null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().Contain("Failed");
        _ = markdown.Should().Contain("Dump file not found");
    }

    /// <summary>
    /// Verifies that the tool returns a clear error when the dump file cannot be read.
    /// This test uses a directory path instead of a file to reliably trigger an IO error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NexusOpenDumpAnalyzeSession_WhenFileIsNotReadable_ReturnsReadableErrorMessage()
    {
        // Arrange - use a directory path which will fail OpenRead/Read
        var unreadablePath = Directory.GetCurrentDirectory();

        // Act
        var result = await OpenDumpAnalyzeSessionTool.nexus_open_dump_analyze_session(unreadablePath, null);

        // Assert
        _ = result.Should().NotBeNull();
        var markdown = result.ToString()!;
        _ = markdown.Should().Contain("Failed");
        _ = (markdown.Contains("Cannot open dump file for read") || markdown.Contains("Dump file not found")).Should().BeTrue();
    }
}


