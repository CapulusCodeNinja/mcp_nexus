using System.Text.Json;
using System.Reflection;

using FluentAssertions;

using ModelContextProtocol.Protocol;

using Moq;

using WinAiDbg.Config;
using WinAiDbg.Config.Models;
using WinAiDbg.External.Apis.FileSystem;
using WinAiDbg.External.Apis.ProcessManagement;
using WinAiDbg.Engine.Share;
using WinAiDbg.Engine.Share.Models;
using WinAiDbg.Protocol.Services;
using WinAiDbg.Protocol.Tools;

using Xunit;

namespace WinAiDbg.Protocol.Unittests.Tools;

/// <summary>
/// Unit tests for <see cref="GetDumpAnalyzeCommandsStatusTool"/>.
/// </summary>
[Collection("EngineService")]
public class GetDumpAnalyzeCommandsStatusToolTests
{
    /// <summary>
    /// Verifies unexpected/extra arguments do not crash invocation and missing required args are reported.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithUnexpectedArgumentsAndMissingSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["random"] = JsonSerializer.SerializeToElement(123),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_get_dump_analyze_commands_status",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Missing required parameter(s)");
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies wrong-typed known argument values are rejected with an actionable error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithWrongTypedSessionId_ReturnsActionableToolError()
    {
        // Arrange
        var sut = CreateToolCallService();
        var arguments = new Dictionary<string, JsonElement>
        {
            ["sessionId"] = JsonSerializer.SerializeToElement(new[] { "session-123" }),
        };

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_get_dump_analyze_commands_status",
            arguments,
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("Invalid type for parameter `sessionId`");
    }

    /// <summary>
    /// Verifies invalid sessionId is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithInvalidSessionId_ReturnsErrorResponse()
    {
        // Arrange
        var sut = CreateToolCallService();

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_get_dump_analyze_commands_status",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement("invalid-session-999"),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
        _ = GetText(result).Should().Contain("sessionId");
    }

    /// <summary>
    /// Verifies empty sessionId is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CallToolAsync_WithEmptySessionId_ReturnsErrorResponse()
    {
        // Arrange
        var sut = CreateToolCallService();

        // Act
        var result = await sut.CallToolAsync(
            "winaidbg_get_dump_analyze_commands_status",
            new Dictionary<string, JsonElement>
            {
                ["sessionId"] = JsonSerializer.SerializeToElement(string.Empty),
            },
            CancellationToken.None);

        // Assert
        _ = result.IsError.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a valid request returns a status summary markdown payload.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithValidSession_ReturnsSummaryMarkdown()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();

            const string sessionId = "sess-test";
            var now = DateTime.Now;
            var executing = CommandInfo.Executing(sessionId, "cmd-1", "k", now, now, processId: 1);
            var completed = CommandInfo.Completed(sessionId, "cmd-2", "!analyze -v", now.AddSeconds(-2), now.AddSeconds(-1), now, "OK", string.Empty, processId: 1);

            var engine = new Mock<IDebugEngine>(MockBehavior.Strict);
            _ = engine.Setup(e => e.Dispose());
            _ = engine.Setup(e => e.GetSessionState(sessionId)).Returns(SessionState.Active);
            _ = engine.Setup(e => e.GetAllCommandInfos(sessionId)).Returns(
                new Dictionary<string, CommandInfo>
                {
                    ["cmd-1"] = executing,
                    ["cmd-2"] = completed,
                });

            SetEngineForTesting(engine.Object);

            var sut = new GetDumpAnalyzeCommandsStatusTool();

            // Act
            var result = await sut.Execute(sessionId);
            var markdown = result.ToString() ?? string.Empty;

            // Assert
            _ = markdown.Should().Contain("## Command Status Summary");
            _ = markdown.Should().Contain(sessionId);
            _ = markdown.Should().Contain("cmd-1");
            _ = markdown.Should().Contain("cmd-2");

            engine.Verify(e => e.GetSessionState(sessionId), Times.Once);
            engine.Verify(e => e.GetAllCommandInfos(sessionId), Times.Once);
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Verifies that an unknown sessionId results in an actionable user input error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task Execute_WithUnknownSessionId_ThrowsMcpToolUserInputException()
    {
        try
        {
            // Arrange
            EngineService.Shutdown();
            var sut = new GetDumpAnalyzeCommandsStatusTool();

            // Act
            var act = async () => await sut.Execute("sess-test");

            // Assert
            _ = await act.Should()
                .ThrowAsync<McpToolUserInputException>()
                .WithMessage("*Invalid `sessionId`*");
        }
        finally
        {
            EngineService.Shutdown();
        }
    }

    /// <summary>
    /// Replaces the <see cref="EngineService"/> singleton instance for tests using reflection.
    /// </summary>
    /// <param name="engine">The engine instance to set.</param>
    private static void SetEngineForTesting(IDebugEngine engine)
    {
        var field = typeof(EngineService).GetField("m_DebugEngine", BindingFlags.NonPublic | BindingFlags.Static);
        _ = field.Should().NotBeNull();
        field!.SetValue(null, engine);
    }

    /// <summary>
    /// Creates a tool call service instance with engine initialized.
    /// </summary>
    /// <returns>The tool call service.</returns>
    private static McpToolCallService CreateToolCallService()
    {
        var settings = new Mock<ISettings>();
        var fileSystem = new Mock<IFileSystem>();
        var processManager = new Mock<IProcessManager>();

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

        _ = settings.Setup(s => s.Get()).Returns(sharedConfig);
        EngineService.Initialize(fileSystem.Object, processManager.Object, settings.Object);
        return new McpToolCallService(new McpToolDefinitionService());
    }

    /// <summary>
    /// Extracts the first text content block from a tool call result.
    /// </summary>
    /// <param name="result">The call result.</param>
    /// <returns>The text content.</returns>
    private static string GetText(CallToolResult result)
    {
        var textBlock = result.Content[0].Should().BeOfType<TextContentBlock>().Subject;
        return textBlock.Text;
    }
}

