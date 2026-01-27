using System.Text.Json;

using FluentAssertions;

using ModelContextProtocol.Protocol;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.Protocol.Services;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for close dump analyze session tool invocation edge cases.
/// </summary>
[Collection("EngineService")]
public class CloseDumpAnalyzeSessionToolTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloseDumpAnalyzeSessionToolTests"/> class.
    /// </summary>
    public CloseDumpAnalyzeSessionToolTests()
    {
        m_Settings = new Mock<ISettings>();
        m_FileSystem = new Mock<IFileSystem>();
        m_ProcessManager = new Mock<IProcessManager>();

        var sharedConfig = new SharedConfiguration
        {
            WinAiDbg = new WinAiDbgSettings
            {
                Extensions = new ExtensionsSettings
                {
                    CallbackPort = 0,
                },
            },
        };
        _ = m_Settings.Setup(s => s.Get()).Returns(sharedConfig);
        EngineService.Initialize(m_FileSystem.Object, m_ProcessManager.Object, m_Settings.Object);
    }

    /// <summary>
    /// Verifies unexpected/extra arguments do not crash invocation and missing required args are reported.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_CloseSession_WithUnexpectedArgumentsAndMissingSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["random"] = JsonSerializer.SerializeToElement(new { x = 1 }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_close_dump_analyze_session",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Missing required parameter(s)");
        _ = textBlock.Text.Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies wrong-typed known argument values are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_CloseSession_WithWrongTypedSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement(new[] { "session-123" }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_close_dump_analyze_session",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Invalid type for parameter `sessionId`");
    }

    /// <summary>
    /// Verifies a "smart" but invalid string input still produces a descriptive error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_CloseSession_WithInvalidSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_close_dump_analyze_session",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Invalid `sessionId`");
    }

    /// <summary>
    /// Creates a new tool call service instance for tests.
    /// </summary>
    /// <returns>The tool call service.</returns>
    private static McpToolCallService CreateSut()
    {
        return new McpToolCallService(new McpToolDefinitionService());
    }
}

