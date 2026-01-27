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
/// Unit tests for read dump analyze command result tool invocation edge cases.
/// </summary>
public class ReadDumpAnalyzeCommandResultToolInvocationTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadDumpAnalyzeCommandResultToolInvocationTests"/> class.
    /// </summary>
    public ReadDumpAnalyzeCommandResultToolInvocationTests()
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
    public async Task CallToolAsync_ReadResult_WithUnexpectedArgumentsAndMissingRequired_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["random"] = JsonSerializer.SerializeToElement(123),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_read_dump_analyze_command_result",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Missing required parameter(s)");
        _ = textBlock.Text.Should().Contain("sessionId");
        _ = textBlock.Text.Should().Contain("commandId");
        _ = textBlock.Text.Should().Contain("maxWaitSeconds");
    }

    /// <summary>
    /// Verifies wrong-typed known argument values are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_ReadResult_WithWrongTypedCommandId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            ["commandId"] = JsonSerializer.SerializeToElement(new { id = "cmd-123" }),
            ["maxWaitSeconds"] = JsonSerializer.SerializeToElement(30),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_read_dump_analyze_command_result",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Invalid type for parameter `commandId`");
    }

    /// <summary>
    /// Verifies a "smart" but invalid string input still produces a descriptive error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_ReadResult_WithNullStringCommandId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            ["commandId"] = JsonSerializer.SerializeToElement("null"),
            ["maxWaitSeconds"] = JsonSerializer.SerializeToElement(30),
            ["random"] = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_read_dump_analyze_command_result",
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

