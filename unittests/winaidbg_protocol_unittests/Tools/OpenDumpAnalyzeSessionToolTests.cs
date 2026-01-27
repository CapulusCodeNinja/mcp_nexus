using System.Text.Json;

using FluentAssertions;

using ModelContextProtocol.Protocol;

using WinAiDbg.Protocol.Services;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for open dump analyze session tool invocation edge cases.
/// </summary>
[Collection("EngineService")]
public class OpenDumpAnalyzeSessionToolTests
{
    /// <summary>
    /// Verifies that unexpected/extra arguments do not crash invocation and missing required args are still reported.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_OpenSession_WithUnexpectedArgumentsAndMissingDumpPath_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["randomNumber"] = JsonSerializer.SerializeToElement(123),
            ["randomObject"] = JsonSerializer.SerializeToElement(new { a = 1, b = "x" }),
            ["randomArray"] = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_open_dump_analyze_session",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Tool Invocation Error");
        _ = textBlock.Text.Should().Contain("Missing required parameter(s)");
        _ = textBlock.Text.Should().Contain("dumpPath");
    }

    /// <summary>
    /// Verifies that wrong-typed known arguments are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_OpenSession_WithWrongTypedDumpPath_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["dumpPath"] = JsonSerializer.SerializeToElement(new { path = "C:\\dumps\\x.dmp" }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_open_dump_analyze_session",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Invalid type for parameter `dumpPath`");
    }

    /// <summary>
    /// Verifies that "intelligent" but invalid dumpPath values return a descriptive error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_OpenSession_WithDumpPathNullString_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["dumpPath"] = JsonSerializer.SerializeToElement("null"),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_open_dump_analyze_session",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Invalid `dumpPath`");
        _ = textBlock.Text.Should().Contain("file not found");
    }

    /// <summary>
    /// Creates a new tool call service instance for tests.
    /// </summary>
    /// <returns>The tool call service.</returns>
    private static McpToolCallService CreateSut()
    {
        var toolDefinitionService = new McpToolDefinitionService();
        return new McpToolCallService(toolDefinitionService);
    }
}

