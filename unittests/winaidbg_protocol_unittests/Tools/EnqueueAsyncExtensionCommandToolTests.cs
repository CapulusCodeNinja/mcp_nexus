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
/// Unit tests for enqueue extension command tool invocation edge cases.
/// </summary>
public class EnqueueAsyncExtensionCommandToolTests
{
    private readonly Mock<ISettings> m_Settings;
    private readonly Mock<IFileSystem> m_FileSystem;
    private readonly Mock<IProcessManager> m_ProcessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnqueueAsyncExtensionCommandToolTests"/> class.
    /// </summary>
    public EnqueueAsyncExtensionCommandToolTests()
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
    public async Task CallToolAsync_EnqueueExtensionCommand_WithUnexpectedArgumentsAndMissingRequired_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["random"] = JsonSerializer.SerializeToElement("x"),
            ["parameters"] = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Missing required parameter(s)");
        _ = textBlock.Text.Should().Contain("sessionId");
        _ = textBlock.Text.Should().Contain("extensionName");
    }

    /// <summary>
    /// Verifies wrong-typed known argument values are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_EnqueueExtensionCommand_WithWrongTypedSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement(new { id = "session-123" }),
            ["extensionName"] = JsonSerializer.SerializeToElement("basic_crash_analysis"),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        _ = textBlock.Text.Should().Contain("Invalid type for parameter `sessionId`");
    }

    /// <summary>
    /// Verifies that unusual 'parameters' payloads do not crash deserialization.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_EnqueueExtensionCommand_WithWeirdParametersPayload_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateSut();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            ["extensionName"] = JsonSerializer.SerializeToElement("basic_crash_analysis"),
            ["parameters"] = JsonSerializer.SerializeToElement(new { nested = new[] { 1, 2, 3 }, flag = true }),
            ["unknown"] = JsonSerializer.SerializeToElement("anything"),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_enqueue_async_extension_command",
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

