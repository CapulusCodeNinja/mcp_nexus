using System.Text.Json;

using FluentAssertions;

using ModelContextProtocol.Protocol;

using WinAiDbg.Protocol.Services;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Services;

/// <summary>
/// Unit tests for the <see cref="McpToolCallService"/> class.
/// </summary>
public class McpToolCallServiceTests
{
    /// <summary>
    /// Verifies that calling a tool with missing required parameters returns an actionable error
    /// as a tool execution error (result with <c>isError: true</c>), rather than a generic message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithMissingRequiredArgument_ReturnsActionableToolError()
    {
        // Arrange
        var toolDefinitionService = new McpToolDefinitionService();
        var sut = new McpToolCallService(toolDefinitionService);

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_open_dump_analyze_session",
            new Dictionary<string, JsonElement>(),
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = result.Content.Should().NotBeNull();
        _ = result.Content.Count.Should().BeGreaterThan(0);
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Tool Invocation Error");
        _ = textBlock.Text.Should().Contain("dumpPath");
        _ = textBlock.Text.Should().Contain("Mandatory `params.arguments`");
    }

    /// <summary>
    /// Verifies that calling a tool with a whitespace string argument returns an actionable error
    /// and does not attempt tool invocation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithWhitespaceStringArgument_ReturnsActionableToolError()
    {
        // Arrange
        var toolDefinitionService = new McpToolDefinitionService();
        var sut = new McpToolCallService(toolDefinitionService);

        var arguments = new Dictionary<string, JsonElement>
        {
            ["dumpPath"] = JsonSerializer.SerializeToElement("   "),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_open_dump_analyze_session",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = result.Content.Should().NotBeNull();
        _ = result.Content.Count.Should().BeGreaterThan(0);
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Tool Invocation Error");
        _ = textBlock.Text.Should().Contain("non-empty string");
        _ = textBlock.Text.Should().Contain("dumpPath");
    }

    /// <summary>
    /// Verifies that unexpected/extra arguments from an AI client do not crash tool invocation
    /// and are ignored for schema validation purposes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithUnexpectedArguments_ReturnsActionableToolError()
    {
        // Arrange
        var toolDefinitionService = new McpToolDefinitionService();
        var sut = new McpToolCallService(toolDefinitionService);

        var arguments = new Dictionary<string, JsonElement>
        {
            // Missing required dumpPath on purpose.
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
    /// Verifies that wrong-typed arguments for known parameters are rejected even when extra
    /// unexpected parameters are present.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithUnexpectedArgumentsAndWrongTypedKnownArgument_ReturnsActionableToolError()
    {
        // Arrange
        var toolDefinitionService = new McpToolDefinitionService();
        var sut = new McpToolCallService(toolDefinitionService);

        var arguments = new Dictionary<string, JsonElement>
        {
            ["dumpPath"] = JsonSerializer.SerializeToElement(123),
            ["randomFlag"] = JsonSerializer.SerializeToElement(true),
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
}

